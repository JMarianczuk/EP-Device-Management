using EpDeviceManagement.Contracts;
using UnitsNet;

namespace EpDeviceManagement.Control.Strategy.Base;

public abstract class PowerRespectingStrategy : CapacityRespectingStrategy
{
    protected PowerRespectingStrategy(
        IStorage battery,
        Energy packetSize)
        : base(
            battery,
            packetSize)
    {
    }

    protected override bool CanRequestIncoming(TimeSpan timeStep, IEnumerable<ILoad> loads, IEnumerable<IGenerator> generators)
    {
        var canRequestWrtCapacity = base.CanRequestIncoming(timeStep, loads, generators);
        var expectedDischargePower = GetLoadsPower(loads)
                                     - GetGeneratorsPower(generators)
                                     - this.PacketSize / timeStep;
        if (expectedDischargePower > Power.Zero)
        {
            return canRequestWrtCapacity && expectedDischargePower <= this.Battery.MaximumDischargePower;
        }
        else
        {
            var expectedChargePower = -expectedDischargePower;
            return canRequestWrtCapacity && expectedChargePower <= this.Battery.MaximumChargePower;
        }
    }

    protected override bool CanRequestOutgoing(TimeSpan timeStep, IEnumerable<ILoad> loads, IEnumerable<IGenerator> generators)
    {
        var canRequestWrtCapacity = base.CanRequestOutgoing(timeStep, loads, generators);
        var expectedDischargePower = GetLoadsPower(loads)
                                     - GetGeneratorsPower(generators)
                                     + this.PacketSize / timeStep;
        if (expectedDischargePower > Power.Zero)
        {
            return canRequestWrtCapacity && expectedDischargePower <= this.Battery.MaximumDischargePower;
        }
        else
        {
            var expectedChargePower = -expectedDischargePower;
            return canRequestWrtCapacity && expectedChargePower <= this.Battery.MaximumChargePower;
        }
    }
}