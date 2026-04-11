using FeborBack.Domain.Entities;
using FeborBack.Infrastructure.DTOs; 

namespace FeborBack.Infrastructure.Repositories;

public interface IAuthRepository
{
    // User Management
    Task<LoginUser?> GetUserByUsernameAsync(string username);
    Task<LoginUser?> GetUserByEmailAsync(string email);
    Task<LoginUser?> GetUserByIdAsync(int userId);
    Task<LoginUser?> GetUserWithRolesAndClaimsAsync(int userId);
    Task<LoginUser> CreateUserAsync(LoginUser user);
    Task UpdateUserAsync(LoginUser user);
    Task<bool> UsernameExistsAsync(string username);
    Task<bool> EmailExistsAsync(string email);
    Task<List<LoginUser>> GetAllUsersAsync(UserFilterDto filter); // ← AHORA FUNCIONA

    // Refresh Tokens
    Task<RefreshToken?> GetRefreshTokenAsync(string token);
    Task<RefreshToken> CreateRefreshTokenAsync(RefreshToken refreshToken);
    Task UpdateRefreshTokenAsync(RefreshToken refreshToken);
    Task RevokeAllUserRefreshTokensAsync(int userId);
    Task CleanupExpiredRefreshTokensAsync();

    // Roles and Claims
    Task<List<string>> GetUserRolesAsync(int userId);
    Task<List<int>> GetUserRoleIdsAsync(int userId);
    Task<List<string>> GetUserClaimsAsync(int userId);
    Task AssignRolesToUserAsync(int userId, List<int> roleIds, int assignedBy);
    Task RemoveUserRolesAsync(int userId);
    Task<Role?> GetRoleByIdAsync(int roleId);
    Task<List<Role>> GetRolesByIdsAsync(List<int> roleIds);
    Task<List<Role>> GetAllRolesAsync();
    Task<Role> CreateRoleAsync(Role role);
    Task UpdateRoleAsync(Role role);
    Task<RoleClaim> CreateRoleClaimAsync(RoleClaim roleClaim);

    // Person Management
    Task<Person> CreatePersonAsync(Person person);
    Task<Person?> GetPersonByIdAsync(int personId);
}