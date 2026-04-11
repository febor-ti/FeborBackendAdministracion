namespace FeborBack.Application.DTOs.Common;

/// <summary>
/// DTO genérico para respuestas paginadas
/// </summary>
public class PagedResultDto<T>
{
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalCount { get; set; }
    public int TotalPages { get; set; }
    public IEnumerable<T> Items { get; set; } = new List<T>();
}
