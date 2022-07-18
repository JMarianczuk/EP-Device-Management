using System.Collections.Generic;
using UnitsNet;

namespace EpDeviceManagement.Contracts;

public interface ITclAsEnergy
{
    Energy CurrentStateOfCharge { get; } // x_TCL

    Energy LowerQoSBound { get; }

    Energy UpperQoSBound { get; }

    Frequency StandingLossRate { get; } // eta_sl

    Ratio HeatEfficiency { get; } // eta_h

    Power HeatPower { get; } // P_h

    IEnumerable<Energy> PredictStandingLoss(); // d_sl

    IEnumerable<Energy> PredictLoss(); // d_l,1

    IEnumerable<Frequency> PredictLossRate(); // d_l,2
}