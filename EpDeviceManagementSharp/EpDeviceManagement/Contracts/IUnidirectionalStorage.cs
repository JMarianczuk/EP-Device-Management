using UnitsNet;

namespace EpDeviceManagement.Contracts;

public interface IUnidirectionalStorage
{
    public Energy TotalCapacity { get; }
    
    public Energy CurrentStateOfCharge { get; }
    
    public Energy MinimumStateOfCharge { get; }
    
    public Power MaximumChargePower { get; }
    
    public Power CurrentLoss { get; }
}