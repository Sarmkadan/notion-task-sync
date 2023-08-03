# CryptoHelperTests

Unit tests for the `CryptoHelper` class, verifying cryptographic operations such as hashing, token generation, and HMAC computation. Ensures correctness and edge-case handling for SHA-256, MD5, and random token generation.

## API

### `public void HashSha256_ReturnsExpectedHash_ForValidInput()`
Verifies that `CryptoHelper.HashSha256` produces the correct SHA-256 hash for a given input string. The test asserts that the output matches a precomputed expected hash value.

### `public void HashSha256_ReturnsEmptyString_ForNullOrEmptyInput()`
Ensures that `CryptoHelper.HashSha256` returns an empty string when the input is `null` or empty. Validates graceful handling of edge cases.

### `public void HashMd5_ReturnsExpectedHash_ForValidInput()`
Confirms that `CryptoHelper.HashMd5` generates the correct MD5 hash for a valid input string. Compares the result against a known hash value.

### `public void GenerateRandomToken_ReturnsStringOfExpectedLength_ForValidLength()`
Tests that `CryptoHelper.GenerateRandomToken` returns a string of the requested length when provided with a valid positive integer. Validates length consistency.

### `public void GenerateRandomToken_ThrowsArgumentException_ForInvalidLength()`
Checks that `CryptoHelper.GenerateRandomToken` throws an `ArgumentException` when the requested token length is zero or negative. Ensures input validation.

### `public void VerifyHashSha256_ReturnsTrue_ForMatchingHash()`
Validates that `CryptoHelper.VerifyHashSha256` returns `true` when the provided hash matches the computed hash of the input data. Tests correct verification logic.

### `public void ComputeHmacSha256_ReturnsExpectedSignature_ForValidInput()`
Ensures that `CryptoHelper.ComputeHmacSha256` produces the expected HMAC-SHA256 signature for a given input and key. Compares the output to a known signature.

## Usage
