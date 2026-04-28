using Data;
using Microsoft.EntityFrameworkCore;
using Models;
using Repositories.Interfaces;

namespace Repositories;

public class ProductRepository : IProductRepository
{
    private readonly AppDbContext _context;

    public ProductRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<List<Product>> GetByIdsAsync(List<int> ids)
    {
        return await _context.Products
            .Where(p => ids.Contains(p.Id))
            .ToListAsync();
    }

    public async Task<List<Product>> GetActiveByIdsAsync(List<int> ids)
    {
        return await _context.Products
            .Where(p => ids.Contains(p.Id) && p.IsActive)
            .ToListAsync();
    }
}