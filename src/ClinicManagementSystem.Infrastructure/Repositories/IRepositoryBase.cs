using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using ClinicManagementSystem.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;


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

public class RepositoryBase<T> : IRepositoryBase<T> where T : class
    {
        private readonly ClinicManagementDbContext _context;
        private readonly DbSet<T> _dbSet;

        public RepositoryBase(ClinicManagementDbContext context)
        {
            _context = context;
            _dbSet = _context.Set<T>();
        }

        public async Task<IQueryable<T>> GetAllAsync()
        {
            return await Task.FromResult(_dbSet.AsQueryable());
        }

        public async Task<T?> GetByIdAsync(dynamic id)
        {
            return await _dbSet.FindAsync(new object?[] { id });
        }

        public async Task AddAsync(T entity)
        {
            await _dbSet.AddAsync(entity);
        }

        public Task UpdateAsync(T entity)
        {
            _dbSet.Update(entity);
            return Task.CompletedTask;
        }

        public async Task DeleteAsync(dynamic id)
        {
            var entity = await GetByIdAsync(id);

            if (entity != null)
            {
                _dbSet.Remove(entity);
            }
        }

        public async Task<T?> SingleOrDefault(
            Expression<Func<T, bool>> predicate)
        {
            return await _dbSet.FirstOrDefaultAsync(predicate);
        }

        public Task<IQueryable<T>> Where(
            Expression<Func<T, bool>> predicate)
        {
            return Task.FromResult(_dbSet.Where(predicate));
        }

        public async Task AddListItemsAsync(List<T> entities)
        {
            await _dbSet.AddRangeAsync(entities);
        }

        public Task UpdateListItemsAsync(List<T> entities)
        {
            _dbSet.UpdateRange(entities);
            return Task.CompletedTask;
        }

        public IQueryable<T> WhereSql(
            Expression<Func<T, bool>> predicate)
        {
            return _dbSet.Where(predicate);
        }
    }