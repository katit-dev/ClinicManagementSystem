using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;


public interface IRepositoryBase<T> where T : class
{
    Task<IQueryable<T>> GetAllAsync();

    Task<T?> GetByIdAsync(dynamic id);

    Task AddAsync(T entity);

    Task UpdateAsync(T entity);

    Task DeleteAsync(dynamic id);

    Task AddListItemsAsync(List<T> entities);

    Task UpdateListItemsAsync(List<T> entities);

    Task<T?> SingleOrDefault(Expression<Func<T, bool>> predicate);

    Task<IQueryable<T>> Where(Expression<Func<T, bool>> predicate);

    IQueryable<T> WhereSql(Expression<Func<T, bool>> predicate);
}
