using System.Numerics;
using Dalamud.Plugin;
using Dalamud.Plugin.Services;
using Dalamud.Utility;
using ImGuiScene;
using Lumina.Data.Files;

namespace HimbeertoniRaidTool.Plugin.Services;

//Inspired by InventoryTools by Critical-Impact 
public class IconCache : IDisposable
{
    private readonly DalamudPluginInterface _pi;
    private readonly IDataManager _gameData;
    private readonly Dictionary<uint, TextureWrap> _icons;

    public IconCache(DalamudPluginInterface pi, IDataManager gameData, int size = 0)
    {
        _pi = pi;
        _gameData = gameData;
        _icons = new Dictionary<uint, TextureWrap>(size);
    }

    public TextureWrap this[uint id]
        => LoadIcon(id);

    public TextureWrap LoadIcon(int id)
    {
        return LoadIcon((uint)id);
    }

    public TextureWrap LoadIcon(uint id, bool hq = false)
    {
        if (_icons.TryGetValue(id, out TextureWrap? ret))
            return ret;

        TexFile icon = _gameData.GetIcon(hq, id)!;
        byte[] iconData = icon.GetRgbaImageData();

        ret = _pi.UiBuilder.LoadImageRaw(iconData, icon.Header.Width, icon.Header.Height, 4);
        _icons[id] = ret;
        return ret;
    }

    public void Dispose()
    {
        foreach (TextureWrap icon in _icons.Values)
            icon.Dispose();
        _icons.Clear();
    }

    ~IconCache()
    {
        Dispose();
    }
}

public static class IconExtensions
{
    public static Vector2 Size(this TextureWrap tex)
    {
        return new Vector2(tex.Width, tex.Height);
    }
}