using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Dalamud.Game;
using HimbeertoniRaidTool.UI;

namespace HimbeertoniRaidTool.HrtServices
{
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
            {
                callBack(task.Result);
                task.Dispose();
            }
            _tasks.RemoveAll(t => t.task.IsCompleted);
            foreach (var task in _tasks.Where(t => t.task.Status == TaskStatus.Created))
            {
                task.task.Start();
            }
        }
        internal void RegisterTask<T, S>(IHrtModule<T, S> hrtModule, Func<HrtUiMessage> task) where T : new() where S : IHrtConfigUi
            => RegisterTask(hrtModule.HandleMessage, new Task<HrtUiMessage>(task));
        internal void RegisterTask<T, S>(IHrtModule<T, S> hrtModule, Task<HrtUiMessage> task) where T : new() where S : IHrtConfigUi
            => RegisterTask(hrtModule.HandleMessage, task);
        internal void RegisterTask(Action<HrtUiMessage> callBack, Func<HrtUiMessage> task) =>
            RegisterTask(callBack, new Task<HrtUiMessage>(task.Invoke));
        internal void RegisterTask(Action<HrtUiMessage> callBack, Task<HrtUiMessage> task)
        {
            _tasks.Add((callBack, task));
        }
        internal void RegisterTask<T, S>(IHrtModule<T, S> hrtModule, Func<bool> task, string success, string failure) where T : new() where S : IHrtConfigUi
            => RegisterTask(hrtModule.HandleMessage, new Task<bool>(task), success, failure);
        internal void RegisterTask<T, S>(IHrtModule<T, S> hrtModule, Task<bool> task, string success, string failure) where T : new() where S : IHrtConfigUi
            => RegisterTask(hrtModule.HandleMessage, task, success, failure);
        internal void RegisterTask(Action<HrtUiMessage> callBack, Func<bool> task, string success, string failure)
            => RegisterTask(callBack, new Task<bool>(task), success, failure);
        internal void RegisterTask(Action<HrtUiMessage> callBack, Task<bool> task, string success, string failure)
        {
            RegisterTask(callBack, MapReturn(task,
                (r) => r ? new HrtUiMessage() { MessageType = HrtUiMessageType.Success, Message = success }
                                : new HrtUiMessage() { MessageType = HrtUiMessageType.Failure, Message = failure })
                );
        }
        private static async Task<S> MapReturn<T, S>(Task<T> input, Func<T, S> mapper)
        {
            T result = await input;
            return mapper(result);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    _tasks.ForEach(t => t.task.Wait(1000));
                }
                _tasks.ForEach(t => t.task.Dispose());
                _tasks.Clear();
                // TODO: free unmanaged resources (unmanaged objects) and override finalizer
                // TODO: set large fields to null
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
}
