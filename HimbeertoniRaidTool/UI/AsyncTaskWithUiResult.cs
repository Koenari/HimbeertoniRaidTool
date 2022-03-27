using System;
using System.Threading.Tasks;

namespace HimbeertoniRaidTool.UI
{
    public class AsyncTaskWithUiResult : IDisposable
    {
        private Task _Task;
        public Task Task
        {
            private get => _Task;
            set
            {
                if (_Task is null || Task.Status == TaskStatus.Created)
                    _Task = value;
            }
        }
        public TimeSpan TimeToShow = TimeSpan.FromSeconds(10);
        private DateTime? StartedShowingMessage;
        public TaskStatus Status => _Task.Status;
        public bool FinishedShowing { get; private set; } = false;
        private Action<Task> _Action;
        public Action<Task> Action
        {
            get => _Action;
            set
            {
                if (Task is null || Task.Status == TaskStatus.Created)
                    _Action = value;
            }
        }
        public AsyncTaskWithUiResult()
        {
            _Action = (t) => { };
            _Task = new(() => { });
        }
        public AsyncTaskWithUiResult(Action<Task> action, Task task)
        {
            _Action = action;
            _Task = task;
        }
        public void DrawResult()
        {
            if (Task is null || !Task.IsCompleted)
                return;
            StartedShowingMessage ??= DateTime.Now;
            Action.Invoke(Task);
            FinishedShowing = DateTime.Now > StartedShowingMessage + TimeToShow;

        }

        public void Dispose()
        {
            if (Task is not null && Task.Status != TaskStatus.Created)
            {
                Task.Wait(1000);
                Task.Dispose();
            }
        }
    }
}
