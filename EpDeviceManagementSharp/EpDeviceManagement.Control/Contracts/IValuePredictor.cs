namespace EpDeviceManagement.Control.Contracts;

public interface IValuePredictor<out TValue>
{
    IEnumerable<TValue> Predict(int steps);
}