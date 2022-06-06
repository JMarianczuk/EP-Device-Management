using UnitsNet;

namespace EpDeviceManagement.Contracts;

public interface IThermostaticallyControlledLoad
{
    Temperature MinimumTemperature { get; }

    Temperature MaximumTemperature { get; }

    Temperature CurrentTemperature { get; }

    SpecificEntropy MediumSpecificEntropy { get; }

    Mass MediumMass { get; }

    Power PowerUse { get; }

    Power CurrentLoss { get; }

    bool IsHeating { get; }
}
