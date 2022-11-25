using EpDeviceManagement.Contracts;

namespace EpDeviceManagement.Control.Strategy.Guards;

public sealed class OscillationGuard : IControlGuard
{
    private TransferResult? lastTransfer;

    public bool CanRequestToReceive(TimeSpan timeStep, ILoad load, IGenerator generator)
    {
        var sentPacketLastStep = lastTransfer is TransferResult.Granted
        {
            PerformedAction: PacketTransferAction.Send
        };
        return !sentPacketLastStep;
    }

    public bool CanRequestToSend(TimeSpan timeStep, ILoad load, IGenerator generator)
    {
        var receivedPacketLastStep = lastTransfer is TransferResult.Granted
        {
            PerformedAction: PacketTransferAction.Receive
        };
        return !receivedPacketLastStep;
    }

    public void ReportLastTransfer(TransferResult lastTransfer)
    {
        this.lastTransfer = lastTransfer;
    }
}