using ImGuiNET;
using System;
using System.Numerics;

namespace HimbeertoniRaidTool.UI
{
    // It is good to have this be disposable in general, in case you ever need it
    // to do any cleanup
    public abstract class HrtUI : IDisposable
    {
        protected bool Visible = false;
        public bool IsVisible => Visible;

        public HrtUI() => Services.PluginInterface.UiBuilder.Draw += this.Draw;

        public virtual void Show() => Visible = true;

        public virtual void Hide() => Visible = false;

        public virtual void Dispose() => Services.PluginInterface.UiBuilder.Draw -= this.Draw;

        public abstract void Draw();
    }
}
