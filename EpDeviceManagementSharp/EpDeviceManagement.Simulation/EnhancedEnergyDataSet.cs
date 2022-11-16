using System.Diagnostics;
using System.Net.Security;
using EpDeviceManagement.UnitsExtensions;
using UnitsNet;

namespace EpDeviceManagement.Simulation;

[DebuggerDisplay("{" + nameof(Timestamp) + "}")]
public class EnhancedEnergyDataSet
{
    public DateTimeOffset Timestamp { get; init; }

    public PowerFast Residential1_Load { get; init; }

    public PowerFast Residential1_Generation { get; init; }

    public PowerFast Residential2_Load { get; init; }

    public PowerFast Residential4_Load { get; init; }

    public PowerFast Residential4_ControllableLoad { get; init; }

    public PowerFast Residential4_Generation { get; init; }

    public override string ToString()
    {
        return $"Res1 -{Residential1_Load}, +{Residential1_Generation}, Res2 -{Residential2_Load}, Res4 -{Residential4_Load + Residential4_ControllableLoad} +{Residential4_Generation}";
        //return $"Dish {Residential1_Dishwasher}, Freezer {Residential1_Freezer}, HeatPump {Residential1_HeatPump}, Wash {Residential1_WashingMachine}, PV {Residential1_PV}";
    }
}

[DebuggerDisplay("{" + nameof(Timestamp) + "}")]
public class EnhancedPowerDataSet
{
    public DateTimeOffset Timestamp { get; init; }

    public PowerFast Residential1_Load { get; init; }

    public PowerFast Residential1_Generation { get; init; }

    public PowerFast Residential2_Load { get; init; }

    public PowerFast Residential3_Load { get; init; }

    public PowerFast Residential3_Generation { get; init; }

    public PowerFast Residential4_Load { get; init; }

    public PowerFast Residential4_Generation { get; init; }

    public PowerFast Residential4_Import { get; init; }

    public PowerFast Residential4_Export { get; init; }

    public PowerFast Residential5_Load { get; init; }

    public PowerFast Industrial3_Load { get; init; }

    public PowerFast Industrial3_ControllableLoad { get; init; }

    public PowerFast Industrial3_Generation { get; init; }

    public EnhancedPowerDataSet? FineResDataSet { get; init; }
}