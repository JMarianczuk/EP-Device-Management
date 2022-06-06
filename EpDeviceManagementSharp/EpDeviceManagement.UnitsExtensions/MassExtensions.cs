using UnitsNet;
using UnitsNet.Units;

namespace EpDeviceManagement.UnitsExtensions;

public static class MassExtensions
{
    public static Entropy Multiply(this Mass mass, SpecificEntropy specificEntropy)
    {
        return new Entropy(
            mass.Kilograms * specificEntropy.JoulesPerKilogramKelvin,
            EntropyUnit.JoulePerKelvin);
    }
}