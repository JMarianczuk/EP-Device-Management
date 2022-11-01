using EpDeviceManagement.Contracts;
using EpDeviceManagement.Control.Strategy.Base;
using EpDeviceManagement.Control.Strategy.Guards;
using UnitsNet;

namespace EpDeviceManagement.Control.Strategy;

public class AlwaysRequestIncomingPackets : GuardedStrategy, IEpDeviceController
{
    public AlwaysRequestIncomingPackets(
        IStorage battery,
        Energy packetSize)
        : base(
            new BatteryCapacityGuard(battery, packetSize),
            new BatteryPowerGuard(battery, packetSize))
    {
    }

    protected override ControlDecision DoUnguardedControl(
        int dataPoint,
        TimeSpan timeStep,
        ILoad[] loads,
        IGenerator[] generators,
        TransferResult lastTransferResult)
    {
        return ControlDecision.RequestTransfer.Incoming;
    }

    public override string Name => "Always Request Incoming";

    public override string Configuration => string.Empty;

    public override string PrettyConfiguration => string.Empty;
}