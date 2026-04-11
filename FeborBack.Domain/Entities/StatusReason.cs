using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FeborBack.Domain.Entities;

[Table("status_reason", Schema = "auth")]
public class StatusReason
{
    [Key]
    [Column("status_reason_id")]
    public int StatusReasonId { get; set; }

    [Column("reason_name")]
    [MaxLength(100)]
    public string ReasonName { get; set; } = string.Empty;

    // Relación con LoginUser
    public virtual ICollection<LoginUser> LoginUsers { get; set; } = new List<LoginUser>();
}