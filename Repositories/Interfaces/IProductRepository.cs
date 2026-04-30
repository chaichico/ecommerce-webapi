using Models.Entities;

namespace Repositories.Interfaces;

public interface IProductRepository
{
    Task<List<Product>> GetByIds(List<int> ids);
    Task<List<Product>> GetActiveByIds(List<int> ids);
}