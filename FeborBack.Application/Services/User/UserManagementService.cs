using FeborBack.Application.DTOs.Auth;
using FeborBack.Application.DTOs.User;
using FeborBack.Infrastructure.Repositories;
using FeborBack.Infrastructure.DTOs;
using FeborBack.Application.Services.Notifications;

namespace FeborBack.Application.Services.User;

public class UserManagementService : IUserManagementService
{
    private readonly IUserRepository _userRepository;
    private readonly IAuthRepository _authRepository;
    private readonly IPasswordService _passwordService;
    private readonly IEmailNotificationService _emailNotificationService;

    public UserManagementService(
        IUserRepository userRepository,
        IAuthRepository authRepository,
        IPasswordService passwordService,
        IEmailNotificationService emailNotificationService)
    {
        _userRepository = userRepository;
        _authRepository = authRepository;
        _passwordService = passwordService;
        _emailNotificationService = emailNotificationService;
    }

    public async Task<PagedUsersDto> GetUsersAsync(Application.DTOs.User.UserFilterDto filter)
    {
        try
        {
            // Mapear de Application DTO a Infrastructure DTO
            var infraFilter = new Infrastructure.DTOs.UserFilterDto
            {
                Username = filter.Username,
                Email = filter.Email,
                FullName = filter.FullName,
                StatusId = filter.StatusId,
                RoleIds = filter.RoleIds,
                CreatedFrom = filter.CreatedFrom,
                CreatedTo = filter.CreatedTo,
                PageNumber = filter.PageNumber,
                PageSize = filter.PageSize,
                SortBy = filter.SortBy,
                SortDescending = filter.SortDescending
            };

            var (users, totalCount) = await _userRepository.GetUsersPagedAsync(infraFilter);

            var userDtos = new List<UserDetailDto>();
            foreach (var user in users)
            {
                var userDto = MapToUserDetailDto(user);
                userDto.Roles = await GetUserRolesDetailAsync(user.UserId);
                userDto.Claims = await _authRepository.GetUserClaimsAsync(user.UserId);
                userDtos.Add(userDto);
            }

            var totalPages = (int)Math.Ceiling((double)totalCount / filter.PageSize);

            return new PagedUsersDto
            {
                Users = userDtos,
                TotalCount = totalCount,
                PageNumber = filter.PageNumber,
                PageSize = filter.PageSize,
                TotalPages = totalPages,
                HasNextPage = filter.PageNumber < totalPages,
                HasPreviousPage = filter.PageNumber > 1
            };
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Error al obtener usuarios: {ex.Message}", ex);
        }
    }

    public async Task<UserDetailDto?> GetUserByIdAsync(int userId)
    {
        try
        {
            var user = await _userRepository.GetUserByIdWithDetailsAsync(userId);
            if (user == null) return null;

            var userDto = MapToUserDetailDto(user);
            userDto.Roles = await GetUserRolesDetailAsync(userId);
            userDto.Claims = await _authRepository.GetUserClaimsAsync(userId);

            return userDto;
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Error al obtener usuario: {ex.Message}", ex);
        }
    }

    public async Task<UserDetailDto> UpdateUserAsync(int userId, UpdateUserDto request, int updatedBy)
    {
        try
        {
            // Verificar que el usuario existe
            var existingUser = await _authRepository.GetUserByIdAsync(userId);
            if (existingUser == null)
                throw new InvalidOperationException("Usuario no encontrado");

            // Verificar si el email ya existe en otro usuario
            if (existingUser.Email != request.Email)
            {
                var emailExists = await _authRepository.EmailExistsAsync(request.Email);
                if (emailExists)
                    throw new InvalidOperationException("El email ya está registrado en otro usuario");
            }

            // Verificar si el username ya existe en otro usuario
            if (existingUser.Username != request.Username)
            {
                var usernameExists = await _authRepository.UsernameExistsAsync(request.Username);
                if (usernameExists)
                    throw new InvalidOperationException("El username ya está registrado en otro usuario");
            }

            // Actualizar datos del usuario
            existingUser.Username = request.Username;
            existingUser.Email = request.Email;
            existingUser.AvatarName = request.AvatarName;
            existingUser.StatusId = request.StatusId;
            existingUser.StatusReasonId = request.StatusReasonId;
            existingUser.UpdatedBy = updatedBy;
            existingUser.UpdatedAt = DateTime.UtcNow;

            // Actualizar contraseña si se proporciona
            if (!string.IsNullOrWhiteSpace(request.Password))
            {
                var passwordHash = _passwordService.HashPassword(request.Password, out var salt);
                existingUser.PasswordHash = passwordHash;
                existingUser.PasswordSalt = salt;
                existingUser.IsTemporaryPassword = false;
            }

            // Actualizar persona
            if (existingUser.Person != null)
            {
                existingUser.Person.FullName = request.FullName;
            }

            await _authRepository.UpdateUserAsync(existingUser);

            // Actualizar roles si han cambiado
            if (request.RoleIds.Any())
            {
                await _authRepository.RemoveUserRolesAsync(userId);
                await _authRepository.AssignRolesToUserAsync(userId, request.RoleIds, updatedBy);
            }

            // Retornar usuario actualizado
            return await GetUserByIdAsync(userId) ?? throw new InvalidOperationException("Error al recuperar usuario actualizado");
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Error al actualizar usuario: {ex.Message}", ex);
        }
    }

