using EpDeviceManagement.Contracts;

namespace EpDeviceManagement.Control;

public class AlwaysRequestOutgoingPackets : IEpDeviceController
{
    public ControlDecision DoControl(TimeSpan timeStep, IEnumerable<ILoad> loads)
    {
        return new ControlDecision.RequestTransfer()
        {
            RequestedDirection = PacketTransferDirection.Outgoing,
        };
    }
}