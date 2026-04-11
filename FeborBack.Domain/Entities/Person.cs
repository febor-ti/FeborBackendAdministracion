using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FeborBack.Domain.Entities;

[Table("person", Schema = "auth")]
public class Person
{
    [Key]
    [Column("person_id")]
    public int PersonId { get; set; }

    [Column("full_name")]
    [MaxLength(255)]
    public string FullName { get; set; } = string.Empty;

    // Relación con LoginUser
    public virtual ICollection<LoginUser> LoginUsers { get; set; } = new List<LoginUser>();
}