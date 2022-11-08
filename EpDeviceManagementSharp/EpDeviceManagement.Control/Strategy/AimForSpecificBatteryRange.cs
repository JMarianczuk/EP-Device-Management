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
    private readonly bool withGeneration;
    private readonly Energy desiredMinimumStateOfCharge;
    private readonly Energy desiredMaximumStateOfCharge;

    public AimForSpecificBatteryRange(
        IStorage battery,
        Energy packetSize,
        Ratio desiredMinimumLevel,
        Ratio desiredMaximumLevel,
        bool withGeneration,
        bool withOscillationGuard = true)
        : base(
            new BatteryCapacityGuard(battery, packetSize),
            new BatteryPowerGuard(battery, packetSize),
            withOscillationGuard ? new OscillationGuard() : DummyGuard.Instance)
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
        this.withGeneration = withGeneration;
        this.desiredMinimumStateOfCharge = battery.TotalCapacity * desiredMinimumLevel.DecimalFractions;
        this.desiredMaximumStateOfCharge = battery.TotalCapacity * desiredMaximumLevel.DecimalFractions;
    }

    private IStorage Battery { get; }

    protected override ControlDecision DoUnguardedControl(
        int dataPoint,
        TimeSpan timeStep,
        ILoad[] loads,
        IGenerator[] generators,
        TransferResult lastTransferResult)
    {
        if (this.Battery.CurrentStateOfCharge < desiredMinimumStateOfCharge)
        {
            return ControlDecision.RequestTransfer.Incoming;
        }
        else if (this.Battery.CurrentStateOfCharge > desiredMaximumStateOfCharge
                 && this.withGeneration)
        {
            return ControlDecision.RequestTransfer.Outgoing;
        }
        else
        {
            return ControlDecision.NoAction.Instance;
        }
    }

    public override string Name => "Battery Range";

    public override string Configuration => string.Create(CultureInfo.InvariantCulture, $"[{this.desiredMinimumLevel.DecimalFractions:F2}, {this.desiredMaximumLevel.DecimalFractions:F2}]");

    public override string PrettyConfiguration => $"[{desiredMinimumStateOfCharge},{desiredMaximumStateOfCharge}]";
}