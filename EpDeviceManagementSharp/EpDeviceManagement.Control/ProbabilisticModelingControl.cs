using System.Security.Cryptography;
using EpDeviceManagement.Contracts;
using Stateless;
using UnitsNet;

namespace EpDeviceManagement.Control;

public class ProbabilisticModelingControl : IEpDeviceController
{
    private readonly IStorage battery;
    private readonly Energy probabilisticModeUpperLimit;
    private readonly Energy probabilisticModeLowerLimit;
    private readonly StateMachine<State, Event> stateMachine;
    private readonly RandomNumberGenerator random;
    private readonly Ratio p1probability;
    private readonly Ratio p2probability;
    private readonly Ratio p3probability;

    public ProbabilisticModelingControl(
        IStorage battery,
        Energy probabilisticModeUpperLimit,
        Energy probabilisticModeLowerLimit,
        RandomNumberGenerator random)
    {
        this.battery = battery;
        this.probabilisticModeUpperLimit = probabilisticModeUpperLimit;
        this.probabilisticModeLowerLimit = probabilisticModeLowerLimit;
        this.random = random;

        this.p1probability = Ratio.FromPercent(70);
        this.p2probability = Ratio.FromPercent(50);
        this.p3probability = Ratio.FromPercent(30);

        if (probabilisticModeUpperLimit < probabilisticModeLowerLimit)
        {
            throw new ArgumentException(
                $"{nameof(probabilisticModeUpperLimit)} cannot be lower than {nameof(probabilisticModeLowerLimit)}");
        }

        State initialState = State.P2;
        if (battery.CurrentStateOfCharge > probabilisticModeUpperLimit)
        {
            initialState = State.BatteryHigh;
        }
        if (battery.CurrentStateOfCharge < probabilisticModeLowerLimit)
        {
            initialState = State.BatteryLow;
        }
        this.stateMachine = BuildMachine(initialState);
    }

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

    public ControlDecision DoControl(TimeSpan timeStep, IEnumerable<ILoad> loads, TransferResult transferResult)
    {
        if (this.battery.CurrentStateOfCharge > this.probabilisticModeUpperLimit)
        {
            this.stateMachine.Fire(Event.BatteryAboveSetpoint);
        }
        else if (this.battery.CurrentStateOfCharge < this.probabilisticModeLowerLimit)
        {
            this.stateMachine.Fire(Event.BatteryBelowSetpoint);
        }
        else
        {
            this.stateMachine.Fire(Event.BatteryWithinLimits);
        }

        switch (transferResult)
        {
            case TransferResult.Success _:
                this.stateMachine.Fire(Event.TransferAccepted);
                break;
            case TransferResult.Failure _:
                this.stateMachine.Fire(Event.TransferDenied);
                break;
            case TransferResult.NoTransferRequested _:
                break;
        }

        switch (this.stateMachine.State)
        {
            case State.BatteryLow:
            {
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
                if (this.random.NextDouble() <= this.p1probability.DecimalFractions)
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
                if (this.random.NextDouble() <= this.p2probability.DecimalFractions)
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
                if (this.random.NextDouble() <= p3probability.DecimalFractions)
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
                var interruptibles = loads.OfType<IInterruptibleLoad>().ToList();
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

    public override string ToString()
    {
        return $"{nameof(ProbabilisticModelingControl)}: [{this.probabilisticModeLowerLimit}, {this.probabilisticModeUpperLimit}]";
    }

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