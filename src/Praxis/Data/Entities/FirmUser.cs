using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Praxis.Data.Entities;

/// <summary>
/// Firm user entity - membership linking user to firm with role.
/// Maps to: firm_user table
/// </summary>
[Table("firm_user")]
public class FirmUser
{
    [Key]
    [Column("firm_user_id")]
    public Guid FirmUserId { get; set; }

    [Required]
    [Column("firm_id")]
    public Guid FirmId { get; set; }

    [Required]
    [Column("user_profile_id")]
    public Guid UserProfileId { get; set; }

    [Required]
    [Column("role")]
    public string Role { get; set; } = "Staff";

    [Column("is_active")]
    public bool IsActive { get; set; } = true;

    [Column("last_login_utc")]
    public DateTime? LastLoginUtc { get; set; }

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
    [ForeignKey("FirmId")]
    public virtual Firm? Firm { get; set; }

    [ForeignKey("UserProfileId")]
    public virtual UserProfileEntity? UserProfile { get; set; }

    public virtual Therapist? Therapist { get; set; }
}
