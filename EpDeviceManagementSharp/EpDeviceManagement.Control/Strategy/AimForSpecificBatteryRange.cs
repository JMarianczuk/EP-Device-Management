using EpDeviceManagement.Contracts;
using EpDeviceManagement.Control.Strategy.Base;
using UnitsNet;

namespace EpDeviceManagement.Control.Strategy;

public class AimForSpecificBatteryRange : PowerRespectingStrategy, IEpDeviceController
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
            battery,
            packetSize)
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
    public ControlDecision DoControl(TimeSpan timeStep, IEnumerable<ILoad> loads, IEnumerable<IGenerator> generators, TransferResult lastTransferResult)
    {
        PacketTransferDirection direction;
        if (this.Battery.CurrentStateOfCharge < desiredMinimumStateOfCharge
            && this.CanRequestIncoming(timeStep, loads, generators))
        {
            direction = PacketTransferDirection.Incoming;
        }
        else if (this.Battery.CurrentStateOfCharge > desiredMaximumStateOfCharge
            && this.CanRequestOutgoing(timeStep, loads, generators))
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

    public string Name => nameof(AimForSpecificBatteryRange);

    public string Configuration => $"[{this.desiredMinimumLevel.DecimalFractions:F2}, {this.desiredMaximumLevel.DecimalFractions:F2}]";

    public string PrettyConfiguration => $"[{desiredMinimumStateOfCharge},{desiredMaximumStateOfCharge}]";
}