# ChangeLogExtensions

Provides extension methods for `ChangeLog` objects that facilitate auditing, display formatting, and classification of property changes within the notion-task-sync system. These helpers centralize common transformations and queries performed on change-log entries, ensuring consistent representation and filtering logic across the application.

## API

### ToAuditString

```csharp
public static string ToAuditString(this ChangeLog changeLog)
```

Converts a `ChangeLog` instance into a human-readable, single-line string suitable for audit trails and log output. The format includes the timestamp, user identifier, and a summary of the change.

**Parameters**
- `changeLog` (`ChangeLog`): The change-log entry to format. Must not be `null`.

**Return Value**
- `string`: A formatted audit string representing the change.

**Exceptions**
- `ArgumentNullException`: Thrown when `changeLog` is `null`.

---

### IsPropertyChange

```csharp
public static bool IsPropertyChange(this ChangeLog changeLog)
```

Determines whether the change-log entry represents a modification to a tracked property value, as opposed to a structural or lifecycle event (e.g., creation, deletion, or relationship changes).

**Parameters**
- `changeLog` (`ChangeLog`): The entry to evaluate. Must not be `null`.

**Return Value**
- `bool`: `true` if the change is a property-level modification; otherwise `false`.

**Exceptions**
- `ArgumentNullException`: Thrown when `changeLog` is `null`.

---

### ToDisplayString

```csharp
public static string ToDisplayString(this ChangeLog changeLog)
```

Produces a user-facing, formatted description of the change, intended for UI presentation or notification messages. The output is localized or styled according to the current display conventions of the application.

**Parameters**
- `changeLog` (`ChangeLog`): The entry to format. Must not be `null`.

**Return Value**
- `string`: A display-ready description of the change.

**Exceptions**
- `ArgumentNullException`: Thrown when `changeLog` is `null`.

---

### IsUserInitiated

```csharp
public static bool IsUserInitiated(this ChangeLog changeLog)
```

Indicates whether the change originated from an explicit user action, distinguishing user-driven modifications from automated system operations, synchronization side effects, or internal maintenance tasks.

**Parameters**
- `changeLog` (`ChangeLog`): The entry to classify. Must not be `null`.

**Return Value**
- `bool`: `true` if the change was initiated by a user; otherwise `false`.

**Exceptions**
- `ArgumentNullException`: Thrown when `changeLog` is `null`.

## Usage

### Example 1: Building an Audit Report

```csharp
IEnumerable<ChangeLog> recentChanges = task.GetRecentChanges();

var auditLines = recentChanges
    .Where(change => change.IsUserInitiated())
    .Select(change => change.ToAuditString());

foreach (var line in auditLines)
{
    Console.WriteLine(line);
}
```

This example filters a collection of change-log entries to only those initiated by users, then formats each as an audit string for logging or console output.

### Example 2: Displaying Property Changes in a UI

```csharp
ChangeLog latestChange = task.LatestChange;

if (latestChange.IsPropertyChange())
{
    string displayText = latestChange.ToDisplayString();
    notificationBanner.Show(displayText);
}
```

Here, the code checks whether the most recent change is a property modification before presenting a user-facing notification. Non-property changes (such as task creation or archival) are suppressed from this particular UI element.

## Notes

- All methods perform a null-guard on the `changeLog` parameter and will throw `ArgumentNullException` if it is `null`. Callers should either validate inputs beforehand or handle the exception at the call site.
- The classification performed by `IsPropertyChange` and `IsUserInitiated` relies on metadata embedded within the `ChangeLog` object. Changes that lack sufficient metadata (e.g., legacy entries or those produced by older synchronization runs) may return `false` for both methods.
- `ToAuditString` and `ToDisplayString` may produce different representations for the same entry. The audit string is designed for machine-readability and archival consistency, while the display string may include richer formatting or localized text.
- These methods are pure extension methods with no mutable state or side effects. They are safe to call concurrently from multiple threads, provided the underlying `ChangeLog` instance is not being mutated during the call.
