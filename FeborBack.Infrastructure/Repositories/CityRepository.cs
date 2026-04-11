using Dapper;
using FeborBack.Infrastructure.DTOs.Catalog;
using FeborBack.Infrastructure.DTOs.Common;
using Microsoft.Extensions.Configuration;
using Npgsql;

namespace FeborBack.Infrastructure.Repositories;

public class CityRepository : ICityRepository
{
    private readonly string _connectionString;

    public CityRepository(IConfiguration configuration)
    {
        _connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");
    }

    private async Task<T> CallFunction<T>(string functionName, object? parameters = null)
    {
        using var connection = new NpgsqlConnection(_connectionString);
        var paramList = parameters?.GetType().GetProperties().Select(p => p.Name) ?? Array.Empty<string>();
        var paramString = paramList.Any() ? $"(@{string.Join(", @", paramList)})" : "()";
        return await connection.QuerySingleAsync<T>($"SELECT {functionName}{paramString}", parameters);
    }

    private async Task<T?> CallFunctionOrDefault<T>(string functionName, object? parameters = null)
    {
        using var connection = new NpgsqlConnection(_connectionString);
        var paramList = parameters?.GetType().GetProperties().Select(p => p.Name) ?? Array.Empty<string>();
        var paramString = paramList.Any() ? $"(@{string.Join(", @", paramList)})" : "()";
        return await connection.QuerySingleOrDefaultAsync<T>($"SELECT {functionName}{paramString}", parameters);
    }

    private async Task<IEnumerable<T>> CallTableFunction<T>(string functionName, object? parameters = null)
    {
        using var connection = new NpgsqlConnection(_connectionString);
        var paramList = parameters?.GetType().GetProperties().Select(p => p.Name) ?? Array.Empty<string>();
        var paramString = paramList.Any() ? $"(@{string.Join(", @", paramList)})" : "()";
        return await connection.QueryAsync<T>($"SELECT * FROM {functionName}{paramString}", parameters);
    }

    public async Task<PagedResultDto<CityPagedDto>> GetCitiesPagedAsync(PagedRequestDto request)
    {
        var offset = (request.Page - 1) * request.PageSize;

        // Obtener el total de registros
        var totalCount = await CallFunction<int>("catalog.get_cities_count", new
        {
            SearchTerm = request.SearchTerm,
            StartDate = request.StartDate,
            EndDate = request.EndDate
        });

        // Obtener los datos paginados
        var items = await CallTableFunction<CityPagedDto>("catalog.get_cities_paged", new
        {
            PageSize = request.PageSize,
            Offset = offset,
            SearchTerm = request.SearchTerm,
            StartDate = request.StartDate,
            EndDate = request.EndDate,
            SortBy = request.SortBy ?? "nombre",
            SortDirection = request.SortDirection ?? "ASC"
        });

        return new PagedResultDto<CityPagedDto>
        {
            Items = items,
            TotalCount = totalCount,
            Page = request.Page,
            PageSize = request.PageSize
        };
    }

    public async Task<IEnumerable<CityMainDto>> GetMainCitiesAsync()
    {
        return await CallTableFunction<CityMainDto>("catalog.get_main_cities");
    }

    public async Task<IEnumerable<CityDto>> GetAllCitiesAsync()
    {
        return await CallTableFunction<CityDto>("catalog.get_all_cities");
    }

    public async Task<CityDto?> GetCityByIdAsync(int id)
    {
        var result = await CallTableFunction<CityDto>("catalog.get_city_by_id", new { Id = id });
        return result.FirstOrDefault();
    }

    public async Task<CityDto?> GetCityByCodigoAsync(string codigo)
    {
        var result = await CallTableFunction<CityDto>("catalog.get_city_by_codigo", new { Codigo = codigo });
        return result.FirstOrDefault();
    }

    public async Task<IEnumerable<CityDto>> GetCitiesByDepartmentAsync(string departamento)
    {
        return await CallTableFunction<CityDto>("catalog.get_cities_by_department", new { Departamento = departamento });
    }

    public async Task<IEnumerable<CityDto>> SearchCitiesByNameAsync(string searchTerm)
    {
        return await CallTableFunction<CityDto>("catalog.search_cities_by_name", new { SearchTerm = searchTerm });
    }
}