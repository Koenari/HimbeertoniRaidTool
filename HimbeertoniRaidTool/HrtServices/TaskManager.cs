using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Dalamud.Game;
using Dalamud.Logging;
using HimbeertoniRaidTool.Plugin.Modules;
using HimbeertoniRaidTool.Plugin.UI;

namespace HimbeertoniRaidTool.Plugin.HrtServices;

internal class TaskManager : IDisposable
{
    private readonly List<(Action<HrtUiMessage> callBack, Task<HrtUiMessage> task)> _tasks = new();
    private bool disposedValue;

    internal TaskManager(Framework fw)
    {
        fw.Update += Update;
    }
    public void Update(Framework fw)
    {
        if (_tasks.Count == 0 || disposedValue)
            return;
        foreach (var (callBack, task) in _tasks.Where(t => t.task.IsCompleted))
            callBack(task.Result);
        _tasks.RemoveAll(t => t.task.IsCompleted);
    }
    internal void RegisterTask(IHrtModule hrtModule, Func<HrtUiMessage> task)
        => RegisterTask(hrtModule.HandleMessage, Task.Run(task));
    internal void RegisterTask(IHrtModule hrtModule, Task<HrtUiMessage> task)
        => RegisterTask(hrtModule.HandleMessage, task);
    internal void RegisterTask(Action<HrtUiMessage> callBack, Func<HrtUiMessage> task) =>
        RegisterTask(callBack, Task.Run(task));
    internal void RegisterTask(Action<HrtUiMessage> callBack, Task<HrtUiMessage> task)
    {
        _tasks.Add((callBack, task));
    }
    internal void RegisterTask(IHrtModule hrtModule, Func<bool> task, string success, string failure)
        => RegisterTask(hrtModule.HandleMessage, task, success, failure);
    internal void RegisterTask(IHrtModule hrtModule, Task<bool> task, string success, string failure)
        => RegisterTask(hrtModule.HandleMessage, task, success, failure);
    internal void RegisterTask(Action<HrtUiMessage> callBack, Func<bool> task, string success, string failure)
        => RegisterTask(callBack, Task.Run(task), success, failure);
    internal void RegisterTask(Action<HrtUiMessage> callBack, Task<bool> task, string success, string failure)
    {
        RegisterTask(callBack, task.ContinueWith(
            (r) => r.Result ? new HrtUiMessage() { MessageType = HrtUiMessageType.Success, Message = success }
                            : new HrtUiMessage() { MessageType = HrtUiMessageType.Failure, Message = failure })
            );
    }
    protected virtual void Dispose(bool disposing)
    {
        if (!disposedValue)
        {
            if (disposing)
            {
                Services.Framework.Update -= Update;
                _tasks.RemoveAll(t => t.task.Status < TaskStatus.Running);
                try
                {
                    _tasks.ForEach(t => t.task.Wait(1000));
                }
                catch (AggregateException e)
                {
                    PluginLog.Error($"Task failed: {e}");
                }
            }
            _tasks.Clear();
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
