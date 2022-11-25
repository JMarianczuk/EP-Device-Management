using System.Globalization;
using System.Security.Cryptography;
using EpDeviceManagement.Contracts;
using EpDeviceManagement.Control.Strategy.Base;
using EpDeviceManagement.Control.Strategy.Guards;
using EpDeviceManagement.UnitsExtensions;
using Stateless;
using UnitsNet;

namespace EpDeviceManagement.Control.Strategy;

public class PemControl : IEpDeviceController
{
    private readonly Ratio probabilisticModeLowerLevel;
    private readonly Ratio probabilisticModeUpperLevel;
    private readonly EnergyFast probabilisticModeUpperLimit;
    private readonly EnergyFast probabilisticModeLowerLimit;
    private readonly StateMachine stateMachine;
    private readonly RandomNumberGenerator random;
    private readonly bool withGeneration;
    private readonly TimeSpan[] MttrByState;

    public PemControl(
        IStorage battery,
        EnergyFast packetSize,
        Ratio probabilisticModeLowerLevel,
        Ratio probabilisticModeUpperLevel,
        RandomNumberGenerator random,
        bool withGeneration)
    {
        Battery = battery;
        this.random = random;
        this.withGeneration = withGeneration;
        this.MttrByState = new[]
        {
            TimeSpan.FromMinutes(5),
            TimeSpan.FromMinutes(10),
            TimeSpan.FromMinutes(15),
        };

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
        stateMachine = new StateMachine(initialState, withGeneration ? State.P1 : State.P3);
    }

    private IStorage Battery { get; }

    private double GetProbability(TimeSpan timeStep, int state, bool outgoing = false)
    {
        var currentStateOfCharge = this.Battery.CurrentStateOfCharge;
        var distanceFromUpperLimit = this.probabilisticModeUpperLimit - currentStateOfCharge;
        var distanceFromLowerLimit = currentStateOfCharge - this.probabilisticModeLowerLimit;
        var ratio = distanceFromUpperLimit / distanceFromLowerLimit;
        if (outgoing)
        {
            ratio = 1 / ratio;
        }
        var mttr = this.MttrByState[state - 1];
        var M_i = Frequency.FromHertz(1 / (mttr.TotalSeconds));
        var mu = ratio * M_i;
        var P_i = 1 - Math.Exp(-mu.Hertz * timeStep.TotalSeconds);
        return P_i;
    }

    public ControlDecision DoControl(
        TimeSpan timeStep,
        ILoad load,
        IGenerator generator,
        TransferResult transferResult)
    {
        if (this.Battery.CurrentStateOfCharge >= probabilisticModeUpperLimit)
        {
            stateMachine.Fire(Event.BatteryAboveSetpoint);
        }
        else if (this.Battery.CurrentStateOfCharge <= probabilisticModeLowerLimit)
        {
            stateMachine.Fire(Event.BatteryBelowSetpoint);
        }
        else
        {
            stateMachine.Fire(Event.BatteryWithinLimits);
        }

        switch (transferResult)
        {
            case TransferResult.Granted _:
                stateMachine.Fire(Event.TransferAccepted);
                break;
            case TransferResult.Declined _:
                stateMachine.Fire(Event.TransferDenied);
                break;
            case TransferResult.NoTransferRequested _:
                break;
        }

        ControlDecision GetDecision(int state)
        {
            var pr = random.NextDouble();
            var incomingProbability = GetProbability(timeStep, state);
            var outgoingProbability = GetProbability(timeStep, state, outgoing: true);
            var requestIncoming = pr <= incomingProbability;
            var requestOutgoing = this.withGeneration && pr <= outgoingProbability;
            if (requestIncoming && !requestOutgoing)
            {
                return ControlDecision.RequestTransfer.Incoming;
            }
            else if (!requestIncoming && requestOutgoing)
            {
                return ControlDecision.RequestTransfer.Outgoing;
            }
            else
            {
                return ControlDecision.NoAction.Instance;
            }
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
                return GetDecision(1);
            case State.P2:
                return GetDecision(2);
            case State.P3:
                return GetDecision(3);
            case State.BatteryHigh:
            {
                if (this.withGeneration)
                {
                    return ControlDecision.RequestTransfer.Outgoing;
                }
                else
                {
                    return ControlDecision.NoAction.Instance;
                }
            }
        }

        return ControlDecision.NoAction.Instance;
    }

    public string Name => "PEM Control";

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
        private readonly State stateToReturnToFromBatteryHigh;

        public StateMachine(State initialState, State stateToReturnToFromBatteryHigh)
        {
            this.stateToReturnToFromBatteryHigh = stateToReturnToFromBatteryHigh;
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
                        Event.BatteryWithinLimits => this.stateToReturnToFromBatteryHigh,
                        Event.BatteryBelowSetpoint => State.BatteryLow,
                        _ => this.State,
                    };
                    break;
            }
        }
    }
}