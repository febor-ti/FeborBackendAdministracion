using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FeborBack.Domain.Entities;

[Table("refresh_token", Schema = "auth")]
public class RefreshToken
{
    [Key]
    [Column("token_id")]
    public int TokenId { get; set; }

    [Column("user_id")]
    public int UserId { get; set; }

    [Column("token")]
    [MaxLength(500)]
    public string Token { get; set; } = string.Empty;

    [Column("expires_at")]
    public DateTime ExpiresAt { get; set; }

    [Column("created_at")]
    public DateTime CreatedAt { get; set; }

    [Column("revoked_at")]
    public DateTime? RevokedAt { get; set; }

    [Column("is_used")]
    public bool IsUsed { get; set; }

    [Column("replaced_by_token")]
    [MaxLength(500)]
    public string? ReplacedByToken { get; set; }

    // Navegación
    [ForeignKey("UserId")]
    public virtual LoginUser User { get; set; } = null!;

    [NotMapped]
    public bool IsExpired => DateTime.UtcNow >= ExpiresAt;

    [NotMapped]
    public bool IsActive => !IsUsed && !IsExpired && RevokedAt == null;

}