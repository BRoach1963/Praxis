using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Praxis.Data.Entities;

/// <summary>
/// Key ring entity - encryption key metadata.
/// Maps to: key_ring table
/// NOTE: Stores metadata only, not actual key material.
/// </summary>
[Table("key_ring")]
public class KeyRing
{
    [Key]
    [Column("key_id")]
    public Guid KeyId { get; set; }

    [Required]
    [Column("firm_id")]
    public Guid FirmId { get; set; }

    [Required]
    [Column("key_name")]
    public string KeyName { get; set; } = string.Empty;

    [Required]
    [Column("algorithm")]
    public string Algorithm { get; set; } = "AES-256-GCM";

    [Column("key_version")]
    public int KeyVersion { get; set; } = 1;

    [Required]
    [Column("status")]
    public string Status { get; set; } = "Active";

    [Column("created_utc")]
    public DateTime CreatedUtc { get; set; }

    [Column("activated_utc")]
    public DateTime? ActivatedUtc { get; set; }

    [Column("retired_utc")]
    public DateTime? RetiredUtc { get; set; }

    [Column("expires_utc")]
    public DateTime? ExpiresUtc { get; set; }

    [Column("notes")]
    public string? Notes { get; set; }

    // Navigation properties
    [ForeignKey("FirmId")]
    public virtual Firm? Firm { get; set; }

    public virtual ICollection<ClinicalNote> ClinicalNotes { get; set; } = new List<ClinicalNote>();
}
