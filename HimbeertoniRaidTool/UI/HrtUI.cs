using System;
using System.Numerics;
using Dalamud.Interface.Windowing;
using ImGuiNET;
using Loc = HimbeertoniRaidTool.Plugin.HrtServices.Localization;

namespace HimbeertoniRaidTool.Plugin.UI;

public abstract class HRTWindowWithModalChild : HrtWindow
{
    private HrtWindow? _modalChild;
    protected HrtWindow? ModalChild
    {
        get => _modalChild;
        set
        {
            _modalChild = value;
            _modalChild?.Show();
        }
    }
    protected bool ChildIsOpen => _modalChild != null && _modalChild.IsOpen;
    public override void Update()
    {
        if (ModalChild != null && !ModalChild.IsOpen)
        {
            ModalChild = null;
        }
        base.Update();
    }
    public override void PostDraw()
    {
        if (ModalChild == null)
            return;
        bool Open = ModalChild.IsOpen;
        if (ImGui.Begin(ModalChild.WindowName, ref Open, ModalChild.Flags))
        {
            ModalChild.Draw();
            ImGui.End();
        }
        if (!Open)
            ModalChild.IsOpen = Open;
    }
}
public abstract class HrtWindow : Window, IEquatable<HrtWindow>
{
    private readonly bool _volatile;
    private readonly string _id;
    protected string Title;
    protected Vector2 MinSize = default;
    protected Vector2 MaxSize = ImGui.GetIO().DisplaySize * 0.9f;
    protected bool OpenCentered = false;
    public static float ScaleFactor => ImGui.GetIO().FontGlobalScale;
    public HrtWindow(bool @volatile = true, string? id = null) : base(id ?? Guid.NewGuid().ToString())
    {
        _id = WindowName;
        Title = "";
        _volatile = @volatile;
    }
    public void Show()
    {
        IsOpen = true;
    }
    public void Hide()
    {
        IsOpen = false;
    }
    public override void Update()
    {
        WindowName = $"{Title}##{_id}";
        SizeConstraints = new()
        {
            MinimumSize = MinSize,
            MaximumSize = MaxSize
        };
    }
    public override bool DrawConditions()
    {
        return !(Services.Config.HideInBattle && Services.Condition[Dalamud.Game.ClientState.Conditions.ConditionFlag.InCombat])
            && !(Services.Config.HideOnZoneChange && Services.Condition[Dalamud.Game.ClientState.Conditions.ConditionFlag.BetweenAreas])
            && base.DrawConditions();
    }
    public override void PreDraw()
    {
        if (OpenCentered)
        {
            Position = (ImGui.GetIO().DisplaySize - Size) / 2;
            PositionCondition = ImGuiCond.Appearing;
            OpenCentered = false;

        }
    }
    public override bool Equals(object? obj) => Equals(obj as HrtWindow);
    public bool Equals(HrtWindow? other) => _id.Equals(other?._id);

    public override int GetHashCode() => _id.GetHashCode();
}
public class ConfimationDialog : HrtWindow
{
    private readonly Action _Action;
    private readonly string _Title;
    private readonly string _Text;
    private bool Visible = true;

    public ConfimationDialog(Action action, string text, string title = "") : base(true)
    {
        title = title.Equals("") ? Loc.Localize("Confirmation", "Confirmation") : title;
        _Text = text;
        _Title = title;
        _Action = action;
        Show();
    }
    public override void Draw()
    {
        ImGui.OpenPopup(_Title);
        ImGui.SetNextWindowPos(
            new Vector2(ImGui.GetMainViewport().Size.X * 0.5f, ImGui.GetMainViewport().Size.Y * 0.5f),
            ImGuiCond.Appearing, new Vector2(0.5f, 0.5f));

        if (ImGui.BeginPopupModal(_Title, ref Visible, ImGuiWindowFlags.AlwaysAutoResize | ImGuiWindowFlags.NoCollapse | ImGuiWindowFlags.NoScrollbar))
        {
            ImGui.Text(_Text);
            if (ImGuiHelper.Button(Loc.Localize("OK", "OK"), Loc.Localize("Confirm action", "OK")))
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
    public HrtUiMessage(string msg, HrtUiMessageType msgType)
    {
        MessageType = msgType;
        Message = msg;
    }
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
