using System.ComponentModel.DataAnnotations;

namespace FeborBack.Application.DTOs.Auth;

public class RegisterUserDto
{
    [Required(ErrorMessage = "El nombre de usuario es obligatorio")]
    [EmailAddress(ErrorMessage = "Debe ser un email válido")]
    public string Username { get; set; } = string.Empty;

    [Required(ErrorMessage = "El email es obligatorio")]
    [EmailAddress(ErrorMessage = "Debe ser un email válido")]
    public string Email { get; set; } = string.Empty;

    /// <summary>
    /// Contraseña (opcional). Si no se proporciona, se genera una contraseña temporal automáticamente
    /// </summary>
    [MinLength(8, ErrorMessage = "La contraseña debe tener al menos 8 caracteres")]
    public string? Password { get; set; }

    [Required(ErrorMessage = "El nombre completo es obligatorio")]
    [MinLength(2, ErrorMessage = "El nombre debe tener al menos 2 caracteres")]
    public string FullName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Debe especificar al menos un rol")]
    [MinLength(1, ErrorMessage = "Debe seleccionar al menos un rol")]
    public List<int> RoleIds { get; set; } = new();
}