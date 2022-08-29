using EpDeviceManagement.Contracts;
using EpDeviceManagement.Control.Strategy.Base;
using UnitsNet;

namespace EpDeviceManagement.Control.Strategy;

public class AlwaysRequestIncomingPackets : PowerRespectingStrategy, IEpDeviceController
{
    public AlwaysRequestIncomingPackets(
        IStorage battery,
        Energy packetSize)
        : base(
            battery,
            packetSize)
    {
    }

    public ControlDecision DoControl(TimeSpan timeStep, IEnumerable<ILoad> loads, IEnumerable<IGenerator> generators, TransferResult lastTransferResult)
    {
        if (this.CanRequestIncoming(timeStep, loads, generators))
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

    public string Name => nameof(AlwaysRequestIncomingPackets);

    public string Configuration => string.Empty;

    public string PrettyConfiguration => string.Empty;
}