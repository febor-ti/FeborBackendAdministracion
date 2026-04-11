namespace FeborBack.Infrastructure.DTOs.Configuration;

/// <summary>
/// DTO para leer la configuración de email (sin contraseña en texto plano)
/// </summary>
public class EmailConfigDto
{
    public int Id { get; set; }
    public string Host { get; set; } = string.Empty;
    public int Port { get; set; }
    public string Username { get; set; } = string.Empty;
    public bool UseSsl { get; set; }
    public bool UseTls { get; set; }
    public string FromEmail { get; set; } = string.Empty;
    public string FromName { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public DateTime? CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}

/// <summary>
/// DTO para guardar/actualizar la configuración de email (incluye contraseña)
/// </summary>
public class SaveEmailConfigDto
{
    public string Host { get; set; } = string.Empty;
    public int Port { get; set; } = 2525;
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public bool UseSsl { get; set; }
    public bool UseTls { get; set; }
    public string FromEmail { get; set; } = string.Empty;
    public string FromName { get; set; } = "Febor Cooperativa";
}

/// <summary>
/// DTO para enviar correo de prueba
/// </summary>
public class TestEmailDto
{
    public string To { get; set; } = string.Empty;
    public string Subject { get; set; } = "Prueba de Configuración SMTP";
    public string Body { get; set; } = "Este es un correo de prueba para verificar la configuración SMTP.";
}

/// <summary>
/// DTO para verificar conexión SMTP
/// </summary>
public class VerifyConnectionDto
{
    public string Host { get; set; } = string.Empty;
    public int Port { get; set; }
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public bool UseSsl { get; set; }
    public bool UseTls { get; set; }
}
