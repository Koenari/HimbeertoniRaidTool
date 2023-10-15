using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using HimbeertoniRaidTool.Plugin.UI;

namespace HimbeertoniRaidTool.Plugin.Services;

internal class TaskManager : IDisposable
{
    private class TaskWrapper
    {
        public readonly Task SystemTask;
        public readonly HrtTask InternalTask;

        public TaskWrapper(HrtTask task)
        {
            InternalTask = task;
            SystemTask = Task.Run(() => task.CallBack(task.Action()));
        }
    }

    private readonly Timer _taskTimer;
    private readonly Timer _secondTimer;
    private readonly Timer _minuteTimer;

    private readonly ConcurrentQueue<TaskWrapper> _tasksOnce = new();
    private readonly ConcurrentBag<PeriodicTask> _tasksOnSecond = new();
    private readonly ConcurrentBag<PeriodicTask> _tasksOnMinute = new();
    private bool _disposedValue;

    internal TaskManager()
    {
        _secondTimer = new Timer(OnSecondTimer, null, 1000, 1000);
        _taskTimer = new Timer(OnTaskTimer, null, 1000, 1000);
        _minuteTimer = new Timer(OnMinuteTimer, null, 1000, 60 * 1000);
    }

    private void OnTaskTimer(object? _)
    {
        if (_disposedValue || _tasksOnce.IsEmpty) return;
        while (_tasksOnce.TryPeek(out TaskWrapper? oldestTask) && oldestTask.SystemTask.IsCompleted)
            if (_tasksOnce.TryDequeue(out TaskWrapper? completedTask))
            {
                if (completedTask.SystemTask.IsFaulted)
                    ServiceManager.PluginLog.Error(
                        $"Task \"{completedTask.InternalTask.Name}\" finished with an error");
                else
                    ServiceManager.PluginLog.Info(
                        $"Task \"{completedTask.InternalTask.Name}\" finished successful");
            }
    }

    private void OnSecondTimer(object? _)
    {
        if (_disposedValue || _tasksOnSecond.IsEmpty) return;
        DateTime executionTime = DateTime.Now;
        foreach (PeriodicTask task in _tasksOnSecond)
            if (task.ShouldRun && task.LastRun + task.Repeat < executionTime)
            {
                task.CallBack(task.Action());
                task.LastRun = executionTime;
            }
    }

    private void OnMinuteTimer(object? _)
    {
        if (_disposedValue || _tasksOnMinute.IsEmpty) return;
        DateTime executionTime = DateTime.Now;
        foreach (PeriodicTask task in _tasksOnMinute)
            if (task.ShouldRun && task.LastRun + task.Repeat < executionTime)
            {
                task.CallBack(task.Action());
                task.LastRun = executionTime;
            }
    }

    internal void RegisterTask(HrtTask task)
    {
        if (task is PeriodicTask pTask)
        {
            if (pTask.Repeat.Minutes > 0)
                _tasksOnMinute.Add(pTask);
            else
                _tasksOnSecond.Add(pTask);
        }
        else
        {
            _tasksOnce.Enqueue(new TaskWrapper(task));
        }
    }

    protected virtual void Dispose(bool disposing)
    {
        if (!_disposedValue)
        {
            if (disposing)
            {
                _taskTimer.Dispose();
                _secondTimer.Dispose();
                _minuteTimer.Dispose();
                while (_tasksOnce.TryDequeue(out TaskWrapper? task))
                    if (task.SystemTask.Status > TaskStatus.Created)
                        task.SystemTask.Wait(1000);
            }

            _disposedValue = true;
        }
    }

    // // TODO: override finalizer only if 'Dispose(bool disposing)' has code to free unmanaged resources
    // ~TaskManager()
    // {
    //     // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
    //     Dispose(disposing: false);
    // }

    public void Dispose()
    {
        // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
        Dispose(true);
        GC.SuppressFinalize(this);
    }
}

internal class HrtTask
{
    public readonly Action<HrtUiMessage> CallBack;
    public readonly Func<HrtUiMessage> Action;
    public readonly string Name;

    public HrtTask(Func<HrtUiMessage> task, Action<HrtUiMessage> callBack, string name)
    {
        CallBack = callBack;
        Action = task;
        Name = name;
    }
}

internal class PeriodicTask : HrtTask
{
    public DateTime LastRun = DateTime.MinValue;
    public volatile bool ShouldRun = true;
    public TimeSpan Repeat;

    public PeriodicTask(Func<HrtUiMessage> task, Action<HrtUiMessage> callBack, string name, TimeSpan repeat) : base(
        task, callBack, name)
    {
        Repeat = repeat;
    }
}