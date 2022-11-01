using EpDeviceManagement.Contracts;

namespace EpDeviceManagement.Control.Strategy.Guards;

public interface IControlGuard
{
    bool CanRequestIncoming(TimeSpan timeStep, ILoad[] loads, IGenerator[] generators);

    bool CanRequestOutgoing(TimeSpan timeStep, ILoad[] loads, IGenerator[] generators);

    void ReportLastTransfer(TransferResult lastTransfer);
}