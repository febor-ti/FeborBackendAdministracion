using Dapper;
using FeborBack.Domain.Entities.Forms;
using FeborBack.Domain.Interfaces.Forms;
using FeborBack.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace FeborBack.Infrastructure.Repositories.Forms;

public class FormRepository : IFormRepository
{
    private readonly ApplicationDbContext _context;

    public FormRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<FormPage>> GetAllAsync()
    {
        return await _context.Forms
            .OrderByDescending(f => f.CreatedAt)
            .ToListAsync();
    }

    public async Task<FormPage?> GetByIdAsync(int id)
    {
        return await _context.Forms.FindAsync(id);
    }

    public async Task<FormPage?> GetBySlugAsync(string slug)
    {
        return await _context.Forms
            .FirstOrDefaultAsync(f => f.Slug == slug.ToLowerInvariant());
    }

    public async Task<FormPage> CreateAsync(FormPage form)
    {
        _context.Forms.Add(form);
        await _context.SaveChangesAsync();
        return form;
    }

    public async Task<FormPage> UpdateAsync(FormPage form)
    {
        _context.Forms.Update(form);
        await _context.SaveChangesAsync();
        return form;
    }

    public async Task DeleteAsync(FormPage form)
    {
        _context.Forms.Remove(form);
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
