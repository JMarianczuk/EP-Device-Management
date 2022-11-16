using EpDeviceManagement.Contracts;

namespace EpDeviceManagement.Control.Strategy.Guards;

public interface IControlGuard
{
    bool CanRequestIncoming(TimeSpan timeStep, ILoad load, IGenerator generator);

    bool CanRequestOutgoing(TimeSpan timeStep, ILoad load, IGenerator generator);

    void ReportLastTransfer(TransferResult lastTransfer);
}