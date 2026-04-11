using AutoMapper;
using FeborBack.Application.DTOs;
using FeborBack.Application.DTOs.Auth;
using FeborBack.Application.DTOs.Menu;
using FeborBack.Domain.Entities;
using FeborBack.Domain.Entities.Menu;

namespace FeborBack.Application.Mappings;

public class AutoMapperProfile : Profile
{
    public AutoMapperProfile()
    {
        // LoginUser mappings (User → LoginUser)
        CreateMap<LoginUser, UserInfoDto>()
            .ForMember(dest => dest.FullName, opt => opt.MapFrom(src => src.Person.FullName))
            .ForMember(dest => dest.Roles, opt => opt.Ignore())
            .ForMember(dest => dest.Claims, opt => opt.Ignore());

        CreateMap<RegisterUserDto, LoginUser>()
            .ForMember(dest => dest.UserId, opt => opt.Ignore())
            .ForMember(dest => dest.PersonId, opt => opt.Ignore())
            .ForMember(dest => dest.PasswordHash, opt => opt.Ignore())
            .ForMember(dest => dest.PasswordSalt, opt => opt.Ignore())
            .ForMember(dest => dest.StatusId, opt => opt.MapFrom(src => 1))
            .ForMember(dest => dest.IsSessionActive, opt => opt.MapFrom(src => false))
            .ForMember(dest => dest.IsTemporaryPassword, opt => opt.MapFrom(src => false))
            .ForMember(dest => dest.FailedAttempts, opt => opt.MapFrom(src => 0))
            .ForMember(dest => dest.CreatedAt, opt => opt.MapFrom(src => DateTime.UtcNow));

        // Person mappings
        CreateMap<RegisterUserDto, Person>()
            .ForMember(dest => dest.PersonId, opt => opt.Ignore());

        // LoginRequestDto mappings (UserLoginDto → LoginRequestDto)
        CreateMap<LoginRequestDto, LoginUser>()
            .ForMember(dest => dest.UserId, opt => opt.Ignore())
            .ForMember(dest => dest.PersonId, opt => opt.Ignore())
            .ForMember(dest => dest.Email, opt => opt.Ignore())
            .ForMember(dest => dest.PasswordHash, opt => opt.Ignore())
            .ForMember(dest => dest.PasswordSalt, opt => opt.Ignore());

        // Role mappings
        CreateMap<Role, RoleDto>();
        CreateMap<CreateRoleDto, Role>()
            .ForMember(dest => dest.RoleId, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedAt, opt => opt.MapFrom(src => DateTime.UtcNow))
            .ForMember(dest => dest.IsActive, opt => opt.MapFrom(src => true));

        // Menu mappings
        CreateMap<MenuItem, MenuItemDto>();
        CreateMap<CreateMenuItemDto, MenuItem>()
            .ForMember(dest => dest.MenuItemId, opt => opt.Ignore())
            .ForMember(dest => dest.IsActive, opt => opt.MapFrom(src => true))
            .ForMember(dest => dest.CreatedAt, opt => opt.MapFrom(src => DateTime.UtcNow))
            .ForMember(dest => dest.CreatedBy, opt => opt.Ignore())
            .ForMember(dest => dest.UpdatedBy, opt => opt.Ignore())
            .ForMember(dest => dest.UpdatedAt, opt => opt.Ignore());
        CreateMap<UpdateMenuItemDto, MenuItem>()
            .ForMember(dest => dest.CreatedBy, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
            .ForMember(dest => dest.UpdatedAt, opt => opt.MapFrom(src => DateTime.UtcNow));
    }
}