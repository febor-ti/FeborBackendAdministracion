namespace FeborBack.Application.DTOs.Auth;

public class RoleDto
{
    public int RoleId { get; set; }
    public string RoleName { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsActive { get; set; }
    public List<ClaimDto> Claims { get; set; } = new();
}

public class ClaimDto
{
    public string ClaimType { get; set; } = string.Empty;
    public string ClaimValue { get; set; } = string.Empty;
}

public class CreateRoleDto
{
    public string RoleName { get; set; } = string.Empty;
    public string? Description { get; set; }
    public List<CreateClaimDto> Claims { get; set; } = new();
}

public class CreateClaimDto
{
    public string ClaimType { get; set; } = string.Empty;
    public string ClaimValue { get; set; } = string.Empty;
}