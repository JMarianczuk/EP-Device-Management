using Microsoft.CodeAnalysis;

namespace EpDeviceManagement.Data.Generation;

public static class IncrementalValueProviderExtensions
{
    public static IncrementalValueProvider<(TFirst, TSecond, TThird)> Flatten<TFirst, TSecond, TThird>(
        this IncrementalValueProvider<((TFirst, TSecond), TThird)> ivp)
    {
        return ivp.Select((nestedTuple, _) => (nestedTuple.Item1.Item1, nestedTuple.Item1.Item2, nestedTuple.Item2));
    }
}