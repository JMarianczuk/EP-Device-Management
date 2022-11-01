namespace EpDeviceManagement.Contracts;

public abstract class TransferResult
{
    public sealed class Success : TransferResult
    {
        private Success()
        {

        }

        public static Success Incoming { get; } = new Success()
        {
            PerformedDirection = PacketTransferDirection.Incoming,
        };

        public static Success Outgoing { get; } = new Success()
        {
            PerformedDirection = PacketTransferDirection.Outgoing,
        };

        public static TransferResult For(PacketTransferDirection direction)
        {
            switch (direction)
            {
                case PacketTransferDirection.Incoming:
                    return Incoming;
                case PacketTransferDirection.Outgoing:
                    return Outgoing;
            }

            return new Success()
            {
                PerformedDirection = direction,
            };
        }

        public PacketTransferDirection PerformedDirection { get; init; }
    }

    public sealed class Failure : TransferResult
    {
        private Failure()
        {

        }

        public static Failure Incoming { get; } = new Failure()
        {
            RequestedDirection = PacketTransferDirection.Incoming,
        };

        public static Failure Outgoing { get; } = new Failure()
        {
            RequestedDirection = PacketTransferDirection.Outgoing,
        };

        public static Failure For(PacketTransferDirection direction)
        {
            switch (direction)
            {
                case PacketTransferDirection.Incoming:
                    return Incoming;
                case PacketTransferDirection.Outgoing:
                    return Outgoing;
            }

            return new Failure()
            {
                RequestedDirection = direction,
            };
        }

        public PacketTransferDirection RequestedDirection { get; set; }
    }

    public sealed class NoTransferRequested : TransferResult
    {
        private NoTransferRequested()
        {

        }

        public static TransferResult Instance { get; } = new NoTransferRequested();
    }
}