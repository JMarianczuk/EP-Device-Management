using UnitsNet;
using UnitsNet.Units;

namespace EpDeviceManagement.UnitsExtensions;

public static class SpecificEntropyExtensions
{
    public static Entropy Multiply(this SpecificEntropy specificEntropy, Mass mass)
    {
        return new Entropy(
            specificEntropy.JoulesPerKilogramKelvin * mass.Kilograms,
            EntropyUnit.JoulePerKelvin);
    }
}