    public async Task<bool> DeleteUserAsync(int userId, int deletedBy)
    {
        try
        {
            var user = await _authRepository.GetUserByIdAsync(userId);
            if (user == null) return false;

            // Soft delete: cambiar status a inactivo
            user.StatusId = 4; // Deleted
            user.StatusReasonId = 4; // Deleted by admin
            user.UpdatedBy = deletedBy;
            user.UpdatedAt = DateTime.UtcNow;
            user.IsSessionActive = false;

            await _authRepository.UpdateUserAsync(user);

            // Revocar todos los refresh tokens
            await _authRepository.RevokeAllUserRefreshTokensAsync(userId);

            return true;
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Error al eliminar usuario: {ex.Message}", ex);
        }
    }

    public async Task<bool> AssignRolesAsync(int userId, List<int> roleIds, int assignedBy)
    {
        try
        {
            var user = await _authRepository.GetUserByIdAsync(userId);
            if (user == null) return false;

            // Verificar que los roles existen
            var validRoles = await _authRepository.GetRolesByIdsAsync(roleIds);
            if (validRoles.Count != roleIds.Count)
                throw new InvalidOperationException("Uno o más roles no existen");

            // Remover roles actuales y asignar nuevos
            await _authRepository.RemoveUserRolesAsync(userId);
            await _authRepository.AssignRolesToUserAsync(userId, roleIds, assignedBy);

            return true;
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Error al asignar roles: {ex.Message}", ex);
        }
    }

    public async Task<bool> BlockUserAsync(int userId, int reasonId, int blockedBy)
    {
        try
        {
            var user = await _authRepository.GetUserByIdAsync(userId);
            if (user == null) return false;

            user.StatusId = 3; // Blocked
            user.StatusReasonId = reasonId;
            user.UpdatedBy = blockedBy;
            user.UpdatedAt = DateTime.UtcNow;
            user.IsSessionActive = false;

            await _authRepository.UpdateUserAsync(user);

            // Revocar todos los refresh tokens
            await _authRepository.RevokeAllUserRefreshTokensAsync(userId);

            return true;
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Error al bloquear usuario: {ex.Message}", ex);
        }
    }

    public async Task<bool> UnblockUserAsync(int userId, int unblockedBy)
    {
        try
        {
            var user = await _authRepository.GetUserByIdAsync(userId);
            if (user == null) return false;

            user.StatusId = 1; // Active
            user.StatusReasonId = null;
            user.UpdatedBy = unblockedBy;
            user.UpdatedAt = DateTime.UtcNow;
            user.FailedAttempts = 0; // Reset failed attempts

            await _authRepository.UpdateUserAsync(user);

            return true;
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Error al desbloquear usuario: {ex.Message}", ex);
        }
    }

    public async Task<bool> ResetUserPasswordAsync(int userId, int resetBy)
    {
        try
        {
            var user = await _authRepository.GetUserByIdAsync(userId);
            if (user == null) return false;

            // Generar nueva contraseña temporal
            var tempPassword = _passwordService.GenerateRandomPassword();
            var passwordHash = _passwordService.HashPassword(tempPassword, out var salt);

            user.PasswordHash = passwordHash;
            user.PasswordSalt = salt;
            user.IsTemporaryPassword = true;
            user.UpdatedBy = resetBy;
            user.UpdatedAt = DateTime.UtcNow;

            await _authRepository.UpdateUserAsync(user);

            // Revocar todos los refresh tokens para forzar re-login
            await _authRepository.RevokeAllUserRefreshTokensAsync(userId);

            // Enviar correo con la contraseña temporal
            await _emailNotificationService.SendPasswordResetEmailAsync(
                user.Email,
                user.Person?.FullName ?? user.Username,
                tempPassword
            );

            return true;
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Error al resetear contraseña: {ex.Message}", ex);
        }
    }

    private UserDetailDto MapToUserDetailDto(Domain.Entities.LoginUser user)
    {
        return new UserDetailDto
        {
            UserId = user.UserId,
            Username = user.Username,
            Email = user.Email,
            FullName = user.Person?.FullName ?? "",
            AvatarName = user.AvatarName,
            StatusId = user.StatusId,
            StatusName = user.Status?.StatusName ?? "",
            StatusReasonId = user.StatusReasonId,
            StatusReasonName = user.StatusReason?.ReasonName,
            IsSessionActive = user.IsSessionActive,
            IsTemporaryPassword = user.IsTemporaryPassword,
            FailedAttempts = user.FailedAttempts,
            LastAccessAt = user.LastAccessAt,
            CreatedAt = user.CreatedAt,
            UpdatedAt = user.UpdatedAt
        };
    }

    private async Task<List<Application.DTOs.User.UserRoleDto>> GetUserRolesDetailAsync(int userId)
    {
        try
        {
            var infraRoles = await _userRepository.GetUserRolesDetailAsync(userId);

            // Mapear de Infrastructure DTO a Application DTO
            return infraRoles.Select(r => new Application.DTOs.User.UserRoleDto
            {
                RoleId = r.RoleId,
                RoleName = r.RoleName,
                Description = r.Description,
                AssignedAt = r.AssignedAt,
                AssignedBy = r.AssignedBy,
                AssignedByName = r.AssignedByName
            }).ToList();
        }
        catch
        {
            return new List<Application.DTOs.User.UserRoleDto>();
        }
    }
}