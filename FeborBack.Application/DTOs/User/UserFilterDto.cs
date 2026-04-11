namespace FeborBack.Application.DTOs.User;

public class UserFilterDto
{
    public string? Username { get; set; }
    public string? Email { get; set; }
    public string? FullName { get; set; }
    public int? StatusId { get; set; }
    public List<int>? RoleIds { get; set; }
    public DateTime? CreatedFrom { get; set; }
    public DateTime? CreatedTo { get; set; }
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 10;
    public string SortBy { get; set; } = "CreatedAt";
    public bool SortDescending { get; set; } = true;
}