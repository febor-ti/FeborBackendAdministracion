using FeborBack.Domain.Entities.Configuration;

namespace FeborBack.Infrastructure.Repositories.Configuration;

public interface IEmailConfigRepository
{
    Task<EmailSettings?> GetActiveConfigurationAsync();
    Task<EmailSettings?> GetConfigurationByIdAsync(int id);
    Task<int> UpsertConfigurationAsync(EmailSettings config, int userId);
    Task<bool> UpdateTwoFactorEnabledAsync(bool enabled, int userId);
    Task<bool> GetTwoFactorEnabledAsync();
}
