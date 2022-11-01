namespace EpDeviceManagement.Contracts;

public abstract class ControlDecision
{
    public sealed class RequestTransfer : ControlDecision
    {
        private RequestTransfer()
        {

        }

        public static RequestTransfer Incoming { get; } = new RequestTransfer()
        {
            RequestedDirection = PacketTransferDirection.Incoming,
        };

        public static RequestTransfer Outgoing { get; } = new RequestTransfer()
        {
            RequestedDirection = PacketTransferDirection.Outgoing,
        };

        public PacketTransferDirection RequestedDirection { get; init; }
    }

    public sealed class AcceptIncomingRequest : ControlDecision
    {
        public bool AcceptIncoming { get; init; }
        
        public bool AcceptOutgoing { get; init; }
    }

    public sealed class NoAction : ControlDecision
    {
        private NoAction()
        {

        }

        public static NoAction Instance { get; } = new NoAction();
    }
}

public enum PacketTransferDirection
{
    Outgoing,
    Incoming,
}