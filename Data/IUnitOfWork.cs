using Microsoft.EntityFrameworkCore.Storage;
namespace Data;

public interface IUnitOfWork
{
    Task SaveChangesAsync();
    Task<IDbContextTransaction> BeginTransactionAsync();
    Task ExecuteInTransactionAsync(Func<Task> operation);
}