using UnitsNet;

namespace EpDeviceManagement.Contracts;

public interface ILoad
{
    Power MomentaneousDemand { get; }
}