using Content.Shared.Stacks;

namespace Content.Shared._NF.LoggingExtensions;

public static class LoggingExtensions
{
    public static string GetExtraLogs(EntityManager entityManager, EntityUid entity)
    {
        // Get details from the stack component to track amount of things in the stack.
        if (entityManager.TryGetComponent<StackComponent>(entity, out var stack))
        {
            return $"(StackCount: {stack.Count.ToString()})";
        }

        // Add more logging things here when needed.

        return "";
    }
}
