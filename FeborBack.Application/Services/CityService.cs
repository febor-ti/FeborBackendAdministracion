using FeborBack.Infrastructure.DTOs.Catalog;
using FeborBack.Infrastructure.DTOs.Common;
using FeborBack.Infrastructure.Repositories;
using Microsoft.Extensions.Logging;

namespace FeborBack.Application.Services;

public class CityService : ICityService
{
    private readonly ICityRepository _repository;
    private readonly ILogger<CityService> _logger;

    public CityService(ICityRepository repository, ILogger<CityService> logger)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<PagedResultDto<CityPagedDto>> GetCitiesPagedAsync(PagedRequestDto request)
    {
        try
        {
            _logger.LogInformation("Getting cities paged. Page: {Page}, PageSize: {PageSize}, SearchTerm: {SearchTerm}",
                request.Page, request.PageSize, request.SearchTerm ?? "N/A");

            return await _repository.GetCitiesPagedAsync(request);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting cities paged");
            throw;
        }
    }

    public async Task<IEnumerable<CityMainDto>> GetMainCitiesAsync()
    {
        try
        {
            _logger.LogInformation("Getting main cities (capitals)");
            return await _repository.GetMainCitiesAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting main cities");
            return Enumerable.Empty<CityMainDto>();
        }
    }

    public async Task<IEnumerable<CityDto>> GetAllCitiesAsync()
    {
        try
        {
            _logger.LogInformation("Getting all cities");
            return await _repository.GetAllCitiesAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting all cities");
            return Enumerable.Empty<CityDto>();
        }
    }

    public async Task<CityDto?> GetCityByIdAsync(int id)
    {
        try
        {
            _logger.LogInformation("Getting city by ID: {CityId}", id);
            return await _repository.GetCityByIdAsync(id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting city by ID: {CityId}", id);
            return null;
        }
    }

    public async Task<CityDto?> GetCityByCodigoAsync(string codigo)
    {
        try
        {
            _logger.LogInformation("Getting city by codigo DIVIPOLA: {Codigo}", codigo);
            return await _repository.GetCityByCodigoAsync(codigo);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting city by codigo: {Codigo}", codigo);
            return null;
        }
    }

    public async Task<IEnumerable<CityDto>> GetCitiesByDepartmentAsync(string departamento)
    {
        try
        {
            _logger.LogInformation("Getting cities by department: {Departamento}", departamento);
            return await _repository.GetCitiesByDepartmentAsync(departamento);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting cities by department: {Departamento}", departamento);
            return Enumerable.Empty<CityDto>();
        }
    }

    public async Task<IEnumerable<CityDto>> SearchCitiesByNameAsync(string searchTerm)
    {
        try
        {
            _logger.LogInformation("Searching cities by name: {SearchTerm}", searchTerm);
            return await _repository.SearchCitiesByNameAsync(searchTerm);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error searching cities by name: {SearchTerm}", searchTerm);
            return Enumerable.Empty<CityDto>();
        }
    }

    public async Task<IEnumerable<string>> GetDepartmentsAsync()
    {
        try
        {
            _logger.LogInformation("Getting all departments");
            var cities = await _repository.GetAllCitiesAsync();
            return cities.Select(c => c.Departamento).Distinct().OrderBy(d => d);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting departments");
            return Enumerable.Empty<string>();
        }
    }
}