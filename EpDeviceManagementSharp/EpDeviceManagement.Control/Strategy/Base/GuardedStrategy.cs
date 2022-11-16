﻿using System.Runtime.CompilerServices;
using EpDeviceManagement.Contracts;
using EpDeviceManagement.Control.Contracts;
using EpDeviceManagement.Control.Strategy.Guards;

namespace EpDeviceManagement.Control.Strategy.Base;

public abstract class GuardedStrategy : IEpDeviceController
{
    private readonly IControlGuard[] guards;
    private readonly GuardSummaryImpl guardSummary;

    private GuardedStrategy(
        params IControlGuard[] guards)
    {
        this.guards = guards;
        this.guardSummary = new GuardSummaryImpl();
    }

    public IGuardSummary GuardSummary => this.guardSummary;

    private bool CanRequestIncoming(TimeSpan timeStep, ILoad load, IGenerator generator)
    {
        var result = true;
        foreach (var g in guards)
        {
            if (!g.CanRequestIncoming(timeStep, load, generator))
            {
                this.IncrementGuardCounter(g);
                // do not return here just yet, we wish to capture all the guards, not primarily the ones with precedence
                result = false;
            }
        }
        return result;
    }

    private bool CanRequestOutgoing(TimeSpan timeStep, ILoad load, IGenerator generator)
    {
        var result = true;
        foreach (var g in this.guards)
        {
            if (!g.CanRequestOutgoing(timeStep, load, generator))
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

    public abstract bool RequestsOutgoingPackets { get; }

    protected abstract ControlDecision DoUnguardedControl(
        int dataPoint,
        TimeSpan timeStep,
        ILoad load,
        IGenerator generator,
        TransferResult lastTransferResult);

    public ControlDecision DoControl(
        int dataPoint,
        TimeSpan timeStep,
        ILoad load,
        IGenerator generator,
        TransferResult lastTransferResult)
    {
        this.ReportLastTransfer(lastTransferResult);
        var decision = this.DoUnguardedControl(dataPoint, timeStep, load, generator, lastTransferResult);
        if (decision is ControlDecision.RequestTransfer request)
        {
            switch (request.RequestedDirection)
            {
                case PacketTransferDirection.Incoming:
                    if (!this.CanRequestIncoming(timeStep, load, generator))
                    {
                        decision = ControlDecision.NoAction.Instance;
                    }
                    break;
                case PacketTransferDirection.Outgoing:
                    if (!this.CanRequestOutgoing(timeStep, load, generator))
                    {
                        decision = ControlDecision.NoAction.Instance;
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