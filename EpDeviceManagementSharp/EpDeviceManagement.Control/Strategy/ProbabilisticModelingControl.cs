using System.Globalization;
using System.Security.Cryptography;
using EpDeviceManagement.Contracts;
using EpDeviceManagement.Control.Strategy.Base;
using EpDeviceManagement.Control.Strategy.Guards;
using Stateless;
using UnitsNet;

namespace EpDeviceManagement.Control.Strategy;

public class ProbabilisticModelingControl : GuardedStrategy, IEpDeviceController
{
    private readonly Ratio probabilisticModeLowerLevel;
    private readonly Ratio probabilisticModeUpperLevel;
    private readonly Energy probabilisticModeUpperLimit;
    private readonly Energy probabilisticModeLowerLimit;
    private readonly StateMachine<State, Event> stateMachine;
    private readonly RandomNumberGenerator random;
    private readonly Ratio p1probability;
    private readonly Ratio p2probability;
    private readonly Ratio p3probability;

    public ProbabilisticModelingControl(
        IStorage battery,
        Energy packetSize,
        Ratio probabilisticModeLowerLevel,
        Ratio probabilisticModeUpperLevel,
        RandomNumberGenerator random)
        : base(
            new BatteryCapacityGuard(battery, packetSize),
            new BatteryPowerGuard(battery, packetSize),
            new OscillationGuard())
    {
        Battery = battery;
        this.random = random;

        p1probability = Ratio.FromPercent(70);
        p2probability = Ratio.FromPercent(50);
        p3probability = Ratio.FromPercent(30);

        if (probabilisticModeUpperLevel < probabilisticModeLowerLevel)
        {
            throw new ArgumentException(
                $"{nameof(probabilisticModeUpperLevel)} cannot be lower than {nameof(probabilisticModeLowerLevel)}");
        }
        

        if (probabilisticModeLowerLevel < Ratio.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(probabilisticModeLowerLevel), probabilisticModeLowerLevel,
                "cannot be below zero");
        }

        if (probabilisticModeUpperLevel > Ratio.FromPercent(100))
        {
            throw new ArgumentOutOfRangeException(nameof(probabilisticModeUpperLevel), probabilisticModeUpperLevel,
                $"cannot be greater than 100%");
        }

        this.probabilisticModeLowerLevel = probabilisticModeLowerLevel;
        this.probabilisticModeUpperLevel = probabilisticModeUpperLevel;
        this.probabilisticModeLowerLimit = battery.TotalCapacity * probabilisticModeLowerLevel.DecimalFractions;
        this.probabilisticModeUpperLimit = battery.TotalCapacity * probabilisticModeUpperLevel.DecimalFractions;

