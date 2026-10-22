namespace HeatController;

internal class TimerHelper
{
    private readonly TimeSpan _timespan;
    private readonly Action _callback;
    private readonly bool _repeat;

    private bool _running;
    private bool _timerCallbackPending;

    public TimerHelper(TimeSpan timespan, Action callback, bool repeat)
    {
        _timespan = timespan;
        _callback = callback;
        _repeat = repeat;
    }

    public void Start()
    {
        _running = true;
        ScheduleTimerIfNeeded();
    }

    public void Stop()
    {
        _running = false;
    }

    private void ScheduleTimerIfNeeded()
    {
        if (!_timerCallbackPending)
        {
            {
                _timerCallbackPending = true;
                Device.StartTimer(_timespan, TimerCallback);
            }
        }
    }

    private bool TimerCallback()
    {
        if (_running)
        {
            _callback.Invoke();
        }

        bool reschedule = _repeat && _running;

        _timerCallbackPending = reschedule;

        return reschedule;
    }

}