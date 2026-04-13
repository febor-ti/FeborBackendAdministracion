using Dapper;
using FeborBack.Domain.Entities.Courses;
using FeborBack.Domain.Interfaces.Courses;
using FeborBack.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace FeborBack.Infrastructure.Repositories.Courses;

public class CourseRepository : ICourseRepository
{
    private readonly ApplicationDbContext _context;

    public CourseRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<Course>> GetAllAsync()
    {
        return await _context.Courses
            .OrderByDescending(c => c.CreatedAt)
            .ToListAsync();
    }

    public async Task<Course?> GetByIdAsync(int id)
    {
        return await _context.Courses.FindAsync(id);
    }

    public async Task<Course?> GetBySlugAsync(string slug)
    {
        return await _context.Courses
            .FirstOrDefaultAsync(c => c.Slug == slug.ToLowerInvariant());
    }

    public async Task<Course> CreateAsync(Course course)
    {
        _context.Courses.Add(course);
        await _context.SaveChangesAsync();
        return course;
    }

    public async Task<Course> UpdateAsync(Course course)
    {
        _context.Courses.Update(course);
        await _context.SaveChangesAsync();
        return course;
    }

    public async Task DeleteAsync(Course course)
    {
        _context.Courses.Remove(course);
        await _context.SaveChangesAsync();
    }

    public async Task<Dictionary<int, string>> GetUserNamesAsync(IEnumerable<int> userIds)
    {
        var ids = userIds.Distinct().ToList();
        if (ids.Count == 0) return new Dictionary<int, string>();

        var conn = _context.Database.GetDbConnection();
        const string sql = """
            SELECT u.user_id AS UserId, p.full_name AS FullName
            FROM auth.login_user u
            JOIN auth.person p ON p.person_id = u.person_id
            WHERE u.user_id = ANY(@Ids)
            """;

        var rows = await conn.QueryAsync<(int UserId, string FullName)>(sql, new { Ids = ids.ToArray() });
        return rows.ToDictionary(r => r.UserId, r => r.FullName);
    }
}
