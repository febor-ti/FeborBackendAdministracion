namespace FeborBack.Infrastructure.DTOs.Catalog;

public class CityDto
{
    public int Id { get; set; }
    public string? CodigoDivipola { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string Departamento { get; set; } = string.Empty;
    public DateTime? FechaFundacion { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    // Propiedades formateadas
    public string FormattedFechaFundacion => FechaFundacion?.ToString("yyyy-MM-dd") ?? "No disponible";
    public string FormattedCreatedAt => CreatedAt.ToString("yyyy-MM-dd HH:mm:ss");
}