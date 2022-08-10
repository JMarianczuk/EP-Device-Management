using EpDeviceManagement.Contracts;

namespace EpDeviceManagement.Control;

public class NoExchangeWithTheCell : IEpDeviceController
{
    public ControlDecision DoControl(TimeSpan timeStep, IEnumerable<ILoad> loads, TransferResult lastTransferResult)
    {
        return new ControlDecision.NoAction();
    }

    public override string ToString()
    {
        return $"{nameof(NoExchangeWithTheCell)}";
    }
}