using CapFinLoan.Document.Application.Contracts.Requests;
using CapFinLoan.Document.Application.Contracts.Responses;
using CapFinLoan.Document.Application.Interfaces;
using CapFinLoan.Document.Domain.Constants;

namespace CapFinLoan.Document.Application.Services;

public class DocumentService : IDocumentService
{
    private static readonly HashSet<string> AllowedExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".pdf", ".jpg", ".jpeg", ".png"
    };

    private static readonly HashSet<string> AllowedDocumentTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        DocumentTypes.IdentityProof,
        DocumentTypes.AddressProof,
        DocumentTypes.IncomeProof,
        DocumentTypes.BankStatement,
        DocumentTypes.EmploymentProof,
        DocumentTypes.Other
    };

    private const long MaxFileSizeBytes = 5 * 1024 * 1024;

    private readonly IDocumentRepository _documentRepository;
    private readonly IFileStorageService _fileStorageService;

    public DocumentService(IDocumentRepository documentRepository, IFileStorageService fileStorageService)
    {
        _documentRepository = documentRepository;
        _fileStorageService = fileStorageService;
    }

    public async Task<DocumentResponse> UploadAsync(Guid uploaderUserId, UploadDocumentRequest request, CancellationToken cancellationToken = default)
    {
        var extension = Path.GetExtension(request.File.FileName);
        if (!AllowedExtensions.Contains(extension))
        {
            throw new InvalidOperationException($"File type {extension} is not allowed. Allowed types: {string.Join(", ", AllowedExtensions)}");
        }

        if (request.File.Length > MaxFileSizeBytes)
        {
            throw new InvalidOperationException($"File size exceeds maximum allowed size of {MaxFileSizeBytes / (1024 * 1024)} MB.");
        }

        if (!AllowedDocumentTypes.Contains(request.DocumentType))
        {
            throw new InvalidOperationException($"Document type '{request.DocumentType}' is not allowed. Allowed types: {string.Join(", ", AllowedDocumentTypes)}");
        }

        // TODO: Validate that uploaderUserId owns the LoanApplicationId
        // This should be verified via ApplicationService (e.g., API call or message broker)
        // Skipped here due to microservice boundary
        string storagePath;
        using (var stream = request.File.OpenReadStream())
        {
            storagePath = await _fileStorageService.SaveFileAsync(stream, request.File.FileName, cancellationToken);
        }

        var now = DateTime.UtcNow;
        var document = new Domain.Entities.Document
        {
            Id = Guid.NewGuid(),
            LoanApplicationId = request.LoanApplicationId,
            UploadedByUserId = uploaderUserId,
            DocumentType = request.DocumentType.Trim(),
            FileName = request.File.FileName,
            FileExtension = extension,
            FileSizeBytes = request.File.Length,
            StoragePath = storagePath,
            Status = DocumentStatuses.Uploaded,
            UploadedAtUtc = now
        };

        try
        {
            await _documentRepository.AddAsync(document, cancellationToken);
        }
        catch
        {
            await _fileStorageService.DeleteFileAsync(storagePath, cancellationToken);
            throw;
        }

        return Map(document);
    }

    public async Task<DocumentResponse> GetByIdAsync(Guid documentId, Guid requesterUserId, bool isAdmin, CancellationToken cancellationToken = default)
    {
        var document = await _documentRepository.GetByIdAsync(documentId, cancellationToken)
            ?? throw new KeyNotFoundException("Document not found.");

        if (!isAdmin && document.UploadedByUserId != requesterUserId)
        {
            throw new UnauthorizedAccessException("You are not allowed to access this document.");
        }

        return Map(document);
    }

    public async Task<IReadOnlyCollection<DocumentResponse>> GetByLoanApplicationIdAsync(Guid loanApplicationId, Guid requesterUserId, bool isAdmin, CancellationToken cancellationToken = default)
    {
        var documents = await _documentRepository.GetByLoanApplicationIdAsync(loanApplicationId, cancellationToken);

        if (!isAdmin)
        {
            documents = documents.Where(d => d.UploadedByUserId == requesterUserId).ToList();
        }

        return documents.Select(Map).ToArray();
    }

    public async Task<DocumentResponse> VerifyAsync(Guid documentId, Guid verifierUserId, VerifyDocumentRequest request, CancellationToken cancellationToken = default)
    {
        var document = await _documentRepository.GetByIdAsync(documentId, cancellationToken)
            ?? throw new KeyNotFoundException("Document not found.");

        if (!string.Equals(request.Status, DocumentStatuses.Verified, StringComparison.OrdinalIgnoreCase) &&
            !string.Equals(request.Status, DocumentStatuses.Rejected, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("Status must be either Verified or Rejected.");
        }

        if (string.Equals(request.Status, DocumentStatuses.Rejected, StringComparison.OrdinalIgnoreCase) &&
            string.IsNullOrWhiteSpace(request.Remarks))
        {
            throw new InvalidOperationException("Remarks are required when rejecting a document.");
        }

        document.Status = request.Status.Trim();
        document.Remarks = request.Remarks?.Trim() ?? string.Empty;
        document.VerifiedAtUtc = DateTime.UtcNow;
        document.VerifiedByUserId = verifierUserId;

        await _documentRepository.UpdateAsync(document, cancellationToken);
        return Map(document);
    }

    public async Task<byte[]> DownloadAsync(Guid documentId, Guid requesterUserId, bool isAdmin, CancellationToken cancellationToken = default)
    {
        var document = await _documentRepository.GetByIdAsync(documentId, cancellationToken)
            ?? throw new KeyNotFoundException("Document not found.");

        if (!isAdmin && document.UploadedByUserId != requesterUserId)
        {
            throw new UnauthorizedAccessException("You are not allowed to download this document.");
        }

        return await _fileStorageService.GetFileAsync(document.StoragePath, cancellationToken);
    }

    public async Task<(DocumentResponse metadata, byte[] fileBytes)> GetDocumentWithFileAsync(Guid documentId, Guid requesterUserId, bool isAdmin, CancellationToken cancellationToken = default)
    {
        var document = await _documentRepository.GetByIdAsync(documentId, cancellationToken)
            ?? throw new KeyNotFoundException("Document not found.");

        if (!isAdmin && document.UploadedByUserId != requesterUserId)
        {
            throw new UnauthorizedAccessException("You are not allowed to download this document.");
        }

        var fileBytes = await _fileStorageService.GetFileAsync(document.StoragePath, cancellationToken);
        return (Map(document), fileBytes);
    }

    public async Task DeleteAsync(Guid documentId, Guid requesterUserId, bool isAdmin, CancellationToken cancellationToken = default)
    {
        var document = await _documentRepository.GetByIdAsync(documentId, cancellationToken)
            ?? throw new KeyNotFoundException("Document not found.");

        if (!isAdmin && document.UploadedByUserId != requesterUserId)
        {
            throw new UnauthorizedAccessException("You are not allowed to delete this document.");
        }

        await _fileStorageService.DeleteFileAsync(document.StoragePath, cancellationToken);
        await _documentRepository.DeleteAsync(documentId, cancellationToken);
    }

    private static DocumentResponse Map(Domain.Entities.Document document)
    {
        return new DocumentResponse
        {
            Id = document.Id,
            LoanApplicationId = document.LoanApplicationId,
            UploadedByUserId = document.UploadedByUserId,
            DocumentType = document.DocumentType,
            FileName = document.FileName,
            FileExtension = document.FileExtension,
            FileSizeBytes = document.FileSizeBytes,
            Status = document.Status,
            Remarks = document.Remarks,
            UploadedAtUtc = document.UploadedAtUtc,
            VerifiedAtUtc = document.VerifiedAtUtc,
            VerifiedByUserId = document.VerifiedByUserId
        };
    }
}
