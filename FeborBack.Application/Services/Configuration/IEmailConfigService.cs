using FeborBack.Application.DTOs.Configuration;

namespace FeborBack.Application.Services.Configuration;

public interface IEmailConfigService
{
    Task<EmailConfigDto?> GetConfigurationAsync();
    Task<EmailConfigDto> SaveConfigurationAsync(SaveEmailConfigDto dto, int userId);
    Task<bool> SendTestEmailAsync(TestEmailDto dto, int userId);
    Task<bool> VerifyConnectionAsync(VerifyConnectionDto dto);
}
