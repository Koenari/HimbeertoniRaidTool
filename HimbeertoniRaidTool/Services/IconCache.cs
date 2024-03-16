using System.Numerics;
using Dalamud.Interface.Internal;
using Dalamud.Plugin;
using Dalamud.Plugin.Services;
using Dalamud.Utility;
using ImGuiScene;
using Lumina.Data.Files;

namespace HimbeertoniRaidTool.Plugin.Services;

//Inspired by InventoryTools by Critical-Impact 
public class IconCache(DalamudPluginInterface pi, IDataManager gameData, ITextureProvider textureProvider, int size = 0)
    : IDisposable
{
    private readonly Dictionary<uint, IDalamudTextureWrap> _icons = new(size);

    public IDalamudTextureWrap this[uint id] => LoadIcon(id);

    public IDalamudTextureWrap LoadIcon(int id) => LoadIcon((uint)id);

    public IDalamudTextureWrap LoadIcon(uint id, bool hq = false)
    {
        if (_icons.TryGetValue(id, out IDalamudTextureWrap? ret))
            return ret;

        var icon = gameData.GetFile<TexFile>(textureProvider.GetIconPath(id,
                                                                         ITextureProvider.IconFlags.HiRes
                                                                       | (hq ? ITextureProvider.IconFlags
                                                                             .ItemHighQuality : 0))!)!;
        byte[] iconData = icon.GetRgbaImageData();

        ret = pi.UiBuilder.LoadImageRaw(iconData, icon.Header.Width, icon.Header.Height, 4);
        _icons[id] = ret;
        return ret;
    }

    public void Dispose()
    {
        foreach (IDalamudTextureWrap icon in _icons.Values)
        {
            icon.Dispose();
        }
        _icons.Clear();
    }

    ~IconCache()
    {
        Dispose();
    }
}

public static class IconExtensions
{
    public static Vector2 Size(this TextureWrap tex) => new(tex.Width, tex.Height);
}