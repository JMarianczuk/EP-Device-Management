namespace EpDeviceManagement.UnitsExtensions;

public static class EnumerableExtensions
{
    public static PowerFast Max(this IEnumerable<PowerFast> powerValues)
    {
        var result = PowerFast.FromWatts(double.MinValue);
        foreach (var element in powerValues)
        {
            result = Units.Max(result, element);
        }

        if (result.Watts == double.MinValue)
        {
            throw new Exception();
        }

        return result;
    }
}