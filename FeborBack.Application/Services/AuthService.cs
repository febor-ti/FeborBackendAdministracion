using Microsoft.Extensions.Configuration;
using FeborBack.Application.DTOs.Auth;
using FeborBack.Application.Services.Configuration;
using FeborBack.Application.Services.Notifications;
using FeborBack.Domain.Entities;
using FeborBack.Infrastructure.Repositories;

namespace FeborBack.Application.Services;

public class AuthService : IAuthService
{
    private readonly IAuthRepository _authRepository;
    private readonly IJwtService _jwtService;
    private readonly IPasswordService _passwordService;
    private readonly IEmailNotificationService _emailService;
    private readonly IEmailConfigService _emailConfigService;
    private readonly IOtpService _otpService;
    private readonly IConfiguration _configuration;
    private readonly int _refreshTokenExpirationDays;
    private readonly int _maxFailedAttempts;

    public AuthService(
        IAuthRepository authRepository,
        IJwtService jwtService,
        IPasswordService passwordService,
        IEmailNotificationService emailService,
        IEmailConfigService emailConfigService,
        IOtpService otpService,
        IConfiguration configuration)
    {
        _authRepository = authRepository;
        _jwtService = jwtService;
        _passwordService = passwordService;
        _emailService = emailService;
        _emailConfigService = emailConfigService;
        _otpService = otpService;
        _configuration = configuration;
        _refreshTokenExpirationDays = int.Parse(_configuration["Jwt:RefreshTokenExpirationDays"] ?? "7");
        _maxFailedAttempts = int.Parse(_configuration["Auth:MaxFailedAttempts"] ?? "5");
    }

    public async Task<LoginResponseDto> LoginAsync(LoginRequestDto request)
    {
        var user = await _authRepository.GetUserByEmailAsync(request.Email);

        if (user == null)
            throw new UnauthorizedAccessException("Credenciales inválidas");

        if (user.StatusId == 3)
            throw new UnauthorizedAccessException("Cuenta bloqueada");

        if (!_passwordService.VerifyPassword(request.Password, user.PasswordHash, user.PasswordSalt))
        {
            user.FailedAttempts++;

            if (user.FailedAttempts >= _maxFailedAttempts)
            {
                user.StatusId = 3;
                user.StatusReasonId = 3;
            }

            await _authRepository.UpdateUserAsync(user);
            throw new UnauthorizedAccessException("Credenciales inválidas");
        }

        user.FailedAttempts = 0;
        user.LastAccessAt = DateTime.UtcNow;
        user.IsSessionActive = true;

        if (user.StatusId != 1)
        {
            user.StatusId = 1;
            user.StatusReasonId = null;
        }

        await _authRepository.UpdateUserAsync(user);

        // ── Verificar si el 2FA está habilitado ────────────────────────────────
        var twoFactorEnabled = await _emailConfigService.GetTwoFactorEnabledAsync();
        if (twoFactorEnabled)
        {
            var code = _otpService.GenerateCode();
            var sessionToken = _otpService.GenerateSessionToken();
            _otpService.StoreCode(sessionToken, user.UserId, code);

            var fullName = user.Person?.FullName ?? user.Username ?? user.Email;
            try
            {
                await _emailService.SendTwoFactorCodeAsync(user.Email, fullName, code);
            }
            catch (Exception emailEx)
            {
                // Si el correo falla limpiamos el código para no dejar basura en caché
                _otpService.RemoveCode(sessionToken);
                throw new InvalidOperationException(
                    $"No se pudo enviar el código de verificación al correo {user.Email}. " +
                    $"Verifica la configuración SMTP. Detalle: {emailEx.Message}");
            }

            return new LoginResponseDto
            {
                RequiresTwoFactor = true,
                SessionToken = sessionToken
            };
        }

        // ── Login completo sin 2FA ─────────────────────────────────────────────
        return await BuildLoginResponseAsync(user);
    }

    private async Task<LoginResponseDto> BuildLoginResponseAsync(Domain.Entities.LoginUser user)
    {
        var roles = await _authRepository.GetUserRolesAsync(user.UserId);
        var roleIds = await _authRepository.GetUserRoleIdsAsync(user.UserId);
        var claims = await _authRepository.GetUserClaimsAsync(user.UserId);

        var accessToken = _jwtService.GenerateAccessToken(user, roles, roleIds, claims);
        var refreshToken = _jwtService.GenerateRefreshToken();

        var refreshTokenEntity = new RefreshToken
        {
            UserId = user.UserId,
            Token = refreshToken,
            ExpiresAt = DateTime.UtcNow.AddDays(_refreshTokenExpirationDays),
            CreatedAt = DateTime.UtcNow,
            IsUsed = false
        };

        await _authRepository.CreateRefreshTokenAsync(refreshTokenEntity);
        await _authRepository.CleanupExpiredRefreshTokensAsync();

        var userInfo = new UserInfoDto
        {
            UserId = user.UserId,
            Username = user.Username,
            Email = user.Email,
            FullName = user.Person?.FullName ?? "",
            AvatarName = user.AvatarName,
            IsTemporaryPassword = user.IsTemporaryPassword,
            Roles = roles,
            RoleIds = roleIds,
            Claims = claims
        };

        return new LoginResponseDto
        {
            AccessToken = accessToken,
            RefreshToken = refreshToken,
            ExpiresAt = _jwtService.GetTokenExpiration(accessToken),
            User = userInfo
        };
    }

