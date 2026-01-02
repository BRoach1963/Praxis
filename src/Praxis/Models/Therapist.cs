namespace Praxis.Models;

/// <summary>
/// Clinical practitioner profile.
/// May (but doesn't have to) map to a PracticeUser.
/// Therapist ≠ Login.
/// </summary>
public class Therapist
{
    public Guid TherapistId { get; set; } = Guid.NewGuid();

    public Guid PracticeId { get; set; }

    /// <summary>
    /// Optional link to a practice user (login).
    /// </summary>
    public Guid? PracticeUserId { get; set; }

    public string FirstName { get; set; } = string.Empty;

    public string LastName { get; set; } = string.Empty;

    public string? LicenseNumber { get; set; }

    public string? LicenseState { get; set; }

    public string? NPI { get; set; }

    public string? Credential { get; set; }

    public string? Specialty { get; set; }

    public string? Bio { get; set; }

    public string? SignatureBlock { get; set; }

    public bool IsActive { get; set; } = true;

    public DateTime CreatedOnUtc { get; set; } = DateTime.UtcNow;

    public DateTime UpdatedOnUtc { get; set; } = DateTime.UtcNow;

    public bool IsDeleted { get; set; }

    public DateTime? DeletedOnUtc { get; set; }

    // Navigation
    public Practice? Practice { get; set; }
    public PracticeUser? PracticeUser { get; set; }
    public List<ClientAssignment> ClientAssignments { get; set; } = [];
    public List<Session> Sessions { get; set; } = [];
    public List<CaseFile> CaseFilesAsPrimary { get; set; } = [];
    public List<AvailabilityRule> AvailabilityRules { get; set; } = [];
}
