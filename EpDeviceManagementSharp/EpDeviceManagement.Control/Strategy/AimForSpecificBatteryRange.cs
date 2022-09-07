using System.Globalization;
using EpDeviceManagement.Contracts;
using EpDeviceManagement.Control.Strategy.Base;
using EpDeviceManagement.Control.Strategy.Guards;
using UnitsNet;

namespace EpDeviceManagement.Control.Strategy;

public class AimForSpecificBatteryRange : GuardedStrategy, IEpDeviceController
{
    private readonly Ratio desiredMinimumLevel;
    private readonly Ratio desiredMaximumLevel;
    private readonly Energy desiredMinimumStateOfCharge;
    private readonly Energy desiredMaximumStateOfCharge;

    public AimForSpecificBatteryRange(
        IStorage battery,
        Energy packetSize,
        Ratio desiredMinimumLevel,
        Ratio desiredMaximumLevel)
        : base(
            new BatteryCapacityGuard(battery, packetSize),
            new BatteryPowerGuard(battery, packetSize),
            new OscillationGuard())
    {
        if (desiredMinimumLevel > desiredMaximumLevel)
        {
            throw new ArgumentException(
                $"{nameof(desiredMinimumLevel)} ({desiredMinimumLevel}) cannot be greater than {nameof(desiredMaximumLevel)} ({desiredMaximumLevel}).");
        }

        if (desiredMinimumLevel < Ratio.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(desiredMinimumLevel), desiredMinimumLevel,
                "cannot be below zero");
        }

        Battery = battery;

        if (desiredMaximumLevel > Ratio.FromPercent(100))
        {
            throw new ArgumentOutOfRangeException(nameof(desiredMaximumLevel), desiredMaximumLevel,
                $"cannot be greater than 100%");
        }

        this.desiredMinimumLevel = desiredMinimumLevel;
        this.desiredMaximumLevel = desiredMaximumLevel;
        this.desiredMinimumStateOfCharge = battery.TotalCapacity * desiredMinimumLevel.DecimalFractions;
        this.desiredMaximumStateOfCharge = battery.TotalCapacity * desiredMaximumLevel.DecimalFractions;
    }

    private IStorage Battery { get; }

    protected override ControlDecision DoUnguardedControl(
        TimeSpan timeStep,
        IEnumerable<ILoad> loads,
        IEnumerable<IGenerator> generators,
        TransferResult lastTransferResult)
    {
        PacketTransferDirection direction;
        if (this.Battery.CurrentStateOfCharge < desiredMinimumStateOfCharge)
        {
            direction = PacketTransferDirection.Incoming;
        }
        else if (this.Battery.CurrentStateOfCharge > desiredMaximumStateOfCharge)
        {
            direction = PacketTransferDirection.Outgoing;
        }
        else
        {
            return new ControlDecision.NoAction();
        }

        return new ControlDecision.RequestTransfer()
        {
            RequestedDirection = direction,
        };
    }

    public override string Name => nameof(AimForSpecificBatteryRange);

    public override string Configuration => string.Create(CultureInfo.InvariantCulture, $"[{this.desiredMinimumLevel.DecimalFractions:F2}, {this.desiredMaximumLevel.DecimalFractions:F2}]");

    public override string PrettyConfiguration => $"[{desiredMinimumStateOfCharge},{desiredMaximumStateOfCharge}]";
}