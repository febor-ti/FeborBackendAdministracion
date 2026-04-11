using AutoMapper;
using FeborBack.Application.DTOs.Auth;
using FeborBack.Application.DTOs.User;
using FeborBack.Infrastructure.Repositories;

namespace FeborBack.Application.Services;

public class RoleService : IRoleService
{
    private readonly IRoleRepository _roleRepository;
    private readonly IMapper _mapper;

    public RoleService(IRoleRepository roleRepository, IMapper mapper)
    {
        _roleRepository = roleRepository ?? throw new ArgumentNullException(nameof(roleRepository));
        _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
    }

    public async Task<IEnumerable<RoleDto>> GetAllRolesAsync()
    {
        var roles = await _roleRepository.GetAllRolesAsync();
        return _mapper.Map<IEnumerable<RoleDto>>(roles);
    }

    public async Task<IEnumerable<RoleDto>> GetActiveRolesAsync()
    {
        var roles = await _roleRepository.GetActiveRolesAsync();
        return _mapper.Map<IEnumerable<RoleDto>>(roles);
    }

    public async Task<RoleDto?> GetRoleByIdAsync(int roleId)
    {
        var role = await _roleRepository.GetRoleByIdAsync(roleId);
        return role != null ? _mapper.Map<RoleDto>(role) : null;
    }

    public async Task<RoleDto?> GetRoleByNameAsync(string roleName)
    {
        var role = await _roleRepository.GetRoleByNameAsync(roleName);
        return role != null ? _mapper.Map<RoleDto>(role) : null;
    }

    public async Task<IEnumerable<UserByRoleDto>> GetUsersByRoleIdAsync(int roleId)
    {
        var dbDtos = await _roleRepository.GetUsersByRoleIdAsync(roleId);

        // Convert Infrastructure DTOs to Application DTOs
        return dbDtos.Select(dto => new UserByRoleDto
        {
            UserId = dto.user_id,
            PersonId = dto.person_id,
            FullName = dto.full_name,
            Email = dto.email,
            Username = dto.username,
            IsActive = dto.is_active,
            CreatedAt = dto.created_at,
            RoleAssignedAt = dto.role_assigned_at
        });
    }
}