using System;
using System.Security.Cryptography;

namespace Magnetar.ConfigTerminal.Model;

/// <summary>
/// Reproduces the DS server-password hashing exactly, so a password set by this
/// tool actually admits players: PBKDF2 (SHA1), 16-byte random salt, 10000
/// iterations, 20-byte derived key, both stored base64 as
/// <c>ServerPasswordHash</c> / <c>ServerPasswordSalt</c>.
/// </summary>
internal static class PasswordHasher
{
    private const int SaltBytes = 16;
    private const int Iterations = 10000;
    private const int HashBytes = 20;

    public readonly struct HashedPassword
    {
        public HashedPassword(string hashBase64, string saltBase64)
        {
            Hash = hashBase64;
            Salt = saltBase64;
        }

        public string Hash { get; }
        public string Salt { get; }
    }

    public static HashedPassword Hash(string plaintext)
    {
        byte[] salt = new byte[SaltBytes];
        using (var rng = RandomNumberGenerator.Create())
            rng.GetBytes(salt);

        byte[] hash = Derive(plaintext, salt);
        return new HashedPassword(Convert.ToBase64String(hash), Convert.ToBase64String(salt));
    }

    private static byte[] Derive(string plaintext, byte[] salt)
    {
        // The (password, salt, iterations) ctor defaults to SHA1 — matching the
        // DS, which predates configurable HMACs. Kept deliberately over the
        // static Pbkdf2 helper the obsoletion suggests.
#pragma warning disable SYSLIB0060
        using var pbkdf2 = new Rfc2898DeriveBytes(plaintext, salt, Iterations);
#pragma warning restore SYSLIB0060
        return pbkdf2.GetBytes(HashBytes);
    }
}
