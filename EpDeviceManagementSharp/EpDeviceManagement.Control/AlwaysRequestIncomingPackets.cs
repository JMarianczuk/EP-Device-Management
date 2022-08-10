using EpDeviceManagement.Contracts;
using UnitsNet;

namespace EpDeviceManagement.Control;

public class AlwaysRequestIncomingPackets : IEpDeviceController
{
    private readonly IStorage battery;
    private readonly Energy packetSize;

    public AlwaysRequestIncomingPackets(
        IStorage battery,
        Energy packetSize)
    {
        this.battery = battery;
        this.packetSize = packetSize;
    }

    public ControlDecision DoControl(TimeSpan timeStep, IEnumerable<ILoad> loads, TransferResult lastTransferResult)
    {
        if (this.battery.CurrentStateOfCharge - this.packetSize > Energy.Zero)
        {
            return new ControlDecision.RequestTransfer()
            {
                RequestedDirection = PacketTransferDirection.Incoming,
            };
        }
        else
        {
            return new ControlDecision.NoAction();
        }
    }

    public override string ToString()
    {
        return $"{nameof(AlwaysRequestIncomingPackets)}";
    }
}