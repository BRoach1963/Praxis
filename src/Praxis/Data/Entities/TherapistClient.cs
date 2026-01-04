using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Praxis.Data.Entities;

/// <summary>
/// Therapist-client assignment entity.
/// Maps to: therapist_client table
/// </summary>
[Table("therapist_client")]
public class TherapistClient
{
    [Key]
    [Column("therapist_client_id")]
    public Guid TherapistClientId { get; set; }

    [Required]
    [Column("therapist_id")]
    public Guid TherapistId { get; set; }

    [Required]
    [Column("client_id")]
    public Guid ClientId { get; set; }

    [Required]
    [Column("assignment_type")]
    public string AssignmentType { get; set; } = "Primary";

    [Required]
    [Column("assigned_date")]
    public DateOnly AssignedDate { get; set; }

    [Column("unassigned_date")]
    public DateOnly? UnassignedDate { get; set; }

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

    // Navigation properties
    [ForeignKey("TherapistId")]
    public virtual Therapist? Therapist { get; set; }

    [ForeignKey("ClientId")]
    public virtual Client? Client { get; set; }
}
