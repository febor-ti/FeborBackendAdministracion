using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FeborBack.Domain.Entities.Catalog;

[Table("cities", Schema = "catalog")]
public class City
{
    [Key]
    [Column("id")]
    public int Id { get; set; }

    [Column("codigo_divipola")]
    [MaxLength(10)]
    public string? CodigoDivipola { get; set; }

    [Column("nombre")]
    [MaxLength(100)]
    [Required]
    public string Nombre { get; set; } = string.Empty;

    [Column("departamento")]
    [MaxLength(100)]
    [Required]
    public string Departamento { get; set; } = string.Empty;

    [Column("fecha_fundacion")]
    public DateTime? FechaFundacion { get; set; }

    [Column("created_at")]
    public DateTime CreatedAt { get; set; }

    [Column("updated_at")]
    public DateTime UpdatedAt { get; set; }
}