namespace Praxis.Models;

/// <summary>
/// Standardized assessment instrument result.
/// </summary>
public class Assessment
{
    public Guid AssessmentId { get; set; } = Guid.NewGuid();

    public Guid CaseFileId { get; set; }

    public string Instrument { get; set; } = string.Empty; // "PHQ-9", "GAD-7", etc.

    public int Score { get; set; }

    public string? ResponsesJson { get; set; } // Full responses for records

    public string? Severity { get; set; } // "Minimal", "Mild", "Moderate", "Severe"

    public DateTime CompletedOnUtc { get; set; }

    public Guid? CompletedByUserProfileId { get; set; }

    public DateTime CreatedOnUtc { get; set; } = DateTime.UtcNow;

    // Navigation
    public CaseFile? CaseFile { get; set; }
}

/// <summary>
/// Scheduled appointment.
/// </summary>
public class Appointment
{
    public Guid AppointmentId { get; set; } = Guid.NewGuid();

    public Guid ClientId { get; set; }

    public Guid TherapistId { get; set; }

    public DateTime StartUtc { get; set; }

    public DateTime EndUtc { get; set; }

    public string? AppointmentType { get; set; }

    public SessionLocationType LocationType { get; set; }

    public string? TelehealthJoinLink { get; set; }

    public AppointmentStatus Status { get; set; } = AppointmentStatus.Booked;

    public string? Notes { get; set; }

    public DateTime CreatedOnUtc { get; set; } = DateTime.UtcNow;

    public Guid CreatedByUserProfileId { get; set; }

    public DateTime? CancelledOnUtc { get; set; }

    public Guid? CancelledByUserProfileId { get; set; }

    // Navigation
    public Client? Client { get; set; }
    public Therapist? Therapist { get; set; }
}

public enum AppointmentStatus
{
    Booked,
    Confirmed,
    NoShow,
    Cancelled,
    Completed
}

/// <summary>
/// Recurring availability for scheduling.
/// </summary>
public class AvailabilityRule
{
    public Guid AvailabilityRuleId { get; set; } = Guid.NewGuid();

    public Guid TherapistId { get; set; }

    public DayOfWeek DayOfWeek { get; set; }

    public TimeSpan StartTimeUtc { get; set; }

    public TimeSpan EndTimeUtc { get; set; }

    public bool IsActive { get; set; } = true;

    public string? Exceptions { get; set; } // JSON: dates when rule doesn't apply

    public DateTime CreatedOnUtc { get; set; } = DateTime.UtcNow;

    public DateTime UpdatedOnUtc { get; set; } = DateTime.UtcNow;

    // Navigation
    public Therapist? Therapist { get; set; }
}
