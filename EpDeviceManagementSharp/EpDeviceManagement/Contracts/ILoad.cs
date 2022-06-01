using UnitsNet;

namespace EpDeviceManagement.Contracts;

public interface ILoad
{
    Power CurrentDemand { get; }
}