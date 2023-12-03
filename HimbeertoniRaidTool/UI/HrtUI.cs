using System.Numerics;
using Dalamud.Interface.Windowing;
using ImGuiNET;

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
    public bool ChildIsOpen => _modalChild is { IsOpen: true };
    public override void Update()
    {
        if (ModalChild is { IsOpen: false })
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
    private bool _shouldResize = false;
    private bool _hasResizedLastFrame = false;
    private ImGuiCond _savedSizingCond = ImGuiCond.None;
    private Vector2 _newSize;
    public static float ScaleFactor => ImGui.GetIO().FontGlobalScale;

    protected HrtWindow(string? id = null) : base(id ?? $"##{Guid.NewGuid()}")
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
    public override bool DrawConditions() => !(ServiceManager.CoreModule.HideInCombat && ServiceManager.Condition[Dalamud.Game.ClientState.Conditions.ConditionFlag.InCombat])
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
        if (_hasResizedLastFrame)
        {
            SizeCondition = _savedSizingCond;
            _hasResizedLastFrame = false;
        }
        if (_shouldResize)
        {
            Size = _newSize;
            _savedSizingCond = SizeCondition;
            SizeCondition = ImGuiCond.Always;
            _hasResizedLastFrame = true;
            _shouldResize = false;
            ServiceManager.PluginLog.Debug($"Tried Resizing to: {Size.Value.X}x{Size.Value.Y}");
        }

    }
    protected void Resize(Vector2 newSize)
    {
        _newSize = newSize;
        _shouldResize = true;
    }
    public override bool Equals(object? obj) => Equals(obj as HrtWindow);
    public bool Equals(HrtWindow? other) => _id.Equals(other?._id);

    public override int GetHashCode() => _id.GetHashCode();
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