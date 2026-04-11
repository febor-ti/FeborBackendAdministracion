using FeborBack.Infrastructure.DTOs.Catalog;
using FeborBack.Infrastructure.DTOs.Common;

namespace FeborBack.Application.Services;

public interface ICityService
{
    /// <summary>
    /// Obtener todas las ciudades con paginación
    /// </summary>
    Task<PagedResultDto<CityPagedDto>> GetCitiesPagedAsync(PagedRequestDto request);

    /// <summary>
    /// Obtener ciudades principales (capitales de departamento)
    /// </summary>
    Task<IEnumerable<CityMainDto>> GetMainCitiesAsync();

    /// <summary>
    /// Obtener todas las ciudades
    /// </summary>
    Task<IEnumerable<CityDto>> GetAllCitiesAsync();

    /// <summary>
    /// Obtener ciudad por ID
    /// </summary>
    Task<CityDto?> GetCityByIdAsync(int id);

    /// <summary>
    /// Obtener ciudad por código DIVIPOLA
    /// </summary>
    Task<CityDto?> GetCityByCodigoAsync(string codigo);

    /// <summary>
    /// Obtener ciudades por departamento
    /// </summary>
    Task<IEnumerable<CityDto>> GetCitiesByDepartmentAsync(string departamento);

    /// <summary>
    /// Buscar ciudades por nombre
    /// </summary>
    Task<IEnumerable<CityDto>> SearchCitiesByNameAsync(string searchTerm);

    /// <summary>
    /// Obtener lista de departamentos únicos
    /// </summary>
    Task<IEnumerable<string>> GetDepartmentsAsync();
}