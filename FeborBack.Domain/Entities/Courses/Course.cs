using FeborBack.Domain.Common;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FeborBack.Domain.Entities.Courses;

[Table("course", Schema = "courses")]
public class Course : BaseEntity
{
    [Required]
    [MaxLength(200)]
    [Column("name")]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Slug URL (solo letras, números y guiones). Define la ruta: /cursos/{slug}/
    /// </summary>
    [Required]
    [MaxLength(100)]
    [Column("slug")]
    public string Slug { get; set; } = string.Empty;

    [MaxLength(500)]
    [Column("description")]
    public string? Description { get; set; }

    /// <summary>
    /// Ruta absoluta del archivo index.html en el servidor.
    /// </summary>
    [Required]
    [MaxLength(500)]
    [Column("file_path")]
    public string FilePath { get; set; } = string.Empty;

    /// <summary>
    /// URL pública del curso (p.ej. https://virtual.febor.co/cursos/phishing/).
    /// </summary>
    [Required]
    [MaxLength(500)]
    [Column("public_url")]
    public string PublicUrl { get; set; } = string.Empty;
}
