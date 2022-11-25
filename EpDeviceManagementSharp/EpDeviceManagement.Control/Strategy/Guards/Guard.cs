using EpDeviceManagement.Contracts;

namespace EpDeviceManagement.Control.Strategy.Guards;

public class DummyGuard : IControlGuard
{
    public static IControlGuard Instance { get; } = new DummyGuard();

    private DummyGuard()
    {

    }

    public bool CanRequestToReceive(TimeSpan timeStep, ILoad load, IGenerator generator)
    {
        return true;
    }

    public bool CanRequestToSend(TimeSpan timeStep, ILoad load, IGenerator generator)
    {
        return true;
    }

    public void ReportLastTransfer(TransferResult lastTransfer)
    {
    }
}