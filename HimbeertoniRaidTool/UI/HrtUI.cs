using System.Numerics;
using Dalamud.Interface.Windowing;
using ImGuiNET;
using Loc = HimbeertoniRaidTool.Plugin.Services.Localization;

namespace HimbeertoniRaidTool.Plugin.UI;

public abstract class HrtWindowWithModalChild : HrtWindow
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
    public bool ChildIsOpen => _modalChild != null && _modalChild.IsOpen;
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
        bool open = ModalChild.IsOpen;
        if (ImGui.Begin(ModalChild.WindowName, ref open, ModalChild.Flags))
        {
            ModalChild.Draw();
            ImGui.End();
        }
        if (!open)
            ModalChild.IsOpen = open;
    }
    public bool AddChild(HrtWindow child)
    {
        if (_modalChild != null)
            return false;
        ModalChild = child;
        return true;
    }
}

public abstract class HrtWindow : Window, IEquatable<HrtWindow>
{
    private readonly string _id;
    protected string Title;
    protected Vector2 MinSize = default;
    protected Vector2 MaxSize = ImGui.GetIO().DisplaySize * 0.9f;
    protected bool OpenCentered = false;
    public static float ScaleFactor => ImGui.GetIO().FontGlobalScale;

    protected HrtWindow(string? id = null) : base(id ?? Guid.NewGuid().ToString())
    {
        _id = WindowName;
        Title = "";
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
        SizeConstraints = new WindowSizeConstraints
        {
            MinimumSize = MinSize,
            MaximumSize = MaxSize,
        };
    }
    public override bool DrawConditions() => !(ServiceManager.CoreModule.Configuration.Data.HideInCombat && ServiceManager.Condition[Dalamud.Game.ClientState.Conditions.ConditionFlag.InCombat])
                                             && !ServiceManager.Condition[Dalamud.Game.ClientState.Conditions.ConditionFlag.BetweenAreas]
                                             && base.DrawConditions();
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
    private readonly Action _action;
    private readonly string _title;
    private readonly string _text;
    private bool _visible = true;

    public ConfimationDialog(Action action, string text, string title = "") : base()
    {
        title = title.Equals("") ? Loc.Localize("Confirmation", "Confirmation") : title;
        _text = text;
        _title = title;
        _action = action;
        Show();
    }
    public override void Draw()
    {
        ImGui.OpenPopup(_title);
        ImGui.SetNextWindowPos(
            new Vector2(ImGui.GetMainViewport().Size.X * 0.5f, ImGui.GetMainViewport().Size.Y * 0.5f),
            ImGuiCond.Appearing, new Vector2(0.5f, 0.5f));

        if (ImGui.BeginPopupModal(_title, ref _visible, ImGuiWindowFlags.AlwaysAutoResize | ImGuiWindowFlags.NoCollapse | ImGuiWindowFlags.NoScrollbar))
        {
            ImGui.Text(_text);
            if (ImGuiHelper.Button(Loc.Localize("OK", "OK"), Loc.Localize("Confirm action", "OK")))
            {
                _action();
                Hide();
            }
            ImGui.SameLine();
            if (ImGuiHelper.CancelButton())
                Hide();
            ImGui.End();
        }
    }
}

public class HrtUiMessage
{
    public static HrtUiMessage Empty => new("", HrtUiMessageType.Discard);
    public HrtUiMessageType MessageType;
    public string Message;
    public HrtUiMessage(string msg, HrtUiMessageType msgType = HrtUiMessageType.Info)
    {
        MessageType = msgType;
        Message = msg;
    }
}

public enum HrtUiMessageType
{
    Discard,
    Info,
    Success,
    Failure,
    Error,
    Important,
    Warning,
}