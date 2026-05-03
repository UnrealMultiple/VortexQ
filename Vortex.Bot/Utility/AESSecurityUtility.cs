using System.Security.Cryptography;
using System.Text;

namespace Vortex.Bot.Utility;

public class AESSecurityUtility
{
    public static readonly byte[] BufferKey = RandomNumberGenerator.GetBytes(32);

    public static readonly byte[] BufferNonce = RandomNumberGenerator.GetBytes(12);
    public static string Encrypt(string value)
    {
        const int tagSize = 16;
        const int nonceSize = 12;

        using var aes = new AesGcm(BufferKey, tagSize);
        byte[] plainBytes = Encoding.UTF8.GetBytes(value);
        byte[] cipherBytes = new byte[plainBytes.Length];
        byte[] tag = new byte[tagSize];

        aes.Encrypt(BufferNonce, plainBytes, cipherBytes, tag);

        byte[] result = new byte[nonceSize + tagSize + cipherBytes.Length];
        Buffer.BlockCopy(BufferNonce, 0, result, 0, nonceSize);
        Buffer.BlockCopy(tag, 0, result, nonceSize, tagSize);
        Buffer.BlockCopy(cipherBytes, 0, result, nonceSize + tagSize, cipherBytes.Length);

        return Convert.ToBase64String(result);
    }

    public static string Decrypt(string cipherTextBase64)
    {
        const int tagSize = 16;
        const int nonceSize = 12;

        byte[] full = Convert.FromBase64String(cipherTextBase64);

        if (full.Length < nonceSize + tagSize)
            throw new ArgumentException("Invalid cipher text");

        byte[] nonce = new byte[nonceSize];
        byte[] tag = new byte[tagSize];
        byte[] cipherBytes = new byte[full.Length - nonceSize - tagSize];

        Buffer.BlockCopy(full, 0, nonce, 0, nonceSize);
        Buffer.BlockCopy(full, nonceSize, tag, 0, tagSize);
        Buffer.BlockCopy(full, nonceSize + tagSize, cipherBytes, 0, cipherBytes.Length);

        using var aes = new AesGcm(BufferKey, tagSize);
        byte[] plainBytes = new byte[cipherBytes.Length];
        aes.Decrypt(nonce, cipherBytes, tag, plainBytes);

        return Encoding.UTF8.GetString(plainBytes);
    }
}
