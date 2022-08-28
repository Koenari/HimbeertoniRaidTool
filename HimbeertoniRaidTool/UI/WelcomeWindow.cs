using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using ImGuiNET;
using static Dalamud.Localization;

namespace HimbeertoniRaidTool.UI
{
    internal class WelcomeWindow : HrtUI
    {
        protected override void Draw()
        {
            ImGui.SetNextWindowSize(new Vector2(600, 600), ImGuiCond.Appearing);
            if (ImGui.Begin(Localize("Welcome to HRT", "Welcome to Himbeertoni Raid Tool"), ref Visible,
                ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoCollapse))
            {
                ImGui.End();
            }
        }
        protected override void OnHide()
        {
            //HRTPlugin.Configuration.FirstStartup = false;
        }
    }
}
