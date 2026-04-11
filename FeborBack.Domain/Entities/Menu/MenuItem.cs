using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FeborBack.Domain.Entities.Menu;

[Table("menu_item", Schema = "menu")]
public class MenuItem
{
    [Key]
    [Column("menu_item_id")]
    public int MenuItemId { get; set; }

    [Column("parent_id")]
    public int? ParentId { get; set; }

    [Column("title")]
    [MaxLength(100)]
    public string Title { get; set; } = string.Empty;

    [Column("menu_key")]
    [MaxLength(100)]
    public string? MenuKey { get; set; }

    [Column("route_path")]
    [MaxLength(255)]
    public string? RoutePath { get; set; }

    [Column("route_name")]
    [MaxLength(100)]
    public string? RouteName { get; set; }

    [Column("icon")]
    [MaxLength(100)]
    public string? Icon { get; set; }

    [Column("heading")]
    [MaxLength(100)]
    public string? Heading { get; set; }

    [Column("claim_action")]
    [MaxLength(50)]
    public string? ClaimAction { get; set; }

    [Column("claim_subject")]
    [MaxLength(50)]
    public string? ClaimSubject { get; set; }

    [Column("order_index")]
    public int OrderIndex { get; set; } = 0;

    [Column("is_active")]
    public bool IsActive { get; set; } = true;

    [Column("is_view")]
    public bool IsView { get; set; } = false;

    [Column("created_by")]
    public int CreatedBy { get; set; }

    [Column("created_at")]
    public DateTime CreatedAt { get; set; }

    [Column("updated_by")]
    public int? UpdatedBy { get; set; }

    [Column("updated_at")]
    public DateTime? UpdatedAt { get; set; }

    // Navegación
    [ForeignKey("ParentId")]
    public virtual MenuItem? Parent { get; set; }

    public virtual ICollection<MenuItem> Children { get; set; } = new List<MenuItem>();
    public virtual ICollection<MenuRole> MenuRoles { get; set; } = new List<MenuRole>();
}
