namespace EpDeviceManagement.Contracts;

public abstract class ControlDecision
{
    public sealed class RequestTransfer : ControlDecision
    {
        public PacketTransferDirection RequestedDirection { get; init; }

        public bool AcceptIncomingRequestIfOwnRequestFails { get; init; } = true;
    }

    public sealed class AcceptIncomingRequest : ControlDecision
    {
        public bool AcceptIncoming { get; init; }
        
        public bool AcceptOutgoing { get; init; }
    }

    public sealed class NoAction : ControlDecision
    {
        
    }
}

public enum PacketTransferDirection
{
    Outgoing,
    Incoming,
}