namespace Praxis.Models;

/// <summary>
/// Therapist's documentation of a session.
/// **IMMUTABLE once locked** to prevent accidental/deliberate modification.
/// Content encrypted at rest.
/// </summary>
public class ClinicalNote
{
    public Guid ClinicalNoteId { get; set; } = Guid.NewGuid();

    public Guid SessionId { get; set; }

    public ClinicalNoteType NoteType { get; set; }

    /// <summary>
    /// Encrypted content. Decrypted in-memory on load.
    /// </summary>
    public string Content { get; set; } = string.Empty;

    public DateTime CreatedOnUtc { get; set; } = DateTime.UtcNow;

    public Guid CreatedByUserProfileId { get; set; }

    public DateTime? UpdatedOnUtc { get; set; }

    public Guid? UpdatedByUserProfileId { get; set; }

    /// <summary>
    /// When locked, note becomes immutable. Updates/deletes are prevented.
    /// </summary>
    public DateTime? LockedOnUtc { get; set; }

    public Guid? LockedByUserProfileId { get; set; }

    // Navigation
    public Session? Session { get; set; }

    public bool IsLocked => LockedOnUtc.HasValue;
}

public enum ClinicalNoteType
{
    DAP,      // Descriptive, Assessment, Plan
    SOAP,     // Subjective, Objective, Assessment, Plan
    BIRP,     // Behavior, Intervention, Response, Plan
    Progress,
    Intake,
    Termination
}
