using ImGuiNET;
using System;
using System.Numerics;

namespace HimbeertoniRaidTool
{
    // It is good to have this be disposable in general, in case you ever need it
    // to do any cleanup
    public abstract class HrtUI : IDisposable
    {
        [Obsolete("Use HRTPlugin.Plugin")]
        protected HRTPlugin Parent => HRTPlugin.Plugin;
        protected bool visible = false;

        public HrtUI()
        {
            HRTPlugin.Plugin.PluginInterface.UiBuilder.Draw += this.Draw;
        }

        public virtual void Show()
        {
            this.visible = true;
        }

        public virtual void Hide()
        {
            this.visible = false;
        }

        public virtual void Dispose()
        {
            HRTPlugin.Plugin.PluginInterface.UiBuilder.Draw -= this.Draw;
        }

        public abstract void Draw();
    }
}
