using CapFinLoan.Auth.Application.Contracts.Requests;
using CapFinLoan.Auth.Application.Contracts.Responses;
using CapFinLoan.Auth.Application.Interfaces;
using CapFinLoan.Auth.Domain.Constants;
using CapFinLoan.Auth.Domain.Entities;

namespace CapFinLoan.Auth.Application.Services;

public class AuthService : IAuthService
{
    private readonly IUserRepository _userRepository;
    private readonly IJwtTokenGenerator _jwtTokenGenerator;

    public AuthService(IUserRepository userRepository, IJwtTokenGenerator jwtTokenGenerator)
    {
        _userRepository = userRepository;
        _jwtTokenGenerator = jwtTokenGenerator;
    }

    public async Task<AuthResponse> SignupAsync(SignupRequest request, CancellationToken cancellationToken = default)
    {
        var email = request.Email.Trim().ToLowerInvariant();
        if (await _userRepository.ExistsByEmailAsync(email, cancellationToken))
        {
            throw new InvalidOperationException("An account already exists with this email.");
        }

        // Validate and assign role from request
        var role = request.Role?.ToUpper() == RoleNames.Admin 
            ? RoleNames.Admin 
            : RoleNames.Applicant;

        var user = new ApplicationUser
        {
            UserName = email,
            Email = email,
            PhoneNumber = request.Phone.Trim(),
            Name = request.Name.Trim(),
            Role = role,
            IsActive = true,
            CreatedAtUtc = DateTime.UtcNow,
            UpdatedAtUtc = DateTime.UtcNow
        };

        await _userRepository.CreateAsync(user, request.Password, cancellationToken);

        var (token, expiresAtUtc) = _jwtTokenGenerator.GenerateToken(user);
        return new AuthResponse
        {
            Token = token,
            ExpiresAtUtc = expiresAtUtc,
            Role = user.Role,
            UserId = user.Id,
            Name = user.Name,
            Email = user.Email!
        };
    }

    public async Task<AuthResponse> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default)
    {
        var email = request.Email.Trim().ToLowerInvariant();
        var user = await _userRepository.GetByEmailAsync(email, cancellationToken);

        if (user is null || !await _userRepository.CheckPasswordAsync(user, request.Password))
        {
            throw new UnauthorizedAccessException("Invalid email or password.");
        }

        if (!user.IsActive)
        {
            throw new UnauthorizedAccessException("User is deactivated.");
        }

        var (token, expiresAtUtc) = _jwtTokenGenerator.GenerateToken(user);
        return new AuthResponse
        {
            Token = token,
            ExpiresAtUtc = expiresAtUtc,
            Role = user.Role,
            UserId = user.Id,
            Name = user.Name,
            Email = user.Email!
        };
    }

    public async Task<IReadOnlyCollection<UserSummaryResponse>> GetUsersAsync(CancellationToken cancellationToken = default)
    {
        var users = await _userRepository.GetAllAsync(cancellationToken);
        return users
            .OrderByDescending(x => x.CreatedAtUtc)
            .Select(MapUser)
            .ToArray();
    }

    public async Task<UserSummaryResponse> UpdateUserStatusAsync(Guid userId, bool isActive, CancellationToken cancellationToken = default)
    {
        var user = await _userRepository.GetByIdAsync(userId, cancellationToken)
                   ?? throw new KeyNotFoundException("User not found.");

        user.IsActive = isActive;
        user.UpdatedAtUtc = DateTime.UtcNow;
        await _userRepository.UpdateAsync(user, cancellationToken);

        return MapUser(user);
    }

    private static UserSummaryResponse MapUser(ApplicationUser user)
    {
        return new UserSummaryResponse
        {
            Id = user.Id,
            Name = user.Name,
            Email = user.Email ?? string.Empty,
            Phone = user.PhoneNumber ?? string.Empty,
            Role = user.Role,
            IsActive = user.IsActive,
            CreatedAtUtc = user.CreatedAtUtc
        };
    }

    public async Task<MessageResponse> ForgotPasswordAsync(ForgotPasswordRequest request, CancellationToken cancellationToken = default)
    {
        var email = request.Email.Trim().ToLowerInvariant();
        var user = await _userRepository.GetByEmailAsync(email, cancellationToken);

        // Always return success to avoid user enumeration
        if (user is null)
        {
            return new MessageResponse
            {
                Message = "If the email exists, a reset link has been sent."
            };
        }

        // Generate password reset token
        var token = await _userRepository.GeneratePasswordResetTokenAsync(user);

        // Encode token for URL safety
        var encodedToken = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(token));

        // Log reset link to console (for now, until email service is integrated)
        var resetLink = $"http://localhost:5174/reset-password?email={Uri.EscapeDataString(email)}&token={Uri.EscapeDataString(encodedToken)}";
        Console.WriteLine("========================================");
        Console.WriteLine("PASSWORD RESET REQUEST");
        Console.WriteLine($"Email: {email}");
        Console.WriteLine($"Reset Link: {resetLink}");
        Console.WriteLine("========================================");

        return new MessageResponse
        {
            Message = "If the email exists, a reset link has been sent."
        };
    }

    public async Task<MessageResponse> ResetPasswordAsync(ResetPasswordRequest request, CancellationToken cancellationToken = default)
    {
        var email = request.Email.Trim().ToLowerInvariant();
        var user = await _userRepository.GetByEmailAsync(email, cancellationToken);

        if (user is null)
        {
            throw new InvalidOperationException("Invalid reset request.");
        }

        // Decode token
        string decodedToken;
        try
        {
            var tokenBytes = Convert.FromBase64String(request.Token);
            decodedToken = System.Text.Encoding.UTF8.GetString(tokenBytes);
        }
        catch
        {
            throw new InvalidOperationException("Invalid or expired reset token.");
        }

        // Reset password using UserManager
        var success = await _userRepository.ResetPasswordAsync(user, decodedToken, request.NewPassword);

        if (!success)
        {
            throw new InvalidOperationException("Invalid or expired reset token.");
        }

        Console.WriteLine($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] Password reset successful for: {email}");

        return new MessageResponse
        {
            Message = "Password reset successful."
        };
    }
}