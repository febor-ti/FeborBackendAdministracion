using FeborBack.Application.DTOs.Forms;
using FeborBack.Domain.Entities.Forms;
using FeborBack.Domain.Interfaces.Forms;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace FeborBack.Application.Services.Forms;

public class FormService : IFormService
{
    private readonly IFormRepository _repo;
    private readonly IConfiguration  _config;
    private readonly ILogger<FormService> _logger;

    public FormService(IFormRepository repo, IConfiguration config, ILogger<FormService> logger)
    {
        _repo   = repo;
        _config = config;
        _logger = logger;
    }

    public async Task<IEnumerable<FormPageDto>> GetAllAsync()
    {
        var forms = await _repo.GetAllAsync();

        // Recopilar todos los IDs de usuario referenciados para traer nombres en un solo query
        var userIds = forms
            .SelectMany(f => new[] { f.CreatedBy, f.UpdatedBy })
            .Where(id => id.HasValue)
            .Select(id => id!.Value)
            .Distinct();

        var names = await _repo.GetUserNamesAsync(userIds);
        return forms.Select(f => ToDto(f, names));
    }

    public async Task<FormPageDto> CreateAsync(CreateFormPageDto dto, Stream fileStream, string fileName, int createdBy)
    {
        // Validar que no exista ya un formulario con el mismo slug
        var existing = await _repo.GetBySlugAsync(dto.Slug);
        if (existing != null)
            throw new InvalidOperationException($"Ya existe un formulario publicado con el slug '{dto.Slug}'.");

        // Validar extensión del archivo
        var extension = Path.GetExtension(fileName).ToLowerInvariant();
        if (extension != ".html" && extension != ".htm")
            throw new ArgumentException("Solo se permiten archivos HTML (.html, .htm).");

        // Determinar ruta base según entorno
        var basePath = GetBasePath();
        var formDir  = Path.Combine(basePath, dto.Slug);
        var filePath = Path.Combine(formDir, "index.html");

        // Crear directorio y guardar archivo
        Directory.CreateDirectory(formDir);
        await using (var dest = new FileStream(filePath, FileMode.Create, FileAccess.Write))
        {
            await fileStream.CopyToAsync(dest);
        }

        _logger.LogInformation("Formulario '{Slug}' guardado en {Path}", dto.Slug, filePath);

        // Construir URL pública
        var baseUrl   = _config["Forms:BaseUrl"]?.TrimEnd('/') ?? "https://virtual.febor.co/formularios";
        var publicUrl = $"{baseUrl}/{dto.Slug}/";

        // Guardar en base de datos
        var form = new FormPage
        {
            Name        = dto.Name.Trim(),
            Slug        = dto.Slug.Trim().ToLowerInvariant(),
            Description = dto.Description?.Trim(),
            FilePath    = filePath,
            PublicUrl   = publicUrl,
            IsActive    = true,
            CreatedBy   = createdBy
        };

        var created = await _repo.CreateAsync(form);
        return ToDto(created);
    }

    public async Task<FormPageDto> UpdateAsync(int id, UpdateFormPageDto dto, Stream? fileStream, string? fileName, int updatedBy)
    {
        var form = await _repo.GetByIdAsync(id)
            ?? throw new KeyNotFoundException($"Formulario con ID {id} no encontrado.");

        // Actualizar campos de texto
        form.Name        = dto.Name.Trim();
        form.Description = dto.Description?.Trim();
        form.UpdatedBy   = updatedBy;

        // Cambiar slug si se proporcionó uno diferente
        if (!string.IsNullOrEmpty(dto.Slug))
        {
            var newSlug = dto.Slug.Trim().ToLowerInvariant();
            if (newSlug != form.Slug)
            {
                var existing = await _repo.GetBySlugAsync(newSlug);
                if (existing != null && existing.Id != id)
                    throw new InvalidOperationException($"Ya existe un formulario con el slug '{newSlug}'.");

                var basePath     = GetBasePath();
                var inactivePath = GetInactivePath();
                var baseUrl      = _config["Forms:BaseUrl"]?.TrimEnd('/') ?? "https://virtual.febor.co/formularios";

                var oldActiveDir   = Path.Combine(basePath, form.Slug);
                var oldInactiveDir = Path.Combine(inactivePath, form.Slug);
                var newActiveDir   = Path.Combine(basePath, newSlug);
                var newInactiveDir = Path.Combine(inactivePath, newSlug);

                if (form.IsActive && Directory.Exists(oldActiveDir))
                    Directory.Move(oldActiveDir, newActiveDir);
                else if (!form.IsActive && Directory.Exists(oldInactiveDir))
                    Directory.Move(oldInactiveDir, newInactiveDir);

                form.Slug      = newSlug;
                form.FilePath  = Path.Combine(basePath, newSlug, "index.html");
                form.PublicUrl = $"{baseUrl}/{newSlug}/";
                _logger.LogInformation("Slug del formulario {Id} cambiado a '{NewSlug}'", id, newSlug);
            }
        }

        // Reemplazar archivo HTML si se proporcionó uno nuevo
        if (fileStream != null && !string.IsNullOrEmpty(fileName))
        {
            var extension = Path.GetExtension(fileName).ToLowerInvariant();
            if (extension != ".html" && extension != ".htm")
                throw new ArgumentException("Solo se permiten archivos HTML (.html, .htm).");

            Directory.CreateDirectory(Path.GetDirectoryName(form.FilePath)!);
            await using var dest = new FileStream(form.FilePath, FileMode.Create, FileAccess.Write);
            await fileStream.CopyToAsync(dest);
            _logger.LogInformation("HTML del formulario '{Slug}' reemplazado", form.Slug);
        }

        var updated = await _repo.UpdateAsync(form);
        return ToDto(updated);
    }

