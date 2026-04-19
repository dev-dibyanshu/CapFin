using CapFinLoan.Auth.Application.Contracts.Requests;
using CapFinLoan.Auth.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CapFinLoan.Auth.API.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;

    public AuthController(IAuthService authService)
    {
        _authService = authService;
    }

    [HttpPost("signup")]
    [AllowAnonymous]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Signup([FromBody] SignupRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var result = await _authService.SignupAsync(request, cancellationToken);
            return Ok(result);
        }
        catch (InvalidOperationException exception)
        {
            return BadRequest(new { message = exception.Message });
        }
    }

    [HttpPost("login")]
    [AllowAnonymous]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Login([FromBody] LoginRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var result = await _authService.LoginAsync(request, cancellationToken);
            return Ok(result);
        }
        catch (UnauthorizedAccessException exception)
        {
            return Unauthorized(new { message = exception.Message });
        }
    }

    [HttpPost("forgot-password")]
    [AllowAnonymous]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordRequest request, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var result = await _authService.ForgotPasswordAsync(request, cancellationToken);
        return Ok(result);
    }

    [HttpPost("reset-password")]
    [AllowAnonymous]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordRequest request, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        try
        {
            var result = await _authService.ResetPasswordAsync(request, cancellationToken);
            return Ok(result);
        }
        catch (InvalidOperationException exception)
        {
            return BadRequest(new { message = exception.Message });
        }
    }

    [HttpGet("health")]
    [AllowAnonymous]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public IActionResult Health()
    {
        return Ok(new { status = "Auth service running", timestamp = DateTime.UtcNow });
    }

    [HttpGet("test-email")]
    [AllowAnonymous]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> TestEmail([FromQuery] string email, CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(email))
        {
            return BadRequest(new { message = "Email parameter is required. Usage: /api/auth/test-email?email=your@email.com" });
        }

        Console.WriteLine("========================================");
        Console.WriteLine("[TEST EMAIL] Endpoint called");
        Console.WriteLine($"[TEST EMAIL] Target email: {email}");
        Console.WriteLine("========================================");

        try
        {
            var testResetLink = "http://localhost:5174/reset-password?email=test@example.com&token=TEST_TOKEN_123";
            
            // Get email service from DI
            var emailService = HttpContext.RequestServices.GetRequiredService<IEmailService>();
            
            Console.WriteLine("[TEST EMAIL] Calling email service...");
            await emailService.SendPasswordResetEmailAsync(email, "Test User", testResetLink, cancellationToken);
            
            Console.WriteLine("[TEST EMAIL] ✅ Email sent successfully");
            return Ok(new 
            { 
                success = true,
                message = $"Test email sent successfully to {email}",
                timestamp = DateTime.UtcNow
            });
        }
        catch (Exception ex)
        {
            Console.WriteLine("[TEST EMAIL] ❌ Failed to send email");
            Console.WriteLine($"[TEST EMAIL ERROR]: {ex.Message}");
            Console.WriteLine(ex.ToString());
            
            return StatusCode(500, new 
            { 
                success = false,
                message = $"Failed to send email: {ex.Message}",
                error = ex.ToString(),
                timestamp = DateTime.UtcNow
            });
        }
    }
}