    public async Task<LoginResponseDto> VerifyTwoFactorAsync(VerifyTwoFactorDto request)
    {
        var entry = _otpService.GetCode(request.SessionToken);

        if (entry == null)
            throw new UnauthorizedAccessException("El código ha expirado o el enlace de sesión no es válido. Inicia sesión nuevamente.");

        if (entry.Value.Code != request.Code.Trim())
        {
            // Eliminamos el código tras un intento fallido para evitar fuerza bruta
            _otpService.RemoveCode(request.SessionToken);
            throw new UnauthorizedAccessException("Código incorrecto. Por favor inicia sesión nuevamente.");
        }

        _otpService.RemoveCode(request.SessionToken);

        var user = await _authRepository.GetUserByIdAsync(entry.Value.UserId);
        if (user == null || user.StatusId != 1)
            throw new UnauthorizedAccessException("Usuario no autorizado.");

        return await BuildLoginResponseAsync(user);
    }

    public async Task<(UserInfoDto User, string? TemporaryPassword)> RegisterUserAsync(RegisterUserDto request, int createdBy)
    {
        if (await _authRepository.EmailExistsAsync(request.Email))
            throw new InvalidOperationException("El email ya está registrado");

        string passwordToUse;
        string? temporaryPasswordGenerated = null;
        bool isTemporary = false;

        if (string.IsNullOrWhiteSpace(request.Password))
        {
            passwordToUse = _passwordService.GenerateRandomPassword();
            temporaryPasswordGenerated = passwordToUse;
            isTemporary = true;
        }
        else
        {
            if (!_passwordService.IsPasswordStrong(request.Password))
                throw new InvalidOperationException("La contraseña no cumple con los requisitos de seguridad");

            passwordToUse = request.Password;
            isTemporary = false;
        }

        var passwordHash = _passwordService.HashPassword(passwordToUse, out var salt);

        var user = new LoginUser
        {
            Username = request.Email,
            Email = request.Email,
            PasswordHash = passwordHash,
            PasswordSalt = salt,
            StatusId = 1,
            IsSessionActive = false,
            IsTemporaryPassword = isTemporary,
            FailedAttempts = 0,
            CreatedBy = createdBy,
            CreatedAt = DateTime.UtcNow,
            Person = new Person
            {
                FullName = request.FullName
            }
        };

        user = await _authRepository.CreateUserAsync(user);

        if (!request.RoleIds.Any())
            throw new InvalidOperationException("Debe especificar al menos un rol para el usuario");

        var validRoles = await _authRepository.GetRolesByIdsAsync(request.RoleIds);
        if (!validRoles.Any())
            throw new InvalidOperationException("No se encontraron roles válidos con los IDs especificados");

        if (validRoles.Count() != request.RoleIds.Count)
            throw new InvalidOperationException("Algunos de los roles especificados no existen");

        await _authRepository.AssignRolesToUserAsync(user.UserId, validRoles.Select(r => r.RoleId).ToList(), createdBy);

        var roles = await _authRepository.GetUserRolesAsync(user.UserId);
        var roleIds = await _authRepository.GetUserRoleIdsAsync(user.UserId);
        var claims = await _authRepository.GetUserClaimsAsync(user.UserId);

        var userInfo = new UserInfoDto
        {
            UserId = user.UserId,
            Username = user.Username,
            Email = user.Email,
            FullName = request.FullName,
            AvatarName = user.AvatarName,
            IsTemporaryPassword = user.IsTemporaryPassword,
            Roles = roles,
            RoleIds = roleIds,
            Claims = claims
        };

        return (userInfo, temporaryPasswordGenerated);
    }

    public async Task<LoginResponseDto> RefreshTokenAsync(RefreshTokenRequestDto request)
    {
        var refreshToken = await _authRepository.GetRefreshTokenAsync(request.RefreshToken);

        if (refreshToken == null || !refreshToken.IsActive)
            throw new UnauthorizedAccessException("Token de refresh inválido");

        var user = await _authRepository.GetUserWithRolesAndClaimsAsync(refreshToken.UserId);
        if (user == null || user.StatusId != 1)
            throw new UnauthorizedAccessException("Usuario no autorizado");

        refreshToken.IsUsed = true;
        refreshToken.RevokedAt = DateTime.UtcNow;

        var newRefreshToken = _jwtService.GenerateRefreshToken();
        refreshToken.ReplacedByToken = newRefreshToken;

        await _authRepository.UpdateRefreshTokenAsync(refreshToken);

        var newRefreshTokenEntity = new RefreshToken
        {
            UserId = user.UserId,
            Token = newRefreshToken,
            ExpiresAt = DateTime.UtcNow.AddDays(_refreshTokenExpirationDays),
            CreatedAt = DateTime.UtcNow,
            IsUsed = false
        };

        await _authRepository.CreateRefreshTokenAsync(newRefreshTokenEntity);

        var roles = await _authRepository.GetUserRolesAsync(user.UserId);
        var roleIds = await _authRepository.GetUserRoleIdsAsync(user.UserId);
        var claims = await _authRepository.GetUserClaimsAsync(user.UserId);

        var accessToken = _jwtService.GenerateAccessToken(user, roles, roleIds, claims);

        var userInfo = new UserInfoDto
        {
            UserId = user.UserId,
            Username = user.Username,
            Email = user.Email,
            FullName = user.Person?.FullName ?? "",
            AvatarName = user.AvatarName,
            IsTemporaryPassword = user.IsTemporaryPassword,
            Roles = roles,
            RoleIds = roleIds,
            Claims = claims
        };

        return new LoginResponseDto
        {
            AccessToken = accessToken,
            RefreshToken = newRefreshToken,
            ExpiresAt = _jwtService.GetTokenExpiration(accessToken),
            User = userInfo
        };
    }

