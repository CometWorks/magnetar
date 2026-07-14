# ConfigTerminal/Model/PasswordHasher.cs

**Project:** ConfigTerminal · **Namespace:** `Magnetar.ConfigTerminal.Model` · **Kind:** static class · **Lines:** 50

## Summary
Reproduces the DS server-password hashing exactly, so a password set by this tool actually admits players: PBKDF2 (SHA1), 16-byte random salt, 10000 iterations, 20-byte derived key, both stored base64 as `ServerPasswordHash` / `ServerPasswordSalt`.

## Types
### PasswordHasher — static class, internal
- **Consts:** `SaltBytes` (16), `Iterations` (10000), `HashBytes` (20).
- **Methods:**
  - `Hash(string plaintext)` — generates a random salt via `RandomNumberGenerator`, derives the key, returns a `HashedPassword` with both base64-encoded.
  - `Derive(plaintext, salt)` (private static) — uses `Rfc2898DeriveBytes(password, salt, iterations)` which defaults to SHA1 (matching the DS); `SYSLIB0060` obsoletion is suppressed deliberately.
### PasswordHasher.HashedPassword — readonly struct, public
Carries `Hash` and `Salt` (base64 strings).

## Cross-references
- **Uses:** `System.Security.Cryptography` (`RandomNumberGenerator`, `Rfc2898DeriveBytes`).
- **Used by:** [DedicatedConfigDocument.cs](DedicatedConfigDocument.cs.md), [ProcessAndFileTests.cs](../../ConfigTerminalTests/ProcessAndFileTests.cs.md)
