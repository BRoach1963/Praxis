namespace Praxis.Models;

/// <summary>
/// Membership + role binding between Practice and UserProfile.
/// </summary>
public class PracticeUser
{
    public Guid PracticeUserId { get; set; } = Guid.NewGuid();

    public Guid PracticeId { get; set; }

    public Guid UserProfileId { get; set; }

    public PracticeRole Role { get; set; }

    public bool IsActive { get; set; } = true;

    public DateTime InvitedOnUtc { get; set; } = DateTime.UtcNow;

    public DateTime? AcceptedOnUtc { get; set; }

    public DateTime CreatedOnUtc { get; set; } = DateTime.UtcNow;

    public DateTime UpdatedOnUtc { get; set; } = DateTime.UtcNow;

    public Guid? CreatedByUserProfileId { get; set; }

    // Navigation
    public Practice? Practice { get; set; }
    public UserProfile? UserProfile { get; set; }
}

public enum PracticeRole
{
    Owner,
    Admin,
    Therapist,
    Biller,
    Staff,
    ReadOnly
}
