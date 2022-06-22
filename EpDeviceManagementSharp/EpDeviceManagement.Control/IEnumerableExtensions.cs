namespace EpDeviceManagement.Control;

public static class IEnumerableExtensions
{
    public static (IEnumerable<TDeriving1>, IEnumerable<TElement>)
        GroupByDerivingType<TDeriving1, TElement>(
            this IEnumerable<TElement> source)
            where TDeriving1 : TElement
    {
        var groups = source.GroupBy(
            x =>
            {
                switch (x)
                {
                    case TDeriving1 _:
                        return typeof(TDeriving1);
                    default:
                        return typeof(TElement);
                }
            })
            .ToList();
        return
        (
            groups.First(x => x.Key == typeof(TDeriving1)).Cast<TDeriving1>(),
            groups.First(x => x.Key == typeof(TElement))
        );
    }
    
    public static (IEnumerable<TDeriving1>, IEnumerable<TDeriving2>, IEnumerable<TElement>)
        GroupByDerivingType<TDeriving1, TDeriving2, TElement>(
            this IEnumerable<TElement> source)
            where TDeriving1 : TElement
            where TDeriving2 : TElement
    {
        var groups = source.GroupBy(
            x =>
            {
                switch (x)
                {
                    case TDeriving1 _:
                        return typeof(TDeriving1);
                    case TDeriving2 _:
                        return typeof(TDeriving2);
                    default:
                        return typeof(TElement);
                }
            })
            .ToList();
        return
        (
            groups.First(x => x.Key == typeof(TDeriving1)).Cast<TDeriving1>(),
            groups.First(x => x.Key == typeof(TDeriving2)).Cast<TDeriving2>(),
            groups.First(x => x.Key == typeof(TElement))
        );
    }
}