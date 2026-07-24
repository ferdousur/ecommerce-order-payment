using System.Linq.Expressions;

namespace ECommerce.Application.Interfaces;

public interface IRepository<T> where T : class
{
    Task<T?> GetByIdAsync(Guid id);
    Task<IEnumerable<T>> GetAllAsync();
    Task<T> CreateAsync(T entity);
    Task SaveChangesAsync();
    Task UpdateAsync(T entity);
    Task DeleteAsync(Guid id);
    IQueryable<T> GetQueryable();
    Task<IEnumerable<T>> GetAsync(Expression<Func<T, bool>> predicate);
}