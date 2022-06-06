using System;
using UnitsNet;

namespace EpDeviceManagement.Contracts;

public interface IDeferrableLoad : ILoad
{
    TimeSpan MaximumPossibleDeferral { get; }

    Energy ExpectedTotalDemand { get; }
}