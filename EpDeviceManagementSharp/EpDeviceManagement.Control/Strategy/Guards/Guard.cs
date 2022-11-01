using EpDeviceManagement.Contracts;

namespace EpDeviceManagement.Control.Strategy.Guards;

public class DummyGuard : IControlGuard
{
    public static IControlGuard Instance { get; } = new DummyGuard();

    private DummyGuard()
    {

    }

    public bool CanRequestIncoming(TimeSpan timeStep, ILoad[] loads, IGenerator[] generators)
    {
        return true;
    }

    public bool CanRequestOutgoing(TimeSpan timeStep, ILoad[] loads, IGenerator[] generators)
    {
        return true;
    }

    public void ReportLastTransfer(TransferResult lastTransfer)
    {
    }
}