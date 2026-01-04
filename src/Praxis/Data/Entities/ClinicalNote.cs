using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Praxis.Data.Entities;

/// <summary>
/// Clinical note entity - encrypted clinical documentation.
/// Maps to: clinical_note table
/// </summary>
[Table("clinical_note")]
public class ClinicalNote
{
    [Key]
    [Column("clinical_note_id")]
    public Guid ClinicalNoteId { get; set; }

    [Required]
    [Column("session_id")]
    public Guid SessionId { get; set; }

    [Required]
    [Column("key_id")]
    public Guid KeyId { get; set; }

    [Required]
    [Column("note_type")]
    public string NoteType { get; set; } = "Progress";

    // Encrypted payload
    [Required]
    [Column("ciphertext")]
    public byte[] Ciphertext { get; set; } = Array.Empty<byte>();

    [Required]
    [Column("nonce")]
    public byte[] Nonce { get; set; } = Array.Empty<byte>();

    [Required]
    [Column("algorithm")]
    public string Algorithm { get; set; } = "AES-256-GCM";

    [Column("aad")]
    public byte[]? Aad { get; set; }

    [Column("content_hash")]
    public byte[]? ContentHash { get; set; }

    // Metadata
    [Required]
    [Column("status")]
    public string Status { get; set; } = "Draft";

    [Column("finalized_utc")]
    public DateTime? FinalizedUtc { get; set; }

    [Column("finalized_by_id")]
    public Guid? FinalizedById { get; set; }

    [Column("word_count")]
    public int? WordCount { get; set; }

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
    [ForeignKey("SessionId")]
    public virtual Session? Session { get; set; }

    [ForeignKey("KeyId")]
    public virtual KeyRing? KeyRing { get; set; }

    [ForeignKey("FinalizedById")]
    public virtual Therapist? FinalizedBy { get; set; }
}
