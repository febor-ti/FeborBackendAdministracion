using Microsoft.Extensions.Configuration;
using FeborBack.Domain.Entities;
using FeborBack.Infrastructure.DTOs;

namespace FeborBack.Infrastructure.Repositories;

public class AuthRepository : BaseRepository, IAuthRepository
{
    public AuthRepository(IConfiguration configuration)
        : base(configuration.GetConnectionString("DefaultConnection")
               ?? throw new ArgumentNullException("ConnectionString no configurado"))
    {
    }

    #region User Management

    public async Task<LoginUser?> GetUserByEmailAsync(string email)
    {
        try
        {
            var result = await CallTableFunction<UserWithPersonDto>(
                "auth.sp_get_user_by_email",
                new { p_email = email });

            return result.FirstOrDefault()?.ToLoginUser();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error en GetUserByEmailAsync: {ex.Message}");
            throw;
        }
    }

    public async Task<LoginUser?> GetUserByIdAsync(int userId)
    {
        try
        {
            var result = await CallTableFunction<UserWithPersonDto>(
                "auth.sp_get_user_by_id",
                new { p_user_id = userId });

            return result.FirstOrDefault()?.ToLoginUser();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error en GetUserByIdAsync: {ex.Message}");
            throw;
        }
    }

    public async Task<LoginUser?> GetUserByUsernameAsync(string username)
    {
        try
        {
            var result = await CallTableFunction<UserWithPersonDto>(
                "auth.sp_get_user_by_username",
                new { p_username = username });

            return result.FirstOrDefault()?.ToLoginUser();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error en GetUserByUsernameAsync: {ex.Message}");
            throw;
        }
    }

    public async Task<LoginUser?> GetUserWithRolesAndClaimsAsync(int userId)
    {
        try
        {
            // Usar método simplificado por ahora
            var user = await GetUserByIdAsync(userId);
            return user;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error en GetUserWithRolesAndClaimsAsync: {ex.Message}");
            throw;
        }
    }

    public async Task<LoginUser> CreateUserAsync(LoginUser user)
    {
        try
        {
            var result = await CallTableFunction<CreateUserResultDto>(
                "auth.sp_create_user",
                new
                {
                    p_full_name = user.Person.FullName,
                    p_username = user.Username,
                    p_email = user.Email,
                    p_password_hash = user.PasswordHash,
                    p_password_salt = user.PasswordSalt,
                    p_is_temporary_password = user.IsTemporaryPassword,
                    p_created_by = user.CreatedBy,
                    p_status_id = user.StatusId
                });

            var createResult = result.First();
            user.UserId = createResult.user_id;
            user.PersonId = createResult.person_id;

            return user;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error en CreateUserAsync: {ex.Message}");
            throw;
        }
    }

