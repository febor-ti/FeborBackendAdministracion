using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using FeborBack.Domain.Common;
using FeborBack.Domain.Interfaces;
using FeborBack.Infrastructure.Data;

namespace FeborBack.Infrastructure.Repositories.EntityFramework;

public class EFBaseRepository<T> : IBaseRepository<T> where T : BaseEntity
{
    protected readonly ApplicationDbContext _context;
    protected readonly DbSet<T> _dbSet;

    public EFBaseRepository(ApplicationDbContext context)
    {
        _context = context;
        _dbSet = context.Set<T>();
    }

    public virtual async Task<T?> GetByIdAsync(int id)
    {
        return await _dbSet.FirstOrDefaultAsync(e => e.Id == id);
    }

    public virtual async Task<T?> GetByIdAsync(int id, params Expression<Func<T, object>>[] includes)
    {
        var query = _dbSet.AsQueryable();

        foreach (var include in includes)
        {
            query = query.Include(include);
        }

        return await query.FirstOrDefaultAsync(e => e.Id == id);
    }

    public virtual async Task<IEnumerable<T>> GetAllAsync()
    {
        return await _dbSet.ToListAsync();
    }

    public virtual async Task<IEnumerable<T>> GetAllAsync(params Expression<Func<T, object>>[] includes)
    {
        var query = _dbSet.AsQueryable();

        foreach (var include in includes)
        {
            query = query.Include(include);
        }

        return await query.ToListAsync();
    }

    public virtual async Task<IEnumerable<T>> GetActiveAsync()
    {
        return await _dbSet.Where(e => e.IsActive).ToListAsync();
    }

    public virtual async Task<IEnumerable<T>> GetActiveAsync(params Expression<Func<T, object>>[] includes)
    {
        var query = _dbSet.Where(e => e.IsActive);

        foreach (var include in includes)
        {
            query = query.Include(include);
        }

        return await query.ToListAsync();
    }

    public virtual async Task<IEnumerable<T>> FindAsync(Expression<Func<T, bool>> predicate)
    {
        return await _dbSet.Where(predicate).ToListAsync();
    }

    public virtual async Task<IEnumerable<T>> FindAsync(Expression<Func<T, bool>> predicate, params Expression<Func<T, object>>[] includes)
    {
        var query = _dbSet.Where(predicate);

        foreach (var include in includes)
        {
            query = query.Include(include);
        }

        return await query.ToListAsync();
    }

    public virtual async Task<T?> FindSingleAsync(Expression<Func<T, bool>> predicate)
    {
        return await _dbSet.FirstOrDefaultAsync(predicate);
    }

    public virtual async Task<T?> FindSingleAsync(Expression<Func<T, bool>> predicate, params Expression<Func<T, object>>[] includes)
    {
        var query = _dbSet.AsQueryable();

        foreach (var include in includes)
        {
            query = query.Include(include);
        }

        return await query.FirstOrDefaultAsync(predicate);
    }

    public virtual async Task<(IEnumerable<T> items, int totalCount)> GetPagedAsync(
        int page,
        int pageSize,
        Expression<Func<T, bool>>? filter = null,
        Func<IQueryable<T>, IOrderedQueryable<T>>? orderBy = null,
        params Expression<Func<T, object>>[] includes)
    {
        var query = _dbSet.AsQueryable();

        // Apply includes
        foreach (var include in includes)
        {
            query = query.Include(include);
        }

        // Apply filter
        if (filter != null)
        {
            query = query.Where(filter);
        }

        // Get total count before pagination
        var totalCount = await query.CountAsync();

        // Apply ordering
        if (orderBy != null)
        {
            query = orderBy(query);
        }
        else
        {
            // Default ordering by Id descending
            query = query.OrderByDescending(e => e.Id);
        }

        // Apply pagination
        var items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return (items, totalCount);
    }

    public virtual async Task<T> CreateAsync(T entity)
    {
        entity.CreatedAt = DateTime.UtcNow;
        _dbSet.Add(entity);
        await _context.SaveChangesAsync();
        return entity;
    }

