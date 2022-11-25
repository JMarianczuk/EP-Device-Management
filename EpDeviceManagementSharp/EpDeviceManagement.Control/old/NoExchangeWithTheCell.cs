using EpDeviceManagement.Contracts;

namespace EpDeviceManagement.Control.old;

public class NoExchangeWithTheCell : IEpDeviceController
{
    public ControlDecision DoControl(
        TimeSpan timeStep,
        ILoad load,
        IGenerator generator,
        TransferResult lastTransferResult)
    {
        return ControlDecision.NoAction.Instance;
    }

    public string Name => "No Exchange";

    public string Configuration => string.Empty;

    public string PrettyConfiguration => string.Empty;

    public bool RequestsOutgoingPackets => false;
}