namespace FeborBack.Infrastructure.DTOs;

public class UserFilterDto
{
    public string? Username { get; set; }
    public string? Email { get; set; }
    public string? FullName { get; set; }
    public int? StatusId { get; set; }
    public List<int>? RoleIds { get; set; }           // ⚠️ AGREGAR ESTA
    public DateTime? CreatedFrom { get; set; }        // ⚠️ AGREGAR ESTA
    public DateTime? CreatedTo { get; set; }          // ⚠️ AGREGAR ESTA
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 10;
    public string SortBy { get; set; } = "CreatedAt"; // ⚠️ QUITAR ? (nullable)
    public bool SortDescending { get; set; } = true;
}