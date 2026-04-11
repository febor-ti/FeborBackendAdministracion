using Microsoft.AspNetCore.Mvc;
using FeborBack.Application.Services;
using FeborBack.Infrastructure.DTOs.Common;

namespace FeborBack.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class CityController : ControllerBase
{
    private readonly ICityService _cityService;
    private readonly ILogger<CityController> _logger;

    public CityController(ICityService cityService, ILogger<CityController> logger)
    {
        _cityService = cityService ?? throw new ArgumentNullException(nameof(cityService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Obtener ciudades principales (capitales de departamento)
    /// </summary>
    [HttpGet("main")]
    public async Task<IActionResult> GetMainCities()
    {
        try
        {
            var cities = await _cityService.GetMainCitiesAsync();

            return Ok(new
            {
                success = true,
                message = "Ciudades principales obtenidas exitosamente",
                data = cities
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al obtener ciudades principales");
            return StatusCode(500, new
            {
                success = false,
                message = "Error interno del servidor"
            });
        }
    }

    /// <summary>
    /// Obtener todas las ciudades con paginación
    /// </summary>
    [HttpGet("all")]
    public async Task<IActionResult> GetAllCitiesPaged([FromQuery] PagedRequestDto request)
    {
        try
        {
            if (request.PageSize < 10)
                request.PageSize = 10;
            if (request.PageSize > 100)
                request.PageSize = 100;

            var result = await _cityService.GetCitiesPagedAsync(request);

            return Ok(new
            {
                success = true,
                message = "Ciudades obtenidas exitosamente",
                data = result
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al obtener ciudades paginadas");
            return StatusCode(500, new
            {
                success = false,
                message = "Error interno del servidor"
            });
        }
    }

    /// <summary>
    /// Obtener ciudad por ID
    /// </summary>
    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetCityById(int id)
    {
        try
        {
            var city = await _cityService.GetCityByIdAsync(id);

            if (city == null)
            {
                return NotFound(new
                {
                    success = false,
                    message = "Ciudad no encontrada"
                });
            }

            return Ok(new
            {
                success = true,
                message = "Ciudad obtenida exitosamente",
                data = city
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al obtener ciudad por ID: {CityId}", id);
            return StatusCode(500, new
            {
                success = false,
                message = "Error interno del servidor"
            });
        }
    }

    /// <summary>
    /// Obtener ciudad por código DIVIPOLA
    /// </summary>
    [HttpGet("codigo/{codigo}")]
    public async Task<IActionResult> GetCityByCodigo(string codigo)
    {
        try
        {
            var city = await _cityService.GetCityByCodigoAsync(codigo);

            if (city == null)
            {
                return NotFound(new
                {
                    success = false,
                    message = "Ciudad no encontrada"
                });
            }

            return Ok(new
            {
                success = true,
                message = "Ciudad obtenida exitosamente",
                data = city
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al obtener ciudad por código: {Codigo}", codigo);
            return StatusCode(500, new
            {
                success = false,
                message = "Error interno del servidor"
            });
        }
    }

    /// <summary>
    /// Obtener ciudades por departamento
    /// </summary>
    [HttpGet("department/{departamento}")]
    public async Task<IActionResult> GetCitiesByDepartment(string departamento)
    {
        try
        {
            var cities = await _cityService.GetCitiesByDepartmentAsync(departamento);

            return Ok(new
            {
                success = true,
                message = $"Ciudades del departamento {departamento} obtenidas exitosamente",
                data = cities
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al obtener ciudades por departamento: {Departamento}", departamento);
            return StatusCode(500, new
            {
                success = false,
                message = "Error interno del servidor"
            });
        }
    }

    /// <summary>
    /// Buscar ciudades por nombre
    /// </summary>
    [HttpGet("search")]
    public async Task<IActionResult> SearchCitiesByName([FromQuery] string searchTerm)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(searchTerm))
            {
                return BadRequest(new
                {
                    success = false,
                    message = "El término de búsqueda es requerido"
                });
            }

            var cities = await _cityService.SearchCitiesByNameAsync(searchTerm);

            return Ok(new
            {
                success = true,
                message = "Búsqueda de ciudades realizada exitosamente",
                data = cities
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al buscar ciudades por nombre: {SearchTerm}", searchTerm);
            return StatusCode(500, new
            {
                success = false,
                message = "Error interno del servidor"
            });
        }
    }

    /// <summary>
    /// Obtener lista de departamentos
    /// </summary>
    [HttpGet("departments")]
    public async Task<IActionResult> GetDepartments()
    {
        try
        {
            var departments = await _cityService.GetDepartmentsAsync();

            return Ok(new
            {
                success = true,
                message = "Departamentos obtenidos exitosamente",
                data = departments
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al obtener departamentos");
            return StatusCode(500, new
            {
                success = false,
                message = "Error interno del servidor"
            });
        }
    }
}