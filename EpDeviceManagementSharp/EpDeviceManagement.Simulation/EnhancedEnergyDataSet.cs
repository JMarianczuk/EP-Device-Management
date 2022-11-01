using UnitsNet;

namespace EpDeviceManagement.Simulation;

public class EnhancedEnergyDataSet
{
    public DateTimeOffset Timestamp { get; init; }

    //public Power Residential1_Dishwasher { get; init; }

    //public Power Residential1_Freezer { get; init; }

    //public Power Residential1_HeatPump { get; init; }

    //public Power Residential1_WashingMachine { get; init; }

    //public Power Residential1_PV { get; init; }

    //public Power Residential2_CirculationPump { get; init; }

    //public Power Residential2_Freezer { get; init; }

    //public Power Residential2_Dishwasher { get; init; }

    //public Power Residential2_WashingMachine { get; init; }

    public Power Residential1_Load { get; init; }

    public Power Residential1_Generation { get; init; }

    public Power Residential2_Load { get; init; }

    public Power Residential4_Load { get; init; }

    public Power Residential4_ControllableLoad { get; init; }

    public Power Residential4_Generation { get; init; }

    public override string ToString()
    {
        return $"Res1 -{Residential1_Load}, +{Residential1_Generation}, Res2 -{Residential2_Load}, Res4 -{Residential4_Load + Residential4_ControllableLoad} +{Residential4_Generation}";
        //return $"Dish {Residential1_Dishwasher}, Freezer {Residential1_Freezer}, HeatPump {Residential1_HeatPump}, Wash {Residential1_WashingMachine}, PV {Residential1_PV}";
    }
}

public class EnhancedPowerDataSet
{
    public DateTimeOffset Timestamp { get; init; }

    public Power Residential1_Load { get; init; }

    public Power Residential1_Generation { get; init; }

    public Power Residential2_Load { get; init; }

    public Power Residential3_Load { get; init; }

    public Power Residential3_Generation { get; init; }

    public Power Residential4_Load { get; init; }

    public Power Residential4_ControllableLoad { get; init; }

    public Power Residential4_Generation { get; init; }

    public Power Residential5_Load { get; init; }

    public Power Industrial3_Load { get; init; }

    public Power Industrial3_ControllableLoad { get; init; }

    public Power Industrial3_Generation { get; init; }

    public EnhancedPowerDataSet? FineResDataSet { get; init; }
}