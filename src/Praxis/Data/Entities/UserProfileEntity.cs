using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Praxis.Data.Entities;

/// <summary>
/// User profile entity - authentication identity.
/// Maps to: user_profile table
/// </summary>
[Table("user_profile")]
public class UserProfileEntity
{
    [Key]
    [Column("user_profile_id")]
    public Guid UserProfileId { get; set; }

    [Required]
    [Column("auth_user_id")]
    public Guid AuthUserId { get; set; }

    [Required]
    [Column("email")]
    public string Email { get; set; } = string.Empty;

    [Column("password_hash")]
    public string? PasswordHash { get; set; }

    [Column("display_name")]
    public string? DisplayName { get; set; }

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
}
