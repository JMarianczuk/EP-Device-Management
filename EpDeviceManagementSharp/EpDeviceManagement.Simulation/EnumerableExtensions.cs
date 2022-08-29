using System.Collections;

namespace EpDeviceManagement.Simulation;

public static class EnumerableExtensions
{
    public static int Count(this IEnumerable source)
    {
        int result = 0;
        foreach (var _ in source)
        {
            result += 1;
        }

        return result;
    }
}