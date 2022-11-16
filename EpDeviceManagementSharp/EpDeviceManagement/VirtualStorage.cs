using System;
using EpDeviceManagement.Contracts;
using EpDeviceManagement.UnitsExtensions;
using UnitsNet;

namespace EpDeviceManagement;

public class VirtualStorage : IStorage
{
    private readonly IStorage commonStorage;
    private readonly double shareOfCommonStorage;

    public VirtualStorage(
        IStorage commonStorage,
        double shareOfCommonStorage)
    {
        this.commonStorage = commonStorage;
        if (shareOfCommonStorage is < 0 or > 1)
        {
            throw new ArgumentOutOfRangeException(nameof(shareOfCommonStorage), "Invalid share of common storage");
        }
        this.shareOfCommonStorage = shareOfCommonStorage;
        this.TotalCapacity = this.commonStorage.TotalCapacity * this.shareOfCommonStorage;
        this.MaximumChargePower = this.commonStorage.MaximumChargePower * this.shareOfCommonStorage;
        this.MaximumDischargePower = this.commonStorage.MaximumDischargePower * this.shareOfCommonStorage;
    }
    
    public double VirtualShare { get; set; }

    public EnergyFast TotalCapacity { get; }
    public EnergyFast CurrentStateOfCharge { get; set; }
    public PowerFast MaximumChargePower { get; }
    public PowerFast MaximumDischargePower { get; }
}