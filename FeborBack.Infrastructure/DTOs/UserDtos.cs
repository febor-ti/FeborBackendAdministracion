namespace FeborBack.Infrastructure.DTOs;

/// <summary>
/// DTO para roles de usuario con detalles (Infrastructure)
/// </summary>
public class UserRoleDto
{
    public int RoleId { get; set; }
    public string RoleName { get; set; } = string.Empty;
    public string? Description { get; set; }
    public DateTime AssignedAt { get; set; }
    public int AssignedBy { get; set; }
    public string AssignedByName { get; set; } = string.Empty;
}

/// <summary>
/// DTO para estados de usuario (Infrastructure)
/// </summary>
public class StatusDto
{
    public int StatusId { get; set; }
    public string StatusName { get; set; } = string.Empty;
}

/// <summary>
/// DTO para razones de estado (Infrastructure)
/// </summary>
public class StatusReasonDto
{
    public int StatusReasonId { get; set; }
    public string ReasonName { get; set; } = string.Empty;
}

/// <summary>
/// DTO para estadísticas de usuarios (Infrastructure)
/// </summary>
public class UserStatisticsDto
{
    public int TotalUsers { get; set; }
    public int ActiveUsers { get; set; }
    public int BlockedUsers { get; set; }
    public int InactiveUsers { get; set; }
    public int UsersWithTempPassword { get; set; }
    public int UsersCreatedToday { get; set; }
    public int UsersCreatedThisWeek { get; set; }
    public int UsersCreatedThisMonth { get; set; }
    public decimal ActiveUsersPercentage { get; set; }
    public decimal BlockedUsersPercentage { get; set; }
}

/// <summary>
/// DTO para resultados de búsqueda (Infrastructure)
/// </summary>
public class UserSearchResultDto
{
    public int UserId { get; set; }
    public string Username { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string StatusName { get; set; } = string.Empty;
}