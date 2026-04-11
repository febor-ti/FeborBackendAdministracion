using FeborBack.Application.DTOs.Auth;
using FeborBack.Application.DTOs.User;

namespace FeborBack.Application.Services;

public interface IRoleService
{
    Task<IEnumerable<RoleDto>> GetAllRolesAsync();
    Task<IEnumerable<RoleDto>> GetActiveRolesAsync();
    Task<RoleDto?> GetRoleByIdAsync(int roleId);
    Task<RoleDto?> GetRoleByNameAsync(string roleName);
    Task<IEnumerable<UserByRoleDto>> GetUsersByRoleIdAsync(int roleId);
}