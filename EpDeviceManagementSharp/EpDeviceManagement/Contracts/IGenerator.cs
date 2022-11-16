using EpDeviceManagement.UnitsExtensions;
using UnitsNet;

namespace EpDeviceManagement.Contracts;

public interface IGenerator
{
    PowerFast MomentaryGeneration { get; }
}