using System;
using UnitsNet;

namespace EpDeviceManagement.Contracts;

public interface IDeferrableLoad : ILoad
{
    TimeSpan MaximumPossibleDeferral { get; }

    Energy ExpectedTotalDemand { get; }
    
    bool IsDeferred { get; }
    
    TimeSpan TimeUntilStart { get; }

    void DeferFor(TimeSpan timeSpan);
}