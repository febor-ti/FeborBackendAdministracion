using FeborBack.Application.DTOs.Courses;
using FeborBack.Domain.Entities.Courses;
using FeborBack.Domain.Interfaces.Courses;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace FeborBack.Application.Services.Courses;

public class CourseService : ICourseService
{
    private readonly ICourseRepository _repo;
    private readonly IConfiguration     _config;
    private readonly ILogger<CourseService> _logger;

    public CourseService(ICourseRepository repo, IConfiguration config, ILogger<CourseService> logger)
    {
        _repo   = repo;
        _config = config;
        _logger = logger;
    }

    public async Task<IEnumerable<CourseDto>> GetAllAsync()
    {
        var courses = await _repo.GetAllAsync();
        return courses.Select(ToDto);
    }

    public async Task<CourseDto> CreateAsync(CreateCourseDto dto, Stream fileStream, string fileName, int createdBy)
    {
        // Validar que no exista ya un curso con el mismo slug
        var existing = await _repo.GetBySlugAsync(dto.Slug);
        if (existing != null)
            throw new InvalidOperationException($"Ya existe un curso publicado con el slug '{dto.Slug}'.");

        // Validar extensión del archivo
        var extension = Path.GetExtension(fileName).ToLowerInvariant();
        if (extension != ".html" && extension != ".htm")
            throw new ArgumentException("Solo se permiten archivos HTML (.html, .htm).");

        // Determinar ruta base según entorno
        var basePath = GetBasePath();
        var courseDir = Path.Combine(basePath, dto.Slug);
        var filePath  = Path.Combine(courseDir, "index.html");

        // Crear directorio y guardar archivo
        Directory.CreateDirectory(courseDir);
        await using (var dest = new FileStream(filePath, FileMode.Create, FileAccess.Write))
        {
            await fileStream.CopyToAsync(dest);
        }

        _logger.LogInformation("Curso '{Slug}' guardado en {Path}", dto.Slug, filePath);

        // Construir URL pública
        var baseUrl   = _config["Courses:BaseUrl"]?.TrimEnd('/') ?? "https://virtual.febor.co/cursos";
        var publicUrl = $"{baseUrl}/{dto.Slug}/";

        // Guardar en base de datos
        var course = new Course
        {
            Name        = dto.Name.Trim(),
            Slug        = dto.Slug.Trim().ToLowerInvariant(),
            Description = dto.Description?.Trim(),
            FilePath    = filePath,
            PublicUrl   = publicUrl,
            IsActive    = true,
            CreatedBy   = createdBy
        };

        var created = await _repo.CreateAsync(course);
        return ToDto(created);
    }

    public async Task<CourseDto> UpdateAsync(int id, UpdateCourseDto dto, Stream? fileStream, string? fileName, int updatedBy)
    {
        var course = await _repo.GetByIdAsync(id)
            ?? throw new KeyNotFoundException($"Curso con ID {id} no encontrado.");

        // Actualizar campos de texto
        course.Name        = dto.Name.Trim();
        course.Description = dto.Description?.Trim();
        course.UpdatedBy   = updatedBy;

        // Reemplazar archivo HTML si se proporcionó uno nuevo
        if (fileStream != null && !string.IsNullOrEmpty(fileName))
        {
            var extension = Path.GetExtension(fileName).ToLowerInvariant();
            if (extension != ".html" && extension != ".htm")
                throw new ArgumentException("Solo se permiten archivos HTML (.html, .htm).");

            Directory.CreateDirectory(Path.GetDirectoryName(course.FilePath)!);
            await using var dest = new FileStream(course.FilePath, FileMode.Create, FileAccess.Write);
            await fileStream.CopyToAsync(dest);
            _logger.LogInformation("HTML del curso '{Slug}' reemplazado", course.Slug);
        }

        var updated = await _repo.UpdateAsync(course);
        return ToDto(updated);
    }

    public async Task DeleteAsync(int id, int deletedBy)
    {
        var course = await _repo.GetByIdAsync(id)
            ?? throw new KeyNotFoundException($"Curso con ID {id} no encontrado.");

        // Eliminar directorio del servidor
        var courseDir = Path.GetDirectoryName(course.FilePath);
        if (courseDir != null && Directory.Exists(courseDir))
        {
            Directory.Delete(courseDir, recursive: true);
            _logger.LogInformation("Directorio del curso '{Slug}' eliminado: {Dir}", course.Slug, courseDir);
        }

        await _repo.DeleteAsync(course);
    }

    public async Task UploadErrorPageAsync(Stream fileStream, string fileName)
    {
        var ext = Path.GetExtension(fileName).ToLowerInvariant();
        if (ext != ".html" && ext != ".htm")
            throw new ArgumentException("Solo se permiten archivos HTML (.html, .htm).");

        var destPath = GetErrorPagePath();
        var dir = Path.GetDirectoryName(destPath);
        if (!string.IsNullOrEmpty(dir))
            Directory.CreateDirectory(dir);

        await using var dest = new FileStream(destPath, FileMode.Create, FileAccess.Write);
        await fileStream.CopyToAsync(dest);

        _logger.LogInformation("Página 404 actualizada en {Path}", destPath);
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private string GetBasePath()
    {
        var isWindows = OperatingSystem.IsWindows();
        var key       = isWindows ? "Courses:BasePath" : "Courses:ProductionBasePath";
        var path      = _config[key];

        if (string.IsNullOrEmpty(path))
            throw new InvalidOperationException($"La configuración '{key}' no está definida en appsettings.");

        return path;
    }

    private string GetErrorPagePath()
    {
        var isWindows = OperatingSystem.IsWindows();
        var key       = isWindows ? "ErrorPages:NotFoundPath" : "ErrorPages:ProductionNotFoundPath";
        var path      = _config[key];

        if (string.IsNullOrEmpty(path))
            throw new InvalidOperationException($"La configuración '{key}' no está definida en appsettings.");

        return path;
    }

    private static CourseDto ToDto(Course c) => new()
    {
        Id          = c.Id,
        Name        = c.Name,
        Slug        = c.Slug,
        Description = c.Description,
        PublicUrl   = c.PublicUrl,
        IsActive    = c.IsActive,
        CreatedAt   = c.CreatedAt
    };
}
