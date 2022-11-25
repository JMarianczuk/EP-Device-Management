using EpDeviceManagement.Contracts;

namespace EpDeviceManagement.Control.Strategy.Guards;

public interface IControlGuard
{
    bool CanRequestToReceive(TimeSpan timeStep, ILoad load, IGenerator generator);

    bool CanRequestToSend(TimeSpan timeStep, ILoad load, IGenerator generator);

    void ReportLastTransfer(TransferResult lastTransfer);
}