using CapFinLoan.Application.Application.Contracts.Requests;
using CapFinLoan.Application.Application.Contracts.Responses;
using CapFinLoan.Application.Application.Interfaces;
using CapFinLoan.Application.Application.Events;
using CapFinLoan.Application.Domain.Constants;
using CapFinLoan.Application.Domain.Entities;
using RabbitMQ.Client;
using System.Text;
using System.Text.Json;

namespace CapFinLoan.Application.Application.Services;

public class LoanApplicationService : ILoanApplicationService
{
    private readonly ILoanApplicationRepository _loanApplicationRepository;
    private readonly IApplicationStatusHistoryRepository _statusHistoryRepository;

    public LoanApplicationService(
        ILoanApplicationRepository loanApplicationRepository,
        IApplicationStatusHistoryRepository statusHistoryRepository)
    {
        _loanApplicationRepository = loanApplicationRepository;
        _statusHistoryRepository = statusHistoryRepository;
    }

    public async Task<LoanApplicationResponse> CreateDraftAsync(Guid applicantUserId, SaveLoanApplicationRequest request, CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        var application = new LoanApplication
        {
            ApplicantUserId = applicantUserId,
            ApplicationNumber = $"APP-{now:yyyyMMdd}-{Random.Shared.Next(1000, 9999)}",
            CreatedAtUtc = now,
            UpdatedAtUtc = now
        };

        ApplyRequest(application, request, now);

        await _loanApplicationRepository.AddAsync(application, cancellationToken);

        var history = new ApplicationStatusHistory
        {
            Id = Guid.NewGuid(),
            LoanApplicationId = application.Id,
            FromStatus = string.Empty,
            ToStatus = ApplicationStatuses.Draft,
            Remarks = "Application draft created.",
            ChangedByUserId = applicantUserId,
            ChangedAtUtc = now
        };

        await _statusHistoryRepository.AddAsync(history, cancellationToken);

        return Map(application);
    }

