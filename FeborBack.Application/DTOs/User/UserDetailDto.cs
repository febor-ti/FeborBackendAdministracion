namespace FeborBack.Application.DTOs.User;

public class UserDetailDto
{
    public int UserId { get; set; }
    public string Username { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string? AvatarName { get; set; }
    public int StatusId { get; set; }
    public string StatusName { get; set; } = string.Empty;
    public int? StatusReasonId { get; set; }
    public string? StatusReasonName { get; set; }
    public bool IsSessionActive { get; set; }
    public bool IsTemporaryPassword { get; set; }
    public int FailedAttempts { get; set; }
    public DateTime? LastAccessAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public List<UserRoleDto> Roles { get; set; } = new();
    public List<string> Claims { get; set; } = new();
}

public class UserRoleDto
{
    public int RoleId { get; set; }
    public string RoleName { get; set; } = string.Empty;
    public string? Description { get; set; }
    public DateTime AssignedAt { get; set; }
    public int AssignedBy { get; set; }
    public string AssignedByName { get; set; } = string.Empty;
}

public class PagedUsersDto
{
    public List<UserDetailDto> Users { get; set; } = new();
    public int TotalCount { get; set; }
    public int PageNumber { get; set; }
    public int PageSize { get; set; }
    public int TotalPages { get; set; }
    public bool HasNextPage { get; set; }
    public bool HasPreviousPage { get; set; }
}

public class BlockUserDto
{
    public int UserId { get; set; }
    public int StatusReasonId { get; set; }
    public string? Comment { get; set; }
}

public class AssignRolesDto
{
    public int UserId { get; set; }
    public List<int> RoleIds { get; set; } = new();
}

/// <summary>
/// DTO para usuarios obtenidos por rol específico
/// </summary>
public class UserByRoleDto
{
    public int UserId { get; set; }
    public int PersonId { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Username { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime RoleAssignedAt { get; set; }
}