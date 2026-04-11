using FeborBack.Domain.Entities;

namespace FeborBack.Infrastructure.DTOs;

public class RefreshTokenDto
{
    public int token_id { get; set; }
    public int user_id { get; set; }
    public string token { get; set; } = string.Empty;
    public DateTime expires_at { get; set; }
    public DateTime created_at { get; set; }
    public DateTime? revoked_at { get; set; }
    public bool is_used { get; set; }
    public string? replaced_by_token { get; set; }

    public RefreshToken ToRefreshToken()
    {
        return new RefreshToken
        {
            TokenId = token_id,
            UserId = user_id,
            Token = token,
            ExpiresAt = expires_at,
            CreatedAt = created_at,
            RevokedAt = revoked_at,
            IsUsed = is_used,
            ReplacedByToken = replaced_by_token
        };
    }
}