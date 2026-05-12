using Data;
using Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;
using Models.Entities;
namespace Repositories;

public class UserRepository : IUserRepository
{
    private readonly AppDbContext _context;

    public UserRepository(AppDbContext context)
    {
        _context = context;
    }

    public Task<User?> GetByEmail(string email)
    {
        return _context.Users
            .FirstOrDefaultAsync(u => u.Email == email);
    }

    public Task Create(User user)
    {
        _context.Users.Add(user);
        return _context.SaveChangesAsync();
    }

    public Task<bool> EmailExists(string email)
    {
        return _context.Users
            .AnyAsync(u => u.Email == email);
    }
}