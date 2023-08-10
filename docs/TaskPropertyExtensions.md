# TaskPropertyExtensions

Utility methods for working with `TaskProperty` objects, including type-safe value access, comparison, cloning, and formatted display.

## API

### `GetTypedValueInvariant<T>`

Retrieves the property value as the specified type `T` using invariant culture formatting rules. Returns `null` if the value cannot be converted to `T`.

- **Parameters**
  - `property`: The `TaskProperty` instance from which to extract the value.
- **Type Parameter**
  - `T`: The target type to convert the value to (e.g., `string`, `int`, `DateTime`).
- **Returns**
  - The typed value if conversion succeeds; otherwise, `null`.
- **Throws**
  - `ArgumentNullException` if `property` is `null`.
  - `InvalidCastException` if the underlying value cannot be converted to `T`.

---

### `SafeUpdateValue`

Safely updates the value of a `TaskProperty` if the new value differs from the current one. Avoids unnecessary updates and ensures thread-safe comparison.

- **Parameters**
  - `property`: The `TaskProperty` instance to update.
  - `newValue`: The new value to assign.
- **Returns**
  - `true` if the value was updated; `false` if the new value equals the current value or if the update failed.
- **Throws**
  - `ArgumentNullException` if `property` is `null`.

---

### `ValueEquals`

Determines whether two `TaskProperty` instances have equivalent values, accounting for type differences and formatted representations.

- **Parameters**
  - `left`: The first `TaskProperty` to compare.
  - `right`: The second `TaskProperty` to compare.
- **Returns**
  - `true` if the values are equivalent; otherwise, `false`.
- **Throws**
  - `ArgumentNullException` if either `left` or `right` is `null`.

---
### `Clone`

Creates a deep copy of a `TaskProperty` instance, including its value and metadata.

- **Parameters**
  - `property`: The `TaskProperty` instance to clone.
- **Returns**
  - A new `TaskProperty` with identical properties and value.
- **Throws**
  - `ArgumentNullException` if `property` is `null`.

---
### `GetFormattedValue`

Returns the string representation of a `TaskProperty`'s value, formatted according to its metadata (e.g., date format, number precision).

- **Parameters**
  - `property`: The `TaskProperty` instance whose value should be formatted.
- **Returns**
  - A culture-invariant string representation of the value.
- **Throws**
  - `ArgumentNullException` if `property` is `null`.

---
## Usage

### Example 1: Type-safe value access and comparison
