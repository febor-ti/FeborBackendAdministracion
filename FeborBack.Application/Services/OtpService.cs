using Microsoft.Extensions.Caching.Memory;

namespace FeborBack.Application.Services;

public class OtpService : IOtpService
{
    private readonly IMemoryCache _cache;
    private static readonly TimeSpan CodeExpiry = TimeSpan.FromMinutes(15);

    public OtpService(IMemoryCache cache)
    {
        _cache = cache;
    }

    public string GenerateSessionToken() => Guid.NewGuid().ToString("N");

    public string GenerateCode() => Random.Shared.Next(100_000, 999_999).ToString();

    public void StoreCode(string sessionToken, int userId, string code)
    {
        // Si el usuario ya tenía un código activo, lo invalidamos antes de guardar el nuevo
        var userKey = UserKey(userId);
        if (_cache.TryGetValue(userKey, out string? previousSessionToken) && previousSessionToken != null)
        {
            _cache.Remove(CacheKey(previousSessionToken));
        }

        // Guardar el nuevo código
        _cache.Set(CacheKey(sessionToken), (userId, code), new MemoryCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = CodeExpiry
        });

        // Registrar el sessionToken activo para este usuario
        _cache.Set(userKey, sessionToken, new MemoryCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = CodeExpiry
        });
    }

    public (int UserId, string Code)? GetCode(string sessionToken)
    {
        return _cache.TryGetValue(CacheKey(sessionToken), out (int, string) entry) ? entry : null;
    }

    public void RemoveCode(string sessionToken)
    {
        if (_cache.TryGetValue(CacheKey(sessionToken), out (int UserId, string Code) entry))
        {
            _cache.Remove(UserKey(entry.UserId));
        }
        _cache.Remove(CacheKey(sessionToken));
    }

    private static string CacheKey(string sessionToken) => $"2fa_{sessionToken}";
    private static string UserKey(int userId)            => $"2fa_user_{userId}";
}
