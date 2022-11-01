using UnitsNet;

namespace EpDeviceManagement.Contracts;

public interface IGenerator
{
    Power MomentaneousGeneration { get; }
}