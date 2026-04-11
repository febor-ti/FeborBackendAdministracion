using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FeborBack.Domain.Entities;

[Table("login_user", Schema = "auth")]
public class LoginUser
{
    [Key]
    [Column("user_id")]
    public int UserId { get; set; }

    [Column("person_id")]
    public int PersonId { get; set; }

    [Column("username")]
    [MaxLength(100)]
    public string Username { get; set; } = string.Empty;

    [Column("password_hash")]
    [MaxLength(255)]
    public string PasswordHash { get; set; } = string.Empty;

    [Column("password_salt")]
    [MaxLength(255)]
    public string PasswordSalt { get; set; } = string.Empty;

    [Column("email")]
    [MaxLength(255)]
    public string Email { get; set; } = string.Empty;

    [Column("is_session_active")]
    public bool IsSessionActive { get; set; }

    [Column("is_temporary_password")]
    public bool IsTemporaryPassword { get; set; }

    [Column("failed_attempts")]
    public int FailedAttempts { get; set; }

    [Column("last_access_at")]
    public DateTime? LastAccessAt { get; set; }

    [Column("authorized_by_user_id")]
    public int? AuthorizedByUserId { get; set; }

    [Column("avatar_name")]
    [MaxLength(255)]
    public string? AvatarName { get; set; }

    [Column("status_id")]
    public int StatusId { get; set; }

    [Column("status_reason_id")]
    public int? StatusReasonId { get; set; }

    [Column("created_by")]
    public int CreatedBy { get; set; }

    [Column("created_at")]
    public DateTime CreatedAt { get; set; }

    [Column("updated_by")]
    public int? UpdatedBy { get; set; }

    [Column("updated_at")]
    public DateTime? UpdatedAt { get; set; }

    // Navegación
    [ForeignKey("PersonId")]
    public virtual Person Person { get; set; } = null!;

    [ForeignKey("StatusId")]
    public virtual Status Status { get; set; } = null!;

    [ForeignKey("StatusReasonId")]
    public virtual StatusReason? StatusReason { get; set; }

    [ForeignKey("AuthorizedByUserId")]
    public virtual LoginUser? AuthorizedBy { get; set; }

    // Refresh Tokens
    public virtual ICollection<RefreshToken> RefreshTokens { get; set; } = new List<RefreshToken>();

    // Roles
    public virtual ICollection<UserRole> UserRoles { get; set; } = new List<UserRole>();
}