    public virtual async Task<T> UpdateAsync(T entity)
    {
        entity.UpdatedAt = DateTime.UtcNow;
        _dbSet.Update(entity);
        await _context.SaveChangesAsync();
        return entity;
    }

    public virtual async Task DeleteAsync(int id)
    {
        var entity = await GetByIdAsync(id);
        if (entity != null)
        {
            _dbSet.Remove(entity);
            await _context.SaveChangesAsync();
        }
    }

    public virtual async Task SoftDeleteAsync(int id, int deletedBy)
    {
        var entity = await _dbSet.IgnoreQueryFilters().FirstOrDefaultAsync(e => e.Id == id);
        if (entity != null)
        {
            entity.SoftDelete(deletedBy);
            await _context.SaveChangesAsync();
        }
    }

    public virtual async Task SetActiveStatusAsync(int id, bool isActive, int updatedBy)
    {
        var entity = await GetByIdAsync(id);
        if (entity != null)
        {
            if (isActive)
            {
                entity.Activate(updatedBy);
            }
            else
            {
                entity.Deactivate(updatedBy);
            }
            await _context.SaveChangesAsync();
        }
    }

    public virtual async Task ActivateAsync(int id, int updatedBy)
    {
        await SetActiveStatusAsync(id, true, updatedBy);
    }

    public virtual async Task DeactivateAsync(int id, int updatedBy)
    {
        await SetActiveStatusAsync(id, false, updatedBy);
    }

    public virtual async Task<bool> ExistsAsync(int id)
    {
        return await _dbSet.AnyAsync(e => e.Id == id);
    }

    public virtual async Task<bool> ExistsAsync(Expression<Func<T, bool>> predicate)
    {
        return await _dbSet.AnyAsync(predicate);
    }

    public virtual async Task<IEnumerable<T>> CreateBatchAsync(IEnumerable<T> entities)
    {
        var entityList = entities.ToList();
        foreach (var entity in entityList)
        {
            entity.CreatedAt = DateTime.UtcNow;
        }

        _dbSet.AddRange(entityList);
        await _context.SaveChangesAsync();
        return entityList;
    }

    public virtual async Task UpdateBatchAsync(IEnumerable<T> entities)
    {
        var entityList = entities.ToList();
        foreach (var entity in entityList)
        {
            entity.UpdatedAt = DateTime.UtcNow;
        }

        _dbSet.UpdateRange(entityList);
        await _context.SaveChangesAsync();
    }

    public virtual async Task SoftDeleteBatchAsync(IEnumerable<int> ids, int deletedBy)
    {
        var entities = await _dbSet.IgnoreQueryFilters()
            .Where(e => ids.Contains(e.Id))
            .ToListAsync();

        foreach (var entity in entities)
        {
            entity.SoftDelete(deletedBy);
        }

        await _context.SaveChangesAsync();
    }

    public virtual async Task<int> CountAsync()
    {
        return await _dbSet.CountAsync();
    }

    public virtual async Task<int> CountAsync(Expression<Func<T, bool>> predicate)
    {
        return await _dbSet.CountAsync(predicate);
    }

    public virtual async Task<int> CountActiveAsync()
    {
        return await _dbSet.CountAsync(e => e.IsActive);
    }

    protected IQueryable<T> ApplyIncludes(IQueryable<T> query, params Expression<Func<T, object>>[] includes)
    {
        foreach (var include in includes)
        {
            query = query.Include(include);
        }
        return query;
    }

    protected IQueryable<T> ApplyOrdering<TKey>(IQueryable<T> query, Expression<Func<T, TKey>> keySelector, bool descending = false)
    {
        return descending
            ? query.OrderByDescending(keySelector)
            : query.OrderBy(keySelector);
    }

    protected async Task<(IQueryable<T> query, int totalCount)> ApplyFilterAndCount(
        IQueryable<T> query,
        Expression<Func<T, bool>>? filter)
    {
        if (filter != null)
        {
            query = query.Where(filter);
        }

        var totalCount = await query.CountAsync();
        return (query, totalCount);
    }
}