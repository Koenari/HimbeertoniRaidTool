using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using Dalamud.Plugin.Services;
using HimbeertoniRaidTool.Plugin.UI;

namespace HimbeertoniRaidTool.Plugin.Services;

internal class TaskManager : IDisposable
{
    private readonly IFramework _framework;

    private interface ITaskWrapper
    {
        public Task SystemTask { get; }
        public string Name { get; }
        public bool HasError { get; }
        public string ErrorMsg { get; }
    }

    private class TaskWrapper<TData>(HrtTask<TData> task) : ITaskWrapper
    {
        public Task SystemTask { get; } = Task.Run(() =>
        {
            task.Result = task.Action();
            task.CallBack(task.Result);
        });
        public string Name => task.Name;
        public bool HasError => task.Result is HrtUiMessage
        {
            MessageType: HrtUiMessageType.Failure or HrtUiMessageType.Error,
        };
        public string ErrorMsg => task.Result is HrtUiMessage msg ? msg.Message : string.Empty;

    }

    private readonly Timer _taskTimer;
    private readonly Timer _secondTimer;
    private readonly Timer _minuteTimer;

    private readonly ConcurrentQueue<ITaskWrapper> _tasksOnce = new();
    private readonly ConcurrentBag<PeriodicTask> _tasksOnSecond = new();
    private readonly ConcurrentBag<PeriodicTask> _tasksOnMinute = new();
    private bool _disposedValue;

    internal TaskManager(IFramework framework)
    {
        _framework = framework;
        _secondTimer = new Timer(OnSecondTimer, null, 1000, 1000);
        _taskTimer = new Timer(OnTaskTimer, null, 1000, 1000);
        _minuteTimer = new Timer(OnMinuteTimer, null, 1000, 60 * 1000);
    }

    private void OnTaskTimer(object? _)
    {
        if (_disposedValue || _tasksOnce.IsEmpty) return;
        while (_tasksOnce.TryPeek(out var oldestTask) && oldestTask.SystemTask.IsCompleted)
        {
            if (_tasksOnce.TryDequeue(out var completedTask))
            {
                if (completedTask.SystemTask.IsFaulted)
                    ServiceManager.Logger.Error(completedTask.SystemTask.Exception,
                                                $"Task \"{completedTask.Name}\" finished with an error");
                else if (completedTask.HasError)
                    ServiceManager.Logger.Error(
                        $"Task \"{completedTask.Name}\" finished with an error: {completedTask.ErrorMsg}");
                else
                    ServiceManager.Logger.Info(
                        $"Task \"{completedTask.Name}\" finished successful");
            }
        }
    }

    private void OnSecondTimer(object? _)
    {
        if (_disposedValue || _tasksOnSecond.IsEmpty) return;
        var executionTime = DateTime.Now;
        foreach (var task in _tasksOnSecond)
        {
            if (task.ShouldRun && task.LastRun + task.Repeat < executionTime)
            {
                ServiceManager.Logger.Info($"Starting task: {task.Name}");
                task.CallBack(task.Action());
                task.LastRun = executionTime;
            }
        }
    }

    private void OnMinuteTimer(object? _)
    {
        if (_disposedValue || _tasksOnMinute.IsEmpty) return;
        var executionTime = DateTime.Now;
        foreach (var task in _tasksOnMinute)
        {
            if (task.ShouldRun && task.LastRun + task.Repeat < executionTime)
            {
                ServiceManager.Logger.Info($"Starting task: {task.Name}");
                task.CallBack(task.Action());
                task.LastRun = executionTime;
            }
        }
    }

    internal void RunOnFrameworkThread(Action action) => _framework.RunOnFrameworkThread(action);

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
            ServiceManager.Logger.Info($"Starting task: {task.Name}");
            _tasksOnce.Enqueue(new TaskWrapper<TData>(task));
        }
    }

    private void Dispose(bool disposing)
    {
        if (!_disposedValue)
        {
            if (disposing)
            {
                _taskTimer.Dispose();
                _secondTimer.Dispose();
                _minuteTimer.Dispose();
                while (_tasksOnce.TryDequeue(out var task))
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
    public TReturn? Result;
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