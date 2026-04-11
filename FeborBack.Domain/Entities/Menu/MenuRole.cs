using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FeborBack.Domain.Entities.Menu;

[Table("menu_role", Schema = "menu")]
public class MenuRole
{
    [Key]
    [Column("menu_role_id")]
    public int MenuRoleId { get; set; }

    [Column("menu_item_id")]
    public int MenuItemId { get; set; }

    [Column("role_id")]
    public int RoleId { get; set; }

    [Column("assigned_by")]
    public int AssignedBy { get; set; }

    [Column("assigned_at")]
    public DateTime AssignedAt { get; set; }

    // Navegación
    [ForeignKey("MenuItemId")]
    public virtual MenuItem MenuItem { get; set; } = null!;

    [ForeignKey("RoleId")]
    public virtual Role Role { get; set; } = null!;
}
