using EpDeviceManagement.Contracts;
using UnitsNet;

namespace EpDeviceManagement.Control;

public class AimForSpecificBatteryLevel : IEpDeviceController
{
    private readonly IStorage battery;
    private readonly Energy desiredStateOfCharge;

    public AimForSpecificBatteryLevel(
        IStorage battery,
        Energy desiredStateOfCharge)
    {
        this.battery = battery;
        this.desiredStateOfCharge = desiredStateOfCharge;
    }

    public ControlDecision DoControl(TimeSpan timeStep, IEnumerable<ILoad> loads)
    {
        PacketTransferDirection direction;
        if (this.battery.CurrentStateOfCharge < this.desiredStateOfCharge)
        {
            direction = PacketTransferDirection.Incoming;
        }
        else if (this.battery.CurrentStateOfCharge > this.desiredStateOfCharge)
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
}