using EpDeviceManagement.Contracts;
using EpDeviceManagement.Control.Strategy.Base;
using EpDeviceManagement.Control.Strategy.Guards;
using UnitsNet;

namespace EpDeviceManagement.Control.Strategy;

public class AlwaysRequestOutgoingPackets : GuardedStrategy, IEpDeviceController
{
    public AlwaysRequestOutgoingPackets(
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
        return ControlDecision.RequestTransfer.Outgoing;
    }

    public override string Name => "Always Request Outgoing";

    public override string Configuration => string.Empty;

    public override string PrettyConfiguration => string.Empty;
}