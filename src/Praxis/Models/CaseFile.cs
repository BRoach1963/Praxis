namespace Praxis.Models;

/// <summary>
/// Episode of Care — a course of treatment for a client.
/// </summary>
public class CaseFile
{
    public Guid CaseFileId { get; set; } = Guid.NewGuid();

    public Guid ClientId { get; set; }

    public Guid PrimaryTherapistId { get; set; }

    public DateTime StartDate { get; set; } = DateTime.Today;

    public DateTime? EndDate { get; set; }

    public string? PresentingProblems { get; set; }

    public CaseFileStatus Status { get; set; } = CaseFileStatus.Active;

    public int Version { get; set; } = 1;

    public DateTime CreatedOnUtc { get; set; } = DateTime.UtcNow;

    public DateTime UpdatedOnUtc { get; set; } = DateTime.UtcNow;

    public Guid UpdatedByUserProfileId { get; set; }

    // Navigation
    public Client? Client { get; set; }
    public Therapist? PrimaryTherapist { get; set; }
    public List<Session> Sessions { get; set; } = [];
    public List<TreatmentPlan> TreatmentPlans { get; set; } = [];
    public List<Assessment> Assessments { get; set; } = [];
}

public enum CaseFileStatus
{
    Active,
    Paused,
    Closed
}
