using System;

namespace EpDeviceManagement.Contracts;

public interface IInterruptibleLoad : ILoad
{
    bool CanCurrentlyBeInterrupted { get; }
    
    TimeSpan MinimumInterruptTime { get; }
    
    TimeSpan MaximumInterruptTime { get; }
}