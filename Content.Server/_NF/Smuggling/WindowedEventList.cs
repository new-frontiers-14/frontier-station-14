using Robust.Shared.Timing;

namespace Content.Server._NF.Smuggling;

// <summary>
//  A counter to keep track of the number of events that happened over a shifting window of fixed length (e.g. "an hour ago").
// </summary>
public sealed class WindowedCounter
{
    [Dependency] private IGameTiming _timing = default!;
    private List<TimeSpan> _times = new();
    private TimeSpan _window;

    public WindowedCounter(TimeSpan window)
    {
        _times = new();
        _window = window;
    }

    public void Clear()
    {
        _times.Clear();
    }

    public void SetWindow(TimeSpan newWindow)
    {
        _window = newWindow;
        RemoveStaleEvents();
    }

    public void AddEvent()
    {
        _times.Add(_timing.CurTime);
        RemoveStaleEvents();
    }

    public int Count()
    {
        RemoveStaleEvents();
        return _times.Count;
    }

    void RemoveStaleEvents()
    {
        while (_times.Count > 0)
        {
            if (_times[0] < _timing.CurTime - _window)
                _times.RemoveAt(0);
            else
                break;
        }
    }
}