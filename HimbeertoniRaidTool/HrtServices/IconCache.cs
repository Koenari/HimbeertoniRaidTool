using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Dalamud.Data;
using Dalamud.Plugin;
using Dalamud.Utility;
using ImGuiScene;
using Lumina.Data.Files;

namespace HimbeertoniRaidTool.HrtServices
{
    //Inspired by InventoryTools by Critical-Impact 
    public class IconCache : IDisposable
    {
        private readonly DalamudPluginInterface _pi;
        private readonly DataManager _gameData;
        private readonly Dictionary<uint, TextureWrap> _icons;
        public IconCache(DalamudPluginInterface pi, DataManager gameData, int size = 0)
        {
            _pi = pi;
            _gameData = gameData;
            _icons = new Dictionary<uint, TextureWrap>(size);
        }
        public TextureWrap this[uint id]
        => LoadIcon(id);
        public TextureWrap LoadIcon(int id)
        => LoadIcon((uint)id);
        public TextureWrap LoadIcon(uint id, bool hq = false)
        {
            if (_icons.TryGetValue(id, out var ret))
                return ret;

            var icon = _gameData.GetIcon(hq, id)!;
            var iconData = icon.GetRgbaImageData();

            ret = _pi.UiBuilder.LoadImageRaw(iconData, icon.Header.Width, icon.Header.Height, 4);
            _icons[id] = ret;
            return ret;
        }
        private TexFile? LoadIconHq(uint id)
        {
            var path = $"ui/icon/{id / 1000 * 1000:000000}/{id:000000}_hr1.tex";
            return _gameData.GetFile<TexFile>(path);
        }
        public void Dispose()
        {
            foreach (var icon in _icons.Values)
                icon.Dispose();
            _icons.Clear();
        }
        ~IconCache()
        => Dispose();
    }
}
