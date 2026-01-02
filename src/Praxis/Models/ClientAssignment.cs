namespace Praxis.Models;

/// <summary>
/// Links a Client to one or more Therapists.
/// </summary>
public class ClientAssignment
{
    public Guid ClientAssignmentId { get; set; } = Guid.NewGuid();

    public Guid ClientId { get; set; }

    public Guid TherapistId { get; set; }

    public ClientAssignmentRole Role { get; set; }

    public DateTime StartDate { get; set; } = DateTime.Today;

    public DateTime? EndDate { get; set; }

    public DateTime CreatedOnUtc { get; set; } = DateTime.UtcNow;

    public DateTime UpdatedOnUtc { get; set; } = DateTime.UtcNow;

    // Navigation
    public Client? Client { get; set; }
    public Therapist? Therapist { get; set; }
}

public enum ClientAssignmentRole
{
    Primary,
    Secondary,
    Supervisor
}
