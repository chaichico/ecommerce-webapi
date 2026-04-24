namespace Services;

using System.Security.Cryptography;
using Services.Interfaces;
using Microsoft.AspNetCore.Cryptography.KeyDerivation;

public class PasswordHasher : IPasswordHasher
{
    // Hash password when Register
    public string HashPassword(string password)
    {
        byte[] salt = RandomNumberGenerator.GetBytes(128 / 8);
        string hashed = Convert.ToBase64String(KeyDerivation.Pbkdf2(
            password:password,
            salt: salt,
            prf: KeyDerivationPrf.HMACSHA256,
            iterationCount: 100000,
            numBytesRequested: 256 / 8 ));

        return $"{Convert.ToBase64String(salt)}.{hashed}";
    }

    // Verify password when Login
    public bool VerifyPassword(string password, string passwordHash)
    {
        // Password in Database
        string[] parts = passwordHash.Split('.');
        byte[] salt = Convert.FromBase64String(parts[0]);
        string hash = parts[1];

        // Password from Login
        string hashed = Convert.ToBase64String(KeyDerivation.Pbkdf2(
            password: password,
            salt: salt,
            prf: KeyDerivationPrf.HMACSHA256,
            iterationCount: 100000,
            numBytesRequested: 256 / 8
        ));

        return hash == hashed;
    }

}

