using System;
using EpDeviceManagement.Contracts;
using UnitsNet;

namespace EpDeviceManagement;

public class VirtualStorage : IStorage
{
    private readonly IStorage commonStorage;
    private readonly decimal shareOfCommonStorage;

    public VirtualStorage(
        IStorage commonStorage,
        decimal shareOfCommonStorage)
    {
        this.commonStorage = commonStorage;
        if (shareOfCommonStorage is < 0 or > 1)
        {
            throw new ArgumentOutOfRangeException(nameof(shareOfCommonStorage), "Invalid share of common storage");
        }
        this.shareOfCommonStorage = shareOfCommonStorage;
        this.TotalCapacity = this.commonStorage.TotalCapacity * (double)this.shareOfCommonStorage;
        this.MaximumChargePower = this.commonStorage.MaximumChargePower * this.shareOfCommonStorage;
        this.MaximumDischargePower = this.commonStorage.MaximumDischargePower * this.shareOfCommonStorage;
    }
    
    public double VirtualShare { get; set; }

    public Energy TotalCapacity { get; }
    public Energy CurrentStateOfCharge { get; set; }
    public Power MaximumChargePower { get; }
    public Power MaximumDischargePower { get; }
}