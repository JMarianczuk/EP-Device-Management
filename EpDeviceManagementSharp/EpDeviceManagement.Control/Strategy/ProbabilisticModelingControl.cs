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
    private readonly StateMachine stateMachine;
    private readonly RandomNumberGenerator random;
    private readonly double p1probability;
    private readonly double p2probability;
    private readonly double p3probability;

    public ProbabilisticModelingControl(
        IStorage battery,
        Energy packetSize,
        Ratio probabilisticModeLowerLevel,
        Ratio probabilisticModeUpperLevel,
        RandomNumberGenerator random,
        bool withOscillationGuard)
        : base(
            new BatteryCapacityGuard(battery, packetSize),
            new BatteryPowerGuard(battery, packetSize),
            withOscillationGuard ? new OscillationGuard() : DummyGuard.Instance)
    {
        Battery = battery;
        this.random = random;

        p1probability = Ratio.FromPercent(70).DecimalFractions;
        p2probability = Ratio.FromPercent(50).DecimalFractions;
        p3probability = Ratio.FromPercent(30).DecimalFractions;

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
        stateMachine = new StateMachine(initialState);
    }

    private IStorage Battery { get; }

    //public StateMachine<State, Event> BuildMachine(State initialState)
    //{
    //    var sm = new StateMachine<State, Event>(initialState);

    //    sm.Configure(State.BatteryLow)
    //        .PermitReentry(Event.BatteryBelowSetpoint)
    //        .Permit(Event.BatteryWithinLimits, State.P1)
    //        .Permit(Event.BatteryAboveSetpoint, State.BatteryHigh)
    //        .PermitReentry(Event.TransferAccepted)
    //        .PermitReentry(Event.TransferDenied);

    //    sm.Configure(State.P1)
    //        .PermitReentry(Event.TransferAccepted)
    //        .Permit(Event.TransferDenied, State.P2)
    //        .Permit(Event.BatteryBelowSetpoint, State.BatteryLow)
    //        .Permit(Event.BatteryAboveSetpoint, State.BatteryHigh)
    //        .PermitReentry(Event.BatteryWithinLimits);

    //    sm.Configure(State.P2)
    //        .Permit(Event.TransferAccepted, State.P1)
    //        .Permit(Event.TransferDenied, State.P3)
    //        .Permit(Event.BatteryBelowSetpoint, State.BatteryLow)
    //        .Permit(Event.BatteryAboveSetpoint, State.BatteryHigh)
    //        .PermitReentry(Event.BatteryWithinLimits);

    //    sm.Configure(State.P3)
    //        .PermitReentry(Event.TransferDenied)
    //        .Permit(Event.TransferAccepted, State.P2)
    //        .Permit(Event.BatteryBelowSetpoint, State.BatteryLow)
    //        .Permit(Event.BatteryAboveSetpoint, State.BatteryHigh)
    //        .PermitReentry(Event.BatteryWithinLimits);

    //    sm.Configure(State.BatteryHigh)
    //        .PermitReentry(Event.BatteryAboveSetpoint)
    //        .Permit(Event.BatteryWithinLimits, State.P3)
    //        .Permit(Event.BatteryBelowSetpoint, State.BatteryLow)
    //        .PermitReentry(Event.TransferAccepted)
    //        .PermitReentry(Event.TransferDenied);

    //    return sm;
    //}

    //public StateMachine<State, Event> BuildSimplifiedMachine(State initialState)
    //{
    //    var sm = new StateMachine<State, Event>(initialState);

    //    sm.Configure(State.BatteryLow)
    //        .Permit(Event.BatteryWithinLimits, State.P1);

    //    sm.Configure(State.P1)
    //        .PermitReentry(Event.TransferAccepted)
    //        .Permit(Event.TransferDenied, State.P2)
    //        .Permit(Event.BatteryBelowSetpoint, State.BatteryLow)
    //        .Permit(Event.BatteryAboveSetpoint, State.BatteryHigh);

    //    sm.Configure(State.P2)
    //        .Permit(Event.TransferAccepted, State.P1)
    //        .Permit(Event.TransferDenied, State.P3)
    //        .Permit(Event.BatteryBelowSetpoint, State.BatteryLow)
    //        .Permit(Event.BatteryAboveSetpoint, State.BatteryHigh);

    //    sm.Configure(State.P3)
    //        .PermitReentry(Event.TransferDenied)
    //        .Permit(Event.TransferAccepted, State.P2)
    //        .Permit(Event.BatteryBelowSetpoint, State.BatteryLow)
    //        .Permit(Event.BatteryAboveSetpoint, State.BatteryHigh);

    //    sm.Configure(State.BatteryHigh)
    //        .Permit(Event.BatteryWithinLimits, State.P3);

    //    return sm;
    //}

    protected override ControlDecision DoUnguardedControl(
        int dataPoint,
        TimeSpan timeStep,
        ILoad[] loads,
        IGenerator[] generators,
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
                foreach (var load in loads)
                {
                    if (load is IInterruptibleLoad { CanCurrentlyBeInterrupted: true, IsCurrentlyInInterruptedState: false } i)
                    {
                        i.Interrupt();
                    }
                }

                return ControlDecision.RequestTransfer.Incoming;
            }
            case State.P1:
                if (random.NextDouble() <= p1probability)
                {
                    return ControlDecision.RequestTransfer.Incoming;
                }
                else
                {
                    return ControlDecision.NoAction.Instance;
                }
            case State.P2:
                if (random.NextDouble() <= p2probability)
                {
                    return ControlDecision.RequestTransfer.Incoming;
                }
                else
                {
                    return ControlDecision.NoAction.Instance;
                }
            case State.P3:
                if (random.NextDouble() <= p3probability)
                {
                    return ControlDecision.RequestTransfer.Incoming;
                }
                else
                {
                    return ControlDecision.NoAction.Instance;
                }
            case State.BatteryHigh:
            {
                foreach (var load in loads)
                {
                    if (load is IInterruptibleLoad { IsCurrentlyInInterruptedState: true, CanCurrentlyBeResumed: true } i)
                    {
                        i.Resume();
                    }
                }

                return ControlDecision.RequestTransfer.Outgoing;
            }
        }

        return ControlDecision.NoAction.Instance;
    }

    public override string Name => "TCL Control";

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

    public class StateMachine
    {
        public StateMachine(State initialState)
        {
            this.State = initialState;
        }

        public State State { get; private set; }

        public void Fire(Event trigger)
        {
            switch (this.State)
            {
                case State.BatteryLow:
                    this.State = trigger switch
                    {
                        Event.BatteryWithinLimits => State.P1,
                        Event.BatteryAboveSetpoint => State.BatteryHigh,
                        _ => this.State,
                    };
                    break;
                case State.P1:
                    this.State = trigger switch
                    {
                        Event.TransferDenied => State.P2,
                        Event.BatteryBelowSetpoint => State.BatteryLow,
                        Event.BatteryAboveSetpoint => State.BatteryHigh,
                        _ => this.State,
                    };
                    break;
                case State.P2:
                    this.State = trigger switch
                    {
                        Event.TransferAccepted => State.P1,
                        Event.TransferDenied => State.P3,
                        Event.BatteryBelowSetpoint => State.BatteryLow,
                        Event.BatteryAboveSetpoint => State.BatteryHigh,
                        _ => this.State,
                    };
                    break;
                case State.P3:
                    this.State = trigger switch
                    {
                        Event.TransferAccepted => State.P2,
                        Event.BatteryBelowSetpoint => State.BatteryLow,
                        Event.BatteryAboveSetpoint => State.BatteryHigh,
                        _ => this.State,
                    };
                    break;
                case State.BatteryHigh:
                    this.State = trigger switch
                    {
                        Event.BatteryWithinLimits => State.P3,
                        Event.BatteryBelowSetpoint => State.BatteryLow,
                        _ => this.State,
                    };
                    break;
            }
        }
    }
}