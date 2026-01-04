using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Praxis.Data.Entities;

/// <summary>
/// Firm (tenant) entity - the root organizational unit.
/// All business data is scoped to a firm.
/// Maps to: firm table
/// </summary>
[Table("firm")]
public class Firm
{
    [Key]
    [Column("firm_id")]
    public Guid FirmId { get; set; }

    [Required]
    [Column("name")]
    public string Name { get; set; } = string.Empty;

    [Required]
    [Column("time_zone_iana")]
    public string TimeZoneIana { get; set; } = "America/Chicago";

    [Required]
    [Column("status")]
    public string Status { get; set; } = "Active";

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

    // Navigation properties
    public virtual ICollection<FirmUser> FirmUsers { get; set; } = new List<FirmUser>();
    public virtual ICollection<Therapist> Therapists { get; set; } = new List<Therapist>();
    public virtual ICollection<Client> Clients { get; set; } = new List<Client>();
    public virtual ICollection<KeyRing> KeyRings { get; set; } = new List<KeyRing>();
}
