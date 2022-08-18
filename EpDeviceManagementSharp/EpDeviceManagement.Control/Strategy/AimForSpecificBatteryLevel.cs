using EpDeviceManagement.Contracts;
using UnitsNet;

namespace EpDeviceManagement.Control.Strategy;

public class AimForSpecificBatteryLevel : CapacityRespectingStrategy, IEpDeviceController
{
    private readonly Energy desiredStateOfCharge;

    public AimForSpecificBatteryLevel(
        IStorage battery,
        Energy packetSize,
        Energy desiredStateOfCharge)
        : base(battery, packetSize)
    {

        if (desiredStateOfCharge < Energy.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(desiredStateOfCharge), desiredStateOfCharge,
                "cannot be below zero");
        }
        if (desiredStateOfCharge > battery.TotalCapacity)
        {
            throw new ArgumentOutOfRangeException(nameof(desiredStateOfCharge), desiredStateOfCharge,
                $"cannot be greater than the battery capacity ({battery.TotalCapacity})");
        }
        this.desiredStateOfCharge = desiredStateOfCharge;
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

    public string Configuration => desiredStateOfCharge.ToString();
}