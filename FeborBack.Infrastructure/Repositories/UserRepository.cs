using Microsoft.Extensions.Configuration;
using FeborBack.Infrastructure.DTOs;
using FeborBack.Domain.Entities;

namespace FeborBack.Infrastructure.Repositories;

public class UserRepository : BaseRepository, IUserRepository
{
    public UserRepository(IConfiguration configuration)
        : base(configuration.GetConnectionString("DefaultConnection")
               ?? throw new ArgumentNullException("ConnectionString no configurado"))
    {
    }

    public async Task<(List<LoginUser> users, int totalCount)> GetUsersPagedAsync(UserFilterDto filter)
    {
        try
        {
            var result = await CallTableFunction<UserWithPersonDto>(
                "auth.sp_get_users_paged",
                new
                {
                    p_username = filter.Username,
                    p_email = filter.Email,
                    p_full_name = filter.FullName,
                    p_status_id = filter.StatusId,
                    p_role_ids = filter.RoleIds?.ToArray(),
                    p_created_from = filter.CreatedFrom,
                    p_created_to = filter.CreatedTo,
                    p_page_number = filter.PageNumber,
                    p_page_size = filter.PageSize,
                    p_sort_by = filter.SortBy,
                    p_sort_descending = filter.SortDescending
                });

            var users = result.Select(u => u.ToLoginUser()).ToList();
            var totalCount = result.FirstOrDefault()?.total_count ?? 0;

            return (users, (int)totalCount);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error en GetUsersPagedAsync: {ex.Message}");
            throw;
        }
    }

    public async Task<LoginUser?> GetUserByIdWithDetailsAsync(int userId)
    {
        try
        {
            var result = await CallTableFunction<UserWithPersonDto>(
                "auth.sp_get_user_by_id_with_details",
                new { p_user_id = userId });

            return result.FirstOrDefault()?.ToLoginUser();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error en GetUserByIdWithDetailsAsync: {ex.Message}");
            throw;
        }
    }

    public async Task<List<UserRoleDto>> GetUserRolesDetailAsync(int userId)
    {
        try
        {
            var result = await CallTableFunction<dynamic>(
                "auth.sp_get_user_roles_detail",
                new { p_user_id = userId });

            return result.Select(r => new UserRoleDto
            {
                RoleId = (int)r.role_id,
                RoleName = (string)r.role_name,
                Description = r.description as string,
                AssignedAt = (DateTime)r.assigned_at,
                AssignedBy = (int)r.assigned_by,
                AssignedByName = (string)r.assigned_by_name
            }).ToList();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error en GetUserRolesDetailAsync: {ex.Message}");
            return new List<UserRoleDto>();
        }
    }

    public async Task<List<StatusDto>> GetStatusListAsync()
    {
        try
        {
            var result = await CallTableFunction<dynamic>("auth.sp_get_status_list");

            return result.Select(s => new StatusDto
            {
                StatusId = (int)s.status_id,
                StatusName = (string)s.status_name
            }).ToList();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error en GetStatusListAsync: {ex.Message}");
            return new List<StatusDto>();
        }
    }

    public async Task<List<StatusReasonDto>> GetStatusReasonsAsync()
    {
        try
        {
            var result = await CallTableFunction<dynamic>("auth.sp_get_status_reasons");

            return result.Select(sr => new StatusReasonDto
            {
                StatusReasonId = (int)sr.status_reason_id,
                ReasonName = (string)sr.reason_name
            }).ToList();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error en GetStatusReasonsAsync: {ex.Message}");
            return new List<StatusReasonDto>();
        }
    }

    public async Task<List<UserSearchResultDto>> SearchUsersAsync(string searchTerm, int limit = 10)
    {
        try
        {
            var result = await CallTableFunction<dynamic>(
                "auth.sp_search_users",
                new { p_search_term = searchTerm, p_limit = limit });

            return result.Select(u => new UserSearchResultDto
            {
                UserId = (int)u.user_id,
                Username = (string)u.username,
                Email = (string)u.email,
                FullName = (string)u.full_name,
                StatusName = (string)u.status_name
            }).ToList();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error en SearchUsersAsync: {ex.Message}");
            return new List<UserSearchResultDto>();
        }
    }
}