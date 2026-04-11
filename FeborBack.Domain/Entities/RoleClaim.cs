using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FeborBack.Domain.Entities;

[Table("role_claim", Schema = "auth")]
public class RoleClaim
{
    [Key]
    [Column("claim_id")]
    public int ClaimId { get; set; }

    [Column("role_id")]
    public int RoleId { get; set; }

    [Column("claim_type")]
    [MaxLength(100)]
    public string ClaimType { get; set; } = string.Empty;

    [Column("claim_value")]
    [MaxLength(255)]
    public string ClaimValue { get; set; } = string.Empty;

    // Navegación
    [ForeignKey("RoleId")]
    public virtual Role Role { get; set; } = null!;
}