using FeborBack.Application.DTOs.Courses;

namespace FeborBack.Application.Services.Courses;

public interface ICourseService
{
    Task<IEnumerable<CourseDto>> GetAllAsync();
    Task<CourseDto> CreateAsync(CreateCourseDto dto, Stream fileStream, string fileName, int createdBy);
    Task<CourseDto> UpdateAsync(int id, UpdateCourseDto dto, Stream? fileStream, string? fileName, int updatedBy);
    Task DeleteAsync(int id, int deletedBy);
    Task UploadErrorPageAsync(Stream fileStream, string fileName);
}
