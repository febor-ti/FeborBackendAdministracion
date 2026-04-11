using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace FeborBack.Infrastructure.Repositories.Authorization;

public class MenuAuthorizationRepository : BaseRepository, IMenuAuthorizationRepository
{
    private readonly ILogger<MenuAuthorizationRepository> _logger;

    public MenuAuthorizationRepository(IConfiguration configuration, ILogger<MenuAuthorizationRepository> logger)
        : base(configuration.GetConnectionString("DefaultConnection")
               ?? throw new ArgumentNullException("ConnectionString no configurado"))
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<bool> UserHasClaimAccessAsync(int userId, string action, string subject)
    {
        _logger.LogInformation("Calling sp_user_has_claim_access for userId={UserId}, action={Action}, subject={Subject}", userId, action, subject);

        var result = await CallFunction<bool>(
            "menu.sp_user_has_claim_access",
            new
            {
                p_user_id = userId,
                p_claim_action = action,
                p_claim_subject = subject
            });

        _logger.LogInformation("sp_user_has_claim_access returned: {Result} for userId={UserId}, action={Action}, subject={Subject}", result, userId, action, subject);

        return result;
    }

    public async Task<bool> UserHasMenuKeyAccessAsync(int userId, string menuKey)
    {
        var result = await CallFunction<bool>(
            "menu.sp_user_has_menu_key_access",
            new
            {
                p_user_id = userId,
                p_menu_key = menuKey
            });

        return result;
    }

    public async Task<IEnumerable<UserClaimDto>> GetUserClaimsAsync(int userId)
    {
        return await CallTableFunction<UserClaimDto>(
            "menu.sp_get_user_claims",
            new { p_user_id = userId });
    }
}
