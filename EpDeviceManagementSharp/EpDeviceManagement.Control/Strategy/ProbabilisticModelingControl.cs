using System.Security.Cryptography;
using EpDeviceManagement.Contracts;
using Stateless;
using UnitsNet;

namespace EpDeviceManagement.Control.Strategy;

public class ProbabilisticModelingControl : CapacityRespectingStrategy, IEpDeviceController
{
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
        Energy probabilisticModeUpperLimit,
        Energy probabilisticModeLowerLimit,
        RandomNumberGenerator random)
        : base(
            battery,
            packetSize)
    {
        this.probabilisticModeUpperLimit = probabilisticModeUpperLimit;
        this.probabilisticModeLowerLimit = probabilisticModeLowerLimit;
        this.random = random;

        p1probability = Ratio.FromPercent(70);
        p2probability = Ratio.FromPercent(50);
        p3probability = Ratio.FromPercent(30);

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
        stateMachine = BuildMachine(initialState);
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

    public ControlDecision DoControl(TimeSpan timeStep, IEnumerable<ILoad> loads, IEnumerable<IGenerator> generators, TransferResult transferResult)
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

                if (CanRequestIncoming(timeStep, loads, generators))
                {
                    return new ControlDecision.RequestTransfer()
                    {
                        RequestedDirection = PacketTransferDirection.Incoming,
                    };
                }

                break;
            }
            case State.P1:
                if (random.NextDouble() <= p1probability.DecimalFractions
                    && this.CanRequestIncoming(timeStep, loads, generators))
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
                if (random.NextDouble() <= p2probability.DecimalFractions
                    && this.CanRequestIncoming(timeStep, loads, generators))
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
                if (random.NextDouble() <= p3probability.DecimalFractions
                    && this.CanRequestIncoming(timeStep, loads, generators))
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

                if (this.CanRequestOutgoing(timeStep, loads, generators))
                {
                    return new ControlDecision.RequestTransfer()
                    {
                        RequestedDirection = PacketTransferDirection.Outgoing,
                    };
                }

                break;
            }
        }

        return new ControlDecision.NoAction();
    }

    public string Name => nameof(ProbabilisticModelingControl);

    public string Configuration => $"[{probabilisticModeLowerLimit}, {probabilisticModeUpperLimit}]";

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