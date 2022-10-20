namespace EpDeviceManagement.Control.Contracts;

public interface IGuardSummary
{
    public int PowerGuards { get; }

    public int CapacityGuards { get; }

    public int OscillationGuards { get; }
}