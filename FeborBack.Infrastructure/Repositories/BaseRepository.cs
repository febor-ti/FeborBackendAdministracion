using System.Data;
using Dapper;
using Npgsql;

namespace FeborBack.Infrastructure.Repositories;

public abstract class BaseRepository
{
    protected readonly string _connectionString;

    protected BaseRepository(string connectionString)
    {
        _connectionString = connectionString ?? throw new ArgumentNullException(nameof(connectionString));
    }

    protected async Task<T?> CallFunction<T>(string functionName, object? parameters = null)
    {
        using var connection = new NpgsqlConnection(_connectionString);

        if (parameters == null)
        {
            return await connection.QueryFirstOrDefaultAsync<T>($"SELECT {functionName}()");
        }

        var paramNames = GetParameterNames(parameters);
        var paramString = string.Join(", ", paramNames.Select(p => $"@{p}"));

        return await connection.QueryFirstOrDefaultAsync<T>(
            $"SELECT {functionName}({paramString})",
            parameters);
    }

    protected async Task<IEnumerable<T>> CallTableFunction<T>(string functionName, object? parameters = null)
    {
        using var connection = new NpgsqlConnection(_connectionString);

        if (parameters == null)
        {
            return await connection.QueryAsync<T>($"SELECT * FROM {functionName}()");
        }

        var paramNames = GetParameterNames(parameters);
        var paramString = string.Join(", ", paramNames.Select(p => $"@{p}"));

        return await connection.QueryAsync<T>(
            $"SELECT * FROM {functionName}({paramString})",
            parameters);
    }

    protected async Task ExecuteFunction(string functionName, object? parameters = null)
    {
        using var connection = new NpgsqlConnection(_connectionString);

        if (parameters == null)
        {
            await connection.ExecuteAsync($"SELECT {functionName}()");
            return;
        }

        var paramNames = GetParameterNames(parameters);
        var paramString = string.Join(", ", paramNames.Select(p => $"@{p}"));

        await connection.ExecuteAsync(
            $"SELECT {functionName}({paramString})",
            parameters);
    }

    private static IEnumerable<string> GetParameterNames(object? parameters)
    {
        if (parameters == null)
            return Enumerable.Empty<string>();

        return parameters.GetType()
            .GetProperties()
            .Select(p => p.Name);
    }
}