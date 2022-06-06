using UnitsNet;

namespace EpDeviceManagement.UnitsExtensions;

public static class FrequencyExtensions
{
    public static double Multiply(this Frequency frequency, TimeSpan timeSpan)
    {
        return frequency.Hertz * timeSpan.TotalSeconds;
    }

    public static Power Times(this Frequency frequency, Energy energy)
    {
        return Power.FromWatts(energy.Joules * frequency.PerSecond);
    }
}