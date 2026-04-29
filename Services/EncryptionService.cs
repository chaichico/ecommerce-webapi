namespace Services;
using Interfaces;
using System.Security.Cryptography;
using System.Text;

public class EncryptionService : IEncryptionService
{
    private readonly byte[] _key;

    public EncryptionService(IConfiguration configuration)
    {
        // อ่าน key จาก appsettings หรือ ENV
        string keyString = configuration["Encryption:Key"] ?? throw new InvalidOperationException("Encryption:Key is not configured");
        
        _key = Encoding.UTF8.GetBytes(keyString);
        if (_key.Length != 16 && _key.Length != 24 && _key.Length != 32)
        {
            throw new InvalidOperationException("Encryption:Key must be 16, 24, or 32 bytes");
        }
    }

    public string Encrypt(string plainText)
    {
        if (string.IsNullOrEmpty(plainText)) return plainText;
        
        using Aes aes = Aes.Create();
        aes.Key = _key;
        aes.GenerateIV();
        
        ICryptoTransform encryptor = aes.CreateEncryptor(aes.Key, aes.IV);
        using MemoryStream ms = new MemoryStream();
        using CryptoStream cs = new CryptoStream(ms, encryptor, CryptoStreamMode.Write);
        using (StreamWriter sw = new StreamWriter(cs))
        {
            sw.Write(plainText);
        }

        byte[] cipherBytes = ms.ToArray();
        byte[] result = new byte[aes.IV.Length + cipherBytes.Length];
        Buffer.BlockCopy(aes.IV, 0, result, 0, aes.IV.Length);
        Buffer.BlockCopy(cipherBytes, 0, result, aes.IV.Length, cipherBytes.Length);
        
        return Convert.ToBase64String(result);
    }
    
    public string Decrypt(string encryptedText)
    {
        if (string.IsNullOrEmpty(encryptedText)) return encryptedText;

        byte[] fullCipher = Convert.FromBase64String(encryptedText);
        if (fullCipher.Length <= 16)
        {
            throw new CryptographicException("Invalid encrypted payload.");
        }

        byte[] iv = new byte[16];
        byte[] cipherBytes = new byte[fullCipher.Length - iv.Length];
        Buffer.BlockCopy(fullCipher, 0, iv, 0, iv.Length);
        Buffer.BlockCopy(fullCipher, iv.Length, cipherBytes, 0, cipherBytes.Length);
        
        using Aes aes = Aes.Create();
        aes.Key = _key;
        aes.IV = iv;
        
        ICryptoTransform decryptor = aes.CreateDecryptor(aes.Key, aes.IV);
        using MemoryStream ms = new MemoryStream(cipherBytes);
        using CryptoStream cs = new CryptoStream(ms, decryptor, CryptoStreamMode.Read);
        using StreamReader sr = new StreamReader(cs);
        
        return sr.ReadToEnd();
    }
}