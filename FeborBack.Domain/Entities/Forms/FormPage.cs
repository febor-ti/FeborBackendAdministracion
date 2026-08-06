using FeborBack.Domain.Common;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FeborBack.Domain.Entities.Forms;

/// <summary>
/// Página HTML publicada en la sección de formularios (/formularios/{slug}/).
/// Mismo modelo que <see cref="Courses.Course"/> pero con su propio espacio de URLs.
/// </summary>
[Table("form", Schema = "forms")]
public class FormPage : BaseEntity
{
    [Required]
    [MaxLength(200)]
    [Column("name")]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Slug URL (solo letras, números y guiones). Define la ruta: /formularios/{slug}/
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
    /// URL pública del formulario (p.ej. https://virtual.febor.co/formularios/ev-riesgo/).
    /// </summary>
    [Required]
    [MaxLength(500)]
    [Column("public_url")]
    public string PublicUrl { get; set; } = string.Empty;
}
