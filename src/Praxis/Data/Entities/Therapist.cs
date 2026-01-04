using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Praxis.Data.Entities;

/// <summary>
/// Therapist entity - clinical practitioner profile.
/// Maps to: therapist table
/// </summary>
[Table("therapist")]
public class Therapist
{
    [Key]
    [Column("therapist_id")]
    public Guid TherapistId { get; set; }

    [Required]
    [Column("firm_id")]
    public Guid FirmId { get; set; }

    [Required]
    [Column("firm_user_id")]
    public Guid FirmUserId { get; set; }

    [Required]
    [Column("first_name")]
    public string FirstName { get; set; } = string.Empty;

    [Required]
    [Column("last_name")]
    public string LastName { get; set; } = string.Empty;

    [Column("license_type")]
    public string? LicenseType { get; set; }

    [Column("license_number")]
    public string? LicenseNumber { get; set; }

    [Column("license_state")]
    public string? LicenseState { get; set; }

    [Column("npi_number")]
    public string? NpiNumber { get; set; }

    [Column("is_clinical_active")]
    public bool IsClinicalActive { get; set; } = true;

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

    // Navigation properties
    [ForeignKey("FirmId")]
    public virtual Firm? Firm { get; set; }

    [ForeignKey("FirmUserId")]
    public virtual FirmUser? FirmUser { get; set; }

    public virtual ICollection<TherapistClient> TherapistClients { get; set; } = new List<TherapistClient>();
    public virtual ICollection<CaseFile> CaseFiles { get; set; } = new List<CaseFile>();
    public virtual ICollection<Session> Sessions { get; set; } = new List<Session>();
}
