using EpDeviceManagement.Contracts;

namespace EpDeviceManagement.Control;

public class NoExchangeWithTheCell : IEpDeviceController
{
    public ControlDecision DoControl(TimeSpan timeStep, IEnumerable<ILoad> loads)
    {
        return new ControlDecision.NoAction();
    }
}