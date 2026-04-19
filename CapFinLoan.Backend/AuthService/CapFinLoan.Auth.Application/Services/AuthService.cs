using CapFinLoan.Auth.Application.Contracts.Requests;
using CapFinLoan.Auth.Application.Contracts.Responses;
using CapFinLoan.Auth.Application.Events;
using CapFinLoan.Auth.Application.Interfaces;
using CapFinLoan.Auth.Domain.Constants;
using CapFinLoan.Auth.Domain.Entities;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Logging;
using System.Text;

namespace CapFinLoan.Auth.Application.Services;

public class AuthService : IAuthService
{
    private readonly IUserRepository _userRepository;
    private readonly IJwtTokenGenerator _jwtTokenGenerator;
    private readonly IMessagePublisher _messagePublisher;
    private readonly ILogger<AuthService> _logger;

    public AuthService(
        IUserRepository userRepository, 
        IJwtTokenGenerator jwtTokenGenerator,
        IMessagePublisher messagePublisher,
        ILogger<AuthService> logger)
    {
        _userRepository = userRepository;
        _jwtTokenGenerator = jwtTokenGenerator;
        _messagePublisher = messagePublisher;
        _logger = logger;
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
        
        Console.WriteLine("========================================");
        Console.WriteLine($"[FORGOT PASSWORD] Email requested: {email}");
        Console.WriteLine($"[FORGOT PASSWORD] Timestamp: {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss}");
        _logger.LogInformation("Password reset requested for email: {Email}", email);
        
        var user = await _userRepository.GetByEmailAsync(email, cancellationToken);
        
        Console.WriteLine($"[FORGOT PASSWORD] User exists: {user != null}");
        if (user != null)
        {
            Console.WriteLine($"[FORGOT PASSWORD] User ID: {user.Id}");
            Console.WriteLine($"[FORGOT PASSWORD] User Name: {user.Name}");
        }

        // Always return success to avoid user enumeration
        if (user is null)
        {
            Console.WriteLine("[FORGOT PASSWORD] User not found - returning generic success message");
            _logger.LogWarning("Password reset requested for non-existent email: {Email}", email);
            return new MessageResponse
            {
                Message = "If the email exists, a reset link has been sent."
            };
        }

        try
        {
            // Generate password reset token
            Console.WriteLine("[FORGOT PASSWORD] Generating password reset token...");
            _logger.LogInformation("Generating password reset token for user: {UserId}", user.Id);
            
            var token = await _userRepository.GeneratePasswordResetTokenAsync(user);
            Console.WriteLine($"[FORGOT PASSWORD] Raw token generated successfully (length: {token.Length})");
            Console.WriteLine($"[FORGOT PASSWORD] Raw token (first 50 chars): {token.Substring(0, Math.Min(50, token.Length))}...");

            // Encode token for URL safety using WebEncoders.Base64UrlEncode
            var encodedToken = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(token));
            Console.WriteLine($"[FORGOT PASSWORD] Token URL-encoded successfully (length: {encodedToken.Length})");
            Console.WriteLine($"[FORGOT PASSWORD] Encoded token (first 50 chars): {encodedToken.Substring(0, Math.Min(50, encodedToken.Length))}...");

            // Build reset link
            var resetLink = $"http://localhost:5174/reset-password?email={Uri.EscapeDataString(email)}&token={encodedToken}";
            
            Console.WriteLine($"[FORGOT PASSWORD] Reset link generated:");
            Console.WriteLine($"[RESET LINK]: {resetLink}");
            _logger.LogInformation("Reset link generated for {Email}", email);

            // Publish event to RabbitMQ instead of sending email directly
            Console.WriteLine("[FORGOT PASSWORD] Publishing password reset event to RabbitMQ...");
            
            var passwordResetEvent = new PasswordResetRequestedEvent
            {
                Email = email,
                UserName = user.Name,
                ResetLink = resetLink
            };

            await _messagePublisher.PublishAsync("password-reset", passwordResetEvent, cancellationToken);
            
            Console.WriteLine("[FORGOT PASSWORD] ✅ Event published to RabbitMQ successfully");
            Console.WriteLine("========================================");
            _logger.LogInformation("Password reset event published for {Email}", email);
        }
        catch (Exception ex)
        {
            Console.WriteLine("========================================");
            Console.WriteLine("[FORGOT PASSWORD ERROR] Exception caught:");
            Console.WriteLine($"[ERROR TYPE]: {ex.GetType().Name}");
            Console.WriteLine($"[ERROR MESSAGE]: {ex.Message}");
            Console.WriteLine($"[ERROR STACK TRACE]:");
            Console.WriteLine(ex.ToString());
            Console.WriteLine("========================================");
            
            _logger.LogError(ex, "Failed to publish password reset event for {Email}", email);
            
            // Don't expose the error to the user for security reasons
        }

