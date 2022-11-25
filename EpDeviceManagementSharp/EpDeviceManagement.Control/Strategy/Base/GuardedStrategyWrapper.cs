using System.Runtime.CompilerServices;
using EpDeviceManagement.Contracts;
using EpDeviceManagement.Control.Contracts;
using EpDeviceManagement.Control.Strategy.Guards;

namespace EpDeviceManagement.Control.Strategy.Base;

public class GuardedStrategyWrapper : IEpDeviceController
{
    private readonly IEpDeviceController strategy;
    private readonly IControlGuard[] guards;
    private readonly GuardSummaryImpl guardSummary;

    public GuardedStrategyWrapper(
        IEpDeviceController strategy,
        params IControlGuard[] guards)
    {
        this.strategy = strategy;
        this.guards = guards;
        guardSummary = new GuardSummaryImpl();

        var powerConfig = guards
            .OfType<BatteryPowerGuard>()
            .FirstOrDefault()?.Configuration ?? string.Empty;
        var capacityConfig = guards
            .OfType<BatteryCapacityGuard>()
            .FirstOrDefault()?.Configuration ?? string.Empty;
        GuardConfiguration = $"p({powerConfig}) c({capacityConfig})";
    }

    public ControlDecision DoControl(
        TimeSpan timeStep,
        ILoad load,
        IGenerator generator,
        TransferResult lastTransferResult)
    {
        ReportLastTransfer(lastTransferResult);
        var decision = strategy.DoControl(timeStep, load, generator, lastTransferResult);
        if (decision is ControlDecision.RequestTransfer request)
        {
            switch (request.RequestedAction)
            {
                case PacketTransferAction.Receive:
                    if (!CanRequestIncoming(timeStep, load, generator))
                    {
                        decision = ControlDecision.NoAction.Instance;
                    }
                    break;
                case PacketTransferAction.Send:
                    if (!CanRequestOutgoing(timeStep, load, generator))
                    {
                        decision = ControlDecision.NoAction.Instance;
                    }
                    break;
            }
        }

        return decision;
    }

    private bool CanRequestIncoming(TimeSpan timeStep, ILoad load, IGenerator generator)
    {
        var result = true;
        foreach (var g in guards)
        {
            if (!g.CanRequestToReceive(timeStep, load, generator))
            {
                IncrementGuardCounter(g);
                // do not return here just yet, we wish to capture all the guards, not primarily the ones with precedence
                result = false;
            }
        }
        return result;
    }

    private bool CanRequestOutgoing(TimeSpan timeStep, ILoad load, IGenerator generator)
    {
        var result = true;
        foreach (var g in guards)
        {
            if (!g.CanRequestToSend(timeStep, load, generator))
            {
                IncrementGuardCounter(g);
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
                    guardSummary.IncomingPowerGuards += 1;
                }
                else
                {
                    guardSummary.OutgoingPowerGuards += 1;
                }
                break;
            case BatteryCapacityGuard:
                if (incoming)
                {
                    guardSummary.FullCapacityGuards += 1;
                }
                else
                {
                    guardSummary.EmptyCapacityGuards += 1;
                }
                break;
            case OscillationGuard:
                guardSummary.OscillationGuards += 1;
                break;
        }
    }

    private void ReportLastTransfer(TransferResult lastTransferResult)
    {
        foreach (var guard in guards)
        {
            guard.ReportLastTransfer(lastTransferResult);
        }
    }

    public string Name => strategy.Name;

    public string Configuration => strategy.Configuration;

    public string PrettyConfiguration => strategy.PrettyConfiguration;

    public bool RequestsOutgoingPackets => strategy.RequestsOutgoingPackets;

    public string GuardConfiguration { get; }

    public IGuardSummary GuardSummary => guardSummary;

    private class GuardSummaryImpl : IGuardSummary
    {
        public int IncomingPowerGuards { get; set; }

        public int OutgoingPowerGuards { get; set; }

        public int EmptyCapacityGuards { get; set; }

        public int FullCapacityGuards { get; set; }

        public int OscillationGuards { get; set; }
    }
}