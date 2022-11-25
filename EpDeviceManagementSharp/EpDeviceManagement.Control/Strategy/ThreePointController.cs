using System.Globalization;
using EpDeviceManagement.Contracts;
using EpDeviceManagement.Control.Strategy.Base;
using EpDeviceManagement.Control.Strategy.Guards;
using EpDeviceManagement.UnitsExtensions;
using UnitsNet;

namespace EpDeviceManagement.Control.Strategy;

public class ThreePointController : IEpDeviceController
{
    private readonly Ratio desiredMinimumLevel;
    private readonly Ratio desiredMaximumLevel;
    private readonly bool withGeneration;
    private readonly EnergyFast desiredMinimumStateOfCharge;
    private readonly EnergyFast desiredMaximumStateOfCharge;

    public ThreePointController(
        IStorage battery,
        EnergyFast packetSize,
        Ratio desiredMinimumLevel,
        Ratio desiredMaximumLevel,
        bool withGeneration)
    {
        if (desiredMinimumLevel > desiredMaximumLevel)
        {
            throw new ArgumentException(
                $"{nameof(desiredMinimumLevel)} ({desiredMinimumLevel}) cannot be greater than {nameof(desiredMaximumLevel)} ({desiredMaximumLevel}).");
        }

        if (desiredMinimumLevel < Ratio.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(desiredMinimumLevel), desiredMinimumLevel,
                "cannot be below zero");
        }

        Battery = battery;

        if (desiredMaximumLevel > Ratio.FromPercent(100))
        {
            throw new ArgumentOutOfRangeException(nameof(desiredMaximumLevel), desiredMaximumLevel,
                $"cannot be greater than 100%");
        }

        this.desiredMinimumLevel = desiredMinimumLevel;
        this.desiredMaximumLevel = desiredMaximumLevel;
        this.withGeneration = withGeneration;
        this.desiredMinimumStateOfCharge = battery.TotalCapacity * desiredMinimumLevel.DecimalFractions;
        this.desiredMaximumStateOfCharge = battery.TotalCapacity * desiredMaximumLevel.DecimalFractions;
    }

    private IStorage Battery { get; }

    public ControlDecision DoControl(
        TimeSpan timeStep,
        ILoad load,
        IGenerator generator,
        TransferResult lastTransferResult)
    {
        if (this.Battery.CurrentStateOfCharge < desiredMinimumStateOfCharge)
        {
            return ControlDecision.RequestTransfer.Incoming;
        }
        else if (this.Battery.CurrentStateOfCharge > desiredMaximumStateOfCharge
                 && this.withGeneration)
        {
            return ControlDecision.RequestTransfer.Outgoing;
        }
        else
        {
            return ControlDecision.NoAction.Instance;
        }
    }

    public string Name => "Three-Step Switching Controller";

    public string Configuration => string.Create(CultureInfo.InvariantCulture,
        $"[{this.desiredMinimumLevel.DecimalFractions:F1}, {this.desiredMaximumLevel.DecimalFractions:F1}]");

    public string PrettyConfiguration => $"[{desiredMinimumStateOfCharge},{desiredMaximumStateOfCharge}]";

    public bool RequestsOutgoingPackets => this.withGeneration;
}