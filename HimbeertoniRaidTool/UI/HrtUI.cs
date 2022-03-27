using ImGuiNET;
using System;
using System.Numerics;
using static Dalamud.Localization;

namespace HimbeertoniRaidTool.UI
{
    // It is good to have this be disposable in general, in case you ever need it
    // to do any cleanup
    public abstract class HrtUI : IDisposable
    {
        protected bool Visible = false;
        public bool IsVisible => Visible;

        public HrtUI() => Services.PluginInterface.UiBuilder.Draw += Draw;

        public virtual void Show() => Visible = true;

        public virtual void Hide() => Visible = false;

        public virtual void Dispose() => Services.PluginInterface.UiBuilder.Draw -= Draw;

        public abstract void Draw();
    }
    public class ConfimationDialog : HrtUI
    {
        private readonly Action _Action;
        private readonly string _Title;
        private readonly string _Text;

        public ConfimationDialog(Action action, string text, string title = "")
        {
            title = title.Equals("") ? Localize("Confirmation", "Confirmation") : title;
            _Text = text;
            _Title = title;
            _Action = action;
            Show();
        }
        public override void Draw()
        {
            if (!Visible)
                return;
            if (Visible)
                ImGui.OpenPopup(_Title);
            ImGui.SetNextWindowPos(
                new Vector2(ImGui.GetMainViewport().Size.X * 0.5f, ImGui.GetMainViewport().Size.Y * 0.5f),
                ImGuiCond.Appearing, new Vector2(0.5f, 0.5f));

            if (ImGui.BeginPopupModal(_Title, ref Visible, ImGuiWindowFlags.AlwaysAutoResize | ImGuiWindowFlags.NoCollapse | ImGuiWindowFlags.NoScrollbar))
            {
                ImGui.Text(_Text);
                if (ImGui.Button(Localize("OK", "OK")))
                {
                    _Action();
                    Hide();
                }
                ImGui.SameLine();
                if (ImGui.Button(Localize("Cancel", "Cancel")))
                    Hide();
                ImGui.End();
            }
        }
    }
}
