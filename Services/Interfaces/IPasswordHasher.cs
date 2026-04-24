namespace Services.Interfaces;
// Interface for Hashing
public interface IPasswordHasher
{
    string HashPassword(string password);
    // Login 
    bool VerifyPassword(string password, string passwordHash);
}