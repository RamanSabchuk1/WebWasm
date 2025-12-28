using System.Text;

namespace WebWasm.Services;

public class EncryptionService
{
    private readonly string _key = "a-very-secret-and-super-long-key-that-is-not-really-secret";

    public string Encrypt(string data)
    {
        if (string.IsNullOrEmpty(data))
        {
            return string.Empty;
        }

        var dataBytes = Encoding.UTF8.GetBytes(data);
        var keyBytes = Encoding.UTF8.GetBytes(_key);
        var encryptedBytes = new byte[dataBytes.Length];

        for (int i = 0; i < dataBytes.Length; i++)
        {
            encryptedBytes[i] = (byte)(dataBytes[i] ^ keyBytes[i % keyBytes.Length]);
        }

        return Convert.ToBase64String(encryptedBytes);
    }

    public string Decrypt(string encryptedData)
    {
        if (string.IsNullOrEmpty(encryptedData))
        {
            return string.Empty;
        }

        try
        {
            var encryptedBytes = Convert.FromBase64String(encryptedData);
            var keyBytes = Encoding.UTF8.GetBytes(_key);
            var decryptedBytes = new byte[encryptedBytes.Length];

            for (int i = 0; i < encryptedBytes.Length; i++)
            {
                decryptedBytes[i] = (byte)(encryptedBytes[i] ^ keyBytes[i % keyBytes.Length]);
            }

            return Encoding.UTF8.GetString(decryptedBytes);
        }
        catch
        {
            return string.Empty;
        }
    }
}