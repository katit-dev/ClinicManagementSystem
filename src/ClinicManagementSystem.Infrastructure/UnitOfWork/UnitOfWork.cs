using ClinicManagementSystem.Infrastructure.Data;
using ClinicManagementSystem.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore.Storage;

namespace ClinicManagementSystem.Infrastructure.UnitOfWork;

public interface IUnitOfWork : IAsyncDisposable
{
    IUserRepository UserRepository { get; }

    Task BeginTransactionAsync();

    Task CommitTransactionAsync();

    Task RollbackTransactionAsync();

    Task SaveChangesAsync();
}

public class UnitOfWork : IUnitOfWork
{
    private readonly ClinicManagementDbContext _context;
    private IDbContextTransaction? _transaction;

    public IUserRepository UserRepository { get; private set; }

    public UnitOfWork(ClinicManagementDbContext context)
    {
        _context = context;

        UserRepository = new UserRepository(_context);
    }

    public async Task BeginTransactionAsync()
    {
        if (_transaction != null)
        {
            throw new InvalidOperationException(
                "A transaction is already in progress.");
        }

        _transaction = await _context.Database.BeginTransactionAsync();
    }

    public async Task CommitTransactionAsync()
    {
        if (_transaction == null)
        {
            throw new InvalidOperationException(
                "No transaction is in progress.");
        }

        try
        {
            await _transaction.CommitAsync();
        }
        finally
        {
            await _transaction.DisposeAsync();
            _transaction = null;
        }
    }

    public async Task RollbackTransactionAsync()
    {
        if (_transaction == null)
        {
            throw new InvalidOperationException(
                "No transaction is in progress.");
        }

        try
        {
            await _transaction.RollbackAsync();
        }
        finally
        {
            await _transaction.DisposeAsync();
            _transaction = null;
        }
    }

    public async Task SaveChangesAsync()
    {
        await _context.SaveChangesAsync();
    }

    public async ValueTask DisposeAsync()
    {
        if (_transaction != null)
        {
            await _transaction.DisposeAsync();
            _transaction = null;
        }
    }
}