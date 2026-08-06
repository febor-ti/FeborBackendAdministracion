using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using FeborBack.Application.Services.Forms;
using FeborBack.Application.DTOs.Forms;
using FeborBack.Api.Authorization;
using System.Security.Claims;

namespace FeborBack.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class FormsController : ControllerBase
{
    private readonly IFormService _formService;
    private readonly ILogger<FormsController> _logger;

    public FormsController(IFormService formService, ILogger<FormsController> logger)
    {
        _formService = formService;
        _logger      = logger;
    }

    /// <summary>
    /// Lista todos los formularios publicados.
    /// </summary>
    [HttpGet]
    [MenuAuthorize("manage", "forms")]
    public async Task<ActionResult<object>> GetAll()
    {
        try
        {
            var forms = await _formService.GetAllAsync();
            return Ok(new { success = true, message = "OK", data = forms });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error obteniendo formularios");
            return StatusCode(500, new { success = false, message = "Error interno del servidor" });
        }
    }

    /// <summary>
    /// Publica un nuevo formulario. El archivo HTML se recibe en Base64 para evitar
    /// que el WAF de GoDaddy bloquee el request al detectar HTML crudo (XSS014).
    /// </summary>
    [HttpPost]
    [MenuAuthorize("manage", "forms")]
    [RequestSizeLimit(20_000_000)]
    public async Task<ActionResult<object>> Create([FromForm] CreateFormPageDto dto,
        [FromForm] string fileBase64, [FromForm] string fileName)
    {
        if (!ModelState.IsValid)
            return BadRequest(new { success = false, message = "Datos inválidos", errors = ModelState });

        if (string.IsNullOrWhiteSpace(fileBase64))
            return BadRequest(new { success = false, message = "El archivo HTML es requerido" });

        try
        {
            var userId = GetUserId();
            var bytes  = Convert.FromBase64String(fileBase64);
            await using var stream = new MemoryStream(bytes);
            var form = await _formService.CreateAsync(dto, stream, fileName, userId);
            _logger.LogInformation("Formulario '{Slug}' publicado por usuario {UserId}", form.Slug, userId);
            return Ok(new { success = true, message = $"Formulario '{form.Name}' publicado correctamente", data = form });
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { success = false, message = ex.Message });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { success = false, message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error publicando formulario '{Slug}'", dto.Slug);
            return StatusCode(500, new { success = false, message = ex.Message });
        }
    }

    /// <summary>
    /// Actualiza nombre/descripción/slug y opcionalmente reemplaza el HTML (en Base64).
    /// </summary>
    [HttpPut("{id:int}")]
    [MenuAuthorize("manage", "forms")]
    [RequestSizeLimit(20_000_000)]
    public async Task<ActionResult<object>> Update(int id, [FromForm] UpdateFormPageDto dto,
        [FromForm] string? fileBase64, [FromForm] string? fileName)
    {
        if (!ModelState.IsValid)
            return BadRequest(new { success = false, message = "Datos inválidos", errors = ModelState });

        try
        {
            var userId = GetUserId();
            Stream? stream = null;

            if (!string.IsNullOrWhiteSpace(fileBase64))
            {
                var bytes = Convert.FromBase64String(fileBase64);
                stream = new MemoryStream(bytes);
            }

            await using (stream)
            {
                var form = await _formService.UpdateAsync(id, dto, stream, fileName, userId);
                _logger.LogInformation("Formulario {Id} actualizado por usuario {UserId}", id, userId);
                return Ok(new { success = true, message = "Formulario actualizado correctamente", data = form });
            }
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { success = false, message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { success = false, message = ex.Message });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { success = false, message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error actualizando formulario {Id}", id);
            return StatusCode(500, new { success = false, message = ex.Message });
        }
    }

    /// <summary>
    /// Activa o desactiva un formulario sin eliminarlo.
    /// </summary>
    [HttpPatch("{id:int}/toggle-active")]
    [MenuAuthorize("manage", "forms")]
    public async Task<ActionResult<object>> ToggleActive(int id)
    {
        try
        {
            var userId = GetUserId();
            var form   = await _formService.ToggleActiveAsync(id, userId);
            var estado = form.IsActive ? "activado" : "desactivado";
            _logger.LogInformation("Formulario {Id} {Estado} por usuario {UserId}", id, estado, userId);
            return Ok(new { success = true, message = $"Formulario '{form.Name}' {estado} correctamente", data = form });
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { success = false, message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogError(ex, "Error de configuración al cambiar estado del formulario {Id}", id);
            return StatusCode(500, new { success = false, message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error cambiando estado del formulario {Id}", id);
            return StatusCode(500, new { success = false, message = ex.Message });
        }
    }

    /// <summary>
    /// Elimina un formulario y su archivo del servidor.
    /// </summary>
    [HttpDelete("{id:int}")]
    [MenuAuthorize("manage", "forms")]
    public async Task<ActionResult<object>> Delete(int id)
    {
        try
        {
            var userId = GetUserId();
            await _formService.DeleteAsync(id, userId);
            _logger.LogInformation("Formulario {Id} eliminado por usuario {UserId}", id, userId);
            return Ok(new { success = true, message = "Formulario eliminado correctamente" });
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { success = false, message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error eliminando formulario {Id}", id);
            return StatusCode(500, new { success = false, message = "Error interno del servidor" });
        }
    }

    private int GetUserId()
    {
        var claim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return int.TryParse(claim, out var id) ? id : 0;
    }
}
