using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using Dalamud.Game;
using ImGuiNET;
using static HimbeertoniRaidTool.HrtServices.Localization;

namespace HimbeertoniRaidTool.UI
{
    public abstract class Window : IDisposable, IEquatable<Window>
    {
        private bool _disposed = false;
        private bool _volatile;
        private bool IsChild => Parent != null;
        protected virtual bool HideInBattle { get; } = false;
        protected bool Visible = false;
        private readonly string _id;
        protected Window? Parent { get; private set; }
        protected string Title;
        private Vector2 LastSize = default;
        protected Vector2 Size = default;
        private Vector2 ScaledSize = default;
        protected ImGuiCond SizingCondition = ImGuiCond.Appearing;
        protected ImGuiWindowFlags WindowFlags = ImGuiWindowFlags.None;
        private readonly List<Window> Children = new();
        public static float ScaleFactor => ImGui.GetIO().FontGlobalScale;
        public Window(bool @volatile = true, string? id = null)
        {
            _id = id ?? Guid.NewGuid().ToString();
            Title = "";
            _volatile = @volatile;
            RegisterActions();
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
            if (_disposed)
                return;
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
        }
        protected virtual void OnHide() { }
        protected virtual void OnUpdate() { }
        private void Update(Framework fw)
        {
            if (!Visible)
                Children.ForEach(x => x.Hide());
            if (!Visible && _volatile)
                Dispose();
            Children.ForEach(x => x.Update(fw));
            Children.RemoveAll(x => x._disposed);
            OnUpdate();
        }
        protected bool AddChild(Window child, bool showOnAdd = false)
        {
            if (!ChildExists(child))
            {
                child.SetUpAsChild(this);
                Children.Add(child);
                if (showOnAdd)
                    child.Show();
                return true;
            }
            else
            {
                child.Dispose();
                return false;
            }

        }
        protected bool ChildExists<T>([DisallowNull] T c) => Children.Exists(x => c.Equals(x));
        protected void ClearChildren() => Children.ForEach(x => x.Dispose());
        private void SetUpAsChild(Window parent)
        {
            _volatile = true;
            Parent = parent;
            UnRegisterActions();
            Show();
        }
        protected virtual void BeforeDispose() { }
        public void Dispose()
        {
            if (_disposed)
                return;
            Hide();
            BeforeDispose();
            Children.ForEach(c => c.Dispose());
            Children.Clear();
            if (!IsChild)
                UnRegisterActions();
            _disposed = true;
        }
        private void InternalDraw()
        {
            if (!Visible
                || _disposed
                || (Services.Config.HideInBattle && Services.Condition[Dalamud.Game.ClientState.Conditions.ConditionFlag.InCombat])
                || (Services.Config.HideOnZoneChange && Services.Condition[Dalamud.Game.ClientState.Conditions.ConditionFlag.BetweenAreas])
                )
                return;
            Children.ForEach(_ => _.InternalDraw());
            ScaledSize = Size * ScaleFactor;
            ImGui.SetNextWindowSize(ScaledSize, (LastSize != ScaledSize) ? ImGuiCond.Always : SizingCondition);
            if (ImGui.Begin($"{Title}##{_id}", ref Visible, WindowFlags))
            {
                ScaledSize = ImGui.GetWindowSize();
                Size = ScaledSize / ScaleFactor;
                LastSize = ScaledSize;
                Draw();
                ImGui.End();
            }

        }
        protected abstract void Draw();
        public override bool Equals(object? obj) => (obj is Window w) && Equals(w);
        public bool Equals(Window? other) => _id.Equals(other?._id);

        public override int GetHashCode() => _id.GetHashCode();
    }
    public class ConfimationDialog : Window
    {
        private readonly Action _Action;
        private readonly string _Title;
        private readonly string _Text;

        public ConfimationDialog(Action action, string text, string title = "") : base()
        {
            title = title.Equals("") ? Localize("Confirmation", "Confirmation") : title;
            _Text = text;
            _Title = title;
            _Action = action;
            Show();
        }
        protected override void Draw()
        {
            ImGui.OpenPopup(_Title);
            ImGui.SetNextWindowPos(
                new Vector2(ImGui.GetMainViewport().Size.X * 0.5f, ImGui.GetMainViewport().Size.Y * 0.5f),
                ImGuiCond.Appearing, new Vector2(0.5f, 0.5f));

            if (ImGui.BeginPopupModal(_Title, ref Visible, ImGuiWindowFlags.AlwaysAutoResize | ImGuiWindowFlags.NoCollapse | ImGuiWindowFlags.NoScrollbar))
            {
                ImGui.Text(_Text);
                if (ImGuiHelper.Button(Localize("OK", "OK"), Localize("Confirm action", "OK")))
                {
                    _Action();
                    Hide();
                }
                ImGui.SameLine();
                if (ImGuiHelper.CancelButton())
                    Hide();
                ImGui.End();
            }
        }
    }
    public struct HrtUiMessage
    {
        public HrtUiMessageType MessageType;
        public string Message;
    }
    public enum HrtUiMessageType
    {
        Info,
        Success,
        Failure,
        Error,
        Important,
        Warning
    }
}
