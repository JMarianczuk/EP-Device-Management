using System;

namespace EpDeviceManagement.Contracts;

public interface IDeferrableLoad : ILoad
{
    public TimeSpan MaximumPossibleDeferral { get; }
}