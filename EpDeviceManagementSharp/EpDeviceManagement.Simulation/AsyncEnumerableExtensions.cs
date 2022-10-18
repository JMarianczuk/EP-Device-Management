namespace EpDeviceManagement.Simulation;

public static class AsyncEnumerableExtensions
{
    public static IAsyncEnumerable<TElement> SkipLast<TElement>(this IAsyncEnumerable<TElement> source, int skipLast)
    {
        if (skipLast == 0)
        {
            return source;
        }

        return SkipLastGenerator(source, skipLast);
    }

    private static async IAsyncEnumerable<TElement> SkipLastGenerator<TElement>(
        IAsyncEnumerable<TElement> source,
        int skipLast)
    {
        
        TElement[] buffer = new TElement[skipLast];
        int index = 0;
        bool initialFill = false;
        await foreach (var element in source)
        {
            if (!initialFill)
            {
                initialFill = index == skipLast - 1;
            }
            else
            {
                yield return buffer[index];
            }

            buffer[index] = element;
            index = (index + 1) % skipLast;
        }
    }

    public static async IAsyncEnumerable<TElement> AsAsyncEnumerable<TElement>(this IEnumerable<TElement> source)
    {
        foreach (var element in source)
        {
            yield return element;
        }
    }

    public static async Task<IEnumerable<TElement>> CollectAsync<TElement>(this IAsyncEnumerable<TElement> source)
    {
        var result = new List<TElement>();
        await foreach (var element in source)
        {
            result.Add(element);
        }

        return result;
    }
}