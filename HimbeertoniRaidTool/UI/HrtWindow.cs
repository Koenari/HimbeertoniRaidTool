using System.Numerics;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Windowing;

namespace HimbeertoniRaidTool.Plugin.UI;

public abstract class HrtWindowWithModalChild(
    IUiSystem uiSystem,
    string? id = null,
    ImGuiWindowFlags flags = ImGuiWindowFlags.None)
    : HrtWindow(uiSystem, id, flags)
{
    protected HrtWindow? ModalChild
    {
        get;
        set
        {
            field = value;
            field?.Show();
        }
    }
    public bool ChildIsOpen => ModalChild is { IsOpen: true };
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
        }
        ImGui.End();
        if (!open)
            ModalChild.IsOpen = open;
    }
    public void AddChild(HrtWindow? child) => ModalChild ??= child;
}

public abstract class HrtWindow : Window, IEquatable<HrtWindow>
{
    private readonly string _id;
    private bool _hasResizedLastFrame;
    private Vector2 _newSize;
    private ImGuiCond _savedSizingCond = ImGuiCond.None;
    private bool _shouldResize;
    protected Vector2 MaxSize = ImGui.GetIO().DisplaySize * 0.9f;
    protected Vector2 MinSize = default;
    protected bool OpenCentered;
    protected string Title = "";
    public bool Persistent { get; protected init; }
    protected IUiSystem UiSystem { get; }

    protected HrtWindow(IUiSystem uiSystem, string? id = null, ImGuiWindowFlags flags = ImGuiWindowFlags.None) : base(
        id ?? $"##{Guid.NewGuid()}", flags)
    {
        UiSystem = uiSystem;
        _id = WindowName;
        IsOpen = true;
    }
    public static float ScaleFactor => ImGui.GetIO().FontGlobalScale;
    public bool Equals(HrtWindow? other) => _id.Equals(other?._id);
    public void Show() => IsOpen = true;
    // ReSharper disable once MemberCanBeProtected.Global
    public void Hide() => IsOpen = false;
    public override void Update()
    {
        WindowName = $"{Title}##{_id}";
        SizeConstraints = new WindowSizeConstraints
        {
            MinimumSize = MinSize,
            MaximumSize = MaxSize,
        };
    }
    public override bool DrawConditions() => UiSystem.DrawConditionsMet() && base.DrawConditions();
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
        }

    }
    protected void Resize(Vector2 newSize)
    {
        _newSize = newSize;
        _shouldResize = true;
    }

    public static void Dispose() { }

    public override bool Equals(object? obj) => Equals(obj as HrtWindow);

    public override int GetHashCode() => _id.GetHashCode();
}

public readonly struct HrtUiMessage(string msg, HrtUiMessageType msgType = HrtUiMessageType.Info)
{
    public readonly string Message = msg;
    public readonly HrtUiMessageType MessageType = msgType;
    public static HrtUiMessage Empty => new("", HrtUiMessageType.Discard);
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