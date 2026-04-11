using FeborBack.Domain.Entities;
using FeborBack.Infrastructure.DTOs;

namespace FeborBack.Infrastructure.Repositories;

public interface IRoleRepository
{
    Task<IEnumerable<Role>> GetAllRolesAsync();
    Task<IEnumerable<Role>> GetActiveRolesAsync();
    Task<Role?> GetRoleByIdAsync(int roleId);
    Task<Role?> GetRoleByNameAsync(string roleName);
    Task<IEnumerable<UserByRoleDbDto>> GetUsersByRoleIdAsync(int roleId);
}