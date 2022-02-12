using ImGuiNET;
using System;
using System.Numerics;

namespace HimbeertoniRaidTool
{
    // It is good to have this be disposable in general, in case you ever need it
    // to do any cleanup
    public abstract class HrtUI : IDisposable
    {
        protected HRTPlugin Parent;

        internal bool visible = false;

        public HrtUI(HRTPlugin parent)
        {
            this.Parent = parent;
            this.Parent.PluginInterface.UiBuilder.Draw += this.Draw;
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
            this.Parent.PluginInterface.UiBuilder.Draw -= this.Draw;
        }

        public abstract void Draw();
    }
}
