using Microsoft.Extensions.Configuration;
using FeborBack.Domain.Entities;
using FeborBack.Infrastructure.DTOs;

namespace FeborBack.Infrastructure.Repositories;

public class RoleRepository : BaseRepository, IRoleRepository
{
    public RoleRepository(IConfiguration configuration)
        : base(configuration.GetConnectionString("DefaultConnection")
               ?? throw new ArgumentNullException("ConnectionString no configurado"))
    {
    }

    public async Task<IEnumerable<Role>> GetAllRolesAsync()
    {
        var roleDtos = await CallTableFunction<RoleDto>("auth.get_all_roles");
        return roleDtos.Select(dto => dto.ToRole());
    }

    public async Task<IEnumerable<Role>> GetActiveRolesAsync()
    {
        var roleDtos = await CallTableFunction<RoleDto>("auth.get_active_roles");
        return roleDtos.Select(dto => dto.ToRole());
    }

    public async Task<Role?> GetRoleByIdAsync(int roleId)
    {
        var roleDto = await CallFunction<RoleDto>("auth.get_role_by_id", new { role_id = roleId });
        return roleDto?.ToRole();
    }

    public async Task<Role?> GetRoleByNameAsync(string roleName)
    {
        var roleDto = await CallFunction<RoleDto>("auth.get_role_by_name", new { role_name = roleName });
        return roleDto?.ToRole();
    }

    public async Task<IEnumerable<UserByRoleDbDto>> GetUsersByRoleIdAsync(int roleId)
    {
        return await CallTableFunction<UserByRoleDbDto>("auth.sp_get_users_by_role", new { p_role_id = roleId });
    }
}