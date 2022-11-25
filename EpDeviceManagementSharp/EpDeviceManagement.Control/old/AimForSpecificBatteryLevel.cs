//using System.Globalization;
//using EpDeviceManagement.Contracts;
//using EpDeviceManagement.Control.Strategy.Base;
//using EpDeviceManagement.Control.Strategy.Guards;
//using UnitsNet;

//namespace EpDeviceManagement.Control.Strategy;

//public class AimForSpecificBatteryLevel : GuardedStrategy, IEpDeviceController
//{
//    private readonly Ratio desiredLevel;
//    private readonly Energy desiredStateOfCharge;

//    public AimForSpecificBatteryLevel(
//        IStorage battery,
//        Energy packetSize,
//        Ratio desiredLevel)
//        : base(
//            new BatteryCapacityGuard(battery, packetSize),
//            new BatteryPowerGuard(battery, packetSize),
//            new OscillationGuard())
//    {
//        if (desiredLevel < Ratio.Zero)
//        {
//            throw new ArgumentOutOfRangeException(nameof(desiredLevel), desiredLevel,
//                "cannot be below zero");
//        }

//        Battery = battery;
//        if (desiredLevel > Ratio.FromPercent(100))
//        {
//            throw new ArgumentOutOfRangeException(nameof(desiredLevel), desiredLevel,
//                $"cannot be greater than 100%");
//        }

//        this.desiredLevel = desiredLevel;
//        this.desiredStateOfCharge = battery.TotalCapacity * desiredLevel.DecimalFractions;
//    }
    
//    private IStorage Battery { get; }

//    protected override ControlDecision DoUnguardedControl(
//        int dataPoint,
//        TimeSpan timeStep,
//        ILoad[] loads,
//        IGenerator[] generators,
//        TransferResult lastTransferResult)
//    {
//        PacketTransferDirection direction;
//        if (this.Battery.CurrentStateOfCharge < desiredStateOfCharge)
//        {
//            return ControlDecision.RequestTransfer.Incoming;
//        }
//        else if (this.Battery.CurrentStateOfCharge > desiredStateOfCharge)
//        {
//            return ControlDecision.RequestTransfer.Outgoing;
//        }
//        else
//        {
//            return ControlDecision.NoAction.Instance;
//        }
//    }

//    public override string Name => nameof(AimForSpecificBatteryLevel);

//    public override string Configuration => this.desiredLevel.DecimalFractions.ToString("F2", CultureInfo.InvariantCulture);

//    public override string PrettyConfiguration => desiredStateOfCharge.ToString();
//}