namespace EpDeviceManagementSharp.Simulation.Heating.Tests;

public static class ArrayExtensions
{
    public static IEnumerable<TElement> Iterate<TElement>(
        this TElement[] source,
        int start,
        int count)
    {
        int index = start;
        while (count > 0)
        {
            for (int i = index; i < source.Length && count > 0; i += 1)
            {
                yield return source[i];
                count -= 1;
            }

            index = 0;
        }
    }
}