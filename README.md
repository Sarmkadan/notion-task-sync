// ... existing content ...

## TaskExtensions

The `TaskExtensions` class provides utility methods for evaluating task status, priority, and tags. It includes methods to check if a task is overdue, determine priority levels, and inspect tags.

### Usage Example

```csharp
using Domain.Models;

class Program
{
    static void Main()
    {
        // Assume we have a Task instance (populated elsewhere)
        Task task = new Task
        {
            // Task initialization here
        };

        // Check task status
        bool isOverdue = TaskExtensions.IsOverdue(task);
        bool isHighPriority = TaskExtensions.IsHighPriority(task);
        bool isBlocked = TaskExtensions.IsBlocked(task);

        // Get priority level and age
        string priorityLevel = TaskExtensions.GetPriorityLevel(task);
        int ageInDays = TaskExtensions.GetAgeInDays(task);

        // Check tags
        bool hasUrgentTag = TaskExtensions.HasTag(task, "urgent");
        IEnumerable<string> tags = TaskExtensions.GetTagList(task);

        Console.WriteLine($"Is Overdue: {isOverdue}");
        Console.WriteLine($"Priority Level: {priorityLevel}");
        Console.WriteLine($"Has 'urgent' tag: {hasUrgentTag}");
    }
}
```

// ... existing content ...
