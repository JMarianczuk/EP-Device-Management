using System;
using System.Collections.Generic;

namespace EpDeviceManagement.Contracts;

public interface IEpDeviceController
{
    /*
     * Either needs to be a list of decisions,
     * or the controller makes the decisions itself
     */
    ControlDecision DoControl(TimeSpan timeStep, IEnumerable<ILoad> loads, TransferResult lastTransferResult);
}