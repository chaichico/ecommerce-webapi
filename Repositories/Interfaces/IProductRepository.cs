using Models;

namespace Repositories.Interfaces;

public interface IProductRepository
{
    Task<List<Product>> GetByIdsAsync(List<int> ids);
    Task<List<Product>> GetActiveByIdsAsync(List<int> ids);
}