using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FeborBack.Domain.Entities;

[Table("user_role", Schema = "auth")]
public class UserRole
{
    [Key]
    [Column("user_role_id")]
    public int UserRoleId { get; set; }

    [Column("user_id")]
    public int UserId { get; set; }

    [Column("role_id")]
    public int RoleId { get; set; }

    [Column("assigned_at")]
    public DateTime AssignedAt { get; set; }

    [Column("assigned_by")]
    public int AssignedBy { get; set; }

    // Navegación
    [ForeignKey("UserId")]
    public virtual LoginUser User { get; set; } = null!;

    [ForeignKey("RoleId")]
    public virtual Role Role { get; set; } = null!;
}