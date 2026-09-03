using System.Numerics;
using System.Security.Cryptography;
using System.Text;

namespace Astrolabed.Core.String;

/// <summary>
/// Provides extension methods for generating SHA-256 hashes in various formats from string inputs.
/// </summary>
public static class SHA256Hash
{
    /// <summary>
    /// Computes the SHA-256 hash for the specified input string and converts it to a lowercase hexadecimal string representation.
    /// </summary>
    /// <param name="input">The input string to hash.</param>
    /// <returns>A lowercase hexadecimal string representing the 256-bit SHA-256 hash.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="input"/> is <see langword="null"/>.</exception>
    public static string GetSHA256Hash(this string input)
    {
        ArgumentNullException.ThrowIfNull(input);

        byte[] inputBytes = Encoding.UTF8.GetBytes(input);
        byte[] hashBytes = SHA256.HashData(inputBytes);

        return Convert.ToHexStringLower(hashBytes);
    }

    /// <summary>
    /// Computes the SHA-256 hash for the specified input string and converts it to a Base64 encoded string.
    /// </summary>
    /// <param name="input">The input string to hash.</param>
    /// <returns>A Base64 string representation of the 256-bit SHA-256 hash.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="input"/> is <see langword="null"/>.</exception>
    public static string GetBase64SHA256Hash(this string input)
    {
        ArgumentNullException.ThrowIfNull(input);

        byte[] inputBytes = Encoding.UTF8.GetBytes(input);
        byte[] hashBytes = SHA256.HashData(inputBytes);

        return Convert.ToBase64String(hashBytes);
    }

    /// <summary>
    /// Computes the SHA-256 hash for the specified input string and converts the entire byte payload into a 
    /// deterministic <see cref="BigInteger"/> representation.
    /// </summary>
    /// <param name="input">The input string to hash.</param>
    /// <returns>A signed <see cref="BigInteger"/> computed from the SHA-256 hash byte array using big-endian byte ordering.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="input"/> is <see langword="null"/>.</exception>
    public static BigInteger GetUniqueNumericHash(this string input)
    {
        ArgumentNullException.ThrowIfNull(input);

        byte[] inputBytes = Encoding.UTF8.GetBytes(input);
        byte[] hashBytes = SHA256.HashData(inputBytes);

        return new BigInteger(hashBytes, isUnsigned: false, isBigEndian: true);
    }
}
