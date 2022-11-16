using EpDeviceManagement.Contracts;

namespace EpDeviceManagement.Control.Strategy.Guards;

public sealed class OscillationGuard : IControlGuard
{
    private TransferResult? lastTransfer;

    public bool CanRequestIncoming(TimeSpan timeStep, ILoad load, IGenerator generator)
    {
        var sentPacketLastStep = lastTransfer is TransferResult.Success
        {
            PerformedDirection: PacketTransferDirection.Outgoing
        };
        return !sentPacketLastStep;
    }

    public bool CanRequestOutgoing(TimeSpan timeStep, ILoad load, IGenerator generator)
    {
        var receivedPacketLastStep = lastTransfer is TransferResult.Success
        {
            PerformedDirection: PacketTransferDirection.Incoming
        };
        return !receivedPacketLastStep;
    }

    public void ReportLastTransfer(TransferResult lastTransfer)
    {
        this.lastTransfer = lastTransfer;
    }
}