using EpDeviceManagement.Contracts;
using EpDeviceManagement.Control.Strategy.Base;
using EpDeviceManagement.Control.Strategy.Guards;
using UnitsNet;

namespace EpDeviceManagement.Control.old;

public class AlwaysRequestIncomingPackets : IEpDeviceController
{
    public AlwaysRequestIncomingPackets(
        IStorage battery,
        Energy packetSize)
    {
    }

    public ControlDecision DoControl(
        TimeSpan timeStep,
        ILoad load,
        IGenerator generator,
        TransferResult lastTransferResult)
    {
        return ControlDecision.RequestTransfer.Incoming;
    }

    public string Name => "Always Request Incoming";

    public string Configuration => string.Empty;

    public string PrettyConfiguration => string.Empty;

    public bool RequestsOutgoingPackets => false;
}