using UnitsNet;

namespace EpDeviceManagement.Contracts;

public interface IControllableLoad
{
    Energy MaximumCapacity { get; }

    Energy MinimumCapacity { get; }

    Energy TargetCapacity { get; }

    Energy CurrentCapacity { get; }

    Power LoadConsumption { get; }

    Power CurrentStandingLoss { get; }

    Power CurrentUsage { get; }
}