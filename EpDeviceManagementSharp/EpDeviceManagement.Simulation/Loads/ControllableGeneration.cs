using EpDeviceManagement.Contracts;
using EpDeviceManagement.UnitsExtensions;
using UnitsNet;

namespace EpDeviceManagement.Simulation.Loads;

public class ControllableGeneration : IControllableGenerator
{
    //public Power CurrentGeneration { get; set; }

    public PowerFast MomentaryGeneration { get; set; }

    public bool IsGenerating { get; set; }

    public void DisableGenerationForOneTimeStep()
    {
        this.IsGenerating = false;
    }

    public override string ToString()
    {
        return $"Gen: {this.MomentaryGeneration}";
    }
}