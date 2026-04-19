using CapFinLoan.Auth.Application.Interfaces;
using CapFinLoan.Auth.Domain.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace CapFinLoan.Auth.Persistence.Repositories;

public class UserRepository : IUserRepository
{
    private readonly UserManager<ApplicationUser> _userManager;

    public UserRepository(UserManager<ApplicationUser> userManager)
    {
        _userManager = userManager;
    }

    public async Task<bool> ExistsByEmailAsync(string email, CancellationToken cancellationToken = default)
    {
        return await _userManager.FindByEmailAsync(email) is not null;
    }

    public async Task<ApplicationUser?> GetByEmailAsync(string email, CancellationToken cancellationToken = default)
    {
        return await _userManager.FindByEmailAsync(email);
    }

    public async Task<ApplicationUser?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _userManager.FindByIdAsync(id.ToString());
    }

    public async Task<IReadOnlyCollection<ApplicationUser>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _userManager.Users
            .AsNoTracking()
            .ToListAsync(cancellationToken);
    }

    public async Task CreateAsync(ApplicationUser user, string rawPassword, CancellationToken cancellationToken = default)
    {
        var result = await _userManager.CreateAsync(user, rawPassword);
        if (!result.Succeeded)
        {
            var errors = string.Join("; ", result.Errors.Select(e => e.Description));
            throw new InvalidOperationException($"Failed to create user: {errors}");
        }
    }

    public async Task UpdateAsync(ApplicationUser user, CancellationToken cancellationToken = default)
    {
        var result = await _userManager.UpdateAsync(user);
        if (!result.Succeeded)
        {
            var errors = string.Join("; ", result.Errors.Select(e => e.Description));
            throw new InvalidOperationException($"Failed to update user: {errors}");
        }
    }

    public async Task<bool> CheckPasswordAsync(ApplicationUser user, string password)
    {
        return await _userManager.CheckPasswordAsync(user, password);
    }

    public async Task<string> GeneratePasswordResetTokenAsync(ApplicationUser user)
    {
        return await _userManager.GeneratePasswordResetTokenAsync(user);
    }

    public async Task<bool> ResetPasswordAsync(ApplicationUser user, string token, string newPassword)
    {
        Console.WriteLine("========================================");
        Console.WriteLine("[USER REPOSITORY] Calling UserManager.ResetPasswordAsync...");
        Console.WriteLine($"[USER REPOSITORY] User ID: {user.Id}");
        Console.WriteLine($"[USER REPOSITORY] User Email: {user.Email}");
        Console.WriteLine($"[USER REPOSITORY] Token length: {token.Length}");
        Console.WriteLine($"[USER REPOSITORY] Token (first 50 chars): {token.Substring(0, Math.Min(50, token.Length))}...");
        Console.WriteLine($"[USER REPOSITORY] New password length: {newPassword.Length}");
        Console.WriteLine("----------------------------------------");
        
        var result = await _userManager.ResetPasswordAsync(user, token, newPassword);
        
        Console.WriteLine($"[USER REPOSITORY] Reset result: {(result.Succeeded ? "✅ SUCCESS" : "❌ FAILED")}");
        
        if (!result.Succeeded)
        {
            Console.WriteLine("========================================");
            Console.WriteLine("[USER REPOSITORY] ❌❌❌ RESET PASSWORD FAILED ❌❌❌");
            Console.WriteLine($"[USER REPOSITORY] Error count: {result.Errors.Count()}");
            Console.WriteLine("----------------------------------------");
            
            var errorMessages = new List<string>();
            foreach (var error in result.Errors)
            {
                Console.WriteLine($"[RESET ERROR] Code: {error.Code}");
                Console.WriteLine($"[RESET ERROR] Description: {error.Description}");
                errorMessages.Add($"{error.Code}: {error.Description}");
            }
            Console.WriteLine("========================================");
            
            // Throw exception with detailed error information
            var errorDetails = string.Join("; ", errorMessages);
            throw new InvalidOperationException($"Password reset failed: {errorDetails}");
        }
        
        Console.WriteLine("[USER REPOSITORY] ✅✅✅ RESET PASSWORD SUCCEEDED ✅✅✅");
        Console.WriteLine("========================================");
        return true;
    }
}