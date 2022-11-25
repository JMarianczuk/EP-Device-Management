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
            RequestedAction = PacketTransferAction.Receive,
        };

        public static RequestTransfer Outgoing { get; } = new RequestTransfer()
        {
            RequestedAction = PacketTransferAction.Send,
        };

        public PacketTransferAction RequestedAction { get; init; }
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

public enum PacketTransferAction
{
    Send,
    Receive,
}