using System;
using System.Collections.Generic;
using HimbeertoniRaidTool.Data;
using HimbeertoniRaidTool.UI;
using ImGuiNET;
using static Dalamud.Localization;

namespace HimbeertoniRaidTool.LootMaster
{
    internal class LootSessionUI : HrtUI
    {
        private readonly LootSource _lootSource;
        private readonly LootSession _session;

        internal LootSessionUI(LootSource lootSource, RaidGroup group) : base()
        {
            _lootSource = lootSource;
            _session = new(group,
                HRTPlugin.Configuration.LootRuling,
                LootDB.GetPossibleLoot(_lootSource).ConvertAll(x => (x, 0)).ToArray());
        }

        protected override void Draw()
        {
            if (ImGui.Begin(string.Format("Loot session for {0}", _lootSource), ref Visible, ImGuiWindowFlags.NoCollapse))
            {
                for (int i = 0; i < _session.Loot.Length; i++)
                {
                    if (_session.Loot[i].Item1.Valid)
                    {
                        ImGui.Text(_session.Loot[i].Item1.Item.Name);
                        ImGui.SameLine();
                        ImGui.InputInt("##" + _session.Loot[i].Item1.Item.Name, ref _session.Loot[i].Item2);

                    }
                }
                if (ImGui.Button(Localize("Distribute", "Distribute")))
                {
                    _session.EvaluateAll();
                    foreach (var result in _session.Results)
                    {
                        var lootWindow = new LootResultWindow(result);
                        AddChild(lootWindow);
                        lootWindow.Show();
                    }
                }
                if (ImGui.Button(Localize("Close", "Close")))
                {
                    Hide();
                }
                ImGui.End();
            }
        }
        private class LootResultWindow : HrtUI
        {
            public LootResultWindow(KeyValuePair<(HrtItem, int), List<(Player, string)>> result) : base()
            {
                Item = result.Key.Item1;
                Num = result.Key.Item2;
                Looters = result.Value;
            }

            public HrtItem Item { get; }
            public int Num { get; }
            public List<(Player, string)> Looters { get; }

            private LootRuling LootRuling => HRTPlugin.Configuration.LootRuling;
            protected override void Draw()
            {
                if (ImGui.Begin(String.Format(Localize("LootResultTitle", "Loot Results for {0} number {1}"), Item.Name, Num), ref Visible))
                {
                    ImGui.Text(string.Format(Localize("LootRuleHeader", "Loot Results for {0}: "), Item.Name));
                    ImGui.Text(Localize("Following rules were used:", "Following rules were used:"));
                    foreach (LootRule rule in LootRuling.RuleSet)
                        ImGui.BulletText(rule.ToString());
                    int place = 1;
                    foreach ((Player, string) looter in Looters)
                    {
                        ImGui.Text(string.Format(Localize("LootDistributionLine", "Priority {0} for Player {1} won by rule {2} "),
                            place, looter.Item1.NickName, looter.Item2));
                        place++;
                    }
                    ImGui.End();
                }


            }
        }
    }
}
