namespace EpDeviceManagement.Control.Contracts;

public interface IGuardSummary
{
    public int IncomingPowerGuards { get; }

    public int OutgoingPowerGuards { get; }

    public int EmptyCapacityGuards { get; }

    public int FullCapacityGuards { get; }

    public int OscillationGuards { get; }
}