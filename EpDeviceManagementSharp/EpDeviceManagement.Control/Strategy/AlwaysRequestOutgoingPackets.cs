using EpDeviceManagement.Contracts;
using EpDeviceManagement.Control.Strategy.Base;
using UnitsNet;

namespace EpDeviceManagement.Control.Strategy;

public class AlwaysRequestOutgoingPackets : PowerRespectingStrategy, IEpDeviceController
{
    public AlwaysRequestOutgoingPackets(
        IStorage battery,
        Energy packetSize)
        : base(
            battery,
            packetSize)
    {
    }

    public ControlDecision DoControl(TimeSpan timeStep, IEnumerable<ILoad> loads, IEnumerable<IGenerator> generators, TransferResult lastTransferResult)
    {
        if (this.CanRequestOutgoing(timeStep, loads, generators))
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

    public string Name => nameof(AlwaysRequestOutgoingPackets);

    public string Configuration => string.Empty;

    public string PrettyConfiguration => string.Empty;
}