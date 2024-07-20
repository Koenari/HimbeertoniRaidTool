using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using HimbeertoniRaidTool.Plugin.UI;

namespace HimbeertoniRaidTool.Plugin.Services;

internal class TaskManager : IDisposable
{
    private interface ITaskWrapper
    {
        public Task SystemTask { get; }
        public string Name { get; }
    }

    private class TaskWrapper<TData>(HrtTask<TData> task) : ITaskWrapper
    {
        public Task SystemTask { get; } = Task.Run(() => task.CallBack(task.Action()));
        public string Name => InternalTask.Name;
        private readonly HrtTask<TData> InternalTask = task;

    }

    private readonly Timer _taskTimer;
    private readonly Timer _secondTimer;
    private readonly Timer _minuteTimer;

    private readonly ConcurrentQueue<ITaskWrapper> _tasksOnce = new();
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
        while (_tasksOnce.TryPeek(out ITaskWrapper? oldestTask) && oldestTask.SystemTask.IsCompleted)
        {
            if (_tasksOnce.TryDequeue(out ITaskWrapper? completedTask))
            {
                if (completedTask.SystemTask.IsFaulted)
                    ServiceManager.Logger.Error(
                        $"Task \"{completedTask.Name}\" finished with an error");
                else
                    ServiceManager.Logger.Info(
                        $"Task \"{completedTask.Name}\" finished successful");
            }
        }
    }

    private void OnSecondTimer(object? _)
    {
        if (_disposedValue || _tasksOnSecond.IsEmpty) return;
        DateTime executionTime = DateTime.Now;
        foreach (PeriodicTask task in _tasksOnSecond)
        {
            if (task.ShouldRun && task.LastRun + task.Repeat < executionTime)
            {
                task.CallBack(task.Action());
                task.LastRun = executionTime;
            }
        }
    }

    private void OnMinuteTimer(object? _)
    {
        if (_disposedValue || _tasksOnMinute.IsEmpty) return;
        DateTime executionTime = DateTime.Now;
        foreach (PeriodicTask task in _tasksOnMinute)
        {
            if (task.ShouldRun && task.LastRun + task.Repeat < executionTime)
            {
                task.CallBack(task.Action());
                task.LastRun = executionTime;
            }
        }
    }

    internal void RegisterTask<TData>(HrtTask<TData> task)
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
            _tasksOnce.Enqueue(new TaskWrapper<TData>(task));
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
                while (_tasksOnce.TryDequeue(out ITaskWrapper? task))
                {
                    if (task.SystemTask.Status > TaskStatus.Created)
                        task.SystemTask.Wait(1000);
                }
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

internal class HrtTask<TReturn>(Func<TReturn> task, Action<TReturn> callBack, string name)
{
    public readonly Action<TReturn> CallBack = callBack;
    public readonly Func<TReturn> Action = task;
    public readonly string Name = name;

}

internal class PeriodicTask(Func<HrtUiMessage> task, Action<HrtUiMessage> callBack, string name, TimeSpan repeat)
    : HrtTask<HrtUiMessage>(task, callBack, name)
{
    public DateTime LastRun = DateTime.MinValue;
    public volatile bool ShouldRun = true;
    public TimeSpan Repeat = repeat;

}