using EpDeviceManagement.UnitsExtensions;
using UnitsNet;

namespace EpDeviceManagement.Contracts;

public interface IStorage
{
    public EnergyFast TotalCapacity { get; }
    
    public EnergyFast CurrentStateOfCharge { get; }
    
    public PowerFast MaximumChargePower { get; }
    
    public PowerFast MaximumDischargePower { get; }
}