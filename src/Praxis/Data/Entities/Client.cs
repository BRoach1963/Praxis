using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Praxis.Data.Entities;

/// <summary>
/// Client entity - patient/client.
/// Maps to: client table
/// </summary>
[Table("client")]
public class Client
{
    [Key]
    [Column("client_id")]
    public Guid ClientId { get; set; }

    [Required]
    [Column("firm_id")]
    public Guid FirmId { get; set; }

    [Required]
    [Column("first_name")]
    public string FirstName { get; set; } = string.Empty;

    [Required]
    [Column("last_name")]
    public string LastName { get; set; } = string.Empty;

    [Column("preferred_name")]
    public string? PreferredName { get; set; }

    [Column("date_of_birth")]
    public DateOnly? DateOfBirth { get; set; }

    [Column("email")]
    public string? Email { get; set; }

    [Column("phone")]
    public string? Phone { get; set; }

    [Column("address_line1")]
    public string? AddressLine1 { get; set; }

    [Column("address_line2")]
    public string? AddressLine2 { get; set; }

    [Column("city")]
    public string? City { get; set; }

    [Column("state")]
    public string? State { get; set; }

    [Column("postal_code")]
    public string? PostalCode { get; set; }

    [Column("emergency_contact")]
    public string? EmergencyContact { get; set; }

    [Column("emergency_phone")]
    public string? EmergencyPhone { get; set; }

    [Column("intake_date")]
    public DateOnly? IntakeDate { get; set; }

    [Column("is_active")]
    public bool IsActive { get; set; } = true;

    [Column("created_utc")]
    public DateTime CreatedUtc { get; set; }

    [Column("updated_utc")]
    public DateTime UpdatedUtc { get; set; }

    [Column("version")]
    public int Version { get; set; } = 1;

    [Column("is_deleted")]
    public bool IsDeleted { get; set; }

    [Column("deleted_utc")]
    public DateTime? DeletedUtc { get; set; }

    // Computed
    [NotMapped]
    public string FullName => $"{FirstName} {LastName}".Trim();

    [NotMapped]
    public string DisplayName => !string.IsNullOrEmpty(PreferredName) ? PreferredName : FullName;

    // Navigation properties
    [ForeignKey("FirmId")]
    public virtual Firm? Firm { get; set; }

    public virtual ICollection<TherapistClient> TherapistClients { get; set; } = new List<TherapistClient>();
    public virtual ICollection<CaseFile> CaseFiles { get; set; } = new List<CaseFile>();
}
