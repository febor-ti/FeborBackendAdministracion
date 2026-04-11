using FeborBack.Infrastructure.Repositories.Authorization;

namespace FeborBack.Application.Services.Authorization;

public class MenuAuthorizationService : IMenuAuthorizationService
{
    private readonly IMenuAuthorizationRepository _repository;

    public MenuAuthorizationService(IMenuAuthorizationRepository repository)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
    }

    public async Task<bool> UserHasClaimAccessAsync(int userId, string action, string subject)
    {
        return await _repository.UserHasClaimAccessAsync(userId, action, subject);
    }

    public async Task<bool> UserHasMenuKeyAccessAsync(int userId, string menuKey)
    {
        return await _repository.UserHasMenuKeyAccessAsync(userId, menuKey);
    }

    public async Task<IEnumerable<UserClaimDto>> GetUserClaimsAsync(int userId)
    {
        var dbClaims = await _repository.GetUserClaimsAsync(userId);

        return dbClaims.Select(c => new UserClaimDto
        {
            ClaimAction = c.claim_action,
            ClaimSubject = c.claim_subject,
            MenuKey = c.menu_key,
            MenuTitle = c.menu_title
        });
    }
}
