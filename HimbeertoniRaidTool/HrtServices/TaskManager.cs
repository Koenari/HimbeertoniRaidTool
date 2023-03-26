using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using HimbeertoniRaidTool.Plugin.UI;

namespace HimbeertoniRaidTool.Plugin.HrtServices;

internal class TaskManager : IDisposable
{
    private class TaskWrapper
    {
        public Task SystemTask;
        public HrtTask InternalTask;
        public TaskWrapper(HrtTask task)
        {
            InternalTask = task;
            SystemTask = Task.Run(() => task.CallBack(task.Action()));
        }
    }

    private readonly Timer TaskTimer;
    private readonly Timer SecondTimer;
    private readonly Timer MinuteTimer;

    private readonly ConcurrentQueue<TaskWrapper> TasksOnce = new();
    private readonly ConcurrentBag<PeriodicTask> TasksOnSecond = new();
    private readonly ConcurrentBag<PeriodicTask> TasksOnMinute = new();
    private bool disposedValue;

    internal TaskManager()
    {
        SecondTimer = new(OnSecondTimer, null, 1000, 1000);
        TaskTimer = new(OnTaskTimer, null, 1000, 1000);
        MinuteTimer = new(OnMinuteTimer, null, 1000, 60 * 1000);
    }
    private void OnTaskTimer(object? _)
    {
        if (disposedValue || TasksOnce.IsEmpty) return;
        while (TasksOnce.TryPeek(out var oldestTaks) && oldestTaks.SystemTask.IsCompleted)
            TasksOnce.TryDequeue(out var _);
    }
    private void OnSecondTimer(object? _)
    {
        if (disposedValue || TasksOnSecond.IsEmpty) return;
        var ExecutionTime = DateTime.Now;
        foreach (var task in TasksOnSecond)
        {
            if (task.ShouldRun && task.LastRun + task.Repeat < ExecutionTime)
            {
                task.CallBack(task.Action());
                task.LastRun = ExecutionTime;
            }
        }
    }
    private void OnMinuteTimer(object? _)
    {
        if (disposedValue || TasksOnMinute.IsEmpty) return;
        var ExecutionTime = DateTime.Now;
        foreach (var task in TasksOnMinute)
        {
            if (task.ShouldRun && task.LastRun + task.Repeat < ExecutionTime)
            {
                task.CallBack(task.Action());
                task.LastRun = ExecutionTime;
            }
        }
    }
    internal void RegisterTask(HrtTask task)
    {
        if (task is PeriodicTask pTask)
        {
            if (pTask.Repeat.Minutes > 0)
                TasksOnMinute.Add(pTask);
            else
                TasksOnSecond.Add(pTask);
        }
        else
        {
            TasksOnce.Enqueue(new(task));
        }
    }
    protected virtual void Dispose(bool disposing)
    {
        if (!disposedValue)
        {
            if (disposing)
            {
                TaskTimer.Dispose();
                SecondTimer.Dispose();
                MinuteTimer.Dispose();
                while (TasksOnce.TryDequeue(out var task))
                {
                    if (task.SystemTask.Status > TaskStatus.Created)
                    {
                        task.SystemTask.Wait(1000);
                    }
                }
            }
            disposedValue = true;
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
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }
}

internal class HrtTask
{
    public readonly Action<HrtUiMessage> CallBack;
    public readonly Func<HrtUiMessage> Action;
    public HrtTask(Func<HrtUiMessage> task, Action<HrtUiMessage> callBack)
    {
        CallBack = callBack;
        Action = task;
    }
}
internal class PeriodicTask : HrtTask
{
    public DateTime LastRun = DateTime.MinValue;
    public bool ShouldRun = true;
    public TimeSpan Repeat;
    public PeriodicTask(Func<HrtUiMessage> task, Action<HrtUiMessage> callBack, TimeSpan repeat) : base(task, callBack)
    {
        Repeat = repeat;
    }
}
