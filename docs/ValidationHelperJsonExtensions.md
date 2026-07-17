# ValidationHelperJsonExtensions

Utility class providing JSON serialization and deserialization for `ValidationResult` objects, enabling round-trip validation state preservation across process boundaries or storage.

## API

### `public static string ToJson(ValidationResult result)`

Serializes a `ValidationResult` to a JSON string.

- **Parameters**
  - `result`: The `ValidationResult` to serialize.
- **Return value**
  - A JSON string representing the serialized `ValidationResult`.
- **Exceptions**
  - Throws `ArgumentNullException` if `result` is `null`.

---

### `public static ValidationResult? FromJson(string? json)`

Deserializes a JSON string back into a `ValidationResult`.

- **Parameters**
  - `json`: The JSON string to deserialize. May be `null`.
- **Return value**
  - The deserialized `ValidationResult`, or `null` if `json` is `null` or invalid.
- **Exceptions**
  - None.

---

### `public static bool TryFromJson(string? json, out ValidationResult? result)`

Attempts to deserialize a JSON string into a `ValidationResult`.

- **Parameters**
  - `json`: The JSON string to deserialize. May be `null`.
  - `result`: Output parameter receiving the deserialized `ValidationResult` or `null`.
- **Return value**
  - `true` if deserialization succeeds; otherwise, `false`.
- **Exceptions**
  - None.

---

### `public bool IsValid`

Gets a value indicating whether the validation succeeded.

- **Return value**
  - `true` if validation succeeded; otherwise, `false`.

---

### `public string? ErrorMessage`

Gets the error message associated with a failed validation.

- **Return value**
  - The error message, or `null` if validation succeeded or no message was set.

---

### `public object? Value`

Gets the validated value.

- **Return value**
  - The validated value, or `null` if not set.

---
### `public string? ValidationType`

Gets the type of validation performed.

- **Return value**
  - The validation type identifier, or `null` if not set.

---
### `public ValidationResult`

Gets the underlying `ValidationResult` instance.

- **Return value**
  - The `ValidationResult` instance.

---
### `public static ValidationResult Success`

Gets a `ValidationResult` representing a successful validation.

- **Return value**
  - A `ValidationResult` with `IsValid` set to `true`.

---
### `public static ValidationResult Failure(string errorMessage, object? value = null, string? validationType = null)`

Creates a `ValidationResult` representing a failed validation.

- **Parameters**
  - `errorMessage`: The error message describing the failure.
  - `value`: Optional value associated with the failure.
  - `validationType`: Optional type identifier for the validation.
- **Return value**
  - A `ValidationResult` with `IsValid` set to `false`.
- **Exceptions**
  - Throws `ArgumentNullException` if `errorMessage` is `null`.

## Usage
