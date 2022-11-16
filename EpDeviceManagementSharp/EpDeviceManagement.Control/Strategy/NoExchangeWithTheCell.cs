using EpDeviceManagement.Contracts;

namespace EpDeviceManagement.Control.Strategy;

public class NoExchangeWithTheCell : IEpDeviceController
{
    public ControlDecision DoControl(
        int dataPoint,
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