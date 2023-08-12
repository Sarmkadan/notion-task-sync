# VectorClock

A logical clock used to track causality of operations in a distributed collaborative editing session. Each instance maintains a per‑participant tick count, supports merging of concurrent histories, and provides helpers for operational transformation and inversion of edit operations.

## API

### Tick()
- **Purpose**: Increments the logical tick for the local participant by one.
- **Parameters**: None.
- **Return value**: None (`void`).
- **Exceptions**: Throws `InvalidOperationException` if the increment would exceed `Int64.MaxValue`.

### Get()
- **Purpose**: Retrieves the current logical tick value for the local participant.
- **Parameters**: None.
- **Return value**: The current tick count as an `Int64`.
- **Exceptions**: Throws `InvalidOperationException` if the vector clock is in an inconsistent state (e.g., after being disposed).

### Merge()
- **Purpose**: Reconciles this vector clock with another vector clock (supplied via internal context) by taking the maximum tick for each participant.
- **Parameters**: None.
- **Return value**: None (`void`).
- **Exceptions**: Throws `InvalidOperationException` if the merge would violate causality constraints (e.g., attempting to merge a clock that is not comparable).

### HappensBefore()
- **Purpose**: Determines whether this vector clock precedes another vector clock (the other clock is supplied via internal context).
- **Parameters**: None.
- **Return value**: `true` if this clock happens before the other clock; otherwise `false`.
- **Exceptions**: Throws `ArgumentNullException` if the other clock reference is `null`.

### Clone()
- **Purpose**: Creates a deep copy of the vector clock, preserving all tick counts and associated metadata.
- **Parameters**: None.
- **Return value**: A new `VectorClock` instance with identical state.
- **Exceptions**: Throws `OutOfMemoryException` if memory allocation for the copy fails.

### TransformContext
- **Purpose**: Immutable record that holds contextual data required for operational transformation (e.g., the base vector clock and participant identifier).
- **Fields**: None listed – the record is initialized via its constructor and cannot be modified after creation.
- **Return value**: N/A (type definition).
- **Exceptions**: N/A.

### Operation
- **Purpose**: Immutable record representing a single edit operation (insert, delete, or no‑op) together with its metadata such as position, payload, and originating participant.
- **Fields**: None listed – the record is initialized via its constructor and cannot be modified after creation.
- **Return value**: N/A.
- **Exceptions**: N/A.

### Create()
- **Purpose**: Factory method that constructs an `Operation` instance with the supplied attributes.
- **Parameters**: Not shown in the signature; expected to include operation type, position, payload, and participant identifier.
- **Return value**: A new `Operation`.
- **Exceptions**: Throws `ArgumentException` if required parameters are missing or invalid; throws `ArgumentOutOfRangeException` for numeric parameters outside the allowed range.

### Invert()
- **Purpose**: Produces the inverse of this operation, suitable for undoing its effect.
- **Parameters**: None.
- **Return value**: An `Operation` that reverses the effect of this instance.
- **Exceptions**: Throws `InvalidOperationException` if the operation cannot be inverted (e.g., a no‑op).

### TransformResult
- **Purpose**: Immutable record that holds the outcome of transforming two concurrent operations, including the transformed operation and a flag indicating whether the operation was nullified.
- **Fields**: None listed – the record is initialized via its constructor and cannot be modified after creation.
- **Return value**: N/A.
- **Exceptions**: N/A.

### BatchId
- **Purpose**: Unique identifier for the batch of operations to which this vector clock belongs.
- **Return value**: A `Guid`.
- **Exceptions**: None.

### SessionId
- **Purpose**: Identifier for the session in which the vector clock was created.
- **Return value**: A `string` (required; must be set via object initializer).
- **Exceptions**: Throws `ArgumentNullException` if set to `null`.

### ParticipantId
- **Purpose**: Identifier for the participant that owns this vector clock.
- **Return value**: A `string` (required; must be set via object initializer).
- **Exceptions**: Throws `ArgumentNullException` if set to `null`.

### Operations
- **Purpose**: Read‑only list of operations associated with this vector clock.
- **Return value**: An `IReadOnlyList<Operation>`.
- **Exceptions**: None.

### CreatedAt
- **Purpose**: Timestamp indicating when the vector clock was instantiated.
- **Return value**: A `DateTime`.
- **Exceptions**: None.

## Usage

```csharp
// Example 1: Basic tick and clone usage
var vc = new VectorClock
{
    BatchId = Guid.NewGuid(),
    SessionId = "sess-123",
    ParticipantId = "alice",
    Operations = new List<Operation>(),
    CreatedAt = DateTime.UtcNow
};

vc.Tick();                     // increment local logical clock
long current = vc.Get();       // read the current tick (e.g., 1)
VectorClock copy = vc.Clone(); // obtain an independent snapshot
```

```csharp
// Example 2: Operation creation, inversion, and transformation context
Operation op = Operation.Create(
    type: OperationType.Insert,
    position: 5,
    payload: "hello",
    participantId: "bob"
);

Operation inverse = op.Invert(); // produces a delete of "hello" at position 5

var ctx = new TransformContext
{
    BaseVectorClock = vc,
    ParticipantId = "bob"
};

// Assuming a Transform method exists elsewhere:
// TransformResult result = Transform(op, concurrentOp, ctx);
```

## Notes

- **Thread safety**: `VectorClock` is not thread‑safe. Concurrent calls to `Tick`, `Merge`, or any method that mutates state must be protected by external synchronization (e.g., `lock`). Reading `Get`, `Clone`, or accessing the immutable properties (`BatchId`, `SessionId`, `ParticipantId`, `Operations`, `CreatedAt`) is safe after construction, but not while another thread is mutating the instance.
- **Required properties**: `SessionId` and `ParticipantId` are declared with the `required` modifier; object initialization must assign non‑null values, otherwise compilation fails.
- **Tick overflow**: The logical tick is stored as an `Int64`. Repeated calls to `Tick` will eventually reach `Int64.MaxValue`; the next call throws `InvalidOperationException` to prevent silent wrap‑around.
- **Merge semantics**: `Merge` assumes the other clock is comparable (i.e., no concurrent updates that violate causality). If the clocks are incomparable, the method throws; callers should first use `HappensBefore` or a similar check to determine compatibility.
- **Immutability of nested records**: `TransformContext`, `Operation`, and `TransformResult` are `sealed record` types. Their instances are immutable and safe to publish across threads without additional synchronization.
- **Operation inversion**: Not all operations are invertible (e.g., a no‑op). `Invert` throws `InvalidOperationException` in such cases; callers should catch the exception or verify invertibility beforehand if needed.
