namespace Praxis.Models;

/// <summary>
/// User profile linked to Supabase authentication.
/// One login identity can access multiple practices.
/// </summary>
public class UserProfile
{
    public Guid UserProfileId { get; set; } = Guid.NewGuid();

    /// <summary>
    /// UUID from Supabase auth.users(id)
    /// </summary>
    public Guid AuthUserId { get; set; }

    public string Email { get; set; } = string.Empty;

    public string DisplayName { get; set; } = string.Empty;

    public string? AvatarUrl { get; set; }

    public bool IsActive { get; set; } = true;

    public DateTime CreatedOnUtc { get; set; } = DateTime.UtcNow;

    public DateTime UpdatedOnUtc { get; set; } = DateTime.UtcNow;

    public DateTime? LastLoginUtc { get; set; }

    // Navigation
    public List<PracticeUser> PracticeUsers { get; set; } = [];
}