using System;

namespace EpDeviceManagement.Contracts;

public interface IInterruptibleLoad : ILoad
{
    bool CanCurrentlyBeInterrupted { get; }

    bool CanCurrentlyBeResumed { get; }

    bool IsCurrentlyInInterruptedState { get; }

    TimeSpan MinimumInterruptTime { get; }
    
    TimeSpan MaximumInterruptTime { get; }

    //TimeSpan CurrentlyInterruptedFor { get; }

    void Interrupt();

    void Resume();
}