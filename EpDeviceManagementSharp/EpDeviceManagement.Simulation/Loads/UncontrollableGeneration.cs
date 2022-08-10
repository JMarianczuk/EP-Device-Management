using EpDeviceManagement.Contracts;
using UnitsNet;

namespace EpDeviceManagement.Simulation.Loads;

public class UncontrollableGeneration : IGenerator
{
    public Power CurrentGeneration { get; set; }

    public Energy LastStatus { get; set; }

    public override string ToString()
    {
        return $"Gen: {this.CurrentGeneration}";
    }
}