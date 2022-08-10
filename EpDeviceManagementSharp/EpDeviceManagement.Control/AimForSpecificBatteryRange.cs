using EpDeviceManagement.Contracts;
using UnitsNet;

namespace EpDeviceManagement.Control;

public class AimForSpecificBatteryRange : IEpDeviceController
{
    private readonly IStorage battery;
    private readonly Energy desiredMinimumStateOfCharge;
    private readonly Energy desiredMaximumStateOfCharge;

    public AimForSpecificBatteryRange(
        IStorage battery,
        Energy desiredMinimumStateOfCharge,
        Energy desiredMaximumStateOfCharge)
    {
        this.battery = battery;
        this.desiredMinimumStateOfCharge = desiredMinimumStateOfCharge;
        this.desiredMaximumStateOfCharge = desiredMaximumStateOfCharge;
    }
    public ControlDecision DoControl(TimeSpan timeStep, IEnumerable<ILoad> loads, TransferResult lastTransferResult)
    {
        PacketTransferDirection direction;
        if (this.battery.CurrentStateOfCharge < this.desiredMinimumStateOfCharge)
        {
            direction = PacketTransferDirection.Incoming;
        }
        else if (this.battery.CurrentStateOfCharge > this.desiredMaximumStateOfCharge)
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
}