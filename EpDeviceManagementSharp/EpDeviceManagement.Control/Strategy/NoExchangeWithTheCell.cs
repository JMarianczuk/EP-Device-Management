using EpDeviceManagement.Contracts;

namespace EpDeviceManagement.Control.Strategy;

public class NoExchangeWithTheCell : IEpDeviceController
{
    public ControlDecision DoControl(TimeSpan timeStep, IEnumerable<ILoad> loads, IEnumerable<IGenerator> generators, TransferResult lastTransferResult)
    {
        return new ControlDecision.NoAction();
    }

    public string Name => nameof(NoExchangeWithTheCell);

    public string Configuration => string.Empty;
}