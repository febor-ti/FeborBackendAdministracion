using FeborBack.Application.DTOs.Configuration;
using FeborBack.Domain.Entities.Configuration;
using FeborBack.Infrastructure.Repositories.Configuration;
using System.Collections.Concurrent;
using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;

namespace FeborBack.Application.Services.Configuration;

public class EmailConfigService : IEmailConfigService
{
    private readonly IEmailConfigRepository _repository;
    private readonly IEncryptionService _encryptionService;

    // Rate limiting: máximo 5 correos por minuto por usuario
    private static readonly ConcurrentDictionary<int, Queue<DateTime>> _emailAttempts = new();
    private const int MaxAttemptsPerMinute = 5;
    private static readonly TimeSpan RateLimitWindow = TimeSpan.FromMinutes(1);

    public EmailConfigService(
        IEmailConfigRepository repository,
        IEncryptionService encryptionService)
    {
        _repository = repository;
        _encryptionService = encryptionService;
    }

    public async Task<EmailConfigDto?> GetConfigurationAsync()
    {
        var config = await _repository.GetActiveConfigurationAsync();

        if (config == null)
            return null;

        // El SP no retorna two_factor_enabled — lo leemos directamente
        var twoFactorEnabled = await _repository.GetTwoFactorEnabledAsync();

        return new EmailConfigDto
        {
            Id = config.Id,
            Host = config.SmtpHost,
            Port = config.SmtpPort,
            Username = config.SmtpUsername,
            // NO devolver la contraseña
            UseSsl = config.UseSsl,
            UseTls = config.UseTls,
            FromEmail = config.FromEmail,
            FromName = config.FromName,
            IsActive = config.IsActive,
            TwoFactorEnabled = twoFactorEnabled,
            CreatedAt = config.CreatedAt,
            UpdatedAt = config.UpdatedAt
        };
    }

    public async Task<EmailConfigDto> SaveConfigurationAsync(SaveEmailConfigDto dto, int userId)
    {
        // Validaciones
        if (string.IsNullOrEmpty(dto.Host))
            throw new ArgumentException("El host SMTP es requerido");

        if (dto.Port < 1 || dto.Port > 65535)
            throw new ArgumentException("El puerto debe estar entre 1 y 65535");

        if (string.IsNullOrEmpty(dto.Username))
            throw new ArgumentException("El usuario SMTP es requerido");

        if (string.IsNullOrEmpty(dto.Password))
            throw new ArgumentException("La contraseña SMTP es requerida");

        if (string.IsNullOrEmpty(dto.FromEmail) || !IsValidEmail(dto.FromEmail))
            throw new ArgumentException("El email del remitente es inválido");

        // Encriptar la contraseña
        var encryptedPassword = _encryptionService.Encrypt(dto.Password);

        var config = new EmailSettings
        {
            SmtpHost = dto.Host,
            SmtpPort = dto.Port,
            SmtpUsername = dto.Username,
            SmtpPassword = encryptedPassword,
            UseSsl = dto.UseSsl,
            UseTls = dto.UseTls,
            FromEmail = dto.FromEmail,
            FromName = dto.FromName,
            IsActive = true
        };

        var configId = await _repository.UpsertConfigurationAsync(config, userId);

        // Obtener la configuración guardada
        var savedConfig = await _repository.GetConfigurationByIdAsync(configId);

        if (savedConfig == null)
            throw new Exception("Error al guardar la configuración");

        return new EmailConfigDto
        {
            Id = savedConfig.Id,
            Host = savedConfig.SmtpHost,
            Port = savedConfig.SmtpPort,
            Username = savedConfig.SmtpUsername,
            UseSsl = savedConfig.UseSsl,
            UseTls = savedConfig.UseTls,
            FromEmail = savedConfig.FromEmail,
            FromName = savedConfig.FromName,
            IsActive = savedConfig.IsActive,
            TwoFactorEnabled = savedConfig.TwoFactorEnabled,
            CreatedAt = savedConfig.CreatedAt,
            UpdatedAt = savedConfig.UpdatedAt
        };
    }

