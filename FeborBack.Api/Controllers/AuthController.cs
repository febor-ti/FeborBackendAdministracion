using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using FeborBack.Application.Services;
using FeborBack.Application.Services.Notifications;
using FeborBack.Application.DTOs.Auth;
using System.Security.Claims;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;

namespace FeborBack.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;
    private readonly IEmailNotificationService _emailNotificationService;
    private readonly IReCaptchaService _reCaptchaService;
    private readonly IWebHostEnvironment _environment;
    private readonly ILogger<AuthController> _logger;

    public AuthController(
        IAuthService authService,
        IEmailNotificationService emailNotificationService,
        IReCaptchaService reCaptchaService,
        IWebHostEnvironment environment,
        ILogger<AuthController> logger)
    {
        _authService = authService;
        _emailNotificationService = emailNotificationService;
        _reCaptchaService = reCaptchaService;
        _environment = environment;
        _logger = logger;
    }

    /// <summary>
    /// Crear administrador inicial
    /// </summary>
    [HttpPost("create-initial-admin")]
    [AllowAnonymous]
    public async Task<IActionResult> CreateInitialAdmin()
    {
        try
        {
            var result = await _authService.CreateInitialAdminAsync();
            return Ok(new { success = true, message = "Administrador creado", data = result });
        }
        catch (Exception ex)
        {
            return BadRequest(new { success = false, message = ex.Message });
        }
    }

    /// <summary>
    /// Iniciar sesión
    /// </summary>
    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<IActionResult> Login([FromBody] LoginRequestDto request)
    {
        try
        {
            if (!ModelState.IsValid)
                return BadRequest(new { success = false, message = "Datos inválidos" });

            // 1. Verificar reCAPTCHA
            if (!string.IsNullOrEmpty(request.RecaptchaToken))
            {
                var recaptchaResult = await _reCaptchaService.VerifyTokenAsync(
                    request.RecaptchaToken,
                    "login"
                );

                if (!recaptchaResult.Success)
                {
                    _logger.LogWarning(
                        "Intento de login con reCAPTCHA inválido. Email: {Email}, Errores: {Errors}",
                        request.Email,
                        recaptchaResult.ErrorCodes != null ? string.Join(", ", recaptchaResult.ErrorCodes) : "ninguno"
                    );
                    return BadRequest(new
                    {
                        success = false,
                        message = "Verificación de seguridad falló. Por favor intenta nuevamente.",
                        // Debug info (considera remover en producción)
                        debug = new
                        {
                            score = recaptchaResult.Score,
                            errors = recaptchaResult.ErrorCodes
                        }
                    });
                }

                // En desarrollo, ser más permisivo con el score
                var minimumScore = _environment.IsDevelopment() ? -1.0 : 0.3;

                if (recaptchaResult.Score < minimumScore)
                {
                    _logger.LogWarning(
                        "Score de reCAPTCHA muy bajo en login: {Score}. Email: {Email}, Environment: {Env}",
                        recaptchaResult.Score,
                        request.Email,
                        _environment.EnvironmentName
                    );
                    return BadRequest(new
                    {
                        success = false,
                        message = "Actividad sospechosa detectada.",
                        // Debug info (considera remover en producción)
                        debug = new
                        {
                            score = recaptchaResult.Score,
                            environment = _environment.EnvironmentName,
                            minimumRequired = minimumScore
                        }
                    });
                }

                _logger.LogInformation(
                    "reCAPTCHA validado exitosamente - Action: login, Score: {Score}, Email: {Email}",
                    recaptchaResult.Score,
                    request.Email
                );
            }

            // 2. Continuar con la lógica de login normal
            var result = await _authService.LoginAsync(request);
            return Ok(new { success = true, message = "Login exitoso", data = result });
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(new { success = false, message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogError(ex, "Error operacional en login (2FA / email)");
            return BadRequest(new { success = false, message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error en login");
            return StatusCode(500, new { success = false, message = "Error interno" });
        }
    }

    /// <summary>
    /// Registrar usuario (público para testing)
    /// </summary>
    [HttpPost("register")]
    [AllowAnonymous]
    public async Task<IActionResult> Register([FromBody] RegisterUserDto request)
    {
        try
        {
            if (!ModelState.IsValid)
                return BadRequest(new { success = false, message = "Datos inválidos" });

            // Usar ID 1 como createdBy (admin inicial)
            var (user, temporaryPassword) = await _authService.RegisterUserAsync(request, 1);

            // Si se generó contraseña temporal, enviar correo
            if (!string.IsNullOrEmpty(temporaryPassword))
            {
                try
                {
                    await _emailNotificationService.SendWelcomeEmailWithTemporaryPasswordAsync(
                        user.Email,
                        user.FullName,
                        temporaryPassword
                    );
                }
                catch (Exception emailEx)
                {
                    _logger.LogError(emailEx, "Error al enviar correo a {Email}", user.Email);
                }
            }

            return Ok(new { success = true, message = "Usuario registrado", data = user });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { success = false, message = ex.Message });
        }
        catch (Exception)
        {
            return StatusCode(500, new { success = false, message = "Error interno" });
        }
    }

    /// <summary>
    /// Registrar usuario (requiere autenticación).
    /// Si no se proporciona contraseña, se genera automáticamente y se envía por correo.
    /// </summary>
    [HttpPost("register-admin")]
    [Authorize]
    public async Task<IActionResult> RegisterAdmin([FromBody] RegisterUserDto request)
    {
        try
        {
            if (!ModelState.IsValid)
                return BadRequest(new { success = false, message = "Datos inválidos" });

            var currentUserIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                                  ?? User.FindFirst("nameid")?.Value;
            if (!int.TryParse(currentUserIdClaim, out var currentUserId))
                return Unauthorized(new { success = false, message = "Token inválido" });

            // Registrar usuario (puede generar contraseña temporal automáticamente)
            var (user, temporaryPassword) = await _authService.RegisterUserAsync(request, currentUserId);

            // Si se generó una contraseña temporal, enviar correo de bienvenida
            if (!string.IsNullOrEmpty(temporaryPassword))
            {
                try
                {
                    await _emailNotificationService.SendWelcomeEmailWithTemporaryPasswordAsync(
                        user.Email,
                        user.FullName,
                        temporaryPassword
                    );

                    _logger.LogInformation(
                        "Usuario {Email} registrado con contraseña temporal y correo enviado exitosamente",
                        user.Email
                    );

                    return Ok(new
                    {
                        success = true,
                        message = "Usuario registrado exitosamente. Se ha enviado un correo con las credenciales de acceso.",
                        data = user
                    });
                }
                catch (Exception emailEx)
                {
                    _logger.LogError(
                        emailEx,
                        "Usuario {Email} registrado pero falló el envío de correo",
                        user.Email
                    );

                    return Ok(new
                    {
                        success = true,
                        message = "Usuario registrado exitosamente, pero hubo un problema al enviar el correo. Contacte al administrador.",
                        data = user,
                        warning = "No se pudo enviar el correo de bienvenida"
                    });
                }
            }

            // Usuario registrado con contraseña proporcionada (sin correo)
            return Ok(new
            {
                success = true,
                message = "Usuario registrado exitosamente",
                data = user
            });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { success = false, message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al registrar usuario");
            return StatusCode(500, new { success = false, message = "Error interno" });
        }
    }

    /// <summary>
    /// Refrescar token
    /// </summary>
    [HttpPost("refresh")]
    [AllowAnonymous]
    public async Task<IActionResult> RefreshToken([FromBody] RefreshTokenRequestDto request)
    {
        try
        {
            if (!ModelState.IsValid)
                return BadRequest(new { success = false, message = "Token de refresh requerido" });

            var result = await _authService.RefreshTokenAsync(request);
            return Ok(new { success = true, message = "Token refrescado", data = result });
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(new { success = false, message = ex.Message });
        }
        catch (Exception)
        {
            return StatusCode(500, new { success = false, message = "Error interno" });
        }
    }

    /// <summary>
    /// Verificar código de doble factor (2FA)
    /// </summary>
    [HttpPost("verify-2fa")]
    [AllowAnonymous]
    public async Task<IActionResult> VerifyTwoFactor([FromBody] FeborBack.Application.DTOs.Auth.VerifyTwoFactorDto request)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(request.SessionToken) || string.IsNullOrWhiteSpace(request.Code))
                return BadRequest(new { success = false, message = "El token de sesión y el código son requeridos." });

            var result = await _authService.VerifyTwoFactorAsync(request);
            return Ok(new { success = true, message = "Verificación exitosa", data = result });
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(new { success = false, message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error en verify-2fa");
            return StatusCode(500, new { success = false, message = "Error interno" });
        }
    }

    /// <summary>
    /// Cerrar sesión
    /// </summary>
    [HttpPost("logout")]
    [Authorize]
    public async Task<IActionResult> Logout([FromBody] RefreshTokenRequestDto request)
    {
        try
        {
            var result = await _authService.LogoutAsync(request.RefreshToken);
            return Ok(new { success = true, message = "Sesión cerrada" });
        }
        catch (Exception)
        {
            return StatusCode(500, new { success = false, message = "Error interno" });
        }
    }

    /// <summary>
    /// Cerrar todas las sesiones
    /// </summary>
    [HttpPost("logout-all")]
    [Authorize]
    public async Task<IActionResult> LogoutAll()
    {
        try
        {
            var currentUserIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!int.TryParse(currentUserIdClaim, out var currentUserId))
                return Unauthorized(new { success = false, message = "Token inválido" });

            var result = await _authService.LogoutAllAsync(currentUserId);
            return Ok(new { success = true, message = "Todas las sesiones cerradas" });
        }
        catch (Exception)
        {
            return StatusCode(500, new { success = false, message = "Error interno" });
        }
    }

    /// <summary>
    /// Obtener información del usuario actual
    /// </summary>
    [HttpGet("me")]
    [Authorize]
    public async Task<IActionResult> GetCurrentUser()
    {
        try
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!int.TryParse(userIdClaim, out var userId))
                return Unauthorized(new { success = false, message = "Token inválido" });

            var result = await _authService.GetUserInfoAsync(userId);
            if (result == null)
                return NotFound(new { success = false, message = "Usuario no encontrado" });

            return Ok(new { success = true, message = "Información obtenida", data = result });
        }
        catch (Exception)
        {
            return StatusCode(500, new { success = false, message = "Error interno" });
        }
    }

    /// <summary>
    /// Cambiar contraseña
    /// </summary>
    [HttpPost("change-password")]
    [Authorize]
    public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordDto request)
    {
        try
        {
            if (!ModelState.IsValid)
                return BadRequest(new { success = false, message = "Datos inválidos" });

            var currentUserIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!int.TryParse(currentUserIdClaim, out var currentUserId))
                return Unauthorized(new { success = false, message = "Token inválido" });

            var result = await _authService.ChangePasswordAsync(currentUserId, request.CurrentPassword, request.NewPassword);

            if (!result)
                return BadRequest(new { success = false, message = "No se pudo cambiar la contraseña" });

            return Ok(new { success = true, message = "Contraseña cambiada exitosamente" });
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(new { success = false, message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { success = false, message = ex.Message });
        }
        catch (Exception)
        {
            return StatusCode(500, new { success = false, message = "Error interno" });
        }
    }

    /// <summary>
    /// Resetear contraseña
    /// </summary>
    [HttpPost("reset-password")]
    [AllowAnonymous]
    public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordDto request)
    {
        try
        {
            if (!ModelState.IsValid)
                return BadRequest(new { success = false, message = "Email requerido" });

            var result = await _authService.ResetPasswordAsync(request.Email);

            // Siempre devolver éxito por seguridad
            return Ok(new { success = true, message = "Si el email existe, se ha enviado una contraseña temporal" });
        }
        catch (Exception)
        {
            return StatusCode(500, new { success = false, message = "Error interno" });
        }
    }

    /// <summary>
    /// Validar token
    /// </summary>
    [HttpPost("validate-token")]
    [AllowAnonymous]
    public async Task<IActionResult> ValidateToken([FromBody] ValidateTokenDto request)
    {
        try
        {
            if (string.IsNullOrEmpty(request.Token))
                return BadRequest(new { success = false, message = "Token requerido" });

            var isValid = await _authService.ValidateTokenAsync(request.Token);
            return Ok(new { success = true, message = "Token validado", data = new { isValid } });
        }
        catch (Exception)
        {
            return StatusCode(500, new { success = false, message = "Error interno" });
        }
    }

    /// <summary>
    /// Test de autenticación
    /// </summary>
    [HttpGet("test")]
    [Authorize]
    public IActionResult Test()
    {
        var userInfo = new
        {
            isAuthenticated = User.Identity?.IsAuthenticated,
            name = User.Identity?.Name,
            authenticationType = User.Identity?.AuthenticationType,
            claims = User.Claims.Select(c => new { c.Type, c.Value }).ToList(),
            roles = User.FindAll(ClaimTypes.Role).Select(c => c.Value).ToList()
        };

        return Ok(new
        {
            success = true,
            message = "Autenticado correctamente",
            data = userInfo
        });
    }
}