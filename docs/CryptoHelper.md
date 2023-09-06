# CryptoHelper

Utility class providing common cryptographic helpers such as hashing, HMAC computation, random token generation, and fingerprint creation. All members are static and stateless, intended for easy consumption across the application.

## API

### HashSha256
Computes the SHA‑256 hash of the supplied input.

- **Parameters**  
  - `input`: The string to be hashed.  
- **Return value**  
  - A hexadecimal string representing the SHA‑256 hash.  
- **Exceptions**  
  - `ArgumentNullException` if `input` is `null`.

### HashMd5
Computes the MD5 hash of the supplied input.

- **Parameters**  
  - `input`: The string to be hashed.  
- **Return value**  
  - A hexadecimal string representing the MD5 hash.  
- **Exceptions**  
  - `ArgumentNullException` if `input` is `null`.

### GenerateRandomToken
Generates a cryptographically secure random token of the specified length.

- **Parameters**  
  - `length`: Desired number of characters in the token. Must be greater than zero.  
- **Return value**  
  - A string containing the random token (Base64‑url safe characters).  
- **Exceptions**  
  - `ArgumentOutOfRangeException` if `length` is less than or equal to zero.

### VerifyHashSha256
Verifies that the SHA‑256 hash of `input` matches the supplied `hash`.

- **Parameters**  
  - `input`: The original string to hash.  
  - `hash`: The expected hash string (hexadecimal).  
- **Return value**  
  - `true` if the computed hash equals `hash`; otherwise `false`.  
- **Exceptions**  
  - `ArgumentNullException` if either `input` or `hash` is `null`.

### ComputeHmacSha256
Computes an HMAC‑SHA256 value for the supplied input using the given key.

- **Parameters**  
  - `input`: The data to be authenticated.  
  - `key`: The secret key used for the HMAC operation.  
- **Return value**  
  - A hexadecimal string representing the HMAC‑SHA256 value.  
- **Exceptions**  
  - `ArgumentNullException` if `input` or `key` is `null`.

### VerifyHmacSha256
Verifies that the HMAC‑SHA256 of `input` with `key` matches the supplied `hmac`.

- **Parameters**  
  - `input`: The original data.  
  - `key`: The secret key used for HMAC.  
  - `hmac`: The expected HMAC value (hexadecimal).  
- **Return value**  
  - `true` if the computed HMAC equals `hmac`; otherwise `false`.  
- **Exceptions**  
  - `ArgumentNullException` if any parameter is `null`.

### GenerateFingerprint
Produces a deterministic fingerprint (typically a truncated SHA‑256 hash) for the supplied input.

- **Parameters**  
  - `input`: The string to fingerprint.  
- **Return value**  
  - A string representing the fingerprint.  
- **Exceptions**  
  - `ArgumentNullException` if `input` is `null`.

## Usage

```csharp
// Example 1: Hash a password and later verify it.
string password = "P@ssw0rd!";
string hash = CryptoHelper.HashSha256(password);
// Store `hash` in a database.
// Later, when the user logs in:
bool isValid = CryptoHelper.VerifyHashSha256(password, hash);
Console.WriteLine(isValid ? "Login successful" : "Invalid credentials");
```

```csharp
// Example 2: Generate a random token for email confirmation and verify an HMAC.
string token = CryptoHelper.GenerateRandomToken(32); // 32‑character token
string key = "shared-secret-key";
string hmac = CryptoHelper.ComputeHmacSha256(token, key);
// Send `token` to the user; store `hmac` alongside it.
// When the token is returned:
bool tokenIsAuthentic = CryptoHelper.VerifyHmacSha256(token, key, hmac);
if (tokenIsAuthentic)
{
    // Token is genuine; proceed with confirmation.
}
```

## Notes

- All methods treat a `null` argument as an error and throw `ArgumentNullException`. Empty strings are permitted and will be processed normally (e.g., hashing an empty string yields a deterministic hash).  
- `GenerateRandomToken` uses a cryptographically strong random number generator; requesting a length of zero or a negative value results in an `ArgumentOutOfRangeException`.  
- Because the class contains only static, stateless members, it is inherently thread‑safe; no internal state is modified during invocation.  
- The fingerprint produced by `GenerateFingerprint` is intended for non‑security‑critical identification purposes (e.g., deduplication). If a cryptographically secure identifier is required, prefer `HashSha256` or `ComputeHmacSha256`.  
- Hexadecimal output strings are lower‑case; callers should normalise case if case‑insensitive comparison is needed.