        return new MessageResponse
        {
            Message = "If the email exists, a reset link has been sent."
        };
    }

    public async Task<MessageResponse> ResetPasswordAsync(ResetPasswordRequest request, CancellationToken cancellationToken = default)
    {
        var email = request.Email.Trim().ToLowerInvariant();
        
        Console.WriteLine("========================================");
        Console.WriteLine($"[RESET PASSWORD] Email: {email}");
        Console.WriteLine($"[RESET PASSWORD] Timestamp: {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss}");
        Console.WriteLine($"[RESET PASSWORD] Received token length: {request.Token.Length}");
        Console.WriteLine($"[RESET PASSWORD] Received token (first 50 chars): {request.Token.Substring(0, Math.Min(50, request.Token.Length))}...");
        Console.WriteLine($"[RESET PASSWORD] New password length: {request.NewPassword.Length}");
        _logger.LogInformation("Password reset attempt for email: {Email}", email);
        
        var user = await _userRepository.GetByEmailAsync(email, cancellationToken);

        if (user is null)
        {
            Console.WriteLine("[RESET PASSWORD] ❌ User not found");
            Console.WriteLine("========================================");
            _logger.LogWarning("Password reset failed: User not found for email {Email}", email);
            throw new InvalidOperationException("Invalid reset request.");
        }

        Console.WriteLine($"[RESET PASSWORD] ✅ User found: {user.Id}");
        Console.WriteLine($"[RESET PASSWORD] User Name: {user.Name}");

        // Decode token using WebEncoders.Base64UrlDecode
        string decodedToken;
        try
        {
            Console.WriteLine("----------------------------------------");
            Console.WriteLine("[RESET PASSWORD] Decoding URL-encoded token...");
            var tokenBytes = WebEncoders.Base64UrlDecode(request.Token);
            decodedToken = Encoding.UTF8.GetString(tokenBytes);
            Console.WriteLine($"[RESET PASSWORD] ✅ Token decoded successfully");
            Console.WriteLine($"[RESET PASSWORD] Decoded token length: {decodedToken.Length}");
            Console.WriteLine($"[RESET PASSWORD] Decoded token (first 50 chars): {decodedToken.Substring(0, Math.Min(50, decodedToken.Length))}...");
            _logger.LogInformation("Token decoded successfully for {Email}", email);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[RESET PASSWORD] ❌ Failed to decode token: {ex.Message}");
            Console.WriteLine("========================================");
            _logger.LogWarning(ex, "Failed to decode reset token for {Email}", email);
            throw new InvalidOperationException("Invalid or expired reset token.");
        }

        // Reset password using UserManager (will throw exception with details if it fails)
        Console.WriteLine("----------------------------------------");
        Console.WriteLine("[RESET PASSWORD] Calling UserRepository.ResetPasswordAsync...");
        _logger.LogInformation("Attempting to reset password for user: {UserId}", user.Id);
        
        try
        {
            await _userRepository.ResetPasswordAsync(user, decodedToken, request.NewPassword);
            
            Console.WriteLine("[RESET PASSWORD] ✅✅✅ PASSWORD RESET SUCCESSFUL ✅✅✅");
            Console.WriteLine("========================================");
            _logger.LogInformation("Password reset successful for: {Email}", email);

            return new MessageResponse
            {
                Message = "Password reset successful."
            };
        }
        catch (InvalidOperationException ex)
        {
            Console.WriteLine($"[RESET PASSWORD] ❌ Reset failed: {ex.Message}");
            Console.WriteLine("========================================");
            _logger.LogWarning("Password reset failed for {Email}: {Error}", email, ex.Message);
            throw; // Re-throw to let controller handle it
        }
    }
}