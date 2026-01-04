using Microsoft.EntityFrameworkCore;
using Praxis.Data;
using Praxis.Data.Entities;
using Praxis.Models;

namespace Praxis.Services;

/// <summary>
/// Authentication service using local PostgreSQL database.
/// Handles login, password validation, and password change operations.
/// Uses BCrypt for password hashing.
/// </summary>
public class AuthenticationService
{
    private readonly string _connectionString;

    public AuthenticationService(string connectionString)
    {
        _connectionString = connectionString;
    }

    /// <summary>
    /// Attempts to log in a user with email and password.
    /// Queries the local PostgreSQL database.
    /// </summary>
    public async Task<LoginResult> LoginAsync(string email, string password)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
            {
                return LoginResult.Failed("Email and password are required.");
            }

            await using var db = new PraxisContext(_connectionString);

            // Find user by email (case-insensitive)
            var userProfile = await db.UserProfiles
                .AsNoTracking()
                .FirstOrDefaultAsync(u => 
                    u.Email.ToLower() == email.ToLower() && 
                    !u.IsDeleted);

            if (userProfile == null)
            {
                return LoginResult.Failed("Invalid email or password.");
            }

            // Verify password using BCrypt
            if (string.IsNullOrEmpty(userProfile.PasswordHash))
            {
                return LoginResult.Failed("Invalid email or password.");
            }

            bool passwordValid = BCrypt.Net.BCrypt.Verify(password, userProfile.PasswordHash);
            if (!passwordValid)
            {
                return LoginResult.Failed("Invalid email or password.");
            }

            // Get user's firm memberships with firm details
            var firmUsers = await db.FirmUsers
                .AsNoTracking()
                .Include(fu => fu.Firm)
                .Where(fu => 
                    fu.UserProfileId == userProfile.UserProfileId && 
                    fu.IsActive && 
                    !fu.IsDeleted &&
                    fu.Firm != null && 
                    !fu.Firm.IsDeleted)
                .ToListAsync();

            if (firmUsers.Count == 0)
            {
                return LoginResult.Failed("No active firm memberships found for this account.");
            }

            // Get therapist profile if exists (for the first firm)
            var firstFirmUser = firmUsers[0];
            var therapist = await db.Therapists
                .AsNoTracking()
                .FirstOrDefaultAsync(t => 
                    t.FirmUserId == firstFirmUser.FirmUserId && 
                    !t.IsDeleted);

            // Update last login timestamp
            var userToUpdate = await db.UserProfiles.FindAsync(userProfile.UserProfileId);
            if (userToUpdate != null)
            {
                // Also update firm_user last_login_utc
                var firmUserToUpdate = await db.FirmUsers.FindAsync(firstFirmUser.FirmUserId);
                if (firmUserToUpdate != null)
                {
                    firmUserToUpdate.LastLoginUtc = DateTime.UtcNow;
                    firmUserToUpdate.UpdatedUtc = DateTime.UtcNow;
                }
                await db.SaveChangesAsync();
            }

            // Build result
            var firms = firmUsers
                .Where(fu => fu.Firm != null)
                .Select(fu => new FirmInfo
                {
                    FirmId = fu.FirmId,
                    Name = fu.Firm!.Name,
                    TimeZoneIana = fu.Firm.TimeZoneIana,
                    Role = fu.Role
                })
                .ToList();

            return LoginResult.Succeeded(new AuthenticatedUser
            {
                UserProfileId = userProfile.UserProfileId,
                Email = userProfile.Email,
                DisplayName = userProfile.DisplayName ?? userProfile.Email,
                CurrentFirmId = firstFirmUser.FirmId,
                CurrentFirmName = firstFirmUser.Firm!.Name,
                CurrentRole = firstFirmUser.Role,
                TherapistId = therapist?.TherapistId,
                TherapistName = therapist != null ? $"{therapist.FirstName} {therapist.LastName}" : null,
                AccessibleFirms = firms
            });
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Login error: {ex.Message}");
            System.Diagnostics.Debug.WriteLine($"Stack trace: {ex.StackTrace}");
            return LoginResult.Failed($"Login failed: {ex.Message}");
        }
    }

    /// <summary>
    /// Changes a user's password.
    /// </summary>
    public async Task<(bool Success, string? Error)> ChangePasswordAsync(
        Guid userProfileId,
        string currentPassword,
        string newPassword)
    {
        try
        {
            if (!IsPasswordValid(newPassword))
            {
                return (false, "Password must be at least 6 characters long.");
            }

            await using var db = new PraxisContext(_connectionString);

            var userProfile = await db.UserProfiles.FindAsync(userProfileId);
            if (userProfile == null || userProfile.IsDeleted)
            {
                return (false, "User not found.");
            }

            // Verify current password
            if (string.IsNullOrEmpty(userProfile.PasswordHash))
            {
                return (false, "Current password is incorrect.");
            }

            bool passwordValid = BCrypt.Net.BCrypt.Verify(currentPassword, userProfile.PasswordHash);
            if (!passwordValid)
            {
                return (false, "Current password is incorrect.");
            }

            // Hash and save new password
            userProfile.PasswordHash = BCrypt.Net.BCrypt.HashPassword(newPassword);
            userProfile.UpdatedUtc = DateTime.UtcNow;
            userProfile.Version++;

            await db.SaveChangesAsync();

            return (true, null);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Password change error: {ex.Message}");
            return (false, "An error occurred while changing your password.");
        }
    }

    /// <summary>
    /// Validates password format.
    /// </summary>
    public static bool IsPasswordValid(string password)
    {
        return !string.IsNullOrEmpty(password) && password.Length >= 6;
    }

    /// <summary>
    /// Validates email format.
    /// </summary>
    public static bool IsEmailValid(string email)
    {
        var regex = new System.Text.RegularExpressions.Regex(@"^[^\s@]+@[^\s@]+\.[^\s@]+$");
        return regex.IsMatch(email);
    }
}

/// <summary>
/// Result of a login attempt.
/// </summary>
public class LoginResult
{
    public bool Success { get; private set; }
    public string? ErrorMessage { get; private set; }
    public AuthenticatedUser? User { get; private set; }

    public static LoginResult Succeeded(AuthenticatedUser user) => new() { Success = true, User = user };
    public static LoginResult Failed(string error) => new() { Success = false, ErrorMessage = error };
}

/// <summary>
/// Authenticated user information returned after successful login.
/// </summary>
public class AuthenticatedUser
{
    public Guid UserProfileId { get; set; }
    public string Email { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    
    // Current context
    public Guid CurrentFirmId { get; set; }
    public string CurrentFirmName { get; set; } = string.Empty;
    public string CurrentRole { get; set; } = string.Empty;
    
    // Therapist info (if applicable)
    public Guid? TherapistId { get; set; }
    public string? TherapistName { get; set; }
    
    // All accessible firms
    public List<FirmInfo> AccessibleFirms { get; set; } = new();
}

/// <summary>
/// Firm information for multi-firm access.
/// </summary>
public class FirmInfo
{
    public Guid FirmId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string TimeZoneIana { get; set; } = "America/Chicago";
    public string Role { get; set; } = string.Empty;
}
