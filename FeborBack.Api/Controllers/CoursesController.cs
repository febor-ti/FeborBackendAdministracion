using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using FeborBack.Application.Services.Courses;
using FeborBack.Application.DTOs.Courses;
using FeborBack.Api.Authorization;
using System.Security.Claims;

namespace FeborBack.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class CoursesController : ControllerBase
{
    private readonly ICourseService _courseService;
    private readonly ILogger<CoursesController> _logger;

    public CoursesController(ICourseService courseService, ILogger<CoursesController> logger)
    {
        _courseService = courseService;
        _logger        = logger;
    }

    /// <summary>
    /// Lista todos los cursos publicados.
    /// </summary>
    [HttpGet]
    [MenuAuthorize("manage", "courses")]
    public async Task<ActionResult<object>> GetAll()
    {
        try
        {
            var courses = await _courseService.GetAllAsync();
            return Ok(new { success = true, message = "OK", data = courses });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error obteniendo cursos");
            return StatusCode(500, new { success = false, message = "Error interno del servidor" });
        }
    }

    /// <summary>
    /// Publica un nuevo curso subiendo un archivo HTML.
    /// </summary>
    [HttpPost]
    [MenuAuthorize("manage", "courses")]
    [RequestSizeLimit(20_000_000)]
    public async Task<ActionResult<object>> Create([FromForm] CreateCourseDto dto, IFormFile file)
    {
        if (!ModelState.IsValid)
            return BadRequest(new { success = false, message = "Datos inválidos", errors = ModelState });

        if (file == null || file.Length == 0)
            return BadRequest(new { success = false, message = "El archivo HTML es requerido" });

        try
        {
            var userId = GetUserId();
            await using var stream = file.OpenReadStream();
            var course = await _courseService.CreateAsync(dto, stream, file.FileName, userId);
            _logger.LogInformation("Curso '{Slug}' publicado por usuario {UserId}", course.Slug, userId);
            return Ok(new { success = true, message = $"Curso '{course.Name}' publicado correctamente", data = course });
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
            _logger.LogError(ex, "Error publicando curso '{Slug}'", dto.Slug);
            return StatusCode(500, new { success = false, message = "Error interno del servidor" });
        }
    }

    /// <summary>
    /// Actualiza nombre/descripción y opcionalmente reemplaza el HTML.
    /// </summary>
    [HttpPut("{id:int}")]
    [MenuAuthorize("manage", "courses")]
    [RequestSizeLimit(20_000_000)]
    public async Task<ActionResult<object>> Update(int id, [FromForm] UpdateCourseDto dto, IFormFile? file)
    {
        if (!ModelState.IsValid)
            return BadRequest(new { success = false, message = "Datos inválidos", errors = ModelState });

        try
        {
            var userId = GetUserId();
            Stream? stream   = file != null ? file.OpenReadStream() : null;
            string? fileName = file?.FileName;

            await using (stream)
            {
                var course = await _courseService.UpdateAsync(id, dto, stream, fileName, userId);
                _logger.LogInformation("Curso {Id} actualizado por usuario {UserId}", id, userId);
                return Ok(new { success = true, message = "Curso actualizado correctamente", data = course });
            }
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { success = false, message = ex.Message });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { success = false, message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error actualizando curso {Id}", id);
            return StatusCode(500, new { success = false, message = "Error interno del servidor" });
        }
    }

    /// <summary>
    /// Elimina un curso y su archivo del servidor.
    /// </summary>
    [HttpDelete("{id:int}")]
    [MenuAuthorize("manage", "courses")]
    public async Task<ActionResult<object>> Delete(int id)
    {
        try
        {
            var userId = GetUserId();
            await _courseService.DeleteAsync(id, userId);
            _logger.LogInformation("Curso {Id} eliminado por usuario {UserId}", id, userId);
            return Ok(new { success = true, message = "Curso eliminado correctamente" });
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { success = false, message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error eliminando curso {Id}", id);
            return StatusCode(500, new { success = false, message = "Error interno del servidor" });
        }
    }

    /// <summary>
    /// Sube o reemplaza la página de error 404 del servidor.
    /// </summary>
    [HttpPost("error-pages/not-found")]
    [MenuAuthorize("manage", "courses")]
    [RequestSizeLimit(5_000_000)]
    public async Task<ActionResult<object>> UploadNotFoundPage(IFormFile file)
    {
        if (file == null || file.Length == 0)
            return BadRequest(new { success = false, message = "El archivo HTML es requerido" });

        try
        {
            await using var stream = file.OpenReadStream();
            await _courseService.UploadErrorPageAsync(stream, file.FileName);
            _logger.LogInformation("Página 404 actualizada por usuario {UserId}", GetUserId());
            return Ok(new { success = true, message = "Página 404 actualizada correctamente" });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { success = false, message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return StatusCode(500, new { success = false, message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error actualizando página 404");
            return StatusCode(500, new { success = false, message = "Error interno del servidor" });
        }
    }

    private int GetUserId()
    {
        var claim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return int.TryParse(claim, out var id) ? id : 0;
    }
}
