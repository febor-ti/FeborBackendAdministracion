using System.Security.Claims;
using FeborBack.Domain.Entities;

namespace FeborBack.Application.Services;

public interface IJwtService
{
    string GenerateAccessToken(LoginUser user, List<string> roles, List<int> roleIds, List<string> claims);
    string GenerateRefreshToken();
    ClaimsPrincipal? GetPrincipalFromExpiredToken(string token);
    bool ValidateToken(string token);
    DateTime GetTokenExpiration(string token);
}