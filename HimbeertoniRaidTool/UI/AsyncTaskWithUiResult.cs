using ImGuiNET;
using System;
using System.Threading.Tasks;

namespace HimbeertoniRaidTool.UI
{
    public class AsyncTaskWithUiResult : IDisposable
    {
        private readonly Task Task;
        public TimeSpan TimeToShow = TimeSpan.FromSeconds(10);
        private DateTime? StartedShowingMessage;

        public bool FinishedShowing { get; private set; } = false;
        private readonly Action<Task> Action;

        internal AsyncTaskWithUiResult(Action<Task> action, Task task)
        {
            Action = action;
            Task = task;
        }
        public void DrawResult()
        {
            if (!Task.IsCompleted)
                return;
            //TODO: This is not working!
            bool InsideWindow = ImGui.GetWindowWidth() > 0;
            if (!InsideWindow)
            {
                if (ImGui.BeginPopupModal("TaskFinished"))
                {
                    Action.Invoke(Task);
                    if (ImGui.Button("OK"))
                    {
                        FinishedShowing = true;
                        ImGui.CloseCurrentPopup();
                    }
                    ImGui.EndPopup();
                }
                ImGui.OpenPopup("TaskFinished");
            }
            else
            {
                StartedShowingMessage ??= DateTime.Now;
                Action.Invoke(Task);
                FinishedShowing = DateTime.Now > StartedShowingMessage + TimeToShow;
            }
        }

        public void Dispose()
        {
            Task.Wait();
            Task.Dispose();
        }
    }
}
