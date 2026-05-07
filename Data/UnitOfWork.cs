using Microsoft.EntityFrameworkCore.Storage;

namespace Data;

public class UnitOfWork : IUnitOfWork
{
    private readonly AppDbContext _context;

    public UnitOfWork(AppDbContext context)
    {
        _context = context;
    }

    public Task SaveChangesAsync()
    {
        return _context.SaveChangesAsync();
    }

    public Task<IDbContextTransaction> BeginTransactionAsync()
    {
        return _context.Database.BeginTransactionAsync();
    }

    public Task ExecuteInTransactionAsync(Func<Task> operation)
    {
        IExecutionStrategy strategy = _context.Database.CreateExecutionStrategy();
        return strategy.ExecuteAsync(
            operation,
            async (ctx, op, ct) =>
            {
                await using IDbContextTransaction tx = await ctx.Database.BeginTransactionAsync(ct);
                try
                {
                    await op();
                    await tx.CommitAsync(ct);
                }
                catch
                {
                    await tx.RollbackAsync(ct);
                    throw;
                }
                return true;
            },
            null,
            CancellationToken.None);
    }
}