using System;
using System.Collections.Generic;
using System.Numerics;
using Dalamud.Game;
using ImGuiNET;
using static Dalamud.Localization;

namespace HimbeertoniRaidTool.UI
{
    // It is good to have this be disposable in general, in case you ever need it
    // to do any cleanup
    public abstract class HrtUI : IDisposable
    {
        private bool _disposed = false;
        private bool _volatile;
        private bool _isChild = false;
        protected bool Visible = false;
        private readonly List<HrtUI> Children = new();

        public HrtUI(bool @volatile = true)
        {
            RegisterActions();
            _volatile = @volatile;
        }
        private void RegisterActions()
        {
            Services.PluginInterface.UiBuilder.Draw += InternalDraw;
            Services.Framework.Update += Update;
        }
        private void UnRegisterActions()
        {
            Services.PluginInterface.UiBuilder.Draw -= InternalDraw;
            Services.Framework.Update -= Update;
        }
        public void Show()
        {
            OnShow();
            Visible = true;
            Children.ForEach(x => x.Show());
        }
        protected virtual void OnShow() { }
        public void Hide()
        {
            Children.ForEach(x => x.Hide());
            Visible = false;
            OnHide();
            if (_volatile)
                Dispose();
        }
        protected virtual void OnHide() { }
        private void Update(Framework fw)
        {
            if (!Visible)
                Children.ForEach(x => x.Hide());
            if (!Visible && _volatile)
                Dispose();
            Children.RemoveAll(x => x._disposed);
        }
        protected void AddChild(HrtUI child)
        {
            child.SetUpAsChild();
            Children.Add(child);
        }
        protected bool ChildExists<T>(T c) => Children.Exists(x => c?.Equals(x) ?? false);
        private void SetUpAsChild()
        {
            _volatile = true;
            _isChild = true;
            UnRegisterActions();
        }
        public virtual void BeforeDispose() { }
        public void Dispose()
        {
            if (_disposed)
                return;
            BeforeDispose();
            Children.ForEach(c => c.Dispose());
            Children.Clear();
            if (!_isChild)
                UnRegisterActions();
            _disposed = true;
        }
        private void InternalDraw()
        {
            if (!Visible)
                return;
            Children.ForEach(_ => _.InternalDraw());
            Draw();
        }
        protected abstract void Draw();
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
        protected override void Draw()
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
