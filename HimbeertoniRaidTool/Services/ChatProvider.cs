using Dalamud.Game.Text;
using Dalamud.Game.Text.SeStringHandling;
using Dalamud.Plugin.Services;

namespace HimbeertoniRaidTool.Plugin.Services;

public interface IChatProvider
{
    void Print(XivChatEntry chat);
    void Print(string message, string? messageTag = null, ushort? tagColor = null);
    void Print(SeString message, string? messageTag = null, ushort? tagColor = null);
    void PrintError(string message, string? messageTag = null, ushort? tagColor = null);
    void PrintError(SeString message, string? messageTag = null, ushort? tagColor = null);
}

internal class DalamudChatProxy : IChatProvider
{
    private readonly IChatGui _impl;

    public DalamudChatProxy(IChatGui impl)
    {
        _impl = impl;
    }


    public void Print(XivChatEntry chat) => _impl.Print(chat);
    public void Print(string message, string? messageTag = null, ushort? tagColor = null) =>
        _impl.Print(message, messageTag, tagColor);
    public void Print(SeString message, string? messageTag = null, ushort? tagColor = null) =>
        _impl.Print(message, messageTag, tagColor);
    public void PrintError(string message, string? messageTag = null, ushort? tagColor = null) =>
        _impl.PrintError(message, messageTag, tagColor);
    public void PrintError(SeString message, string? messageTag = null, ushort? tagColor = null) =>
        _impl.PrintError(message, messageTag, tagColor);
}