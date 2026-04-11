namespace FeborBack.Application.DTOs.User;

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

public class UserSearchResultDto
{
    public int UserId { get; set; }
    public string Username { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string StatusName { get; set; } = string.Empty;
}