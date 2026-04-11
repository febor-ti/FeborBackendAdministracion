using System.Linq.Expressions;
using FeborBack.Domain.Common;

namespace FeborBack.Domain.Interfaces;

public interface IBaseRepository<T> where T : BaseEntity
{
    // Consultas básicas
    Task<T?> GetByIdAsync(int id);
    Task<T?> GetByIdAsync(int id, params Expression<Func<T, object>>[] includes);
    Task<IEnumerable<T>> GetAllAsync();
    Task<IEnumerable<T>> GetAllAsync(params Expression<Func<T, object>>[] includes);
    Task<IEnumerable<T>> GetActiveAsync();
    Task<IEnumerable<T>> GetActiveAsync(params Expression<Func<T, object>>[] includes);

    // Consultas con filtros
    Task<IEnumerable<T>> FindAsync(Expression<Func<T, bool>> predicate);
    Task<IEnumerable<T>> FindAsync(Expression<Func<T, bool>> predicate, params Expression<Func<T, object>>[] includes);
    Task<T?> FindSingleAsync(Expression<Func<T, bool>> predicate);
    Task<T?> FindSingleAsync(Expression<Func<T, bool>> predicate, params Expression<Func<T, object>>[] includes);

    // Consultas paginadas
    Task<(IEnumerable<T> items, int totalCount)> GetPagedAsync(
        int page,
        int pageSize,
        Expression<Func<T, bool>>? filter = null,
        Func<IQueryable<T>, IOrderedQueryable<T>>? orderBy = null,
        params Expression<Func<T, object>>[] includes);

    // Operaciones CRUD
    Task<T> CreateAsync(T entity);
    Task<T> UpdateAsync(T entity);
    Task DeleteAsync(int id);
    Task SoftDeleteAsync(int id, int deletedBy);

    // Operaciones de estado
    Task SetActiveStatusAsync(int id, bool isActive, int updatedBy);
    Task ActivateAsync(int id, int updatedBy);
    Task DeactivateAsync(int id, int updatedBy);

    // Verificaciones
    Task<bool> ExistsAsync(int id);
    Task<bool> ExistsAsync(Expression<Func<T, bool>> predicate);

    // Operaciones en lote
    Task<IEnumerable<T>> CreateBatchAsync(IEnumerable<T> entities);
    Task UpdateBatchAsync(IEnumerable<T> entities);
    Task SoftDeleteBatchAsync(IEnumerable<int> ids, int deletedBy);

    // Conteos
    Task<int> CountAsync();
    Task<int> CountAsync(Expression<Func<T, bool>> predicate);
    Task<int> CountActiveAsync();
}

public interface IPagedRequest
{
    int Page { get; set; }
    int PageSize { get; set; }
    string? SortBy { get; set; }
    bool SortDescending { get; set; }
}