    public async Task<LoanApplicationResponse> UpdateDraftAsync(Guid applicationId, Guid requesterUserId, bool isAdmin, SaveLoanApplicationRequest request, CancellationToken cancellationToken = default)
    {
        var application = await GetOwnedOrAdminApplicationAsync(applicationId, requesterUserId, isAdmin, cancellationToken);

        if (!string.Equals(application.Status, ApplicationStatuses.Draft, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("Only draft applications can be updated.");
        }

        ApplyRequest(application, request, DateTime.UtcNow);
        await _loanApplicationRepository.UpdateAsync(application, cancellationToken);
        return Map(application);
    }

    public async Task<LoanApplicationResponse> GetByIdAsync(Guid applicationId, Guid requesterUserId, bool isAdmin, CancellationToken cancellationToken = default)
    {
        var application = await GetOwnedOrAdminApplicationAsync(applicationId, requesterUserId, isAdmin, cancellationToken);
        return Map(application);
    }

    public async Task<IReadOnlyCollection<LoanApplicationResponse>> GetMineAsync(Guid applicantUserId, CancellationToken cancellationToken = default)
    {
        var applications = await _loanApplicationRepository.GetByApplicantUserIdAsync(applicantUserId, cancellationToken);
        return applications
            .OrderByDescending(x => x.UpdatedAtUtc)
            .Select(Map)
            .ToArray();
    }

    public async Task<LoanApplicationResponse> SubmitAsync(Guid applicationId, Guid requesterUserId, CancellationToken cancellationToken = default)
    {
        Console.WriteLine($"[Submit API HIT] ApplicationId: {applicationId}, UserId: {requesterUserId}");
        
        var application = await GetOwnedOrAdminApplicationAsync(applicationId, requesterUserId, false, cancellationToken);

        if (!string.Equals(application.Status, ApplicationStatuses.Draft, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("Only draft applications can be submitted.");
        }

        ValidateForSubmission(application);

        var previousStatus = application.Status;
        var now = DateTime.UtcNow;
        
        // Update application fields
        application.Status = ApplicationStatuses.Submitted;
        application.SubmittedAtUtc = now;
        application.UpdatedAtUtc = now;

        // Create new history record
        var history = new ApplicationStatusHistory
        {
            Id = Guid.NewGuid(),
            LoanApplicationId = application.Id,
            FromStatus = previousStatus,
            ToStatus = ApplicationStatuses.Submitted,
            Remarks = "Application submitted by applicant.",
            ChangedByUserId = requesterUserId,
            ChangedAtUtc = now
        };

        Console.WriteLine("[Saving application...]");
        
        // Add history without saving
        await _statusHistoryRepository.AddWithoutSaveAsync(history, cancellationToken);
        
        // Save both application update and history insert in ONE transaction
        await _statusHistoryRepository.SaveChangesAsync(cancellationToken);
        
        Console.WriteLine("[Application saved successfully]");

        // Publish event to RabbitMQ AFTER successful DB save
        Console.WriteLine("[Publishing event...]");
        await PublishApplicationSubmittedEventAsync(application);

        return Map(application);
    }

    private async Task PublishApplicationSubmittedEventAsync(LoanApplication application)
    {
        try
        {
            var factory = new ConnectionFactory { HostName = "localhost" };
            await using var connection = await factory.CreateConnectionAsync();
            await using var channel = await connection.CreateChannelAsync();

            // Declare queue
            await channel.QueueDeclareAsync(
                queue: "application-submitted",
                durable: false,
                exclusive: false,
                autoDelete: false,
                arguments: null);

            // Create event
            var eventMessage = new ApplicationSubmittedEvent
            {
                ApplicationId = application.Id,
                Email = application.Email,
                ApplicantName = $"{application.FirstName} {application.LastName}"
            };

            // Serialize and publish
            var messageBody = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(eventMessage));
            await channel.BasicPublishAsync(
                exchange: string.Empty,
                routingKey: "application-submitted",
                body: messageBody);

            Console.WriteLine($"[Event published successfully] ApplicationId: {application.Id}, Email: {application.Email}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[RabbitMQ ERROR] Failed to publish event: {ex.Message}");
            // Don't throw - event publishing failure shouldn't break the application submission
        }
    }

    public async Task<LoanApplicationStatusResponse> GetStatusAsync(Guid applicationId, Guid requesterUserId, bool isAdmin, CancellationToken cancellationToken = default)
    {
        var application = await GetOwnedOrAdminApplicationAsync(applicationId, requesterUserId, isAdmin, cancellationToken);
        return new LoanApplicationStatusResponse
        {
            Id = application.Id,
            ApplicationNumber = application.ApplicationNumber,
            CurrentStatus = application.Status,
            Timeline = application.StatusHistory
                .OrderBy(x => x.ChangedAtUtc)
                .Select(x => new ApplicationStatusHistoryResponse
                {
                    FromStatus = x.FromStatus,
                    ToStatus = x.ToStatus,
                    Remarks = x.Remarks,
                    ChangedByUserId = x.ChangedByUserId,
                    ChangedAtUtc = x.ChangedAtUtc
                })
                .ToArray()
        };
    }

    public async Task<LoanApplicationResponse> UpdatePersonalDetailsAsync(Guid applicationId, Guid requesterUserId, PersonalDetailsRequest request, CancellationToken cancellationToken = default)
    {
        var application = await GetOwnedOrAdminApplicationAsync(applicationId, requesterUserId, false, cancellationToken);

        if (!string.Equals(application.Status, ApplicationStatuses.Draft, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("Only draft applications can be updated.");
        }

        application.FirstName = request.FirstName.Trim();
        application.LastName = request.LastName.Trim();
        application.DateOfBirth = request.DateOfBirth;
        application.Gender = request.Gender.Trim();
        application.Email = request.Email.Trim();
        application.Phone = request.Phone.Trim();
        application.AddressLine1 = request.AddressLine1.Trim();
        application.AddressLine2 = request.AddressLine2.Trim();
        application.City = request.City.Trim();
        application.State = request.State.Trim();
        application.PostalCode = request.PostalCode.Trim();
        application.UpdatedAtUtc = DateTime.UtcNow;

        await _loanApplicationRepository.UpdateAsync(application, cancellationToken);
        return Map(application);
    }

    public async Task<LoanApplicationResponse> UpdateEmploymentDetailsAsync(Guid applicationId, Guid requesterUserId, EmploymentDetailsRequest request, CancellationToken cancellationToken = default)
    {
        var application = await GetOwnedOrAdminApplicationAsync(applicationId, requesterUserId, false, cancellationToken);

        if (!string.Equals(application.Status, ApplicationStatuses.Draft, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("Only draft applications can be updated.");
        }

        application.EmployerName = request.EmployerName.Trim();
        application.EmploymentType = request.EmploymentType.Trim();
        application.MonthlyIncome = request.MonthlyIncome;
        application.AnnualIncome = request.AnnualIncome;
        application.ExistingEmiAmount = request.ExistingEmiAmount;
        application.UpdatedAtUtc = DateTime.UtcNow;

        await _loanApplicationRepository.UpdateAsync(application, cancellationToken);
        return Map(application);
    }

    public async Task<LoanApplicationResponse> UpdateLoanDetailsAsync(Guid applicationId, Guid requesterUserId, LoanDetailsRequest request, CancellationToken cancellationToken = default)
    {
        var application = await GetOwnedOrAdminApplicationAsync(applicationId, requesterUserId, false, cancellationToken);

        if (!string.Equals(application.Status, ApplicationStatuses.Draft, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("Only draft applications can be updated.");
        }

        application.RequestedAmount = request.RequestedAmount;
        application.RequestedTenureMonths = request.RequestedTenureMonths;
        application.LoanPurpose = request.LoanPurpose.Trim();
        application.Remarks = request.Remarks.Trim();
        application.UpdatedAtUtc = DateTime.UtcNow;

        await _loanApplicationRepository.UpdateAsync(application, cancellationToken);
        return Map(application);
    }


    private async Task<LoanApplication> GetOwnedOrAdminApplicationAsync(Guid applicationId, Guid requesterUserId, bool isAdmin, CancellationToken cancellationToken)
    {
        var application = await _loanApplicationRepository.GetByIdAsync(applicationId, cancellationToken)
                         ?? throw new KeyNotFoundException("Application not found.");

        if (!isAdmin && application.ApplicantUserId != requesterUserId)
        {
            throw new UnauthorizedAccessException("You are not allowed to access this application.");
        }

        return application;
    }

    private static void ApplyRequest(LoanApplication application, SaveLoanApplicationRequest request, DateTime updatedAtUtc)
    {
        application.FirstName = request.PersonalDetails.FirstName.Trim();
        application.LastName = request.PersonalDetails.LastName.Trim();
        application.DateOfBirth = request.PersonalDetails.DateOfBirth;
        application.Gender = request.PersonalDetails.Gender.Trim();
        application.Email = request.PersonalDetails.Email.Trim();
        application.Phone = request.PersonalDetails.Phone.Trim();
        application.AddressLine1 = request.PersonalDetails.AddressLine1.Trim();
        application.AddressLine2 = request.PersonalDetails.AddressLine2.Trim();
        application.City = request.PersonalDetails.City.Trim();
        application.State = request.PersonalDetails.State.Trim();
        application.PostalCode = request.PersonalDetails.PostalCode.Trim();
        application.EmployerName = request.EmploymentDetails.EmployerName.Trim();
        application.EmploymentType = request.EmploymentDetails.EmploymentType.Trim();
        application.MonthlyIncome = request.EmploymentDetails.MonthlyIncome;
        application.AnnualIncome = request.EmploymentDetails.AnnualIncome;
        application.ExistingEmiAmount = request.EmploymentDetails.ExistingEmiAmount;
        application.RequestedAmount = request.LoanDetails.RequestedAmount;
        application.RequestedTenureMonths = request.LoanDetails.RequestedTenureMonths;
        application.LoanPurpose = request.LoanDetails.LoanPurpose.Trim();
        application.Remarks = request.LoanDetails.Remarks.Trim();
        application.UpdatedAtUtc = updatedAtUtc;
    }

    private static void ValidateForSubmission(LoanApplication application)
    {
        var missingFields = new List<string>();

        if (string.IsNullOrWhiteSpace(application.FirstName)) missingFields.Add(nameof(application.FirstName));
        if (string.IsNullOrWhiteSpace(application.LastName)) missingFields.Add(nameof(application.LastName));
        if (application.DateOfBirth is null) missingFields.Add(nameof(application.DateOfBirth));
        if (string.IsNullOrWhiteSpace(application.Email)) missingFields.Add(nameof(application.Email));
        if (string.IsNullOrWhiteSpace(application.Phone)) missingFields.Add(nameof(application.Phone));
        if (string.IsNullOrWhiteSpace(application.AddressLine1)) missingFields.Add(nameof(application.AddressLine1));
        if (string.IsNullOrWhiteSpace(application.City)) missingFields.Add(nameof(application.City));
        if (string.IsNullOrWhiteSpace(application.State)) missingFields.Add(nameof(application.State));
        if (string.IsNullOrWhiteSpace(application.PostalCode)) missingFields.Add(nameof(application.PostalCode));
        if (string.IsNullOrWhiteSpace(application.EmployerName)) missingFields.Add(nameof(application.EmployerName));
        if (string.IsNullOrWhiteSpace(application.EmploymentType)) missingFields.Add(nameof(application.EmploymentType));
        if (application.MonthlyIncome is null || application.MonthlyIncome <= 0) missingFields.Add(nameof(application.MonthlyIncome));
        if (application.AnnualIncome is null || application.AnnualIncome <= 0) missingFields.Add(nameof(application.AnnualIncome));
        if (application.RequestedAmount <= 0) missingFields.Add(nameof(application.RequestedAmount));
        if (application.RequestedTenureMonths <= 0) missingFields.Add(nameof(application.RequestedTenureMonths));
        if (string.IsNullOrWhiteSpace(application.LoanPurpose)) missingFields.Add(nameof(application.LoanPurpose));

        if (missingFields.Count > 0)
        {
            throw new InvalidOperationException($"Application is incomplete. Missing or invalid: {string.Join(", ", missingFields)}.");
        }

        if (application.RequestedAmount is < 10000 or > 5000000)
        {
            throw new InvalidOperationException("Requested amount must be between 10,000 and 5,000,000.");
        }

        if (application.RequestedTenureMonths is < 6 or > 360)
        {
            throw new InvalidOperationException("Requested tenure must be between 6 and 360 months.");
        }

        if (application.MonthlyIncome.HasValue && application.MonthlyIncome.Value <= application.ExistingEmiAmount)
        {
            throw new InvalidOperationException("Monthly income must be greater than existing EMI obligations.");
        }
    }

    private static LoanApplicationResponse Map(LoanApplication application)
    {
        return new LoanApplicationResponse
        {
            Id = application.Id,
            ApplicationNumber = application.ApplicationNumber,
            ApplicantUserId = application.ApplicantUserId,
            Status = application.Status,
            PersonalDetails = new PersonalDetailsResponse
            {
                FirstName = application.FirstName,
                LastName = application.LastName,
                DateOfBirth = application.DateOfBirth,
                Gender = application.Gender,
                Email = application.Email,
                Phone = application.Phone,
                AddressLine1 = application.AddressLine1,
                AddressLine2 = application.AddressLine2,
                City = application.City,
                State = application.State,
                PostalCode = application.PostalCode
            },
            EmploymentDetails = new EmploymentDetailsResponse
            {
                EmployerName = application.EmployerName,
                EmploymentType = application.EmploymentType,
                MonthlyIncome = application.MonthlyIncome,
                AnnualIncome = application.AnnualIncome,
                ExistingEmiAmount = application.ExistingEmiAmount
            },
            LoanDetails = new LoanDetailsResponse
            {
                RequestedAmount = application.RequestedAmount,
                RequestedTenureMonths = application.RequestedTenureMonths,
                LoanPurpose = application.LoanPurpose,
                Remarks = application.Remarks
            },
            CreatedAtUtc = application.CreatedAtUtc,
            UpdatedAtUtc = application.UpdatedAtUtc,
            SubmittedAtUtc = application.SubmittedAtUtc
        };
    }
}