    public async Task<bool> SendTestEmailAsync(TestEmailDto dto, int userId)
    {
        if (string.IsNullOrEmpty(dto.To) || !IsValidEmail(dto.To))
            throw new ArgumentException("El email de destino es inválido");

        // Verificar rate limit
        if (!CheckRateLimit(userId))
        {
            throw new InvalidOperationException($"Has excedido el límite de {MaxAttemptsPerMinute} correos por minuto. Por favor espera antes de intentar nuevamente.");
        }

        // Obtener configuración activa
        var config = await _repository.GetActiveConfigurationAsync();

        if (config == null)
            throw new Exception("No existe una configuración de correo activa");

        try
        {
            // Desencriptar la contraseña
            var decryptedPassword = _encryptionService.Decrypt(config.SmtpPassword);

            // Crear el mensaje
            var message = new MimeMessage();
            message.From.Add(new MailboxAddress(config.FromName, config.FromEmail));
            message.To.Add(new MailboxAddress("", dto.To));
            message.Subject = dto.Subject;

            var bodyBuilder = new BodyBuilder
            {
                HtmlBody = $@"
                    <html>
                        <body>
                            <h2>Prueba de Configuración SMTP</h2>
                            <p>{dto.Body}</p>
                            <hr>
                            <p><small>Enviado desde Febor Cooperativa</small></p>
                        </body>
                    </html>",
                TextBody = dto.Body
            };

            message.Body = bodyBuilder.ToMessageBody();

            // Enviar el correo
            using var client = new SmtpClient();

            // Configurar opciones de seguridad
            var secureSocketOptions = SecureSocketOptions.Auto;
            if (config.UseSsl)
                secureSocketOptions = SecureSocketOptions.SslOnConnect;
            else if (config.UseTls)
                secureSocketOptions = SecureSocketOptions.StartTls;
            else
                secureSocketOptions = SecureSocketOptions.None;

            await client.ConnectAsync(config.SmtpHost, config.SmtpPort, secureSocketOptions);
            await client.AuthenticateAsync(config.SmtpUsername, decryptedPassword);
            await client.SendAsync(message);
            await client.DisconnectAsync(true);

            return true;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error enviando correo de prueba: {ex.Message}");
            throw new Exception($"Error al enviar el correo: {ex.Message}", ex);
        }
    }

    public async Task<bool> VerifyConnectionAsync(VerifyConnectionDto dto)
    {
        try
        {
            using var client = new SmtpClient();

            var secureSocketOptions = SecureSocketOptions.Auto;
            if (dto.UseSsl)
                secureSocketOptions = SecureSocketOptions.SslOnConnect;
            else if (dto.UseTls)
                secureSocketOptions = SecureSocketOptions.StartTls;
            else
                secureSocketOptions = SecureSocketOptions.None;

            await client.ConnectAsync(dto.Host, dto.Port, secureSocketOptions);
            await client.AuthenticateAsync(dto.Username, dto.Password);
            await client.DisconnectAsync(true);

            return true;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error verificando conexión SMTP: {ex.Message}");
            throw new Exception($"Error al verificar la conexión: {ex.Message}", ex);
        }
    }

    private bool CheckRateLimit(int userId)
    {
        var now = DateTime.UtcNow;
        var attempts = _emailAttempts.GetOrAdd(userId, _ => new Queue<DateTime>());

        lock (attempts)
        {
            // Limpiar intentos antiguos (fuera de la ventana de 1 minuto)
            while (attempts.Count > 0 && (now - attempts.Peek()) > RateLimitWindow)
            {
                attempts.Dequeue();
            }

            // Verificar si se excedió el límite
            if (attempts.Count >= MaxAttemptsPerMinute)
            {
                return false;
            }

            // Agregar el intento actual
            attempts.Enqueue(now);
            return true;
        }
    }

    public async Task<bool> GetTwoFactorEnabledAsync()
    {
        return await _repository.GetTwoFactorEnabledAsync();
    }

    public async Task<bool> SetTwoFactorEnabledAsync(bool enabled, int userId)
    {
        return await _repository.UpdateTwoFactorEnabledAsync(enabled, userId);
    }

    private static bool IsValidEmail(string email)
    {
        try
        {
            var addr = new System.Net.Mail.MailAddress(email);
            return addr.Address == email;
        }
        catch
        {
            return false;
        }
    }
}
