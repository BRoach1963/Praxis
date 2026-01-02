namespace Praxis.Models;

/// <summary>
/// Practice or clinic (top-level tenant).
/// All other entities belong to a practice.
/// </summary>
public class Practice
{
    public Guid PracticeId { get; set; } = Guid.NewGuid();

    public string Name { get; set; } = string.Empty;

    public string? TimeZone { get; set; } = "America/Chicago";

    public string? DefaultCurrency { get; set; } = "USD";

    public int DefaultSessionLengthMinutes { get; set; } = 60;

    public string? AddressLine1 { get; set; }

    public string? AddressLine2 { get; set; }

    public string? City { get; set; }

    public string? StateProvince { get; set; }

    public string? PostalCode { get; set; }

    public string? Country { get; set; }

    public string? Phone { get; set; }

    public bool IsActive { get; set; } = true;

    public DateTime CreatedOnUtc { get; set; } = DateTime.UtcNow;

    public DateTime UpdatedOnUtc { get; set; } = DateTime.UtcNow;

    public bool IsDeleted { get; set; }

    public DateTime? DeletedOnUtc { get; set; }

    public Guid? DeletedByUserProfileId { get; set; }

    // Navigation
    public List<PracticeUser> PracticeUsers { get; set; } = [];
    public List<Therapist> Therapists { get; set; } = [];
    public List<Client> Clients { get; set; } = [];
    public List<ServiceCode> ServiceCodes { get; set; } = [];
    public List<Invoice> Invoices { get; set; } = [];
    public List<Tag> Tags { get; set; } = [];
    public List<PraxisTask> Tasks { get; set; } = [];
    public List<AuditLog> AuditLogs { get; set; } = [];
}