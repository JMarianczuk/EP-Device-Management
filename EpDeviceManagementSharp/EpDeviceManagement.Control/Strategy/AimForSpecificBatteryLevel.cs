using EpDeviceManagement.Contracts;
using EpDeviceManagement.Control.Strategy.Base;
using UnitsNet;

namespace EpDeviceManagement.Control.Strategy;

public class AimForSpecificBatteryLevel : PowerRespectingStrategy, IEpDeviceController
{
    private readonly Ratio desiredLevel;
    private readonly Energy desiredStateOfCharge;

    public AimForSpecificBatteryLevel(
        IStorage battery,
        Energy packetSize,
        Ratio desiredLevel)
        : base(battery, packetSize)
    {
        if (desiredLevel < Ratio.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(desiredLevel), desiredLevel,
                "cannot be below zero");
        }
        if (desiredLevel > Ratio.FromPercent(100))
        {
            throw new ArgumentOutOfRangeException(nameof(desiredLevel), desiredLevel,
                $"cannot be greater than 100%");
        }

        this.desiredLevel = desiredLevel;
        this.desiredStateOfCharge = battery.TotalCapacity * desiredLevel.DecimalFractions;
    }

    public ControlDecision DoControl(TimeSpan timeStep, IEnumerable<ILoad> loads, IEnumerable<IGenerator> generators, TransferResult lastTransferResult)
    {
        PacketTransferDirection direction;
        if (this.Battery.CurrentStateOfCharge < desiredStateOfCharge
            && this.CanRequestIncoming(timeStep, loads, generators))
        {
            direction = PacketTransferDirection.Incoming;
        }
        else if (this.Battery.CurrentStateOfCharge > desiredStateOfCharge
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
            AcceptIncomingRequestIfOwnRequestFails = true,
        };
    }

    public string Name => nameof(AimForSpecificBatteryLevel);

    public string Configuration => this.desiredLevel.DecimalFractions.ToString("F2");

    public string PrettyConfiguration => desiredStateOfCharge.ToString();
}