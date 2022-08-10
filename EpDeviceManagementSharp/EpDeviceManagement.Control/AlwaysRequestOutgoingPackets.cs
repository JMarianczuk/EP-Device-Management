using EpDeviceManagement.Contracts;
using UnitsNet;

namespace EpDeviceManagement.Control;

public class AlwaysRequestOutgoingPackets : IEpDeviceController
{
    private readonly IStorage battery;
    private readonly Energy packetSize;

    public AlwaysRequestOutgoingPackets(
        IStorage battery,
        Energy packetSize)
    {
        this.battery = battery;
        this.packetSize = packetSize;
    }

    public ControlDecision DoControl(TimeSpan timeStep, IEnumerable<ILoad> loads, TransferResult lastTransferResult)
    {
        if (this.battery.CurrentStateOfCharge + this.packetSize < this.battery.TotalCapacity)
        {
            return new ControlDecision.RequestTransfer()
            {
                RequestedDirection = PacketTransferDirection.Outgoing,
            };
        }
        else
        {
            return new ControlDecision.NoAction();
        }
    }

    public override string ToString()
    {
        return $"{nameof(AlwaysRequestOutgoingPackets)}";
    }
}