using EpDeviceManagement.Contracts;
using EpDeviceManagement.UnitsExtensions;
using UnitsNet;

namespace EpDeviceManagement.Simulation.Loads;

public class UncontrollableGeneration : IGenerator
{
    public PowerFast MomentaryGeneration { get; set; }

    public override string ToString()
    {
        return $"Gen: {this.MomentaryGeneration}";
    }
}