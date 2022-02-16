using HimbeertoniRaidTool.Data;
using ImGuiNET;
using System.Collections.Generic;
using System.Numerics;

namespace HimbeertoniRaidTool
{
    class ConfigUI : HrtUI
    {
        public ConfigUI(HRTPlugin parent) : base()
        {
            HRTPlugin.Plugin.PluginInterface.UiBuilder.OpenConfigUi += this.Show;
        }
        public override void Dispose()
        {
        }

        public override void Draw()
        {
            if (!visible)
            {
                return;
            }

            ImGui.SetNextWindowSize(new Vector2(350, 200), ImGuiCond.Always);
            if (ImGui.Begin("A Wonderful Configuration Window", ref this.visible,
                ImGuiWindowFlags.NoResize | ImGuiWindowFlags.NoCollapse))
            {
                foreach(KeyValuePair<AvailableClasses,string> pair in HRTPlugin.Plugin.Configuration.DefaultBIS)
                {
                    string value = pair.Value;
                    if(ImGui.InputText(pair.Key.ToString(), ref value, 100))
                    {
                        HRTPlugin.Plugin.Configuration.DefaultBIS[pair.Key] = value;
                    }
                }
                if (ImGui.Button("Save##Config"))
                {
                    HRTPlugin.Plugin.Configuration.Save();
                    this.Hide();
                }
            }
            ImGui.End();
        }
    }
}
