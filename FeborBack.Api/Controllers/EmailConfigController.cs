using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using FeborBack.Application.Services.Configuration;
using FeborBack.Application.DTOs.Configuration;
using FeborBack.Api.Authorization;
using System.Security.Claims;

namespace FeborBack.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize] // Requiere autenticación
public class EmailConfigController : ControllerBase
{
    private readonly IEmailConfigService _emailConfigService;
    private readonly ILogger<EmailConfigController> _logger;

    public EmailConfigController(
        IEmailConfigService emailConfigService,
        ILogger<EmailConfigController> logger)
    {
        _emailConfigService = emailConfigService;
        _logger = logger;
    }

    /// <summary>
    /// Obtiene la configuración SMTP actual (sin contraseña)
    /// </summary>
    [HttpGet]
    [MenuAuthorize("manage", "admin")]
    public async Task<ActionResult<EmailConfigDto>> GetConfiguration()
    {
        try
        {
            var config = await _emailConfigService.GetConfigurationAsync();

            if (config == null)
            {
                return NotFound(new { message = "No existe una configuración de correo" });
            }

            return Ok(config);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error obteniendo configuración de correo");
            return StatusCode(500, new { error = "Error interno del servidor" });
        }
    }

    /// <summary>
    /// Guarda o actualiza la configuración SMTP
    /// </summary>
    [HttpPost]
    [MenuAuthorize("manage", "admin")]
    public async Task<ActionResult<object>> SaveConfiguration([FromBody] SaveEmailConfigDto dto)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var userId = GetUserId();
            var savedConfig = await _emailConfigService.SaveConfigurationAsync(dto, userId);

            _logger.LogInformation("Configuración de correo guardada por usuario {UserId}", userId);

            return Ok(new
            {
                success = true,
                message = "Configuración guardada correctamente",
                data = savedConfig
            });
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Validación fallida al guardar configuración de correo");
            return BadRequest(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error guardando configuración de correo");
            return StatusCode(500, new { error = "Error interno del servidor" });
        }
    }

    /// <summary>
    /// Envía un correo de prueba con la configuración actual
    /// </summary>
    [HttpPost("test")]
    [MenuAuthorize("manage", "admin")]
    public async Task<ActionResult<object>> SendTestEmail([FromBody] TestEmailDto dto)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var userId = GetUserId();
            var result = await _emailConfigService.SendTestEmailAsync(dto, userId);

            if (result)
            {
                _logger.LogInformation("Correo de prueba enviado a {Email} por usuario {UserId}", dto.To, userId);

                return Ok(new
                {
                    success = true,
                    message = $"Correo enviado correctamente a {dto.To}"
                });
            }

            return BadRequest(new { error = "No se pudo enviar el correo" });
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Rate limit excedido para usuario {UserId}", GetUserId());
            return StatusCode(429, new { error = ex.Message });
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Validación fallida al enviar correo de prueba");
            return BadRequest(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error enviando correo de prueba a {Email}", dto.To);
            return StatusCode(500, new
            {
                success = false,
                error = ex.Message
            });
        }
    }

    /// <summary>
    /// Verifica la conexión SMTP sin enviar correo
    /// </summary>
    [HttpPost("verify")]
    [MenuAuthorize("manage", "admin")]
    public async Task<ActionResult<object>> VerifyConnection([FromBody] VerifyConnectionDto dto)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var result = await _emailConfigService.VerifyConnectionAsync(dto);

            if (result)
            {
                return Ok(new
                {
                    success = true,
                    message = "Conexión SMTP verificada correctamente"
                });
            }

            return BadRequest(new { error = "No se pudo verificar la conexión" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error verificando conexión SMTP");
            return StatusCode(500, new
            {
                success = false,
                error = ex.Message
            });
        }
    }

    // Métodos helper
    private int GetUserId()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return int.TryParse(userIdClaim, out var userId) ? userId : 0;
    }
}
