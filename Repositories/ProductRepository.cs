using Data;
using Microsoft.EntityFrameworkCore;
using Models.Entities;
using Repositories.Interfaces;

namespace Repositories;

public class ProductRepository : IProductRepository
{
    private readonly AppDbContext _context;

    public ProductRepository(AppDbContext context)
    {
        _context = context;
    }

    public Task<List<Product>> GetByIds(List<int> ids)
    {
        return _context.Products
            .Where(p => ids.Contains(p.Id))
            .ToListAsync();
    }

    public Task<List<Product>> GetActiveByIds(List<int> ids)
    {
        return _context.Products
            .Where(p => ids.Contains(p.Id) && p.IsActive)
            .ToListAsync();
    }
}