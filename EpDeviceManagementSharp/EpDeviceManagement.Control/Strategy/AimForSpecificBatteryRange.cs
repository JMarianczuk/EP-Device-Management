using EpDeviceManagement.Contracts;
using UnitsNet;

namespace EpDeviceManagement.Control.Strategy;

public class AimForSpecificBatteryRange : CapacityRespectingStrategy, IEpDeviceController
{
    private readonly Energy desiredMinimumStateOfCharge;
    private readonly Energy desiredMaximumStateOfCharge;

    public AimForSpecificBatteryRange(
        IStorage battery,
        Energy packetSize,
        Energy desiredMinimumStateOfCharge,
        Energy desiredMaximumStateOfCharge)
        : base(
            battery,
            packetSize)
    {
        if (desiredMinimumStateOfCharge > desiredMaximumStateOfCharge)
        {
            throw new ArgumentException(
                $"{nameof(desiredMinimumStateOfCharge)} ({desiredMinimumStateOfCharge}) cannot be greater than {nameof(desiredMaximumStateOfCharge)} ({desiredMaximumStateOfCharge}).");
        }

        if (desiredMinimumStateOfCharge < Energy.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(desiredMinimumStateOfCharge), desiredMinimumStateOfCharge,
                "cannot be below zero");
        }
        this.desiredMinimumStateOfCharge = desiredMinimumStateOfCharge;

        if (desiredMaximumStateOfCharge > battery.TotalCapacity)
        {
            throw new ArgumentOutOfRangeException(nameof(desiredMaximumStateOfCharge), desiredMaximumStateOfCharge,
                $"cannot be greater than the battery capacity ({battery.TotalCapacity})");
        }
        this.desiredMaximumStateOfCharge = desiredMaximumStateOfCharge;
    }
    public ControlDecision DoControl(TimeSpan timeStep, IEnumerable<ILoad> loads, IEnumerable<IGenerator> generators, TransferResult lastTransferResult)
    {
        PacketTransferDirection direction;
        if (this.Battery.CurrentStateOfCharge < desiredMinimumStateOfCharge
            && this.CanRequestIncoming(timeStep, loads, generators))
        {
            direction = PacketTransferDirection.Incoming;
        }
        else if (this.Battery.CurrentStateOfCharge > desiredMaximumStateOfCharge
            && this.CanRequestOutgoing(timeStep, loads, generators))
        {
            direction = PacketTransferDirection.Outgoing;
        }
        else
        {
            return new ControlDecision.NoAction();
        }

        return new ControlDecision.RequestTransfer()
        {
            RequestedDirection = direction,
        };
    }

    public string Name => nameof(AimForSpecificBatteryRange);

    public string Configuration => $"[{desiredMinimumStateOfCharge},{desiredMaximumStateOfCharge}]";
}