    public async Task<FormPageDto> ToggleActiveAsync(int id, int updatedBy)
    {
        var form = await _repo.GetByIdAsync(id)
            ?? throw new KeyNotFoundException($"Formulario con ID {id} no encontrado.");

        var inactivePath = GetInactivePath();                        // /var/www/febor/formularios_inactive
        var activeDir    = Path.GetDirectoryName(form.FilePath)!;    // /var/www/febor/formularios/{slug}
        var inactiveDir  = Path.Combine(inactivePath, form.Slug);    // /var/www/febor/formularios_inactive/{slug}

        _logger.LogInformation(
            "ToggleActive formulario '{Slug}': IsActive={IsActive} | activeDir={AD} exists={ADE} | inactiveDir={ID} exists={IDE}",
            form.Slug, form.IsActive,
            activeDir,   Directory.Exists(activeDir),
            inactiveDir, Directory.Exists(inactiveDir));

        if (form.IsActive)
        {
            // ── Desactivar: mover fuera de /formularios/ → Nginx devuelve 404 ────
            form.Deactivate(updatedBy);

            if (Directory.Exists(activeDir))
            {
                Directory.CreateDirectory(inactivePath);        // garantiza que /formularios_inactive/ exista
                Directory.Move(activeDir, inactiveDir);
                _logger.LogInformation("Movido a inactivo: {From} → {To}", activeDir, inactiveDir);
            }
            else
            {
                _logger.LogWarning("Directorio activo no encontrado: {Dir}", activeDir);
            }
        }
        else
        {
            // ── Activar: restaurar de /formularios_inactive/ → /formularios/ ─────
            form.Activate(updatedBy);

            if (Directory.Exists(inactiveDir))
            {
                Directory.CreateDirectory(Path.GetDirectoryName(activeDir)!);  // garantiza que /formularios/ exista
                Directory.Move(inactiveDir, activeDir);
                _logger.LogInformation("Restaurado a activo: {From} → {To}", inactiveDir, activeDir);
            }
            else if (Directory.Exists(activeDir))
            {
                // El directorio ya está en la ubicación activa (la desactivación anterior
                // no llegó a moverlo — se actualiza solo el estado en BD)
                _logger.LogWarning(
                    "Directorio inactivo no encontrado ({ID}), pero el activo sí existe ({AD}). Solo se actualiza BD.",
                    inactiveDir, activeDir);
            }
            else
            {
                throw new InvalidOperationException(
                    $"No se encontró el directorio del formulario ni en '{activeDir}' ni en '{inactiveDir}'. " +
                    "Verifica los permisos del servidor.");
            }
        }

        var updated = await _repo.UpdateAsync(form);
        _logger.LogInformation("Formulario '{Slug}' {Estado} correctamente por usuario {UserId}",
            form.Slug, form.IsActive ? "activado" : "desactivado", updatedBy);

        return ToDto(updated);
    }

    public async Task DeleteAsync(int id, int deletedBy)
    {
        var form = await _repo.GetByIdAsync(id)
            ?? throw new KeyNotFoundException($"Formulario con ID {id} no encontrado.");

        // Eliminar directorio del servidor
        var formDir = Path.GetDirectoryName(form.FilePath);
        if (formDir != null && Directory.Exists(formDir))
        {
            Directory.Delete(formDir, recursive: true);
            _logger.LogInformation("Directorio del formulario '{Slug}' eliminado: {Dir}", form.Slug, formDir);
        }

        await _repo.DeleteAsync(form);
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private string GetBasePath()
    {
        var isWindows = OperatingSystem.IsWindows();
        var key       = isWindows ? "Forms:BasePath" : "Forms:ProductionBasePath";
        var path      = _config[key];

        if (string.IsNullOrEmpty(path))
            throw new InvalidOperationException($"La configuración '{key}' no está definida en appsettings.");

        return path;
    }

    private string GetInactivePath()
    {
        var isWindows = OperatingSystem.IsWindows();
        var key       = isWindows ? "Forms:InactivePath" : "Forms:ProductionInactivePath";
        var path      = _config[key];

        if (string.IsNullOrEmpty(path))
        {
            // Fallback: carpeta hermana de la ruta activa
            var basePath = GetBasePath();
            path = basePath.TrimEnd('/', '\\') + "_inactive";
        }

        return path;
    }

    private static FormPageDto ToDto(FormPage f, Dictionary<int, string>? names = null) => new()
    {
        Id            = f.Id,
        Name          = f.Name,
        Slug          = f.Slug,
        Description   = f.Description,
        PublicUrl     = f.PublicUrl,
        IsActive      = f.IsActive,
        CreatedBy     = f.CreatedBy,
        CreatedByName = f.CreatedBy.HasValue && names != null
                            ? names.GetValueOrDefault(f.CreatedBy.Value)
                            : null,
        CreatedAt     = f.CreatedAt,
        UpdatedBy     = f.UpdatedBy,
        UpdatedByName = f.UpdatedBy.HasValue && names != null
                            ? names.GetValueOrDefault(f.UpdatedBy.Value)
                            : null,
        UpdatedAt     = f.UpdatedAt
    };
}