        State initialState = State.P2;
        if (battery.CurrentStateOfCharge > probabilisticModeUpperLimit)
        {
            initialState = State.BatteryHigh;
        }
        if (battery.CurrentStateOfCharge < probabilisticModeLowerLimit)
        {
            initialState = State.BatteryLow;
        }
        stateMachine = BuildMachine(initialState);
    }

    private IStorage Battery { get; }

    public StateMachine<State, Event> BuildMachine(State initialState)
    {
        var sm = new StateMachine<State, Event>(initialState);

        sm.Configure(State.BatteryLow)
            .PermitReentry(Event.BatteryBelowSetpoint)
            .Permit(Event.BatteryWithinLimits, State.P1)
            .Permit(Event.BatteryAboveSetpoint, State.BatteryHigh)
            .PermitReentry(Event.TransferAccepted)
            .PermitReentry(Event.TransferDenied);

        sm.Configure(State.P1)
            .PermitReentry(Event.TransferAccepted)
            .Permit(Event.TransferDenied, State.P2)
            .Permit(Event.BatteryBelowSetpoint, State.BatteryLow)
            .Permit(Event.BatteryAboveSetpoint, State.BatteryHigh)
            .PermitReentry(Event.BatteryWithinLimits);

        sm.Configure(State.P2)
            .Permit(Event.TransferAccepted, State.P1)
            .Permit(Event.TransferDenied, State.P3)
            .Permit(Event.BatteryBelowSetpoint, State.BatteryLow)
            .Permit(Event.BatteryAboveSetpoint, State.BatteryHigh)
            .PermitReentry(Event.BatteryWithinLimits);

        sm.Configure(State.P3)
            .PermitReentry(Event.TransferDenied)
            .Permit(Event.TransferAccepted, State.P2)
            .Permit(Event.BatteryBelowSetpoint, State.BatteryLow)
            .Permit(Event.BatteryAboveSetpoint, State.BatteryHigh)
            .PermitReentry(Event.BatteryWithinLimits);

        sm.Configure(State.BatteryHigh)
            .PermitReentry(Event.BatteryAboveSetpoint)
            .Permit(Event.BatteryWithinLimits, State.P3)
            .Permit(Event.BatteryBelowSetpoint, State.BatteryLow)
            .PermitReentry(Event.TransferAccepted)
            .PermitReentry(Event.TransferDenied);

        return sm;
    }

    protected override ControlDecision DoUnguardedControl(
        TimeSpan timeStep,
        IEnumerable<ILoad> loads,
        IEnumerable<IGenerator> generators,
        TransferResult transferResult)
    {
        if (this.Battery.CurrentStateOfCharge > probabilisticModeUpperLimit)
        {
            stateMachine.Fire(Event.BatteryAboveSetpoint);
        }
        else if (this.Battery.CurrentStateOfCharge < probabilisticModeLowerLimit)
        {
            stateMachine.Fire(Event.BatteryBelowSetpoint);
        }
        else
        {
            stateMachine.Fire(Event.BatteryWithinLimits);
        }

        switch (transferResult)
        {
            case TransferResult.Success _:
                stateMachine.Fire(Event.TransferAccepted);
                break;
            case TransferResult.Failure _:
                stateMachine.Fire(Event.TransferDenied);
                break;
            case TransferResult.NoTransferRequested _:
                break;
        }

        switch (stateMachine.State)
        {
            case State.BatteryLow:
            {
                loads = loads is ICollection<ILoad> list ? list : loads.ToList();
                var interruptibles = loads.OfType<IInterruptibleLoad>().ToList();
                foreach (var i in interruptibles)
                {
                    if (i.CanCurrentlyBeInterrupted && !i.IsCurrentlyInInterruptedState)
                    {
                        i.Interrupt();
                    }
                }

                return new ControlDecision.RequestTransfer()
                {
                    RequestedDirection = PacketTransferDirection.Incoming,
                };
            }
            case State.P1:
                if (random.NextDouble() <= p1probability.DecimalFractions)
                {
                    return new ControlDecision.RequestTransfer()
                    {
                        RequestedDirection = PacketTransferDirection.Incoming,
                    };
                }
                else
                {
                    return new ControlDecision.NoAction();
                }
            case State.P2:
                if (random.NextDouble() <= p2probability.DecimalFractions)
                {
                    return new ControlDecision.RequestTransfer()
                    {
                        RequestedDirection = PacketTransferDirection.Incoming,
                    };
                }
                else
                {
                    return new ControlDecision.NoAction();
                }
            case State.P3:
                if (random.NextDouble() <= p3probability.DecimalFractions)
                {
                    return new ControlDecision.RequestTransfer()
                    {
                        RequestedDirection = PacketTransferDirection.Incoming,
                    };
                }
                else
                {
                    return new ControlDecision.NoAction();
                }
            case State.BatteryHigh:
            {
                loads = loads is ICollection<ILoad> list ? list : loads.ToList();
                var interruptibles = loads.OfType<IInterruptibleLoad>();
                foreach (var i in interruptibles)
                {
                    if (i.IsCurrentlyInInterruptedState && i.CanCurrentlyBeResumed)
                    {
                        i.Resume();
                    }
                }
                    
                return new ControlDecision.RequestTransfer()
                {
                    RequestedDirection = PacketTransferDirection.Outgoing,
                };
            }
        }

        return new ControlDecision.NoAction();
    }

    public override string Name => nameof(ProbabilisticModelingControl);

    public override string Configuration => string.Create(CultureInfo.InvariantCulture,
        $"[{this.probabilisticModeLowerLevel.DecimalFractions:F2}, {this.probabilisticModeUpperLevel.DecimalFractions:F2}]");

    public override string PrettyConfiguration => $"[{probabilisticModeLowerLimit}, {probabilisticModeUpperLimit}]";

    public enum State
    {
        P1,
        P2,
        P3,
        BatteryLow,
        BatteryHigh,
    }

    public enum Event
    {
        BatteryBelowSetpoint,
        BatteryAboveSetpoint,
        BatteryWithinLimits,
        TransferAccepted,
        TransferDenied,
    }
}