using FeborBack.Domain.Entities.Courses;

namespace FeborBack.Domain.Interfaces.Courses;

public interface ICourseRepository
{
    Task<IEnumerable<Course>> GetAllAsync();
    Task<Course?> GetByIdAsync(int id);
    Task<Course?> GetBySlugAsync(string slug);
    Task<Course> CreateAsync(Course course);
    Task<Course> UpdateAsync(Course course);
    Task DeleteAsync(Course course);
    Task<Dictionary<int, string>> GetUserNamesAsync(IEnumerable<int> userIds);
}
