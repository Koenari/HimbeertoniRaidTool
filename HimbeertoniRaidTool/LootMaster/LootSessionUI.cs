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
        private UiSortableList<LootRule> _ruleListUi;

        internal LootSessionUI(LootSource lootSource, RaidGroup group) : base()
        {
            _lootSource = lootSource;
            _session = new(group,
                HRTPlugin.Configuration.LootRuling,
                LootDB.GetPossibleLoot(_lootSource).ConvertAll(x => (x, 0)).ToArray());
            _ruleListUi = new(LootRuling.PossibleRules, _session.RulingOptions.RuleSet);
        }
        private void StartLootDistribution()
        {
            _session.RulingOptions.RuleSet = _ruleListUi.List;
            _session.EvaluateAll(true);
            ClearChildren();
            AddChild(new LootResultWindow(_session));
        }
        protected override void Draw()
        {
            if (ImGui.Begin(string.Format("Loot session for {0}", _lootSource), ref Visible, ImGuiWindowFlags.NoCollapse))
            {
                if (ImGui.Button(Localize("Distribute", "Distribute")))
                    StartLootDistribution();
                ImGui.SameLine();
                if (ImGui.Button(Localize("Close", "Close")))
                    Hide();
                //
                ImGui.Columns(2);
                if (ImGui.BeginChild("Loot"))
                {
                    for (int i = 0; i < _session.Loot.Length; i++)
                    {
                        if (_session.Loot[i].Item1.Valid)
                        {
                            ImGui.Text(_session.Loot[i].Item1.Item.Name);
                            ImGui.InputInt($"Input##{i}", ref _session.Loot[i].Item2);
                        }
                    }
                    ImGui.EndChild();
                }
                ImGui.NextColumn();
                if (ImGui.BeginChild("Rules"))
                {
                    _ruleListUi.Draw();
                    ImGui.EndChild();
                }
                ImGui.NextColumn();

                ImGui.Columns(1);
                ImGui.End();
            }
        }
        private class LootResultWindow : HrtUI
        {
            private readonly LootSession _session;
            public LootResultWindow(LootSession session) : base()
            {
                _session = session;
            }
            protected override void Draw()
            {
                if (ImGui.Begin(Localize("LootResultTitle", "Loot Results"), ref Visible))
                {
                    ImGui.Text(Localize("Following rules were used:", "Following rules were used:"));
                    foreach (LootRule rule in _session.RulingOptions.RuleSet)
                        ImGui.BulletText(rule.ToString());
                    ImGui.BeginTabBar("Items");
                    foreach (var result in _session.Results)
                    {
                        if (ImGui.BeginTabItem($"{result.Key.Item1.Name} # {result.Key.Item2 + 1}"))
                        {
                            ImGui.Text(string.Format(Localize("LootRuleHeader", "Loot Results for {0}: "), result.Key.Item1.Name));
                            int place = 1;
                            foreach ((Player Player, string Reason) looter in result.Value)
                            {
                                ImGui.Text($"{place}: {looter.Player.NickName} Rule: { looter.Reason} Roll: {_session.Rolls[looter.Player]}");
                                place++;
                            }
                            ImGui.EndTabItem();
                        }
                    }
                    ImGui.EndTabBar();
                    ImGui.End();
                }


            }
        }
    }
}
