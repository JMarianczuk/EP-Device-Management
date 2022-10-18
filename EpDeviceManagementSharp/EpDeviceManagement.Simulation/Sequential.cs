namespace EpDeviceManagement.Simulation;

public class Sequential
{
    public static ParallelLoopResult ForEach<TSource>(
        IEnumerable<TSource> source,
        ParallelOptions parallelOptions,
        Action<TSource> body)
    {
        foreach (var entry in source)
        {
            body(entry);
        }

        return new ParallelLoopResult();
    }
}