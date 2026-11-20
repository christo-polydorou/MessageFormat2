using System.Collections.ObjectModel;

namespace MessageFormat2.Internal;

internal static class ReadOnly
{
    public static IReadOnlyList<T> Copy<T>(IEnumerable<T> items)
    {
        ArgumentNullException.ThrowIfNull(items);
        T[] array = [.. items];
        return array.Length == 0 ? ReadOnlyCollection<T>.Empty : new ReadOnlyCollection<T>(array);
    }
}
