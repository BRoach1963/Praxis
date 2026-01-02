namespace Praxis.Models
{
    /// <summary>
    /// Application user model from Supabase app_user table.
    /// Represents a login identity in the system.
    /// </summary>
    public class AppUser
    {
        public Guid AppUserId { get; set; }
        public string Email { get; set; } = string.Empty;
        public string? PasswordHash { get; set; } // bcrypt hash - NOT sent from server on queries
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;
        public string? AvatarUrl { get; set; }
        public bool IsActive { get; set; }
        public bool MustChangePassword { get; set; }
        public DateTime CreatedOnUtc { get; set; }
        public DateTime UpdatedOnUtc { get; set; }
        public DateTime? LastLoginUtc { get; set; }

        public string FullName => $"{FirstName} {LastName}".Trim();
    }

    /// <summary>
    /// Practice information - the tenant/organization.
    /// </summary>
    public class PracticeInfo
    {
        public Guid PracticeId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string TimeZone { get; set; } = "UTC";
        public string DefaultCurrency { get; set; } = "USD";
        public int DefaultSessionLengthMinutes { get; set; } = 60;
        public string? Phone { get; set; }
        public string? Address { get; set; }
    }

    /// <summary>
    /// Response from login attempt.
    /// </summary>
    public class LoginResponse
    {
        public bool Success { get; set; }
        public AppUser? User { get; set; }
        public List<PracticeInfo> UserPractices { get; set; } = new();
        public string? ErrorMessage { get; set; }
        public bool MustChangePassword { get; set; }
    }

    /// <summary>
    /// Current session information stored in application.
    /// </summary>
    public class CurrentSession
    {
        public AppUser User { get; set; } = null!;
        public PracticeInfo CurrentPractice { get; set; } = null!;
        public PracticeRole Role { get; set; }
        public List<PracticeInfo> AccessiblePractices { get; set; } = new();
        public string AccessToken { get; set; } = string.Empty;
        public string? RefreshToken { get; set; }
        public DateTime TokenExpiresAt { get; set; }
    }
}
