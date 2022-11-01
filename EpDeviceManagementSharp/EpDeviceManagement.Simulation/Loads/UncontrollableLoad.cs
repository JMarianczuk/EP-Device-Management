using EpDeviceManagement.Contracts;
using UnitsNet;

namespace EpDeviceManagement.Simulation.Loads;

public class UncontrollableLoad : ILoad
{
    public Power CurrentDemand { get; set; }

    public Power MomentaneousDemand { get; set; }

    public override string ToString()
    {
        return $"Load: {this.CurrentDemand}";
    }
}