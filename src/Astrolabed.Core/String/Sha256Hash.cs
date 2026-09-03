using System.Security.Cryptography;
using System.Text;

namespace StringHasher;

public static class SHA256Hash
{

    public static string GetSHA256Hash(this string input)
    {
        byte[] inputBytes = Encoding.UTF8.GetBytes(input);
        byte[] hashBytes = SHA256.HashData(inputBytes);

        return Convert.ToHexStringLower(hashBytes);
    }

    public static string GetBase64SHA256Hash(this string input)
    {
        byte[] inputBytes = Encoding.UTF8.GetBytes(input);
        byte[] hashBytes = SHA256.HashData(inputBytes);

        return Convert.ToBase64String(hashBytes);
    }

}
