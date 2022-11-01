using System;
using System.Collections.Generic;

namespace EpDeviceManagement.Contracts;

public interface IEpDeviceController
{
    string Name { get; }

    string Configuration { get; }

    string PrettyConfiguration { get; }

    /*
     * Either needs to be a list of decisions,
     * or the controller makes the decisions itself
     */
    ControlDecision DoControl(
        int dataPoint,
        TimeSpan timeStep,
        ILoad[] loads,
        IGenerator[] generators,
        TransferResult lastTransferResult);
}