    public async Task<bool> LogoutAsync(string refreshToken)
    {
        var token = await _authRepository.GetRefreshTokenAsync(refreshToken);
        if (token == null) return false;

        token.RevokedAt = DateTime.UtcNow;
        await _authRepository.UpdateRefreshTokenAsync(token);

        var user = await _authRepository.GetUserByIdAsync(token.UserId);
        if (user != null)
        {
            user.IsSessionActive = false;
            await _authRepository.UpdateUserAsync(user);
        }

        return true;
    }

    public async Task<bool> LogoutAllAsync(int userId)
    {
        await _authRepository.RevokeAllUserRefreshTokensAsync(userId);

        var user = await _authRepository.GetUserByIdAsync(userId);
        if (user != null)
        {
            user.IsSessionActive = false;
            await _authRepository.UpdateUserAsync(user);
        }

        return true;
    }

    public async Task<UserInfoDto?> GetUserInfoAsync(int userId)
    {
        var user = await _authRepository.GetUserWithRolesAndClaimsAsync(userId);
        if (user == null) return null;

        var roles = await _authRepository.GetUserRolesAsync(user.UserId);
        var roleIds = await _authRepository.GetUserRoleIdsAsync(user.UserId);
        var claims = await _authRepository.GetUserClaimsAsync(user.UserId);

        return new UserInfoDto
        {
            UserId = user.UserId,
            Username = user.Username,
            Email = user.Email,
            FullName = user.Person?.FullName ?? "",
            AvatarName = user.AvatarName,
            IsTemporaryPassword = user.IsTemporaryPassword,
            Roles = roles,
            RoleIds = roleIds,
            Claims = claims
        };
    }

    public async Task<bool> ChangePasswordAsync(int userId, string currentPassword, string newPassword)
    {
        var user = await _authRepository.GetUserByIdAsync(userId);
        if (user == null) return false;

        if (!_passwordService.VerifyPassword(currentPassword, user.PasswordHash, user.PasswordSalt))
            throw new InvalidOperationException("Contraseña actual incorrecta");

        if (!_passwordService.IsPasswordStrong(newPassword))
            throw new InvalidOperationException("La nueva contraseña no cumple con los requisitos de seguridad");

        user.PasswordHash = _passwordService.HashPassword(newPassword, out var salt);
        user.PasswordSalt = salt;
        user.IsTemporaryPassword = false;
        user.UpdatedBy = userId;

        await _authRepository.UpdateUserAsync(user);
        await _authRepository.RevokeAllUserRefreshTokensAsync(userId);

        return true;
    }

    public async Task<bool> ResetPasswordAsync(string email)
    {
        var user = await _authRepository.GetUserByEmailAsync(email);
        if (user != null)
        {
            var tempPassword = _passwordService.GenerateRandomPassword();
            user.PasswordHash = _passwordService.HashPassword(tempPassword, out var salt);
            user.PasswordSalt = salt;
            user.IsTemporaryPassword = true;

            await _authRepository.UpdateUserAsync(user);

            var fullName = user.Person?.FullName ?? user.Username ?? email;
            await _emailService.SendPasswordResetEmailAsync(email, fullName, tempPassword);

            return true;
        }

        return false;
    }

    public Task<bool> ValidateTokenAsync(string token)
    {
        return Task.FromResult(_jwtService.ValidateToken(token));
    }

    public async Task<UserInfoDto> CreateInitialAdminAsync()
    {
        try
        {
            var existingAdmin = await _authRepository.GetUserByEmailAsync("admin@sistema.com");
            if (existingAdmin != null)
                throw new InvalidOperationException("El usuario administrador ya existe");

            var registerDto = new RegisterUserDto
            {
                Email = "admin@sistema.com",
                Password = "Admin123!@#",
                FullName = "Administrador del Sistema",
                RoleIds = new List<int> { 1 }
            };

            var (user, _) = await RegisterUserAsync(registerDto, 1);
            return user;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error creando usuario administrador inicial: {ex.Message}");
            throw;
        }
    }
}
