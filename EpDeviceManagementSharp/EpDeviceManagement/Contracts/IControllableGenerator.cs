using System;

namespace EpDeviceManagement.Contracts;

public interface IControllableGenerator : IGenerator
{
    public bool IsGenerating { get; }

    public void DisableGenerationForOneTimeStep();
}