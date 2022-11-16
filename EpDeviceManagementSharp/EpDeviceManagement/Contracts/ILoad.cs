using EpDeviceManagement.UnitsExtensions;
using UnitsNet;

namespace EpDeviceManagement.Contracts;

public interface ILoad
{
    PowerFast MomentaryDemand { get; }
}