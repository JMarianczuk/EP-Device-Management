using System.Globalization;
using System.Security.Cryptography;
using EpDeviceManagement.Contracts;
using EpDeviceManagement.Control.Strategy.Base;
using EpDeviceManagement.Control.Strategy.Guards;
using EpDeviceManagement.UnitsExtensions;
using Stateless;
using UnitsNet;

namespace EpDeviceManagement.Control.Strategy;

public class ProbabilisticModelingControl : IEpDeviceController
{
    private readonly Ratio probabilisticModeLowerLevel;
    private readonly Ratio probabilisticModeUpperLevel;
    private readonly EnergyFast probabilisticModeUpperLimit;
    private readonly EnergyFast probabilisticModeLowerLimit;
    private readonly StateMachine stateMachine;
    private readonly RandomNumberGenerator random;
    private readonly bool withGeneration;
    private readonly double p1probability;
    private readonly double p2probability;
    private readonly double p3probability;

    public ProbabilisticModelingControl(
        IStorage battery,
        Energy packetSize,
        Ratio probabilisticModeLowerLevel,
        Ratio probabilisticModeUpperLevel,
        RandomNumberGenerator random,
        bool withGeneration)
    {
        Battery = battery;
        this.random = random;
        this.withGeneration = withGeneration;

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

    public ControlDecision DoControl(
        int dataPoint,
        TimeSpan timeStep,
        ILoad load,
        IGenerator generator,
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
                //if (load is IInterruptibleLoad { CanCurrentlyBeInterrupted: true, IsCurrentlyInInterruptedState: false } i)
                //{
                //    i.Interrupt();
                //}

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
                return ControlDecision.NoAction.Instance;
            }
        }

        return ControlDecision.NoAction.Instance;
    }

    public string Name => "State-Dependent Probabilistic Control";

    public string Configuration => string.Create(CultureInfo.InvariantCulture,
        $"[{this.probabilisticModeLowerLevel.DecimalFractions:F1}, {this.probabilisticModeUpperLevel.DecimalFractions:F1}]");

    public string PrettyConfiguration => $"[{probabilisticModeLowerLimit}, {probabilisticModeUpperLimit}]";

    public bool RequestsOutgoingPackets => this.withGeneration;

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