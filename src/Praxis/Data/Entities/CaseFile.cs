using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Praxis.Data.Entities;

/// <summary>
/// Case file entity - episode of care.
/// Maps to: case_file table
/// </summary>
[Table("case_file")]
public class CaseFile
{
    [Key]
    [Column("case_file_id")]
    public Guid CaseFileId { get; set; }

    [Required]
    [Column("client_id")]
    public Guid ClientId { get; set; }

    [Required]
    [Column("therapist_id")]
    public Guid TherapistId { get; set; }

    [Column("case_number")]
    public string? CaseNumber { get; set; }

    [Required]
    [Column("opened_date")]
    public DateOnly OpenedDate { get; set; }

    [Column("closed_date")]
    public DateOnly? ClosedDate { get; set; }

    [Required]
    [Column("status")]
    public string Status { get; set; } = "Open";

    [Column("presenting_problem")]
    public string? PresentingProblem { get; set; }

    [Column("diagnosis_primary")]
    public string? DiagnosisPrimary { get; set; }

    [Column("diagnosis_secondary")]
    public string? DiagnosisSecondary { get; set; }

    [Column("treatment_modality")]
    public string? TreatmentModality { get; set; }

    [Column("session_frequency")]
    public string? SessionFrequency { get; set; }

    [Column("notes")]
    public string? Notes { get; set; }

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
    [ForeignKey("ClientId")]
    public virtual Client? Client { get; set; }

    [ForeignKey("TherapistId")]
    public virtual Therapist? Therapist { get; set; }

    public virtual ICollection<Session> Sessions { get; set; } = new List<Session>();
}
