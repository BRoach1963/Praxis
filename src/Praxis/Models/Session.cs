namespace Praxis.Models;

/// <summary>
/// A session (appointment completed or in progress).
/// </summary>
public class Session
{
    public Guid SessionId { get; set; } = Guid.NewGuid();

    public Guid CaseFileId { get; set; }

    public Guid TherapistId { get; set; }

    public DateTime StartUtc { get; set; }

    public DateTime EndUtc { get; set; }

    public int DurationMinutes => (int)(EndUtc - StartUtc).TotalMinutes;

    public SessionLocationType LocationType { get; set; }

    public string? TelehealthJoinLink { get; set; }

    public SessionStatus Status { get; set; } = SessionStatus.Scheduled;

    public string? Attendees { get; set; } // JSON

    public DateTime CreatedOnUtc { get; set; } = DateTime.UtcNow;

    public Guid CreatedByUserProfileId { get; set; }

    public DateTime UpdatedOnUtc { get; set; } = DateTime.UtcNow;

    // Navigation
    public CaseFile? CaseFile { get; set; }
    public Therapist? Therapist { get; set; }
    public ClinicalNote? ClinicalNote { get; set; }
    public InvoiceLine? InvoiceLine { get; set; }
}

public enum SessionLocationType
{
    InPerson,
    Telehealth,
    Phone
}

public enum SessionStatus
{
    Scheduled,
    InProgress,
    Completed,
    NoShow,
    Cancelled
}
