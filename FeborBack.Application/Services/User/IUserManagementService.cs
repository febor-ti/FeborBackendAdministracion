using FeborBack.Application.DTOs.User;

namespace FeborBack.Application.Services.User;

public interface IUserManagementService
{
    Task<PagedUsersDto> GetUsersAsync(UserFilterDto filter);
    Task<UserDetailDto?> GetUserByIdAsync(int userId);
    Task<UserDetailDto> UpdateUserAsync(int userId, UpdateUserDto request, int updatedBy);
    Task<bool> DeleteUserAsync(int userId, int deletedBy);
    Task<bool> AssignRolesAsync(int userId, List<int> roleIds, int assignedBy);
    Task<bool> BlockUserAsync(int userId, int reasonId, int blockedBy);
    Task<bool> UnblockUserAsync(int userId, int unblockedBy);
    Task<bool> ResetUserPasswordAsync(int userId, int resetBy);
}