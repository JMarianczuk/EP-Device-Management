namespace EpDeviceManagement.Contracts;

public abstract class TransferResult
{
    public sealed class Granted : TransferResult
    {
        private Granted()
        {

        }

        public static Granted Receive { get; } = new Granted()
        {
            PerformedAction = PacketTransferAction.Receive,
        };

        public static Granted Send { get; } = new Granted()
        {
            PerformedAction = PacketTransferAction.Send,
        };

        public static TransferResult For(PacketTransferAction action)
        {
            switch (action)
            {
                case PacketTransferAction.Receive:
                    return Receive;
                case PacketTransferAction.Send:
                    return Send;
            }

            return new Granted()
            {
                PerformedAction = action,
            };
        }

        public PacketTransferAction PerformedAction { get; init; }
    }

    public sealed class Declined : TransferResult
    {
        private Declined()
        {

        }

        public static Declined Receive { get; } = new Declined()
        {
            RequestedAction = PacketTransferAction.Receive,
        };

        public static Declined Send { get; } = new Declined()
        {
            RequestedAction = PacketTransferAction.Send,
        };

        public static Declined For(PacketTransferAction action)
        {
            switch (action)
            {
                case PacketTransferAction.Receive:
                    return Receive;
                case PacketTransferAction.Send:
                    return Send;
            }

            return new Declined()
            {
                RequestedAction = action,
            };
        }

        public PacketTransferAction RequestedAction { get; set; }
    }

    public sealed class NoTransferRequested : TransferResult
    {
        private NoTransferRequested()
        {

        }

        public static TransferResult Instance { get; } = new NoTransferRequested();
    }
}