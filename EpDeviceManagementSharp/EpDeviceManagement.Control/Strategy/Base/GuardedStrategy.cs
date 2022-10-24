using System.Runtime.CompilerServices;
using EpDeviceManagement.Contracts;
using EpDeviceManagement.Control.Contracts;
using EpDeviceManagement.Control.Strategy.Guards;

namespace EpDeviceManagement.Control.Strategy.Base;

public abstract class GuardedStrategy : IEpDeviceController
{
    private readonly IEnumerable<IControlGuard> guards;
    private readonly GuardSummaryImpl guardSummary;

    protected GuardedStrategy(params IControlGuard[] guards)
    {
        this.guards = guards;
        this.guardSummary = new GuardSummaryImpl();
    }

    public IGuardSummary GuardSummary => this.guardSummary;

    private bool CanRequestIncoming(TimeSpan timeStep, IEnumerable<ILoad> loads, IEnumerable<IGenerator> generators)
    {
        var result = true;
        foreach (var g in guards)
        {
            if (!g.CanRequestIncoming(timeStep, loads, generators))
            {
                this.IncrementGuardCounter(g);
                // do not return here just yet, we wish to capture all the guards, not primarily the ones with precedence
                result = false;
            }
        }
        return result;
    }

    private bool CanRequestOutgoing(TimeSpan timeStep, IEnumerable<ILoad> loads, IEnumerable<IGenerator> generators)
    {
        var result = true;
        foreach (var g in this.guards)
        {
            if (!g.CanRequestOutgoing(timeStep, loads, generators))
            {
                this.IncrementGuardCounter(g);
                result = false;
            }
        }
        return result;
    }

    private void IncrementGuardCounter(IControlGuard guard, [CallerMemberName] string? caller = null)
    {
        bool incoming = caller != nameof(CanRequestOutgoing);
        switch (guard)
        {
            case BatteryPowerGuard:
                if (incoming)
                {
                    this.guardSummary.IncomingPowerGuards += 1;
                }
                else
                {
                    this.guardSummary.OutgoingPowerGuards += 1;
                }
                break;
            case BatteryCapacityGuard:
                if (incoming)
                {
                    this.guardSummary.FullCapacityGuards += 1;
                }
                else
                {
                    this.guardSummary.EmptyCapacityGuards += 1;
                }
                break;
            case OscillationGuard:
                this.guardSummary.OscillationGuards += 1;
                break;
        }
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

    private class GuardSummaryImpl : IGuardSummary
    {
        public int IncomingPowerGuards { get; set; }

        public int OutgoingPowerGuards { get; set; }

        public int EmptyCapacityGuards { get; set; }

        public int FullCapacityGuards { get; set; }

        public int OscillationGuards { get; set; }
    }
}