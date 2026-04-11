using FeborBack.Domain.Entities;

namespace FeborBack.Infrastructure.DTOs;

public class UserWithRolesClaimsDto : UserWithPersonDto
{
    public string roles { get; set; } = string.Empty;
    public string claims { get; set; } = string.Empty;

    public new LoginUser ToLoginUser()
    {
        var user = base.ToLoginUser();

        // Agregar roles
        if (!string.IsNullOrEmpty(roles))
        {
            var roleList = roles.Split(',', StringSplitOptions.RemoveEmptyEntries);
            user.UserRoles = roleList.Select(r => new UserRole
            {
                Role = new Role { RoleName = r.Trim() }
            }).ToList();
        }

        return user;
    }

    public List<string> GetRoles()
    {
        if (string.IsNullOrEmpty(roles))
            return new List<string>();

        return roles.Split(',', StringSplitOptions.RemoveEmptyEntries)
                   .Select(r => r.Trim())
                   .ToList();
    }

    public List<string> GetClaims()
    {
        if (string.IsNullOrEmpty(claims))
            return new List<string>();

        return claims.Split(',', StringSplitOptions.RemoveEmptyEntries)
                    .Select(c => c.Trim())
                    .ToList();
    }
}