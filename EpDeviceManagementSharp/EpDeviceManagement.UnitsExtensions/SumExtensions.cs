using UnitsNet;

namespace EpDeviceManagement.UnitsExtensions;

public static class SumExtensions
{
    public static Power Sum(this IEnumerable<Power> sequence)
    {
        return sequence.Aggregate(Power.Zero, PowerSum);
    }

    private static Power PowerSum(Power left, Power right) => left + right;
}