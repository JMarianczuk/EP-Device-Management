namespace EpDeviceManagement.Simulation;

public class ConsoleProgressBar : IProgressIndicator
{
    private double _elementSlotWidth;

    private readonly char _progress;
    private int _current = 0;
    private int _written = 0;
    private readonly object lockHandle = new object();

    public ConsoleProgressBar(char progress = '-')
    {
        _progress = progress;
    }

    public void Setup(int total, string progressTitle = null)
    {
        _elementSlotWidth = (double) Console.WindowWidth / total;
        _current = 0;
        _written = 0;
        if (progressTitle != null)
        {
            Console.WriteLine(progressTitle);
        }
    }

    public void FinishOne()
    {
        lock (lockHandle)
        {
            _current += 1;

            double slots = _elementSlotWidth * _current;
            while (_written < Math.Ceiling(slots) && _written < Console.WindowWidth)
            {
                Console.Write(_progress);
                _written += 1;
            }
        }
    }

    public void ProgressComplete()
    {
        Console.WriteLine("");
    }
    public void Dispose()
    {
        ProgressComplete();
    }
}

public interface IProgressIndicator : IDisposable
{
    void Setup(int total, string progressTitle = null);
    void FinishOne();
    void ProgressComplete();
}