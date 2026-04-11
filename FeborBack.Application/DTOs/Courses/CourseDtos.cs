using System.ComponentModel.DataAnnotations;

namespace FeborBack.Application.DTOs.Courses;

public class CourseDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string PublicUrl { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class CreateCourseDto
{
    [Required(ErrorMessage = "El nombre es requerido")]
    [MaxLength(200)]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Solo letras minúsculas, números y guiones. Ejemplo: "phishing", "riesgos-2024".
    /// </summary>
    [Required(ErrorMessage = "El slug es requerido")]
    [MaxLength(100)]
    [RegularExpression(@"^[a-z0-9]+(?:-[a-z0-9]+)*$",
        ErrorMessage = "El slug solo puede contener letras minúsculas, números y guiones (sin espacios)")]
    public string Slug { get; set; } = string.Empty;

    [MaxLength(500)]
    public string? Description { get; set; }
}

public class UpdateCourseDto
{
    [Required(ErrorMessage = "El nombre es requerido")]
    [MaxLength(200)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(500)]
    public string? Description { get; set; }
}
