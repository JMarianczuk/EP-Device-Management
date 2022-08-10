using UnitsNet;

namespace EpDeviceManagement.Contracts;

public interface IGenerator
{
    Power CurrentGeneration { get; }
}