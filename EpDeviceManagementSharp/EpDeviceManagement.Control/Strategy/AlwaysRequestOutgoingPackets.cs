using EpDeviceManagement.Contracts;
using EpDeviceManagement.Control.Strategy.Base;
using EpDeviceManagement.Control.Strategy.Guards;
using UnitsNet;

namespace EpDeviceManagement.Control.Strategy;

public class AlwaysRequestOutgoingPackets : IEpDeviceController
{
    public AlwaysRequestOutgoingPackets(
        IStorage battery,
        Energy packetSize)
    {
    }

    public ControlDecision DoControl(
        int dataPoint,
        TimeSpan timeStep,
        ILoad load,
        IGenerator generator,
        TransferResult lastTransferResult)
    {
        return ControlDecision.RequestTransfer.Outgoing;
    }

    public string Name => "Always Request Outgoing";

    public string Configuration => string.Empty;

    public string PrettyConfiguration => string.Empty;

    public bool RequestsOutgoingPackets => true;
}