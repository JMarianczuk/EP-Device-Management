using EpDeviceManagement.Contracts;
using UnitsNet;

namespace EpDeviceManagement.Control.Extensions;

public static class LoadGenerationExtensions
{
    public static Power Sum(this ILoad[] loads)
    {
        var result = Power.Zero;
        for (int i = 0; i < loads.Length; i += 1)
        {
            result += loads[i].MomentaneousDemand;
        }

        return result;
    }

    public static Power Sum(this IGenerator[] generators)
    {
        var result = Power.Zero;
        for (int i = 0; i < generators.Length; i += 1)
        {
            result += generators[i].MomentaneousGeneration;
        }

        return result;
    }
}