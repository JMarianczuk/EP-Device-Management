using UnitsNet;

namespace EpDeviceManagement.Simulation;

public class EnhancedEnergyDataSet
{
    public DateTimeOffset Timestamp { get; init; }

    public Power Residential1_Dishwasher { get; init; }

    public Power Residential1_Freezer { get; init; }

    public Power Residential1_HeatPump { get; init; }

    public Power Residential1_WashingMachine { get; init; }

    public Power Residential1_PV { get; init; }

    public override string ToString()
    {
        return
            $"Dish {Residential1_Dishwasher}, Freezer {Residential1_Freezer}, HeatPump {Residential1_HeatPump}, Wash {Residential1_WashingMachine}, PV {Residential1_PV}";
    }
}