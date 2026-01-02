namespace Praxis.Models;

/// <summary>
/// Individual in care.
/// </summary>
public class Client
{
    public Guid ClientId { get; set; } = Guid.NewGuid();

    public Guid PracticeId { get; set; }

    public string FirstName { get; set; } = string.Empty;

    public string LastName { get; set; } = string.Empty;

    public string? PreferredName { get; set; }

    public string? Pronouns { get; set; }

    public DateTime? DateOfBirth { get; set; }

    public string? Gender { get; set; }

    public string? Email { get; set; }

    public string? Phone { get; set; }

    public string? EmergencyContactName { get; set; }

    public string? EmergencyContactPhone { get; set; }

    public ClientStatus Status { get; set; } = ClientStatus.Active;

    public DateTime? IntakeDate { get; set; }

    public DateTime? TerminationDate { get; set; }

    public DateTime CreatedOnUtc { get; set; } = DateTime.UtcNow;

    public DateTime UpdatedOnUtc { get; set; } = DateTime.UtcNow;

    public bool IsDeleted { get; set; }

    public DateTime? DeletedOnUtc { get; set; }

    // Navigation
    public Practice? Practice { get; set; }
    public List<ClientAssignment> Assignments { get; set; } = [];
    public List<CaseFile> CaseFiles { get; set; } = [];
    public List<Session> Sessions { get; set; } = [];
}

public enum ClientStatus
{
    Active,
    Inactive,
    Archived
}
