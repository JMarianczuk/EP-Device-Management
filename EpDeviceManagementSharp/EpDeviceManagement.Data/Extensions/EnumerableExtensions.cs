namespace EpDeviceManagement.Data.Extensions;

public static class EnumerableExtensions
{
    public static async IAsyncEnumerable<TElement> AsAsyncEnumerable<TElement>(this IEnumerable<TElement> source)
    {
        foreach (var element in source)
        {
            yield return element;
        }
    }
}