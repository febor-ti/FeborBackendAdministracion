namespace FeborBack.Infrastructure.DTOs.Catalog;

/// <summary>
/// DTO para ciudades principales (capitales de departamento)
/// </summary>
public class CityMainDto
{
    public int Id { get; set; }
    public string? CodigoDivipola { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string Departamento { get; set; } = string.Empty;
    public DateTime? FechaFundacion { get; set; }

    // Propiedades formateadas
    public string FormattedFechaFundacion => FechaFundacion?.ToString("yyyy-MM-dd") ?? "No disponible";
    public bool IsCapital => true; // Todas las ciudades principales son capitales
}