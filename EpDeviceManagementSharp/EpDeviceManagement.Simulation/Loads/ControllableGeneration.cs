using EpDeviceManagement.Contracts;
using UnitsNet;

namespace EpDeviceManagement.Simulation.Loads;

public class ControllableGeneration : IControllableGenerator
{
    public Power CurrentGeneration { get; set; }

    public bool IsGenerating { get; set; }

    public void DisableGenerationForOneTimeStep()
    {
        this.IsGenerating = false;
    }
}