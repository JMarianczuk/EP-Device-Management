using UnitsNet;

namespace EpDeviceManagement.Contracts;

public interface IStorage
{
    public Energy TotalCapacity { get; }
    
    public Energy CurrentStateOfCharge { get; }
    
    public Power MaximumChargePower { get; }
    
    public Power MaximumDischargePower { get; }
}