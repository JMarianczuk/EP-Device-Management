
using UnitsNet;

namespace EpDeviceManagement.UnitsExtensions;
public static class EnergyExtensions
{
    public static TemperatureDelta DivideBy(this Energy energy, Entropy entropy)
    {
        return TemperatureDelta.FromKelvins(energy.Joules / entropy.JoulesPerKelvin);
    }

    public static Power Times(this Energy energy, Frequency frequency)
    {
        return Power.FromWatts(energy.Joules * frequency.PerSecond);
    }
}