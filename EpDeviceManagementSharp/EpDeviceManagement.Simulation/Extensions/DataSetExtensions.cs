using EpDeviceManagement.UnitsExtensions;
using UnitsNet;

namespace EpDeviceManagement.Simulation.Extensions;

public static class DataSetExtensions
{
    public static PowerFast Average(
        this IReadOnlyList<EnhancedPowerDataSet> powerDataSets,
        Func<EnhancedPowerDataSet, PowerFast> selector)
    {
        if (powerDataSets.Count == 0)
        {
            return PowerFast.Zero;
        }
        var sum = PowerFast.Zero;
        for (int i = 0; i < powerDataSets.Count; i += 1)
        {
            sum += selector(powerDataSets[i]);
        }
        return sum / powerDataSets.Count;
    }
}