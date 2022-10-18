namespace EpDeviceManagement.Contracts;

public abstract class TransferResult
{
    public sealed class Success : TransferResult
    {
        public PacketTransferDirection PerformedDirection { get; init; }
    }

    public sealed class Failure : TransferResult
    {
        public PacketTransferDirection RequestedDirection { get; set; }
    }

    public sealed class NoTransferRequested : TransferResult
    {

    }
}