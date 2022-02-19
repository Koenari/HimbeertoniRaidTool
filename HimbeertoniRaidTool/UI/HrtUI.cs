using ImGuiNET;
using System;
using System.Numerics;

namespace HimbeertoniRaidTool.UI
{
    // It is good to have this be disposable in general, in case you ever need it
    // to do any cleanup
    public abstract class HrtUI : IDisposable
    {
        protected bool visible = false;

        public HrtUI()
        {
            Services.PluginInterface.UiBuilder.Draw += this.Draw;
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
            Services.PluginInterface.UiBuilder.Draw -= this.Draw;
        }

        public abstract void Draw();
    }
}
