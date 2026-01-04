using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Praxis.Data.Entities;

/// <summary>
/// Session entity - clinical encounter.
/// Maps to: session table
/// </summary>
[Table("session")]
public class Session
{
    [Key]
    [Column("session_id")]
    public Guid SessionId { get; set; }

    [Required]
    [Column("case_file_id")]
    public Guid CaseFileId { get; set; }

    [Required]
    [Column("therapist_id")]
    public Guid TherapistId { get; set; }

    [Required]
    [Column("session_date")]
    public DateOnly SessionDate { get; set; }

    [Column("start_time")]
    public TimeOnly? StartTime { get; set; }

    [Column("end_time")]
    public TimeOnly? EndTime { get; set; }

    [Column("duration_minutes")]
    public int? DurationMinutes { get; set; }

    [Required]
    [Column("session_type")]
    public string SessionType { get; set; } = "Individual";

    [Required]
    [Column("session_format")]
    public string SessionFormat { get; set; } = "InPerson";

    [Required]
    [Column("status")]
    public string Status { get; set; } = "Scheduled";

    [Column("cancellation_reason")]
    public string? CancellationReason { get; set; }

    [Column("billing_code")]
    public string? BillingCode { get; set; }

    [Column("billing_status")]
    public string? BillingStatus { get; set; }

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
    [ForeignKey("CaseFileId")]
    public virtual CaseFile? CaseFile { get; set; }

    [ForeignKey("TherapistId")]
    public virtual Therapist? Therapist { get; set; }

    public virtual ICollection<ClinicalNote> ClinicalNotes { get; set; } = new List<ClinicalNote>();
}
