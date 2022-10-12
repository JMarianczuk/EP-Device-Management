using EpDeviceManagement.Contracts;
using EpDeviceManagement.Control.Strategy.Guards;

namespace EpDeviceManagement.Control.Strategy.Base;

public abstract class GuardedStrategy : IEpDeviceController
{
    private readonly IEnumerable<IControlGuard> guards;

    protected GuardedStrategy(params IControlGuard[] guards)
    {
        this.guards = guards;
    }

    private bool CanRequestIncoming(TimeSpan timeStep, IEnumerable<ILoad> loads, IEnumerable<IGenerator> generators)
    {
        return this.guards.All(x => x.CanRequestIncoming(timeStep, loads, generators));
    }

    private bool CanRequestOutgoing(TimeSpan timeStep, IEnumerable<ILoad> loads, IEnumerable<IGenerator> generators)
    {
        return this.guards.All(x => x.CanRequestOutgoing(timeStep, loads, generators));
    }

    private void ReportLastTransfer(TransferResult lastTransferResult)
    {
        foreach (var guard in this.guards)
        {
            guard.ReportLastTransfer(lastTransferResult);
        }
    }

    public abstract string Name { get; }

    public abstract string Configuration { get; }

    public abstract string PrettyConfiguration { get; }

    protected abstract ControlDecision DoUnguardedControl(
        int dataPoint,
        TimeSpan timeStep,
        IEnumerable<ILoad> loads,
        IEnumerable<IGenerator> generators,
        TransferResult lastTransferResult);

    public ControlDecision DoControl(
        int dataPoint,
        TimeSpan timeStep,
        IEnumerable<ILoad> loads,
        IEnumerable<IGenerator> generators,
        TransferResult lastTransferResult)
    {
        this.ReportLastTransfer(lastTransferResult);
        var decision = this.DoUnguardedControl(dataPoint, timeStep, loads, generators, lastTransferResult);
        if (decision is ControlDecision.RequestTransfer request)
        {
            switch (request.RequestedDirection)
            {
                case PacketTransferDirection.Incoming:
                    if (!this.CanRequestIncoming(timeStep, loads, generators))
                    {
                        decision = new ControlDecision.NoAction();
                    }
                    break;
                case PacketTransferDirection.Outgoing:
                    if (!this.CanRequestOutgoing(timeStep, loads, generators))
                    {
                        decision = new ControlDecision.NoAction();
                    }
                    break;
            }
        }

        return decision;
    }
}