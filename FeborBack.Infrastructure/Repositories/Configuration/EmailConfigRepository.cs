using Microsoft.Extensions.Configuration;
using FeborBack.Domain.Entities.Configuration;

namespace FeborBack.Infrastructure.Repositories.Configuration;

public class EmailConfigRepository : BaseRepository, IEmailConfigRepository
{
    public EmailConfigRepository(IConfiguration configuration)
        : base(configuration.GetConnectionString("DefaultConnection")
               ?? throw new ArgumentNullException("ConnectionString no configurado"))
    {
    }

    public async Task<EmailSettings?> GetActiveConfigurationAsync()
    {
        try
        {
            var result = await CallTableFunction<EmailSettings>(
                "configuration.sp_get_active_email_config");

            return result.FirstOrDefault();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error en GetActiveConfigurationAsync: {ex.Message}");
            return null;
        }
    }

    public async Task<EmailSettings?> GetConfigurationByIdAsync(int id)
    {
        try
        {
            var result = await CallTableFunction<EmailSettings>(
                "configuration.sp_get_email_config_by_id",
                new { p_id = id });

            return result.FirstOrDefault();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error en GetConfigurationByIdAsync: {ex.Message}");
            return null;
        }
    }

    public async Task<int> UpsertConfigurationAsync(EmailSettings config, int userId)
    {
        try
        {
            var configId = await CallFunction<int?>(
                "configuration.sp_upsert_email_config",
                new
                {
                    p_smtp_host = config.SmtpHost,
                    p_smtp_port = config.SmtpPort,
                    p_smtp_username = config.SmtpUsername,
                    p_smtp_password = config.SmtpPassword,
                    p_use_ssl = config.UseSsl,
                    p_use_tls = config.UseTls,
                    p_from_email = config.FromEmail,
                    p_from_name = config.FromName,
                    p_user_id = userId
                });

            return configId ?? 0;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error en UpsertConfigurationAsync: {ex.Message}");
            throw;
        }
    }
}