    public async Task UpdateUserAsync(LoginUser user)
    {
        try
        {
            await ExecuteFunction("auth.sp_update_user", new
            {
                p_user_id = user.UserId,
                p_email = user.Email,
                p_password_hash = user.PasswordHash,
                p_password_salt = user.PasswordSalt,
                p_is_session_active = user.IsSessionActive,
                p_is_temporary_password = user.IsTemporaryPassword,
                p_failed_attempts = user.FailedAttempts,
                p_last_access_at = user.LastAccessAt,
                p_avatar_name = user.AvatarName,
                p_status_id = user.StatusId,
                p_status_reason_id = user.StatusReasonId,
                p_updated_by = user.UpdatedBy
            });

            // Actualizar persona si existe
            if (user.Person != null)
            {
                await ExecuteFunction("auth.sp_update_person", new
                {
                    p_person_id = user.PersonId,
                    p_full_name = user.Person.FullName
                });
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error en UpdateUserAsync: {ex.Message}");
            throw;
        }
    }

    public async Task<bool> EmailExistsAsync(string email)
    {
        try
        {
            var result = await CallFunction<bool>("auth.sp_email_exists",
                new { p_email = email });
            return result;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error en EmailExistsAsync: {ex.Message}");
            return false;
        }
    }

    public async Task<bool> UsernameExistsAsync(string username)
    {
        try
        {
            var result = await CallFunction<bool>("auth.sp_username_exists",
                new { p_username = username });
            return result;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error en UsernameExistsAsync: {ex.Message}");
            return false;
        }
    }

    #endregion

    #region Roles and Claims

    public async Task<List<string>> GetUserRolesAsync(int userId)
    {
        try
        {
            var result = await CallTableFunction<dynamic>(
                "auth.sp_get_user_roles",
                new { p_user_id = userId });

            return result.Select(r => (string)r.role_name).ToList();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error en GetUserRolesAsync: {ex.Message}");
            return new List<string>();
        }
    }

    public async Task<List<int>> GetUserRoleIdsAsync(int userId)
    {
        try
        {
            var result = await CallTableFunction<dynamic>(
                "auth.sp_get_user_roles",
                new { p_user_id = userId });

            return result.Select(r => (int)r.role_id).ToList();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error en GetUserRoleIdsAsync: {ex.Message}");
            return new List<int>();
        }
    }

    public async Task<List<string>> GetUserClaimsAsync(int userId)
    {
        try
        {
            var result = await CallTableFunction<dynamic>(
                "auth.sp_get_user_claims",
                new { p_user_id = userId });

            return result.Select(c => (string)c.claim).ToList();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error en GetUserClaimsAsync: {ex.Message}");
            return new List<string>();
        }
    }

    public async Task AssignRolesToUserAsync(int userId, List<int> roleIds, int assignedBy)
    {
        try
        {
            await ExecuteFunction("auth.sp_assign_user_roles", new
            {
                p_user_id = userId,
                p_role_ids = roleIds.ToArray(),
                p_assigned_by = assignedBy
            });
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error en AssignRolesToUserAsync: {ex.Message}");
            throw;
        }
    }

    public async Task<List<Role>> GetRolesByIdsAsync(List<int> roleIds)
    {
        try
        {
            var result = await CallTableFunction<RoleDto>(
                "auth.sp_get_roles_by_ids",
                new { p_role_ids = roleIds.ToArray() });

            return result.Select(r => r.ToRole()).ToList();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error en GetRolesByIdsAsync: {ex.Message}");
            return new List<Role>();
        }
    }

    #endregion

    #region Refresh Tokens

    public async Task<RefreshToken?> GetRefreshTokenAsync(string token)
    {
        try
        {
            var result = await CallTableFunction<RefreshTokenDto>(
                "auth.sp_get_refresh_token",
                new { p_token = token });

            return result.FirstOrDefault()?.ToRefreshToken();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error en GetRefreshTokenAsync: {ex.Message}");
            return null;
        }
    }

    public async Task<RefreshToken> CreateRefreshTokenAsync(RefreshToken refreshToken)
    {
        try
        {
            var tokenId = await CallFunction<int>("auth.sp_create_refresh_token", new
            {
                p_user_id = refreshToken.UserId,
                p_token = refreshToken.Token,
                p_expires_at = refreshToken.ExpiresAt
            });

            refreshToken.TokenId = tokenId;
            return refreshToken;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error en CreateRefreshTokenAsync: {ex.Message}");
            throw;
        }
    }

    public async Task UpdateRefreshTokenAsync(RefreshToken refreshToken)
    {
        try
        {
            await ExecuteFunction("auth.sp_update_refresh_token", new
            {
                p_token_id = refreshToken.TokenId,
                p_revoked_at = refreshToken.RevokedAt,
                p_is_used = refreshToken.IsUsed,
                p_replaced_by_token = refreshToken.ReplacedByToken
            });
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error en UpdateRefreshTokenAsync: {ex.Message}");
            throw;
        }
    }

    public async Task RevokeAllUserRefreshTokensAsync(int userId)
    {
        try
        {
            await ExecuteFunction("auth.sp_revoke_user_refresh_tokens",
                new { p_user_id = userId });
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error en RevokeAllUserRefreshTokensAsync: {ex.Message}");
        }
    }

    public async Task CleanupExpiredRefreshTokensAsync()
    {
        try
        {
            await ExecuteFunction("auth.sp_cleanup_expired_refresh_tokens");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error en CleanupExpiredRefreshTokensAsync: {ex.Message}");
        }
    }

    #endregion

    #region Métodos Básicos Adicionales

    public Task<List<LoginUser>> GetAllUsersAsync(UserFilterDto filter)
    {
        return Task.FromResult(new List<LoginUser>());
    }

    public async Task RemoveUserRolesAsync(int userId)
    {
        try
        {
            await ExecuteFunction("auth.sp_remove_user_roles",
                new { p_user_id = userId });
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error en RemoveUserRolesAsync: {ex.Message}");
            throw;
        }
    }

    public async Task<Role?> GetRoleByIdAsync(int roleId)
    {
        try
        {
            var result = await CallTableFunction<RoleDto>(
                "auth.sp_get_role_by_id",
                new { p_role_id = roleId });

            return result.FirstOrDefault()?.ToRole();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error en GetRoleByIdAsync: {ex.Message}");
            return null;
        }
    }

    public async Task<List<Role>> GetAllRolesAsync()
    {
        try
        {
            var result = await CallTableFunction<RoleDto>("auth.sp_get_roles");
            return result.Select(r => r.ToRole()).ToList();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error en GetAllRolesAsync: {ex.Message}");
            return new List<Role>();
        }
    }

    public async Task<Role> CreateRoleAsync(Role role)
    {
        try
        {
            var roleId = await CallFunction<int>("auth.sp_create_role", new
            {
                p_role_name = role.RoleName,
                p_description = role.Description,
                p_is_active = role.IsActive
            });

            role.RoleId = roleId;
            return role;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error en CreateRoleAsync: {ex.Message}");
            throw;
        }
    }

    public async Task UpdateRoleAsync(Role role)
    {
        try
        {
            await ExecuteFunction("auth.sp_update_role", new
            {
                p_role_id = role.RoleId,
                p_role_name = role.RoleName,
                p_description = role.Description,
                p_is_active = role.IsActive
            });
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error en UpdateRoleAsync: {ex.Message}");
            throw;
        }
    }

    public async Task<RoleClaim> CreateRoleClaimAsync(RoleClaim roleClaim)
    {
        try
        {
            var claimId = await CallFunction<int>("auth.sp_create_role_claim", new
            {
                p_role_id = roleClaim.RoleId,
                p_claim_type = roleClaim.ClaimType,
                p_claim_value = roleClaim.ClaimValue
            });

            roleClaim.ClaimId = claimId;
            return roleClaim;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error en CreateRoleClaimAsync: {ex.Message}");
            throw;
        }
    }

    public async Task<Person> CreatePersonAsync(Person person)
    {
        try
        {
            var personId = await CallFunction<int>("auth.sp_create_person",
                new { p_full_name = person.FullName });

            person.PersonId = personId;
            return person;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error en CreatePersonAsync: {ex.Message}");
            throw;
        }
    }

    public async Task<Person?> GetPersonByIdAsync(int personId)
    {
        try
        {
            var result = await CallTableFunction<Person>(
                "auth.sp_get_person_by_id",
                new { p_person_id = personId });

            return result.FirstOrDefault();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error en GetPersonByIdAsync: {ex.Message}");
            return null;
        }
    }

    #endregion
}