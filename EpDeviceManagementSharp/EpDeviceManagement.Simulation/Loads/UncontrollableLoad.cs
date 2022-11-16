using EpDeviceManagement.Contracts;
using EpDeviceManagement.UnitsExtensions;
using UnitsNet;

namespace EpDeviceManagement.Simulation.Loads;

public class UncontrollableLoad : ILoad
{
    //public Power CurrentDemand { get; set; }

    public PowerFast MomentaryDemand { get; set; }

    public override string ToString()
    {
        return $"Load: {this.MomentaryDemand}";
    }
}