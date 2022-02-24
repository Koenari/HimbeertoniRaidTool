using HimbeertoniRaidTool.Data;
using ImGuiNET;
using System;
using System.Linq;
using System.Collections.Generic;
using System.Numerics;

namespace HimbeertoniRaidTool.UI
{
    class ConfigUI : HrtUI
    {
        private UiSortableList<LootRule> LootList;
        public ConfigUI() : base()
        {
            Services.PluginInterface.UiBuilder.OpenConfigUi += Show;
            LootList = new(LootRuling.PossibleRules, HRTPlugin.Configuration.LootRuling.RuleSet.Cast<LootRule>());
        }
        public override void Dispose()
        {
            Services.PluginInterface.UiBuilder.OpenConfigUi -= Show;
        }
        public override void Show()
        {
            base.Show();
            LootList = new(LootRuling.PossibleRules, HRTPlugin.Configuration.LootRuling.RuleSet);
        }
        public override void Draw()
        {
            if (!Visible)
            {
                return;
            }

            ImGui.SetNextWindowSize(new Vector2(450, 500), ImGuiCond.Always);
            if (ImGui.Begin("HimbeerToni Raid Tool Configuration", ref Visible,
                ImGuiWindowFlags.NoResize | ImGuiWindowFlags.NoCollapse))
            {
                ImGui.BeginTabBar("Menu");
                if (ImGui.BeginTabItem("BIS"))
                {
                    if(ImGui.BeginChildFrame(1,new Vector2(400,400),ImGuiWindowFlags.NoResize)) {
                        foreach (KeyValuePair<AvailableClasses, string> pair in HRTPlugin.Configuration.DefaultBIS)
                        {
                            string value = pair.Value;
                            if (ImGui.InputText(pair.Key.ToString(), ref value, 100))
                            {
                                HRTPlugin.Configuration.DefaultBIS[pair.Key] = value;
                            }
                        }
                        ImGui.EndChildFrame();
                    }
                    ImGui.EndTabItem();
                }
                if (ImGui.BeginTabItem("Loot"))
                {
                    LootList.Draw();
                    ImGui.EndTabItem();
                }
                if (ImGui.Button("Save##Config"))
                {
                    HRTPlugin.Configuration.LootRuling.RuleSet = LootList.List;
                    HRTPlugin.Configuration.Save();
                    this.Hide();
                }
                ImGui.EndTabBar();
            }
            ImGui.End();
        }
        
        
    }
   
}
