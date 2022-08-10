using EpDeviceManagement.Contracts;

namespace EpDeviceManagement.Control;

public class AlwaysAcceptRequests : IEpDeviceController
{
    public ControlDecision DoControl(TimeSpan timeStep, IEnumerable<ILoad> loads, TransferResult lastTransferResult)
    {
        return new ControlDecision.AcceptIncomingRequest()
        {
            AcceptIncoming = true,
            AcceptOutgoing = true,
        };
    }
}