#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

namespace NotionTaskSync.Utils;

using System;
using System.Security.Cryptography;
using System.Text;

/// <summary>
/// Helper class for cryptographic operations.
/// Provides hashing and encryption utilities for sensitive data protection.
/// Used for API key storage and data integrity verification.
/// </summary>
public sealed class CryptoHelper
{
    /// <summary>
    /// Computes a SHA256 hash of a string.
    /// Uses the modern static <c>SHA256.HashData</c> API (available since .NET 6)
    /// to avoid the older <c>SHA256.Create()</c> disposable pattern.
    /// Output format (Base64) is unchanged for compatibility with persisted data.
    /// </summary>
    public static string HashSha256(string input)
    {
        if (string.IsNullOrEmpty(input))
            return string.Empty;

        // Convert input to UTF‑8 bytes and compute hash using the static API.
        var inputBytes = Encoding.UTF8.GetBytes(input);
        var hashedBytes = SHA256.HashData(inputBytes);
        return Convert.ToBase64String(hashedBytes);
    }

    /// <summary>
    /// Computes an MD5 hash of a string.
    /// Uses the modern static <c>MD5.HashData</c> API (available since .NET 6).
    /// Output format (Base64) is unchanged for compatibility.
    /// </summary>
    public static string HashMd5(string input)
    {
        if (string.IsNullOrEmpty(input))
            return string.Empty;

        var inputBytes = Encoding.UTF8.GetBytes(input);
        var hashedBytes = MD5.HashData(inputBytes);
        return Convert.ToBase64String(hashedBytes);
    }

    /// <summary>
    /// Generates a random token suitable for authentication.
    /// Returns a cryptographically secure random string.
    /// </summary>
    public static string GenerateRandomToken(int length = 32)
    {
        if (length < 8)
            throw new ArgumentException("Token length must be at least 8", nameof(length));

        using (var rng = RandomNumberGenerator.Create())
        {
            var tokenData = new byte[length];
            rng.GetBytes(tokenData);
            return Convert.ToBase64String(tokenData);
        }
    }

    /// <summary>
    /// Verifies if a plaintext matches a SHA256 hash.
    /// Used for password verification without storing plain text.
    /// </summary>
    public static bool VerifyHashSha256(string plaintext, string hash)
    {
        var computedHash = HashSha256(plaintext);
        return computedHash == hash;
    }

    /// <summary>
    /// Computes HMAC‑SHA256 signature of data with a key.
    /// Uses the modern static <c>HMACSHA256.HashData</c> API (available since .NET 6).
    /// Output format (Base64) is unchanged for compatibility.
    /// </summary>
    public static string ComputeHmacSha256(string data, string key)
    {
        if (string.IsNullOrEmpty(data) || string.IsNullOrEmpty(key))
            return string.Empty;

        var keyBytes = Encoding.UTF8.GetBytes(key);
        var dataBytes = Encoding.UTF8.GetBytes(data);
        var signedBytes = HMACSHA256.HashData(keyBytes, dataBytes);
        return Convert.ToBase64String(signedBytes);
    }

    /// <summary>
    /// Verifies an HMAC‑SHA256 signature.
    /// Checks if data hasn't been tampered with.
    /// </summary>
    public static bool VerifyHmacSha256(string data, string signature, string key)
    {
        var computed = ComputeHmacSha256(data, key);
        // Use constant-time comparison to prevent timing attacks
        return ConstantTimeEquals(computed, signature);
    }

    /// <summary>
    /// Constant-time string comparison to prevent timing attacks.
    /// Always takes the same amount of time regardless of match position.
    /// </summary>
    private static bool ConstantTimeEquals(string a, string b)
    {
        if (string.IsNullOrEmpty(a) && string.IsNullOrEmpty(b))
            return true;

        if (string.IsNullOrEmpty(a) || string.IsNullOrEmpty(b))
            return false;

        if (a.Length != b.Length)
            return false;

        var result = 0;
        for (int i = 0; i < a.Length; i++)
        {
            result |= a[i] ^ b[i];
        }

        return result == 0;
    }

    /// <summary>
    /// Generates a cryptographic fingerprint of an object.
    /// Useful for detecting changes without storing full content.
    /// </summary>
    public static string GenerateFingerprint(string content)
    {
        return HashSha256(content).Substring(0, 16);
    }
}
