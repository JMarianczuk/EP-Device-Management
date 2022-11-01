using EpDeviceManagement.Contracts;
using UnitsNet;

namespace EpDeviceManagement.Simulation.Loads;

public class UncontrollableGeneration : IGenerator
{
    public Power MomentaneousGeneration { get; set; }

    public override string ToString()
    {
        return $"Gen: {this.MomentaneousGeneration}";
    }
}