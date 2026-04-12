using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FeborBack.Domain.Entities.Configuration;

[Table("email_settings", Schema = "configuration")]
public class EmailSettings
{
    [Key]
    [Column("id")]
    public int Id { get; set; }

    [Required]
    [Column("smtp_host")]
    [MaxLength(255)]
    public string SmtpHost { get; set; } = string.Empty;

    [Column("smtp_port")]
    public int SmtpPort { get; set; } = 2525;

    [Required]
    [Column("smtp_username")]
    [MaxLength(255)]
    public string SmtpUsername { get; set; } = string.Empty;

    [Required]
    [Column("smtp_password")]
    public string SmtpPassword { get; set; } = string.Empty; // Encriptado

    [Column("use_ssl")]
    public bool UseSsl { get; set; } = false;

    [Column("use_tls")]
    public bool UseTls { get; set; } = false;

    [Required]
    [Column("from_email")]
    [MaxLength(255)]
    public string FromEmail { get; set; } = string.Empty;

    [Column("from_name")]
    [MaxLength(255)]
    public string FromName { get; set; } = "Febor Cooperativa";

    [Column("is_active")]
    public bool IsActive { get; set; } = true;

    [Column("two_factor_enabled")]
    public bool TwoFactorEnabled { get; set; } = false;

    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [Column("updated_at")]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    [Column("created_by")]
    public int? CreatedBy { get; set; }

    [Column("updated_by")]
    public int? UpdatedBy { get; set; }
}
