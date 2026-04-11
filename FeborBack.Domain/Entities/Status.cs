using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FeborBack.Domain.Entities;

[Table("status", Schema = "auth")]
public class Status
{
    [Key]
    [Column("status_id")]
    public int StatusId { get; set; }

    [Column("status_name")]
    [MaxLength(50)]
    public string StatusName { get; set; } = string.Empty;

    // Relación con LoginUser
    public virtual ICollection<LoginUser> LoginUsers { get; set; } = new List<LoginUser>();
}