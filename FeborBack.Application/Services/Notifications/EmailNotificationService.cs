using FeborBack.Infrastructure.Repositories;
using FeborBack.Infrastructure.Repositories.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;
using FeborBack.Application.Services.Configuration;

namespace FeborBack.Application.Services.Notifications;

public class EmailNotificationService : IEmailNotificationService
{
    private readonly IRoleRepository _roleRepository;
    private readonly IEmailConfigRepository _emailConfigRepository;
    private readonly IEncryptionService _encryptionService;
    private readonly IConfiguration _configuration;
    private readonly ILogger<EmailNotificationService> _logger;

    // Role ID del Administrador que recibirá notificaciones
    private const int ADMIN_ROLE_ID = 3;

    public EmailNotificationService(
        IRoleRepository roleRepository,
        IEmailConfigRepository emailConfigRepository,
        IEncryptionService encryptionService,
        IConfiguration configuration,
        ILogger<EmailNotificationService> logger)
    {
        _roleRepository = roleRepository;
        _emailConfigRepository = emailConfigRepository;
        _encryptionService = encryptionService;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task NotifyAdminsNewCDATRequestAsync(int contactRequestId, string nombre, string apellidos, string email)
    {
        try
        {
            // Obtener configuración de email
            var emailConfig = await _emailConfigRepository.GetActiveConfigurationAsync();
            if (emailConfig == null)
            {
                _logger.LogWarning("No se pudo enviar notificación: No hay configuración de email activa");
                return;
            }

            // Obtener usuarios con rol Administrador (role_id = 3)
            var admins = await _roleRepository.GetUsersByRoleIdAsync(ADMIN_ROLE_ID);
            if (!admins.Any())
            {
                _logger.LogWarning("No se encontraron administradores para notificar");
                return;
            }

            // Desencriptar la contraseña SMTP
            var decryptedPassword = _encryptionService.Decrypt(emailConfig.SmtpPassword);

            // Enviar correo a cada administrador
            foreach (var admin in admins)
            {
                if (string.IsNullOrEmpty(admin.email))
                {
                    _logger.LogWarning("El administrador {AdminName} no tiene email configurado", admin.full_name);
                    continue;
                }

                try
                {
                    await SendEmailAsync(
                        emailConfig,
                        decryptedPassword,
                        admin.email,
                        admin.full_name,
                        contactRequestId,
                        nombre,
                        apellidos,
                        email
                    );

                    _logger.LogInformation(
                        "Notificación enviada a {AdminEmail} sobre solicitud CDAT #{RequestId}",
                        admin.email,
                        contactRequestId
                    );
                }
                catch (Exception ex)
                {
                    _logger.LogError(
                        ex,
                        "Error enviando notificación a {AdminEmail} sobre solicitud CDAT #{RequestId}",
                        admin.email,
                        contactRequestId
                    );
                    // Continuar con el siguiente administrador aunque falle uno
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al enviar notificaciones de nueva solicitud CDAT #{RequestId}", contactRequestId);
            // No lanzar la excepción para que no afecte la creación de la solicitud
        }
    }

    public async Task NotifyAdminsNewCreditRequestAsync(int contactRequestId, string nombre, string apellidos, string email)
    {
        try
        {
            // Obtener configuración de email
            var emailConfig = await _emailConfigRepository.GetActiveConfigurationAsync();
            if (emailConfig == null)
            {
                _logger.LogWarning("No se pudo enviar notificación: No hay configuración de email activa");
                return;
            }

            // Obtener usuarios con rol Administrador (role_id = 3)
            var admins = await _roleRepository.GetUsersByRoleIdAsync(ADMIN_ROLE_ID);
            if (!admins.Any())
            {
                _logger.LogWarning("No se encontraron administradores para notificar");
                return;
            }

            // Desencriptar la contraseña SMTP
            var decryptedPassword = _encryptionService.Decrypt(emailConfig.SmtpPassword);

            // Enviar correo a cada administrador
            foreach (var admin in admins)
            {
                if (string.IsNullOrEmpty(admin.email))
                {
                    _logger.LogWarning("El administrador {AdminName} no tiene email configurado", admin.full_name);
                    continue;
                }

                try
                {
                    await SendCreditEmailAsync(
                        emailConfig,
                        decryptedPassword,
                        admin.email,
                        admin.full_name,
                        contactRequestId,
                        nombre,
                        apellidos,
                        email
                    );

                    _logger.LogInformation(
                        "Notificación enviada a {AdminEmail} sobre solicitud de Crédito #{RequestId}",
                        admin.email,
                        contactRequestId
                    );
                }
                catch (Exception ex)
                {
                    _logger.LogError(
                        ex,
                        "Error enviando notificación a {AdminEmail} sobre solicitud de Crédito #{RequestId}",
                        admin.email,
                        contactRequestId
                    );
                    // Continuar con el siguiente administrador aunque falle uno
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al enviar notificaciones de nueva solicitud de Crédito #{RequestId}", contactRequestId);
            // No lanzar la excepción para que no afecte la creación de la solicitud
        }
    }

    private async Task SendEmailAsync(
        Domain.Entities.Configuration.EmailSettings config,
        string decryptedPassword,
        string toEmail,
        string toName,
        int contactRequestId,
        string nombre,
        string apellidos,
        string clientEmail)
    {
        // Obtener la URL del frontend desde la configuración
        var frontendBaseUrl = _configuration["Frontend:BaseUrl"] ?? "http://localhost:5173";
        var loginUrl = $"{frontendBaseUrl}/login";

        var message = new MimeMessage();
        message.From.Add(new MailboxAddress(config.FromName, config.FromEmail));
        message.To.Add(new MailboxAddress(toName, toEmail));
        message.Subject = $"Nueva Solicitud CDAT #{contactRequestId} - Requiere Asignación";

        var bodyBuilder = new BodyBuilder
        {
            HtmlBody = $@"
                <!DOCTYPE html>
                <html>
                <head>
                    <style>
                        body {{ font-family: Arial, sans-serif; line-height: 1.6; color: #333; }}
                        .container {{ max-width: 600px; margin: 0 auto; padding: 20px; }}
                        .header {{ background-color: #50AB51; color: white; padding: 20px; text-align: center; border-radius: 5px 5px 0 0; }}
                        .content {{ background-color: #f9f9f9; padding: 20px; border: 1px solid #ddd; }}
                        .info-item {{ margin: 10px 0; }}
                        .label {{ font-weight: bold; color: #50AB51; }}
                        .footer {{ background-color: #FFDF00; padding: 15px; text-align: center; font-size: 12px; color: #333; border-radius: 0 0 5px 5px; }}
                        .button {{
                            display: inline-block;
                            padding: 12px 24px;
                            margin: 20px 0;
                            background-color: #000000;
                            color: white !important;
                            text-decoration: none;
                            border-radius: 5px;
                            font-weight: bold;
                        }}
                    </style>
                </head>
                <body>
                    <div class=""container"">
                        <div class=""header"">
                            <h2>🆕 Nueva Solicitud del Simulador CDAT Online</h2>
                        </div>
                        <div class=""content"">
                            <p>Hola <strong>{toName}</strong>,</p>

                            <p>Se ha recibido una nueva solicitud de contacto del <strong>Simulador CDAT Online</strong> que requiere asignación.</p>

                            <div class=""info-item"">
                                <span class=""label"">ID de Solicitud:</span> #{contactRequestId}
                            </div>
                            <div class=""info-item"">
                                <span class=""label"">Nombre:</span> {nombre} {apellidos}
                            </div>
                            <div class=""info-item"">
                                <span class=""label"">Email:</span> {clientEmail}
                            </div>

                            <p style=""margin-top: 20px;"">Por favor, ingresa al sistema para revisar los detalles completos y asignar esta solicitud a un asesor.</p>

                            <div style=""text-align: center;"">
                                <a href=""{loginUrl}"" class=""button"">Ingresar al Sistema</a>
                            </div>
                        </div>
                        <div class=""footer"">
                            <p>Este es un mensaje automático del sistema Febor Cooperativa.</p>
                            <p>Por favor, no responder a este correo.</p>
                        </div>
                    </div>
                </body>
                </html>",
            TextBody = $@"Nueva Solicitud del Simulador CDAT Online

Hola {toName},

Se ha recibido una nueva solicitud de contacto del Simulador CDAT Online que requiere asignación.

ID de Solicitud: #{contactRequestId}
Nombre: {nombre} {apellidos}
Email: {clientEmail}

Por favor, ingresa al sistema para revisar los detalles completos y asignar esta solicitud a un asesor.

---
Este es un mensaje automático del sistema Febor Cooperativa.
Por favor, no responder a este correo."
        };

        message.Body = bodyBuilder.ToMessageBody();

        using var client = new SmtpClient();

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
    }

    private async Task SendCreditEmailAsync(
        Domain.Entities.Configuration.EmailSettings config,
        string decryptedPassword,
        string toEmail,
        string toName,
        int contactRequestId,
        string nombre,
        string apellidos,
        string clientEmail)
    {
        // Obtener la URL del frontend desde la configuración
        var frontendBaseUrl = _configuration["Frontend:BaseUrl"] ?? "http://localhost:5173";
        var loginUrl = $"{frontendBaseUrl}/login";

        var message = new MimeMessage();
        message.From.Add(new MailboxAddress(config.FromName, config.FromEmail));
        message.To.Add(new MailboxAddress(toName, toEmail));
        message.Subject = $"Nueva Solicitud de Crédito #{contactRequestId} - Requiere Asignación";

        var bodyBuilder = new BodyBuilder
        {
            HtmlBody = $@"
                <!DOCTYPE html>
                <html>
                <head>
                    <style>
                        body {{ font-family: Arial, sans-serif; line-height: 1.6; color: #333; }}
                        .container {{ max-width: 600px; margin: 0 auto; padding: 20px; }}
                        .header {{ background-color: #50AB51; color: white; padding: 20px; text-align: center; border-radius: 5px 5px 0 0; }}
                        .content {{ background-color: #f9f9f9; padding: 20px; border: 1px solid #ddd; }}
                        .info-item {{ margin: 10px 0; }}
                        .label {{ font-weight: bold; color: #50AB51; }}
                        .footer {{ background-color: #FFDF00; padding: 15px; text-align: center; font-size: 12px; color: #333; border-radius: 0 0 5px 5px; }}
                        .button {{
                            display: inline-block;
                            padding: 12px 24px;
                            margin: 20px 0;
                            background-color: #000000;
                            color: white !important;
                            text-decoration: none;
                            border-radius: 5px;
                            font-weight: bold;
                        }}
                    </style>
                </head>
                <body>
                    <div class=""container"">
                        <div class=""header"">
                            <h2>🆕 Nueva Solicitud del Simulador de Crédito Online</h2>
                        </div>
                        <div class=""content"">
                            <p>Hola <strong>{toName}</strong>,</p>

                            <p>Se ha recibido una nueva solicitud de contacto del <strong>Simulador de Crédito Online</strong> que requiere asignación.</p>

                            <div class=""info-item"">
                                <span class=""label"">ID de Solicitud:</span> #{contactRequestId}
                            </div>
                            <div class=""info-item"">
                                <span class=""label"">Nombre:</span> {nombre} {apellidos}
                            </div>
                            <div class=""info-item"">
                                <span class=""label"">Email:</span> {clientEmail}
                            </div>

                            <p style=""margin-top: 20px;"">Por favor, ingresa al sistema para revisar los detalles completos y asignar esta solicitud a un asesor.</p>

                            <div style=""text-align: center;"">
                                <a href=""{loginUrl}"" class=""button"">Ingresar al Sistema</a>
                            </div>
                        </div>
                        <div class=""footer"">
                            <p>Este es un mensaje automático del sistema Febor Cooperativa.</p>
                            <p>Por favor, no responder a este correo.</p>
                        </div>
                    </div>
                </body>
                </html>",
            TextBody = $@"Nueva Solicitud del Simulador de Crédito Online

Hola {toName},

Se ha recibido una nueva solicitud de contacto del Simulador de Crédito Online que requiere asignación.

ID de Solicitud: #{contactRequestId}
Nombre: {nombre} {apellidos}
Email: {clientEmail}

Por favor, ingresa al sistema para revisar los detalles completos y asignar esta solicitud a un asesor.

---
Este es un mensaje automático del sistema Febor Cooperativa.
Por favor, no responder a este correo."
        };

        message.Body = bodyBuilder.ToMessageBody();

        using var client = new SmtpClient();

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
    }

    public async Task NotifyAdvisorCdatAssignmentAsync(
        int assignmentId,
        int cdatRequestId,
        string clientNombre,
        string clientApellidos,
        string clientEmail,
        string clientTelefono,
        string clientCiudad,
        string advisorEmail,
        string advisorName,
        string assignedByName,
        string? notes)
    {
        try
        {
            // Obtener configuración de email
            var emailConfig = await _emailConfigRepository.GetActiveConfigurationAsync();
            if (emailConfig == null)
            {
                _logger.LogWarning("No se pudo enviar notificación: No hay configuración de email activa");
                return;
            }

            if (string.IsNullOrEmpty(advisorEmail))
            {
                _logger.LogWarning("El asesor {AdvisorName} no tiene email configurado", advisorName);
                return;
            }

            // Desencriptar la contraseña SMTP
            var decryptedPassword = _encryptionService.Decrypt(emailConfig.SmtpPassword);

            await SendAdvisorAssignmentEmailAsync(
                emailConfig,
                decryptedPassword,
                advisorEmail,
                advisorName,
                assignmentId,
                cdatRequestId,
                clientNombre,
                clientApellidos,
                clientEmail,
                clientTelefono,
                clientCiudad,
                assignedByName,
                notes
            );

            _logger.LogInformation(
                "Notificación de asignación enviada a asesor {AdvisorEmail} sobre solicitud CDAT #{CdatRequestId}",
                advisorEmail,
                cdatRequestId
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Error al enviar notificación de asignación al asesor sobre solicitud CDAT #{CdatRequestId}",
                cdatRequestId
            );
            // No lanzar la excepción para que no afecte la asignación
        }
    }

    private async Task SendAdvisorAssignmentEmailAsync(
        Domain.Entities.Configuration.EmailSettings config,
        string decryptedPassword,
        string advisorEmail,
        string advisorName,
        int assignmentId,
        int cdatRequestId,
        string clientNombre,
        string clientApellidos,
        string clientEmail,
        string clientTelefono,
        string clientCiudad,
        string assignedByName,
        string? notes)
    {
        // Obtener la URL del frontend desde la configuración
        var frontendBaseUrl = _configuration["Frontend:BaseUrl"] ?? "http://localhost:5173";
        var loginUrl = $"{frontendBaseUrl}/login";

        var message = new MimeMessage();
        message.From.Add(new MailboxAddress(config.FromName, config.FromEmail));
        message.To.Add(new MailboxAddress(advisorName, advisorEmail));
        message.Subject = $"Nueva Asignación CDAT #{cdatRequestId} - Cliente: {clientNombre} {clientApellidos}";

        var bodyBuilder = new BodyBuilder
        {
            HtmlBody = $@"
                <!DOCTYPE html>
                <html>
                <head>
                    <style>
                        body {{ font-family: Arial, sans-serif; line-height: 1.6; color: #333; }}
                        .container {{ max-width: 600px; margin: 0 auto; padding: 20px; }}
                        .header {{ background-color: #50AB51; color: white; padding: 20px; text-align: center; border-radius: 5px 5px 0 0; }}
                        .content {{ background-color: #f9f9f9; padding: 20px; border: 1px solid #ddd; }}
                        .info-item {{ margin: 15px 0; }}
                        .label {{ font-weight: bold; color: #50AB51; }}
                        .client-info {{ background-color: white; padding: 15px; border-radius: 5px; margin: 15px 0; border-left: 4px solid #FFDF00; }}
                        .footer {{ background-color: #FFDF00; padding: 15px; text-align: center; font-size: 12px; color: #333; border-radius: 0 0 5px 5px; }}
                        .button {{
                            display: inline-block;
                            padding: 12px 24px;
                            margin: 20px 0;
                            background-color: #000000;
                            color: white !important;
                            text-decoration: none;
                            border-radius: 5px;
                            font-weight: bold;
                        }}
                        .highlight {{ color: #50AB51; font-weight: bold; }}
                    </style>
                </head>
                <body>
                    <div class=""container"">
                        <div class=""header"">
                            <h2>📋 Nueva Solicitud CDAT Asignada</h2>
                        </div>
                        <div class=""content"">
                            <p>Hola <strong>{advisorName}</strong>,</p>

                            <p>Se te ha asignado una nueva solicitud del <strong>Simulador CDAT Online</strong>.</p>

                            <div class=""info-item"">
                                <span class=""label"">ID de Asignación:</span> <span class=""highlight"">#{assignmentId}</span>
                            </div>
                            <div class=""info-item"">
                                <span class=""label"">ID de Solicitud CDAT:</span> <span class=""highlight"">#{cdatRequestId}</span>
                            </div>
                            <div class=""info-item"">
                                <span class=""label"">Asignado por:</span> {assignedByName}
                            </div>

                            <div class=""client-info"">
                                <h3 style=""margin-top: 0; color: #50AB51;"">📞 Información del Cliente</h3>
                                <div class=""info-item"">
                                    <span class=""label"">Nombre:</span> {clientNombre} {clientApellidos}
                                </div>
                                <div class=""info-item"">
                                    <span class=""label"">Email:</span> <a href=""mailto:{clientEmail}"">{clientEmail}</a>
                                </div>
                                <div class=""info-item"">
                                    <span class=""label"">Teléfono:</span> <a href=""tel:{clientTelefono}"">{clientTelefono}</a>
                                </div>
                                <div class=""info-item"">
                                    <span class=""label"">Ciudad:</span> {clientCiudad}
                                </div>
                            </div>

                            <p style=""margin-top: 20px;"">Por favor, contacta al cliente lo antes posible para brindarle la asesoría solicitada.</p>

                            <div style=""text-align: center;"">
                                <a href=""{loginUrl}"" class=""button"">Ingresar al Sistema</a>
                            </div>
                        </div>
                        <div class=""footer"">
                            <p>Este es un mensaje automático del sistema Febor Cooperativa.</p>
                            <p>Por favor, no responder a este correo.</p>
                        </div>
                    </div>
                </body>
                </html>",
            TextBody = $@"Nueva Solicitud CDAT Asignada

Hola {advisorName},

Se te ha asignado una nueva solicitud del Simulador CDAT Online.

ID de Asignación: #{assignmentId}
ID de Solicitud CDAT: #{cdatRequestId}
Asignado por: {assignedByName}

--- Información del Cliente ---
Nombre: {clientNombre} {clientApellidos}
Email: {clientEmail}
Teléfono: {clientTelefono}
Ciudad: {clientCiudad}

Por favor, contacta al cliente lo antes posible para brindarle la asesoría solicitada.

Ingresa al sistema: {loginUrl}

---
Este es un mensaje automático del sistema Febor Cooperativa.
Por favor, no responder a este correo."
        };

        message.Body = bodyBuilder.ToMessageBody();

        using var client = new SmtpClient();

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
    }

    public async Task NotifyAdvisorCreditAssignmentAsync(
        int assignmentId,
        int creditRequestId,
        string clientNombre,
        string clientApellidos,
        string clientEmail,
        string clientTelefono,
        string clientCiudad,
        string advisorEmail,
        string advisorName,
        string assignedByName,
        string? notes)
    {
        try
        {
            // Obtener configuración de email
            var emailConfig = await _emailConfigRepository.GetActiveConfigurationAsync();
            if (emailConfig == null)
            {
                _logger.LogWarning("No se pudo enviar notificación: No hay configuración de email activa");
                return;
            }

            if (string.IsNullOrEmpty(advisorEmail))
            {
                _logger.LogWarning("El asesor {AdvisorName} no tiene email configurado", advisorName);
                return;
            }

            // Desencriptar la contraseña SMTP
            var decryptedPassword = _encryptionService.Decrypt(emailConfig.SmtpPassword);

            await SendAdvisorCreditAssignmentEmailAsync(
                emailConfig,
                decryptedPassword,
                advisorEmail,
                advisorName,
                assignmentId,
                creditRequestId,
                clientNombre,
                clientApellidos,
                clientEmail,
                clientTelefono,
                clientCiudad,
                assignedByName,
                notes
            );

            _logger.LogInformation(
                "Notificación de asignación enviada a asesor {AdvisorEmail} sobre solicitud de Crédito #{CreditRequestId}",
                advisorEmail,
                creditRequestId
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Error al enviar notificación de asignación al asesor sobre solicitud de Crédito #{CreditRequestId}",
                creditRequestId
            );
            // No lanzar la excepción para que no afecte la asignación
        }
    }

    private async Task SendAdvisorCreditAssignmentEmailAsync(
        Domain.Entities.Configuration.EmailSettings config,
        string decryptedPassword,
        string advisorEmail,
        string advisorName,
        int assignmentId,
        int creditRequestId,
        string clientNombre,
        string clientApellidos,
        string clientEmail,
        string clientTelefono,
        string clientCiudad,
        string assignedByName,
        string? notes)
    {
        // Obtener la URL del frontend desde la configuración
        var frontendBaseUrl = _configuration["Frontend:BaseUrl"] ?? "http://localhost:5173";
        var loginUrl = $"{frontendBaseUrl}/login";

        var message = new MimeMessage();
        message.From.Add(new MailboxAddress(config.FromName, config.FromEmail));
        message.To.Add(new MailboxAddress(advisorName, advisorEmail));
        message.Subject = $"Nueva Asignación de Crédito #{creditRequestId} - Cliente: {clientNombre} {clientApellidos}";

        var bodyBuilder = new BodyBuilder
        {
            HtmlBody = $@"
                <!DOCTYPE html>
                <html>
                <head>
                    <style>
                        body {{ font-family: Arial, sans-serif; line-height: 1.6; color: #333; }}
                        .container {{ max-width: 600px; margin: 0 auto; padding: 20px; }}
                        .header {{ background-color: #50AB51; color: white; padding: 20px; text-align: center; border-radius: 5px 5px 0 0; }}
                        .content {{ background-color: #f9f9f9; padding: 20px; border: 1px solid #ddd; }}
                        .info-item {{ margin: 15px 0; }}
                        .label {{ font-weight: bold; color: #50AB51; }}
                        .client-info {{ background-color: white; padding: 15px; border-radius: 5px; margin: 15px 0; border-left: 4px solid #FFDF00; }}
                        .footer {{ background-color: #FFDF00; padding: 15px; text-align: center; font-size: 12px; color: #333; border-radius: 0 0 5px 5px; }}
                        .button {{
                            display: inline-block;
                            padding: 12px 24px;
                            margin: 20px 0;
                            background-color: #000000;
                            color: white !important;
                            text-decoration: none;
                            border-radius: 5px;
                            font-weight: bold;
                        }}
                        .highlight {{ color: #50AB51; font-weight: bold; }}
                    </style>
                </head>
                <body>
                    <div class=""container"">
                        <div class=""header"">
                            <h2>📋 Nueva Solicitud de Crédito Asignada</h2>
                        </div>
                        <div class=""content"">
                            <p>Hola <strong>{advisorName}</strong>,</p>

                            <p>Se te ha asignado una nueva solicitud del <strong>Simulador de Crédito Online</strong>.</p>

                            <div class=""info-item"">
                                <span class=""label"">ID de Asignación:</span> <span class=""highlight"">#{assignmentId}</span>
                            </div>
                            <div class=""info-item"">
                                <span class=""label"">ID de Solicitud de Crédito:</span> <span class=""highlight"">#{creditRequestId}</span>
                            </div>
                            <div class=""info-item"">
                                <span class=""label"">Asignado por:</span> {assignedByName}
                            </div>

                            <div class=""client-info"">
                                <h3 style=""margin-top: 0; color: #50AB51;"">📞 Información del Cliente</h3>
                                <div class=""info-item"">
                                    <span class=""label"">Nombre:</span> {clientNombre} {clientApellidos}
                                </div>
                                <div class=""info-item"">
                                    <span class=""label"">Email:</span> <a href=""mailto:{clientEmail}"">{clientEmail}</a>
                                </div>
                                <div class=""info-item"">
                                    <span class=""label"">Teléfono:</span> <a href=""tel:{clientTelefono}"">{clientTelefono}</a>
                                </div>
                                <div class=""info-item"">
                                    <span class=""label"">Ciudad:</span> {clientCiudad}
                                </div>
                            </div>

                            <p style=""margin-top: 20px;"">Por favor, contacta al cliente lo antes posible para brindarle la asesoría solicitada.</p>

                            <div style=""text-align: center;"">
                                <a href=""{loginUrl}"" class=""button"">Ingresar al Sistema</a>
                            </div>
                        </div>
                        <div class=""footer"">
                            <p>Este es un mensaje automático del sistema Febor Cooperativa.</p>
                            <p>Por favor, no responder a este correo.</p>
                        </div>
                    </div>
                </body>
                </html>",
            TextBody = $@"Nueva Solicitud de Crédito Asignada

Hola {advisorName},

Se te ha asignado una nueva solicitud del Simulador de Crédito Online.

ID de Asignación: #{assignmentId}
ID de Solicitud de Crédito: #{creditRequestId}
Asignado por: {assignedByName}

--- Información del Cliente ---
Nombre: {clientNombre} {clientApellidos}
Email: {clientEmail}
Teléfono: {clientTelefono}
Ciudad: {clientCiudad}

Por favor, contacta al cliente lo antes posible para brindarle la asesoría solicitada.

Ingresa al sistema: {loginUrl}

---
Este es un mensaje automático del sistema Febor Cooperativa.
Por favor, no responder a este correo."
        };

        message.Body = bodyBuilder.ToMessageBody();

        using var client = new SmtpClient();

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
    }

    public async Task SendWelcomeEmailWithTemporaryPasswordAsync(
        string userEmail,
        string fullName,
        string temporaryPassword)
    {
        try
        {
            // Obtener configuración de email
            var emailConfig = await _emailConfigRepository.GetActiveConfigurationAsync();
            if (emailConfig == null)
            {
                _logger.LogWarning("No se pudo enviar correo de bienvenida: No hay configuración de email activa");
                return;
            }

            // Desencriptar la contraseña SMTP
            var decryptedPassword = _encryptionService.Decrypt(emailConfig.SmtpPassword);

            // Obtener la URL del frontend desde la configuración
            var frontendBaseUrl = _configuration["Frontend:BaseUrl"] ?? "http://localhost:5173";
            var loginUrl = $"{frontendBaseUrl}/login";

            var message = new MimeMessage();
            message.From.Add(new MailboxAddress(emailConfig.FromName, emailConfig.FromEmail));
            message.To.Add(new MailboxAddress(fullName, userEmail));
            message.Subject = "Bienvenido a Febor - Configura tu Contraseña";

            var bodyBuilder = new BodyBuilder
            {
                HtmlBody = $@"
                <!DOCTYPE html>
                <html>
                <head>
                    <meta charset=""utf-8"">
                    <meta name=""viewport"" content=""width=device-width, initial-scale=1.0"">
                    <style>
                        body {{ font-family: Arial, sans-serif; line-height: 1.6; color: #333; margin: 0; padding: 0; }}
                        .container {{ max-width: 600px; margin: 0 auto; padding: 20px; }}
                        .header {{ background-color: #50AB51; color: white; padding: 30px 20px; text-align: center; border-radius: 5px 5px 0 0; }}
                        .header h1 {{ margin: 0; font-size: 24px; }}
                        .content {{ background-color: #f9f9f9; padding: 30px 20px; border: 1px solid #ddd; border-top: none; }}
                        .welcome-text {{ font-size: 16px; margin-bottom: 20px; }}
                        .credentials-box {{
                            background-color: white;
                            border: 2px solid #50AB51;
                            border-radius: 5px;
                            padding: 20px;
                            margin: 20px 0;
                        }}
                        .credential-item {{
                            margin: 15px 0;
                            padding: 10px;
                            background-color: #f5f5f5;
                            border-radius: 3px;
                        }}
                        .credential-label {{
                            font-weight: bold;
                            color: #50AB51;
                            font-size: 14px;
                            display: block;
                            margin-bottom: 5px;
                        }}
                        .credential-value {{
                            font-family: 'Courier New', monospace;
                            font-size: 16px;
                            color: #000;
                            font-weight: bold;
                        }}
                        .important-box {{
                            background-color: #fff3cd;
                            border-left: 4px solid #ffc107;
                            padding: 15px;
                            margin: 20px 0;
                        }}
                        .important-box h3 {{
                            margin-top: 0;
                            color: #856404;
                            font-size: 16px;
                        }}
                        .important-box ul {{
                            margin: 10px 0;
                            padding-left: 20px;
                        }}
                        .important-box li {{
                            margin: 8px 0;
                            color: #856404;
                        }}
                        .button-container {{
                            text-align: center;
                            margin: 30px 0;
                        }}
                        .button {{
                            display: inline-block;
                            padding: 15px 40px;
                            background-color: #000000;
                            color: white !important;
                            text-decoration: none;
                            border-radius: 5px;
                            font-weight: bold;
                            font-size: 16px;
                        }}
                        .footer {{
                            background-color: #FFDF00;
                            padding: 20px;
                            text-align: center;
                            font-size: 12px;
                            color: #333;
                            border-radius: 0 0 5px 5px;
                        }}
                        .footer p {{ margin: 5px 0; }}
                    </style>
                </head>
                <body>
                    <div class=""container"">
                        <div class=""header"">
                            <h1>¡Bienvenido al Sistema Febor!</h1>
                        </div>
                        <div class=""content"">
                            <p class=""welcome-text"">Hola <strong>{fullName}</strong>,</p>

                            <p>Se ha creado una cuenta para ti en el sistema Febor Cooperativa. A continuación, encontrarás tus credenciales de acceso:</p>

                            <div class=""credentials-box"">
                                <div class=""credential-item"">
                                    <span class=""credential-label"">📧 Usuario:</span>
                                    <span class=""credential-value"">{userEmail}</span>
                                </div>
                                <div class=""credential-item"">
                                    <span class=""credential-label"">🔑 Contraseña Temporal:</span>
                                    <span class=""credential-value"">{temporaryPassword}</span>
                                </div>
                            </div>

                            <div class=""important-box"">
                                <h3>⚠️ Importante:</h3>
                                <ul>
                                    <li>Esta contraseña es <strong>temporal</strong> y debe ser cambiada en tu primer inicio de sesión</li>
                                    <li>No podrás acceder al sistema hasta que cambies tu contraseña</li>
                                    <li>Asegúrate de elegir una contraseña segura que cumpla con los requisitos del sistema</li>
                                    <li>Por seguridad, no compartas esta contraseña con nadie</li>
                                </ul>
                            </div>

                            <div class=""button-container"">
                                <a href=""{loginUrl}"" class=""button"">👉 Iniciar Sesión</a>
                            </div>

                            <p style=""margin-top: 30px; font-size: 14px; color: #666;"">
                                Si tienes problemas para acceder al sistema o necesitas ayuda, por favor contacta al administrador.
                            </p>
                        </div>
                        <div class=""footer"">
                            <p><strong>Febor Cooperativa</strong></p>
                            <p>Este es un mensaje automático del sistema.</p>
                            <p>Por favor, no responder a este correo.</p>
                        </div>
                    </div>
                </body>
                </html>",
                TextBody = $@"¡Bienvenido al Sistema Febor!

Hola {fullName},

Se ha creado una cuenta para ti en el sistema Febor Cooperativa.

CREDENCIALES DE ACCESO:
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
📧 Usuario: {userEmail}
🔑 Contraseña Temporal: {temporaryPassword}
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

⚠️ IMPORTANTE:
• Esta contraseña es TEMPORAL y debe ser cambiada en tu primer inicio de sesión
• No podrás acceder al sistema hasta que cambies tu contraseña
• Asegúrate de elegir una contraseña segura
• Por seguridad, no compartas esta contraseña con nadie

👉 Iniciar sesión: {loginUrl}

Si tienes problemas para acceder al sistema, contacta al administrador.

---
Febor Cooperativa
Este es un mensaje automático del sistema.
Por favor, no responder a este correo."
            };

            message.Body = bodyBuilder.ToMessageBody();

            using var client = new SmtpClient();

            var secureSocketOptions = SecureSocketOptions.Auto;
            if (emailConfig.UseSsl)
                secureSocketOptions = SecureSocketOptions.SslOnConnect;
            else if (emailConfig.UseTls)
                secureSocketOptions = SecureSocketOptions.StartTls;
            else
                secureSocketOptions = SecureSocketOptions.None;

            await client.ConnectAsync(emailConfig.SmtpHost, emailConfig.SmtpPort, secureSocketOptions);
            await client.AuthenticateAsync(emailConfig.SmtpUsername, decryptedPassword);
            await client.SendAsync(message);
            await client.DisconnectAsync(true);

            _logger.LogInformation("Correo de bienvenida enviado exitosamente a {Email}", userEmail);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al enviar correo de bienvenida a {Email}", userEmail);
            // No lanzar la excepción para que no afecte el registro del usuario
        }
    }

    public async Task SendPasswordResetEmailAsync(
        string userEmail,
        string fullName,
        string temporaryPassword)
    {
        try
        {
            // Obtener configuración de email
            var emailConfig = await _emailConfigRepository.GetActiveConfigurationAsync();
            if (emailConfig == null)
            {
                _logger.LogWarning("No se pudo enviar correo de reseteo: No hay configuración de email activa");
                return;
            }

            // Desencriptar la contraseña SMTP
            var decryptedPassword = _encryptionService.Decrypt(emailConfig.SmtpPassword);

            // Obtener la URL del frontend desde la configuración
            var frontendBaseUrl = _configuration["Frontend:BaseUrl"] ?? "http://localhost:5173";
            var loginUrl = $"{frontendBaseUrl}/login";

            var message = new MimeMessage();
            message.From.Add(new MailboxAddress(emailConfig.FromName, emailConfig.FromEmail));
            message.To.Add(new MailboxAddress(fullName, userEmail));
            message.Subject = "Tu contraseña ha sido reseteada - Febor";

            var bodyBuilder = new BodyBuilder
            {
                HtmlBody = $@"
                <!DOCTYPE html>
                <html>
                <head>
                    <meta charset=""utf-8"">
                    <meta name=""viewport"" content=""width=device-width, initial-scale=1.0"">
                    <style>
                        body {{ font-family: Arial, sans-serif; line-height: 1.6; color: #333; margin: 0; padding: 0; }}
                        .container {{ max-width: 600px; margin: 0 auto; padding: 20px; }}
                        .header {{ background-color: #50AB51; color: white; padding: 30px 20px; text-align: center; border-radius: 5px 5px 0 0; }}
                        .header h1 {{ margin: 0; font-size: 24px; }}
                        .content {{ background-color: #f9f9f9; padding: 30px 20px; border: 1px solid #ddd; border-top: none; }}
                        .welcome-text {{ font-size: 16px; margin-bottom: 20px; }}
                        .credentials-box {{
                            background-color: white;
                            border: 2px solid #50AB51;
                            border-radius: 5px;
                            padding: 20px;
                            margin: 20px 0;
                        }}
                        .credential-item {{
                            margin: 15px 0;
                            padding: 10px;
                            background-color: #f5f5f5;
                            border-radius: 3px;
                        }}
                        .credential-label {{
                            font-weight: bold;
                            color: #50AB51;
                            font-size: 14px;
                            display: block;
                            margin-bottom: 5px;
                        }}
                        .credential-value {{
                            font-family: 'Courier New', monospace;
                            font-size: 16px;
                            color: #000;
                            font-weight: bold;
                        }}
                        .important-box {{
                            background-color: #fff3cd;
                            border-left: 4px solid #ffc107;
                            padding: 15px;
                            margin: 20px 0;
                        }}
                        .important-box h3 {{
                            margin-top: 0;
                            color: #856404;
                            font-size: 16px;
                        }}
                        .important-box ul {{
                            margin: 10px 0;
                            padding-left: 20px;
                        }}
                        .important-box li {{
                            margin: 8px 0;
                            color: #856404;
                        }}
                        .button-container {{
                            text-align: center;
                            margin: 30px 0;
                        }}
                        .button {{
                            display: inline-block;
                            padding: 15px 40px;
                            background-color: #000000;
                            color: white !important;
                            text-decoration: none;
                            border-radius: 5px;
                            font-weight: bold;
                            font-size: 16px;
                        }}
                        .footer {{
                            background-color: #FFDF00;
                            padding: 20px;
                            text-align: center;
                            font-size: 12px;
                            color: #333;
                            border-radius: 0 0 5px 5px;
                        }}
                        .footer p {{ margin: 5px 0; }}
                    </style>
                </head>
                <body>
                    <div class=""container"">
                        <div class=""header"">
                            <h1>🔒 Contraseña Reseteada</h1>
                        </div>
                        <div class=""content"">
                            <p class=""welcome-text"">Hola <strong>{fullName}</strong>,</p>

                            <p>Tu contraseña ha sido reseteada por un administrador. A continuación encontrarás tu nueva contraseña temporal:</p>

                            <div class=""credentials-box"">
                                <div class=""credential-item"">
                                    <span class=""credential-label"">📧 Usuario:</span>
                                    <span class=""credential-value"">{userEmail}</span>
                                </div>
                                <div class=""credential-item"">
                                    <span class=""credential-label"">🔑 Nueva Contraseña Temporal:</span>
                                    <span class=""credential-value"">{temporaryPassword}</span>
                                </div>
                            </div>

                            <div class=""important-box"">
                                <h3>⚠️ Importante:</h3>
                                <ul>
                                    <li>Esta contraseña es <strong>temporal</strong> y debe ser cambiada en tu próximo inicio de sesión</li>
                                    <li>No podrás acceder al sistema completamente hasta que cambies tu contraseña</li>
                                    <li>Asegúrate de elegir una contraseña segura que cumpla con los requisitos del sistema</li>
                                    <li>Por seguridad, no compartas esta contraseña con nadie</li>
                                    <li>Si no solicitaste este cambio, contacta inmediatamente al administrador</li>
                                </ul>
                            </div>

                            <div class=""button-container"">
                                <a href=""{loginUrl}"" class=""button"">👉 Iniciar Sesión</a>
                            </div>

                            <p style=""margin-top: 30px; font-size: 14px; color: #666;"">
                                Si tienes problemas para acceder al sistema o necesitas ayuda, por favor contacta al administrador.
                            </p>
                        </div>
                        <div class=""footer"">
                            <p><strong>Febor Cooperativa</strong></p>
                            <p>Este es un mensaje automático del sistema.</p>
                            <p>Por favor, no responder a este correo.</p>
                        </div>
                    </div>
                </body>
                </html>",
                TextBody = $@"🔒 Contraseña Reseteada

Hola {fullName},

Tu contraseña ha sido reseteada por un administrador. A continuación encontrarás tu nueva contraseña temporal:

CREDENCIALES DE ACCESO:
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
📧 Usuario: {userEmail}
🔑 Nueva Contraseña Temporal: {temporaryPassword}
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

⚠️ IMPORTANTE:
• Esta contraseña es TEMPORAL y debe ser cambiada en tu próximo inicio de sesión
• No podrás acceder al sistema completamente hasta que cambies tu contraseña
• Asegúrate de elegir una contraseña segura
• Por seguridad, no compartas esta contraseña con nadie
• Si no solicitaste este cambio, contacta inmediatamente al administrador

👉 Iniciar sesión: {loginUrl}

Si tienes problemas para acceder al sistema, contacta al administrador.

---
Febor Cooperativa
Este es un mensaje automático del sistema.
Por favor, no responder a este correo."
            };

            message.Body = bodyBuilder.ToMessageBody();

            using var client = new SmtpClient();

            var secureSocketOptions = SecureSocketOptions.Auto;
            if (emailConfig.UseSsl)
                secureSocketOptions = SecureSocketOptions.SslOnConnect;
            else if (emailConfig.UseTls)
                secureSocketOptions = SecureSocketOptions.StartTls;
            else
                secureSocketOptions = SecureSocketOptions.None;

            await client.ConnectAsync(emailConfig.SmtpHost, emailConfig.SmtpPort, secureSocketOptions);
            await client.AuthenticateAsync(emailConfig.SmtpUsername, decryptedPassword);
            await client.SendAsync(message);
            await client.DisconnectAsync(true);

            _logger.LogInformation("Correo de reseteo de contraseña enviado exitosamente a {Email}", userEmail);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al enviar correo de reseteo de contraseña a {Email}", userEmail);
            // No lanzar la excepción para que no afecte el reseteo de contraseña
        }
    }

    public async Task NotifyAdminsNewSavingsRequestAsync(
        int requestId,
        string nombre,
        string apellidos,
        string email,
        string productoAhorro)
    {
        try
        {
            // Obtener configuración de email
            var emailConfig = await _emailConfigRepository.GetActiveConfigurationAsync();
            if (emailConfig == null)
            {
                _logger.LogWarning("No se pudo enviar notificación: No hay configuración de email activa");
                return;
            }

            // Obtener usuarios con rol Administrador (role_id = 3)
            var admins = await _roleRepository.GetUsersByRoleIdAsync(ADMIN_ROLE_ID);
            if (!admins.Any())
            {
                _logger.LogWarning("No se encontraron administradores para notificar");
                return;
            }

            // Desencriptar la contraseña SMTP
            var decryptedPassword = _encryptionService.Decrypt(emailConfig.SmtpPassword);

            // Enviar correo a cada administrador
            foreach (var admin in admins)
            {
                if (string.IsNullOrEmpty(admin.email))
                {
                    _logger.LogWarning("El administrador {AdminName} no tiene email configurado", admin.full_name);
                    continue;
                }

                try
                {
                    await SendSavingsEmailAsync(
                        emailConfig,
                        decryptedPassword,
                        admin.email,
                        admin.full_name,
                        requestId,
                        nombre,
                        apellidos,
                        email,
                        productoAhorro
                    );

                    _logger.LogInformation(
                        "Notificación enviada a {AdminEmail} sobre solicitud de Ahorro #{RequestId}",
                        admin.email,
                        requestId
                    );
                }
                catch (Exception ex)
                {
                    _logger.LogError(
                        ex,
                        "Error enviando notificación a {AdminEmail} sobre solicitud de Ahorro #{RequestId}",
                        admin.email,
                        requestId
                    );
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al enviar notificaciones de nueva solicitud de Ahorro #{RequestId}", requestId);
        }
    }

    private async Task SendSavingsEmailAsync(
        Domain.Entities.Configuration.EmailSettings config,
        string decryptedPassword,
        string toEmail,
        string toName,
        int requestId,
        string nombre,
        string apellidos,
        string clientEmail,
        string productoAhorro)
    {
        var frontendBaseUrl = _configuration["Frontend:BaseUrl"] ?? "http://localhost:5173";
        var loginUrl = $"{frontendBaseUrl}/login";

        var message = new MimeMessage();
        message.From.Add(new MailboxAddress(config.FromName, config.FromEmail));
        message.To.Add(new MailboxAddress(toName, toEmail));
        message.Subject = $"Nueva Solicitud de Ahorro #{requestId} - {productoAhorro}";

        var bodyBuilder = new BodyBuilder
        {
            HtmlBody = $@"
                <!DOCTYPE html>
                <html>
                <head>
                    <style>
                        body {{ font-family: Arial, sans-serif; line-height: 1.6; color: #333; }}
                        .container {{ max-width: 600px; margin: 0 auto; padding: 20px; }}
                        .header {{ background-color: #50AB51; color: white; padding: 20px; text-align: center; border-radius: 5px 5px 0 0; }}
                        .content {{ background-color: #f9f9f9; padding: 20px; border: 1px solid #ddd; }}
                        .info-item {{ margin: 10px 0; }}
                        .label {{ font-weight: bold; color: #50AB51; }}
                        .product-badge {{ background-color: #FFDF00; color: #333; padding: 5px 15px; border-radius: 15px; font-weight: bold; display: inline-block; margin: 10px 0; }}
                        .footer {{ background-color: #FFDF00; padding: 15px; text-align: center; font-size: 12px; color: #333; border-radius: 0 0 5px 5px; }}
                        .button {{
                            display: inline-block;
                            padding: 12px 24px;
                            margin: 20px 0;
                            background-color: #000000;
                            color: white !important;
                            text-decoration: none;
                            border-radius: 5px;
                            font-weight: bold;
                        }}
                    </style>
                </head>
                <body>
                    <div class=""container"">
                        <div class=""header"">
                            <h2>🐷 Nueva Solicitud de Producto de Ahorro</h2>
                        </div>
                        <div class=""content"">
                            <p>Hola <strong>{toName}</strong>,</p>

                            <p>Se ha recibido una nueva solicitud de un <strong>Producto de Ahorro</strong> desde el formulario web que requiere asignación.</p>

                            <div class=""info-item"">
                                <span class=""label"">ID de Solicitud:</span> #{requestId}
                            </div>
                            <div class=""info-item"">
                                <span class=""label"">Producto Solicitado:</span>
                                <span class=""product-badge"">{productoAhorro}</span>
                            </div>
                            <div class=""info-item"">
                                <span class=""label"">Nombre:</span> {nombre} {apellidos}
                            </div>
                            <div class=""info-item"">
                                <span class=""label"">Email:</span> {clientEmail}
                            </div>

                            <p style=""margin-top: 20px;"">Por favor, ingresa al sistema para revisar los detalles completos y asignar esta solicitud a un asesor.</p>

                            <div style=""text-align: center;"">
                                <a href=""{loginUrl}"" class=""button"">Ingresar al Sistema</a>
                            </div>
                        </div>
                        <div class=""footer"">
                            <p>Este es un mensaje automático del sistema Febor Cooperativa.</p>
                            <p>Por favor, no responder a este correo.</p>
                        </div>
                    </div>
                </body>
                </html>",
            TextBody = $@"Nueva Solicitud de Producto de Ahorro

Hola {toName},

Se ha recibido una nueva solicitud de un Producto de Ahorro desde el formulario web que requiere asignación.

ID de Solicitud: #{requestId}
Producto Solicitado: {productoAhorro}
Nombre: {nombre} {apellidos}
Email: {clientEmail}

Por favor, ingresa al sistema para revisar los detalles completos y asignar esta solicitud a un asesor.

---
Este es un mensaje automático del sistema Febor Cooperativa.
Por favor, no responder a este correo."
        };

        message.Body = bodyBuilder.ToMessageBody();

        using var client = new SmtpClient();

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
    }

    public async Task NotifyAdvisorSavingsAssignmentAsync(
        int assignmentId,
        int savingsRequestId,
        string clientNombre,
        string clientApellidos,
        string clientEmail,
        string clientTelefono,
        string productoAhorro,
        string advisorEmail,
        string advisorName,
        string assignedByName,
        string? notes)
    {
        try
        {
            var emailConfig = await _emailConfigRepository.GetActiveConfigurationAsync();
            if (emailConfig == null)
            {
                _logger.LogWarning("No se pudo enviar notificación: No hay configuración de email activa");
                return;
            }

            if (string.IsNullOrEmpty(advisorEmail))
            {
                _logger.LogWarning("El asesor {AdvisorName} no tiene email configurado", advisorName);
                return;
            }

            var decryptedPassword = _encryptionService.Decrypt(emailConfig.SmtpPassword);

            await SendAdvisorSavingsAssignmentEmailAsync(
                emailConfig,
                decryptedPassword,
                advisorEmail,
                advisorName,
                assignmentId,
                savingsRequestId,
                clientNombre,
                clientApellidos,
                clientEmail,
                clientTelefono,
                productoAhorro,
                assignedByName,
                notes
            );

            _logger.LogInformation(
                "Notificación de asignación enviada a asesor {AdvisorEmail} sobre solicitud de Ahorro #{SavingsRequestId}",
                advisorEmail,
                savingsRequestId
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Error al enviar notificación de asignación al asesor sobre solicitud de Ahorro #{SavingsRequestId}",
                savingsRequestId
            );
        }
    }

    private async Task SendAdvisorSavingsAssignmentEmailAsync(
        Domain.Entities.Configuration.EmailSettings config,
        string decryptedPassword,
        string advisorEmail,
        string advisorName,
        int assignmentId,
        int savingsRequestId,
        string clientNombre,
        string clientApellidos,
        string clientEmail,
        string clientTelefono,
        string productoAhorro,
        string assignedByName,
        string? notes)
    {
        var frontendBaseUrl = _configuration["Frontend:BaseUrl"] ?? "http://localhost:5173";
        var loginUrl = $"{frontendBaseUrl}/login";

        var message = new MimeMessage();
        message.From.Add(new MailboxAddress(config.FromName, config.FromEmail));
        message.To.Add(new MailboxAddress(advisorName, advisorEmail));
        message.Subject = $"Nueva Asignación de Ahorro #{savingsRequestId} - {productoAhorro}";

        var bodyBuilder = new BodyBuilder
        {
            HtmlBody = $@"
                <!DOCTYPE html>
                <html>
                <head>
                    <style>
                        body {{ font-family: Arial, sans-serif; line-height: 1.6; color: #333; }}
                        .container {{ max-width: 600px; margin: 0 auto; padding: 20px; }}
                        .header {{ background-color: #50AB51; color: white; padding: 20px; text-align: center; border-radius: 5px 5px 0 0; }}
                        .content {{ background-color: #f9f9f9; padding: 20px; border: 1px solid #ddd; }}
                        .info-item {{ margin: 15px 0; }}
                        .label {{ font-weight: bold; color: #50AB51; }}
                        .client-info {{ background-color: white; padding: 15px; border-radius: 5px; margin: 15px 0; border-left: 4px solid #FFDF00; }}
                        .product-badge {{ background-color: #FFDF00; color: #333; padding: 8px 20px; border-radius: 20px; font-weight: bold; display: inline-block; margin: 10px 0; font-size: 16px; }}
                        .footer {{ background-color: #FFDF00; padding: 15px; text-align: center; font-size: 12px; color: #333; border-radius: 0 0 5px 5px; }}
                        .button {{
                            display: inline-block;
                            padding: 12px 24px;
                            margin: 20px 0;
                            background-color: #000000;
                            color: white !important;
                            text-decoration: none;
                            border-radius: 5px;
                            font-weight: bold;
                        }}
                        .highlight {{ color: #50AB51; font-weight: bold; }}
                    </style>
                </head>
                <body>
                    <div class=""container"">
                        <div class=""header"">
                            <h2>🐷 Nueva Solicitud de Ahorro Asignada</h2>
                        </div>
                        <div class=""content"">
                            <p>Hola <strong>{advisorName}</strong>,</p>

                            <p>Se te ha asignado una nueva solicitud de <strong>Producto de Ahorro</strong>.</p>

                            <div class=""info-item"">
                                <span class=""label"">ID de Asignación:</span> <span class=""highlight"">#{assignmentId}</span>
                            </div>
                            <div class=""info-item"">
                                <span class=""label"">ID de Solicitud:</span> <span class=""highlight"">#{savingsRequestId}</span>
                            </div>
                            <div class=""info-item"">
                                <span class=""label"">Producto Solicitado:</span>
                                <span class=""product-badge"">{productoAhorro}</span>
                            </div>
                            <div class=""info-item"">
                                <span class=""label"">Asignado por:</span> {assignedByName}
                            </div>

                            <div class=""client-info"">
                                <h3 style=""margin-top: 0; color: #50AB51;"">📞 Información del Cliente</h3>
                                <div class=""info-item"">
                                    <span class=""label"">Nombre:</span> {clientNombre} {clientApellidos}
                                </div>
                                <div class=""info-item"">
                                    <span class=""label"">Email:</span> <a href=""mailto:{clientEmail}"">{clientEmail}</a>
                                </div>
                                <div class=""info-item"">
                                    <span class=""label"">Teléfono:</span> <a href=""tel:{clientTelefono}"">{clientTelefono}</a>
                                </div>
                            </div>

                            <p style=""margin-top: 20px;"">Por favor, contacta al cliente lo antes posible para brindarle la asesoría solicitada sobre el producto de ahorro.</p>

                            <div style=""text-align: center;"">
                                <a href=""{loginUrl}"" class=""button"">Ingresar al Sistema</a>
                            </div>
                        </div>
                        <div class=""footer"">
                            <p>Este es un mensaje automático del sistema Febor Cooperativa.</p>
                            <p>Por favor, no responder a este correo.</p>
                        </div>
                    </div>
                </body>
                </html>",
            TextBody = $@"Nueva Solicitud de Ahorro Asignada

Hola {advisorName},

Se te ha asignado una nueva solicitud de Producto de Ahorro.

ID de Asignación: #{assignmentId}
ID de Solicitud: #{savingsRequestId}
Producto Solicitado: {productoAhorro}
Asignado por: {assignedByName}

--- Información del Cliente ---
Nombre: {clientNombre} {clientApellidos}
Email: {clientEmail}
Teléfono: {clientTelefono}

Por favor, contacta al cliente lo antes posible para brindarle la asesoría solicitada.

Ingresa al sistema: {loginUrl}

---
Este es un mensaje automático del sistema Febor Cooperativa.
Por favor, no responder a este correo."
        };

        message.Body = bodyBuilder.ToMessageBody();

        using var client = new SmtpClient();

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
    }

    public async Task NotifyAdminsNewConvenioRequestAsync(
        int requestId,
        string nombre,
        string apellidos,
        string email,
        string tipoConvenio)
    {
        try
        {
            var emailConfig = await _emailConfigRepository.GetActiveConfigurationAsync();
            if (emailConfig == null)
            {
                _logger.LogWarning("No se pudo enviar notificación: No hay configuración de email activa");
                return;
            }

            var admins = await _roleRepository.GetUsersByRoleIdAsync(ADMIN_ROLE_ID);
            if (!admins.Any())
            {
                _logger.LogWarning("No se encontraron administradores para notificar");
                return;
            }

            var decryptedPassword = _encryptionService.Decrypt(emailConfig.SmtpPassword);

            foreach (var admin in admins)
            {
                if (string.IsNullOrEmpty(admin.email))
                {
                    _logger.LogWarning("El administrador {AdminName} no tiene email configurado", admin.full_name);
                    continue;
                }

                try
                {
                    await SendConvenioEmailAsync(
                        emailConfig,
                        decryptedPassword,
                        admin.email,
                        admin.full_name,
                        requestId,
                        nombre,
                        apellidos,
                        email,
                        tipoConvenio
                    );

                    _logger.LogInformation(
                        "Notificación enviada a {AdminEmail} sobre solicitud de Convenio #{RequestId}",
                        admin.email,
                        requestId
                    );
                }
                catch (Exception ex)
                {
                    _logger.LogError(
                        ex,
                        "Error enviando notificación a {AdminEmail} sobre solicitud de Convenio #{RequestId}",
                        admin.email,
                        requestId
                    );
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al enviar notificaciones de nueva solicitud de Convenio #{RequestId}", requestId);
        }
    }

    private async Task SendConvenioEmailAsync(
        Domain.Entities.Configuration.EmailSettings config,
        string decryptedPassword,
        string toEmail,
        string toName,
        int requestId,
        string nombre,
        string apellidos,
        string clientEmail,
        string tipoConvenio)
    {
        var frontendBaseUrl = _configuration["Frontend:BaseUrl"] ?? "http://localhost:5173";
        var loginUrl = $"{frontendBaseUrl}/login";

        var message = new MimeMessage();
        message.From.Add(new MailboxAddress(config.FromName, config.FromEmail));
        message.To.Add(new MailboxAddress(toName, toEmail));
        message.Subject = $"Nueva Solicitud de Convenio #{requestId} - {tipoConvenio}";

        var bodyBuilder = new BodyBuilder
        {
            HtmlBody = $@"
                <!DOCTYPE html>
                <html>
                <head>
                    <style>
                        body {{ font-family: Arial, sans-serif; line-height: 1.6; color: #333; }}
                        .container {{ max-width: 600px; margin: 0 auto; padding: 20px; }}
                        .header {{ background-color: #50AB51; color: white; padding: 20px; text-align: center; border-radius: 5px 5px 0 0; }}
                        .content {{ background-color: #f9f9f9; padding: 20px; border: 1px solid #ddd; }}
                        .info-item {{ margin: 10px 0; }}
                        .label {{ font-weight: bold; color: #50AB51; }}
                        .product-badge {{ background-color: #FFDF00; color: #333; padding: 5px 15px; border-radius: 15px; font-weight: bold; display: inline-block; margin: 10px 0; }}
                        .footer {{ background-color: #FFDF00; padding: 15px; text-align: center; font-size: 12px; color: #333; border-radius: 0 0 5px 5px; }}
                        .button {{
                            display: inline-block;
                            padding: 12px 24px;
                            margin: 20px 0;
                            background-color: #000000;
                            color: white !important;
                            text-decoration: none;
                            border-radius: 5px;
                            font-weight: bold;
                        }}
                    </style>
                </head>
                <body>
                    <div class=""container"">
                        <div class=""header"">
                            <h2>🤝 Nueva Solicitud de Convenio</h2>
                        </div>
                        <div class=""content"">
                            <p>Hola <strong>{toName}</strong>,</p>

                            <p>Se ha recibido una nueva solicitud de <strong>Convenio</strong> desde el formulario web que requiere asignación.</p>

                            <div class=""info-item"">
                                <span class=""label"">ID de Solicitud:</span> #{requestId}
                            </div>
                            <div class=""info-item"">
                                <span class=""label"">Tipo de Convenio:</span>
                                <span class=""product-badge"">{tipoConvenio}</span>
                            </div>
                            <div class=""info-item"">
                                <span class=""label"">Nombre:</span> {nombre} {apellidos}
                            </div>
                            <div class=""info-item"">
                                <span class=""label"">Email:</span> {clientEmail}
                            </div>

                            <p style=""margin-top: 20px;"">Por favor, ingresa al sistema para revisar los detalles completos y asignar esta solicitud a un asesor.</p>

                            <div style=""text-align: center;"">
                                <a href=""{loginUrl}"" class=""button"">Ingresar al Sistema</a>
                            </div>
                        </div>
                        <div class=""footer"">
                            <p>Este es un mensaje automático del sistema Febor Cooperativa.</p>
                            <p>Por favor, no responder a este correo.</p>
                        </div>
                    </div>
                </body>
                </html>",
            TextBody = $@"Nueva Solicitud de Convenio

Hola {toName},

Se ha recibido una nueva solicitud de Convenio desde el formulario web que requiere asignación.

ID de Solicitud: #{requestId}
Tipo de Convenio: {tipoConvenio}
Nombre: {nombre} {apellidos}
Email: {clientEmail}

Por favor, ingresa al sistema para revisar los detalles completos y asignar esta solicitud a un asesor.

---
Este es un mensaje automático del sistema Febor Cooperativa.
Por favor, no responder a este correo."
        };

        message.Body = bodyBuilder.ToMessageBody();

        using var client = new SmtpClient();

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
    }

    public async Task NotifyAdvisorConvenioAssignmentAsync(
        int assignmentId,
        int convenioRequestId,
        string clientNombre,
        string clientApellidos,
        string clientEmail,
        string clientTelefono,
        string tipoConvenio,
        string advisorEmail,
        string advisorName,
        string assignedByName,
        string? notes)
    {
        try
        {
            var emailConfig = await _emailConfigRepository.GetActiveConfigurationAsync();
            if (emailConfig == null)
            {
                _logger.LogWarning("No se pudo enviar notificación: No hay configuración de email activa");
                return;
            }

            if (string.IsNullOrEmpty(advisorEmail))
            {
                _logger.LogWarning("El asesor {AdvisorName} no tiene email configurado", advisorName);
                return;
            }

            var decryptedPassword = _encryptionService.Decrypt(emailConfig.SmtpPassword);

            await SendAdvisorConvenioAssignmentEmailAsync(
                emailConfig,
                decryptedPassword,
                advisorEmail,
                advisorName,
                assignmentId,
                convenioRequestId,
                clientNombre,
                clientApellidos,
                clientEmail,
                clientTelefono,
                tipoConvenio,
                assignedByName,
                notes
            );

            _logger.LogInformation(
                "Notificación de asignación enviada a asesor {AdvisorEmail} sobre solicitud de Convenio #{ConvenioRequestId}",
                advisorEmail,
                convenioRequestId
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Error al enviar notificación de asignación al asesor sobre solicitud de Convenio #{ConvenioRequestId}",
                convenioRequestId
            );
        }
    }

    private async Task SendAdvisorConvenioAssignmentEmailAsync(
        Domain.Entities.Configuration.EmailSettings config,
        string decryptedPassword,
        string advisorEmail,
        string advisorName,
        int assignmentId,
        int convenioRequestId,
        string clientNombre,
        string clientApellidos,
        string clientEmail,
        string clientTelefono,
        string tipoConvenio,
        string assignedByName,
        string? notes)
    {
        var frontendBaseUrl = _configuration["Frontend:BaseUrl"] ?? "http://localhost:5173";
        var loginUrl = $"{frontendBaseUrl}/login";

        var message = new MimeMessage();
        message.From.Add(new MailboxAddress(config.FromName, config.FromEmail));
        message.To.Add(new MailboxAddress(advisorName, advisorEmail));
        message.Subject = $"Nueva Asignación de Convenio #{convenioRequestId} - {tipoConvenio}";

        var bodyBuilder = new BodyBuilder
        {
            HtmlBody = $@"
                <!DOCTYPE html>
                <html>
                <head>
                    <style>
                        body {{ font-family: Arial, sans-serif; line-height: 1.6; color: #333; }}
                        .container {{ max-width: 600px; margin: 0 auto; padding: 20px; }}
                        .header {{ background-color: #50AB51; color: white; padding: 20px; text-align: center; border-radius: 5px 5px 0 0; }}
                        .content {{ background-color: #f9f9f9; padding: 20px; border: 1px solid #ddd; }}
                        .info-item {{ margin: 15px 0; }}
                        .label {{ font-weight: bold; color: #50AB51; }}
                        .client-info {{ background-color: white; padding: 15px; border-radius: 5px; margin: 15px 0; border-left: 4px solid #FFDF00; }}
                        .product-badge {{ background-color: #FFDF00; color: #333; padding: 8px 20px; border-radius: 20px; font-weight: bold; display: inline-block; margin: 10px 0; font-size: 16px; }}
                        .footer {{ background-color: #FFDF00; padding: 15px; text-align: center; font-size: 12px; color: #333; border-radius: 0 0 5px 5px; }}
                        .button {{
                            display: inline-block;
                            padding: 12px 24px;
                            margin: 20px 0;
                            background-color: #000000;
                            color: white !important;
                            text-decoration: none;
                            border-radius: 5px;
                            font-weight: bold;
                        }}
                        .highlight {{ color: #50AB51; font-weight: bold; }}
                    </style>
                </head>
                <body>
                    <div class=""container"">
                        <div class=""header"">
                            <h2>🤝 Nueva Solicitud de Convenio Asignada</h2>
                        </div>
                        <div class=""content"">
                            <p>Hola <strong>{advisorName}</strong>,</p>

                            <p>Se te ha asignado una nueva solicitud de <strong>Convenio</strong>.</p>

                            <div class=""info-item"">
                                <span class=""label"">ID de Asignación:</span> <span class=""highlight"">#{assignmentId}</span>
                            </div>
                            <div class=""info-item"">
                                <span class=""label"">ID de Solicitud:</span> <span class=""highlight"">#{convenioRequestId}</span>
                            </div>
                            <div class=""info-item"">
                                <span class=""label"">Tipo de Convenio:</span>
                                <span class=""product-badge"">{tipoConvenio}</span>
                            </div>
                            <div class=""info-item"">
                                <span class=""label"">Asignado por:</span> {assignedByName}
                            </div>

                            <div class=""client-info"">
                                <h3 style=""margin-top: 0; color: #50AB51;"">📞 Información del Cliente</h3>
                                <div class=""info-item"">
                                    <span class=""label"">Nombre:</span> {clientNombre} {clientApellidos}
                                </div>
                                <div class=""info-item"">
                                    <span class=""label"">Email:</span> <a href=""mailto:{clientEmail}"">{clientEmail}</a>
                                </div>
                                <div class=""info-item"">
                                    <span class=""label"">Teléfono:</span> <a href=""tel:{clientTelefono}"">{clientTelefono}</a>
                                </div>
                            </div>

                            <p style=""margin-top: 20px;"">Por favor, contacta al cliente lo antes posible para brindarle la asesoría solicitada sobre el convenio.</p>

                            <div style=""text-align: center;"">
                                <a href=""{loginUrl}"" class=""button"">Ingresar al Sistema</a>
                            </div>
                        </div>
                        <div class=""footer"">
                            <p>Este es un mensaje automático del sistema Febor Cooperativa.</p>
                            <p>Por favor, no responder a este correo.</p>
                        </div>
                    </div>
                </body>
                </html>",
            TextBody = $@"Nueva Solicitud de Convenio Asignada

Hola {advisorName},

Se te ha asignado una nueva solicitud de Convenio.

ID de Asignación: #{assignmentId}
ID de Solicitud: #{convenioRequestId}
Tipo de Convenio: {tipoConvenio}
Asignado por: {assignedByName}

--- Información del Cliente ---
Nombre: {clientNombre} {clientApellidos}
Email: {clientEmail}
Teléfono: {clientTelefono}

Por favor, contacta al cliente lo antes posible para brindarle la asesoría solicitada.

Ingresa al sistema: {loginUrl}

---
Este es un mensaje automático del sistema Febor Cooperativa.
Por favor, no responder a este correo."
        };

        message.Body = bodyBuilder.ToMessageBody();

        using var client = new SmtpClient();

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
    }

    public async Task NotifyAdminsNewCreditExternalRequestAsync(
        int requestId,
        string nombre,
        string apellidos,
        string email,
        string productoCredito)
    {
        try
        {
            var emailConfig = await _emailConfigRepository.GetActiveConfigurationAsync();
            if (emailConfig == null)
            {
                _logger.LogWarning("No se pudo enviar notificación: No hay configuración de email activa");
                return;
            }

            var admins = await _roleRepository.GetUsersByRoleIdAsync(ADMIN_ROLE_ID);
            if (!admins.Any())
            {
                _logger.LogWarning("No se encontraron administradores para notificar");
                return;
            }

            var decryptedPassword = _encryptionService.Decrypt(emailConfig.SmtpPassword);

            foreach (var admin in admins)
            {
                if (string.IsNullOrEmpty(admin.email))
                {
                    _logger.LogWarning("El administrador {AdminName} no tiene email configurado", admin.full_name);
                    continue;
                }

                try
                {
                    await SendCreditExternalEmailAsync(
                        emailConfig,
                        decryptedPassword,
                        admin.email,
                        admin.full_name,
                        requestId,
                        nombre,
                        apellidos,
                        email,
                        productoCredito
                    );

                    _logger.LogInformation(
                        "Notificación enviada a {AdminEmail} sobre solicitud de Crédito Externa #{RequestId}",
                        admin.email,
                        requestId
                    );
                }
                catch (Exception ex)
                {
                    _logger.LogError(
                        ex,
                        "Error enviando notificación a {AdminEmail} sobre solicitud de Crédito Externa #{RequestId}",
                        admin.email,
                        requestId
                    );
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al enviar notificaciones de nueva solicitud de Crédito Externa #{RequestId}", requestId);
        }
    }

    private async Task SendCreditExternalEmailAsync(
        Domain.Entities.Configuration.EmailSettings config,
        string decryptedPassword,
        string toEmail,
        string toName,
        int requestId,
        string nombre,
        string apellidos,
        string clientEmail,
        string productoCredito)
    {
        var frontendBaseUrl = _configuration["Frontend:BaseUrl"] ?? "http://localhost:5173";
        var loginUrl = $"{frontendBaseUrl}/login";

        var message = new MimeMessage();
        message.From.Add(new MailboxAddress(config.FromName, config.FromEmail));
        message.To.Add(new MailboxAddress(toName, toEmail));
        message.Subject = $"Nueva Solicitud de Crédito #{requestId} - {productoCredito}";

        var bodyBuilder = new BodyBuilder
        {
            HtmlBody = $@"
                <!DOCTYPE html>
                <html>
                <head>
                    <style>
                        body {{ font-family: Arial, sans-serif; line-height: 1.6; color: #333; }}
                        .container {{ max-width: 600px; margin: 0 auto; padding: 20px; }}
                        .header {{ background-color: #50AB51; color: white; padding: 20px; text-align: center; border-radius: 5px 5px 0 0; }}
                        .content {{ background-color: #f9f9f9; padding: 20px; border: 1px solid #ddd; }}
                        .info-item {{ margin: 10px 0; }}
                        .label {{ font-weight: bold; color: #50AB51; }}
                        .product-badge {{ background-color: #007bff; color: white; padding: 5px 15px; border-radius: 15px; font-weight: bold; display: inline-block; margin: 10px 0; }}
                        .footer {{ background-color: #FFDF00; padding: 15px; text-align: center; font-size: 12px; color: #333; border-radius: 0 0 5px 5px; }}
                        .button {{
                            display: inline-block;
                            padding: 12px 24px;
                            margin: 20px 0;
                            background-color: #000000;
                            color: white !important;
                            text-decoration: none;
                            border-radius: 5px;
                            font-weight: bold;
                        }}
                    </style>
                </head>
                <body>
                    <div class=""container"">
                        <div class=""header"">
                            <h2>💳 Nueva Solicitud de Producto de Crédito</h2>
                        </div>
                        <div class=""content"">
                            <p>Hola <strong>{toName}</strong>,</p>

                            <p>Se ha recibido una nueva solicitud de un <strong>Producto de Crédito</strong> desde el formulario web que requiere asignación.</p>

                            <div class=""info-item"">
                                <span class=""label"">ID de Solicitud:</span> #{requestId}
                            </div>
                            <div class=""info-item"">
                                <span class=""label"">Producto Solicitado:</span>
                                <span class=""product-badge"">{productoCredito}</span>
                            </div>
                            <div class=""info-item"">
                                <span class=""label"">Nombre:</span> {nombre} {apellidos}
                            </div>
                            <div class=""info-item"">
                                <span class=""label"">Email:</span> {clientEmail}
                            </div>

                            <p style=""margin-top: 20px;"">Por favor, ingresa al sistema para revisar los detalles completos y asignar esta solicitud a un asesor.</p>

                            <div style=""text-align: center;"">
                                <a href=""{loginUrl}"" class=""button"">Ingresar al Sistema</a>
                            </div>
                        </div>
                        <div class=""footer"">
                            <p>Este es un mensaje automático del sistema Febor Cooperativa.</p>
                            <p>Por favor, no responder a este correo.</p>
                        </div>
                    </div>
                </body>
                </html>",
            TextBody = $@"Nueva Solicitud de Producto de Crédito

Hola {toName},

Se ha recibido una nueva solicitud de un Producto de Crédito desde el formulario web que requiere asignación.

ID de Solicitud: #{requestId}
Producto Solicitado: {productoCredito}
Nombre: {nombre} {apellidos}
Email: {clientEmail}

Por favor, ingresa al sistema para revisar los detalles completos y asignar esta solicitud a un asesor.

---
Este es un mensaje automático del sistema Febor Cooperativa.
Por favor, no responder a este correo."
        };

        message.Body = bodyBuilder.ToMessageBody();

        using var client = new SmtpClient();

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
    }

    public async Task NotifyAdvisorCreditExternalAssignmentAsync(
        int assignmentId,
        int creditRequestId,
        string clientNombre,
        string clientApellidos,
        string clientEmail,
        string clientTelefono,
        string productoCredito,
        string advisorEmail,
        string advisorName,
        string assignedByName,
        string? notes)
    {
        try
        {
            var emailConfig = await _emailConfigRepository.GetActiveConfigurationAsync();
            if (emailConfig == null)
            {
                _logger.LogWarning("No se pudo enviar notificación: No hay configuración de email activa");
                return;
            }

            if (string.IsNullOrEmpty(advisorEmail))
            {
                _logger.LogWarning("El asesor {AdvisorName} no tiene email configurado", advisorName);
                return;
            }

            var decryptedPassword = _encryptionService.Decrypt(emailConfig.SmtpPassword);

            await SendAdvisorCreditExternalAssignmentEmailAsync(
                emailConfig,
                decryptedPassword,
                advisorEmail,
                advisorName,
                assignmentId,
                creditRequestId,
                clientNombre,
                clientApellidos,
                clientEmail,
                clientTelefono,
                productoCredito,
                assignedByName,
                notes
            );

            _logger.LogInformation(
                "Notificación de asignación de crédito externo enviada a {AdvisorEmail} - Solicitud #{RequestId}",
                advisorEmail,
                creditRequestId
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Error al enviar notificación de asignación de crédito externo a {AdvisorEmail} - Solicitud #{RequestId}",
                advisorEmail,
                creditRequestId
            );
        }
    }

    private async Task SendAdvisorCreditExternalAssignmentEmailAsync(
        Domain.Entities.Configuration.EmailSettings config,
        string decryptedPassword,
        string advisorEmail,
        string advisorName,
        int assignmentId,
        int creditRequestId,
        string clientNombre,
        string clientApellidos,
        string clientEmail,
        string clientTelefono,
        string productoCredito,
        string assignedByName,
        string? notes)
    {
        var frontendBaseUrl = _configuration["Frontend:BaseUrl"] ?? "http://localhost:5173";
        var loginUrl = $"{frontendBaseUrl}/login";

        var message = new MimeMessage();
        message.From.Add(new MailboxAddress(config.FromName, config.FromEmail));
        message.To.Add(new MailboxAddress(advisorName, advisorEmail));
        message.Subject = $"Nueva Solicitud de Crédito Asignada #{creditRequestId} - {productoCredito}";

        var bodyBuilder = new BodyBuilder
        {
            HtmlBody = $@"
                <!DOCTYPE html>
                <html>
                <head>
                    <style>
                        body {{ font-family: Arial, sans-serif; line-height: 1.6; color: #333; }}
                        .container {{ max-width: 600px; margin: 0 auto; padding: 20px; }}
                        .header {{ background-color: #50AB51; color: white; padding: 20px; text-align: center; border-radius: 5px 5px 0 0; }}
                        .content {{ background-color: #f9f9f9; padding: 20px; border: 1px solid #ddd; }}
                        .info-item {{ margin: 10px 0; }}
                        .label {{ font-weight: bold; color: #50AB51; }}
                        .client-info {{ background-color: #e8f5e9; padding: 15px; border-radius: 5px; margin: 20px 0; border-left: 4px solid #50AB51; }}
                        .product-badge {{ background-color: #007bff; color: white; padding: 5px 15px; border-radius: 15px; font-weight: bold; display: inline-block; margin: 10px 0; }}
                        .footer {{ background-color: #FFDF00; padding: 15px; text-align: center; font-size: 12px; color: #333; border-radius: 0 0 5px 5px; }}
                        .button {{
                            display: inline-block;
                            padding: 12px 24px;
                            margin: 20px 0;
                            background-color: #000000;
                            color: white !important;
                            text-decoration: none;
                            border-radius: 5px;
                            font-weight: bold;
                        }}
                        .highlight {{ color: #50AB51; font-weight: bold; }}
                    </style>
                </head>
                <body>
                    <div class=""container"">
                        <div class=""header"">
                            <h2>💳 Nueva Solicitud de Crédito Asignada</h2>
                        </div>
                        <div class=""content"">
                            <p>Hola <strong>{advisorName}</strong>,</p>

                            <p>Se te ha asignado una nueva solicitud de <strong>Producto de Crédito</strong>.</p>

                            <div class=""info-item"">
                                <span class=""label"">ID de Asignación:</span> <span class=""highlight"">#{assignmentId}</span>
                            </div>
                            <div class=""info-item"">
                                <span class=""label"">ID de Solicitud:</span> <span class=""highlight"">#{creditRequestId}</span>
                            </div>
                            <div class=""info-item"">
                                <span class=""label"">Producto Solicitado:</span>
                                <span class=""product-badge"">{productoCredito}</span>
                            </div>
                            <div class=""info-item"">
                                <span class=""label"">Asignado por:</span> {assignedByName}
                            </div>

                            <div class=""client-info"">
                                <h3 style=""margin-top: 0; color: #50AB51;"">📞 Información del Cliente</h3>
                                <div class=""info-item"">
                                    <span class=""label"">Nombre:</span> {clientNombre} {clientApellidos}
                                </div>
                                <div class=""info-item"">
                                    <span class=""label"">Email:</span> <a href=""mailto:{clientEmail}"">{clientEmail}</a>
                                </div>
                                <div class=""info-item"">
                                    <span class=""label"">Teléfono:</span> <a href=""tel:{clientTelefono}"">{clientTelefono}</a>
                                </div>
                            </div>

                            <p style=""margin-top: 20px;"">Por favor, contacta al cliente lo antes posible para brindarle la asesoría solicitada sobre el producto de crédito.</p>

                            <div style=""text-align: center;"">
                                <a href=""{loginUrl}"" class=""button"">Ingresar al Sistema</a>
                            </div>
                        </div>
                        <div class=""footer"">
                            <p>Este es un mensaje automático del sistema Febor Cooperativa.</p>
                            <p>Por favor, no responder a este correo.</p>
                        </div>
                    </div>
                </body>
                </html>",
            TextBody = $@"Nueva Solicitud de Crédito Asignada

Hola {advisorName},

Se te ha asignado una nueva solicitud de Producto de Crédito.

ID de Asignación: #{assignmentId}
ID de Solicitud: #{creditRequestId}
Producto Solicitado: {productoCredito}
Asignado por: {assignedByName}

--- Información del Cliente ---
Nombre: {clientNombre} {clientApellidos}
Email: {clientEmail}
Teléfono: {clientTelefono}

Por favor, contacta al cliente lo antes posible para brindarle la asesoría solicitada.

Ingresa al sistema: {loginUrl}

---
Este es un mensaje automático del sistema Febor Cooperativa.
Por favor, no responder a este correo."
        };

        message.Body = bodyBuilder.ToMessageBody();

        using var client = new SmtpClient();

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
    }
}
