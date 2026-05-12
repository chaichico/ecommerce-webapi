namespace Repositories.Interfaces;
using Models.Entities;
public interface IUserRepository
{
    Task<User?> GetByEmail(string email);
    Task Create(User user);
    Task<bool> EmailExists(string email);

}