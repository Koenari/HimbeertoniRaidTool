using System;
using System.Numerics;
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
        private Guid _sessionGuid = Guid.NewGuid();
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
            ImGui.SetNextWindowSize(new Vector2(550, 370));
            if (ImGui.Begin(string.Format("Loot session for {0}##{1}", _lootSource, _sessionGuid), ref Visible,
                ImGuiWindowFlags.NoCollapse | ImGuiWindowFlags.NoResize | ImGuiWindowFlags.NoScrollbar))
            {
                if (ImGui.Button(Localize("Distribute", "Distribute")))
                    StartLootDistribution();
                ImGui.SameLine();
                if (ImGui.Button(Localize("Close", "Close")))
                    Hide();
                //
                if (ImGui.BeginChild("Loot", new Vector2(250, 300), false))
                {
                    for (int i = 0; i < _session.Loot.Length; i++)
                    {
                        if (_session.Loot[i].Item1.Valid)
                        {
                            ImGui.Text(_session.Loot[i].Item1.Item.Name);
                            ImGui.InputInt($"##Input{i}", ref _session.Loot[i].Item2);
                        }
                    }
                    ImGui.EndChild();
                }
                ImGui.SameLine();

                if (ImGui.BeginChild("Rules", new Vector2(250, 250), false))
                {
                    if (ImGui.Button(Localize("Reset to default", "Reset to default")))
                    {
                        _ruleListUi = new(LootRuling.PossibleRules, HRTPlugin.Configuration.LootRuling.RuleSet);
                        _session.RulingOptions.StrictRooling = HRTPlugin.Configuration.LootRuling.StrictRooling;
                    }
                    ImGui.Checkbox(Localize("Strict Ruling", "Strict Ruling"), ref _session.RulingOptions.StrictRooling);
                    _ruleListUi.Draw();
                    ImGui.NewLine();

                    ImGui.TextWrapped(Localize("ChangesOnlyForThisLootSesseion",
                        "Changes made here only affect this loot session"));
                    ImGui.EndChild();
                }
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
                ImGui.SetNextWindowSize(new Vector2(450, 310));
                if (ImGui.Begin(Localize("LootResultTitle", "Loot Results"), ref Visible, ImGuiWindowFlags.NoResize))
                {
                    /*
                    ImGui.Text(Localize("Following rules were used:", "Following rules were used:"));
                    foreach (LootRule rule in _session.RulingOptions.RuleSet)
                        ImGui.BulletText(rule.ToString());
                    */
                    ImGui.BeginTabBar("Items", ImGuiTabBarFlags.FittingPolicyScroll);
                    foreach (var result in _session.Results)
                    {
                        if (ImGui.BeginTabItem($"{result.Key.Item1.Name} # {result.Key.Item2 + 1}"))
                        {
                            ImGui.Text(string.Format(Localize("LootRuleHeader", "Loot Results for {0}: "), result.Key.Item1.Name));
                            if (ImGui.BeginTable($"LootTable##{result.Key.Item1.Name} # {result.Key.Item2 + 1}", 4,
                                ImGuiTableFlags.Borders | ImGuiTableFlags.SizingStretchProp))
                            {
                                ImGui.TableSetupColumn(Localize("Pos", "Pos"));
                                ImGui.TableSetupColumn(Localize("Player", "Player"));
                                ImGui.TableSetupColumn(Localize("Rule", "Rule"));
                                ImGui.TableSetupColumn(Localize("Roll", "Roll"));
                                ImGui.TableHeadersRow();

                                int place = 1;
                                foreach ((Player Player, string Reason) looter in result.Value)
                                {
                                    ImGui.TableNextColumn();
                                    ImGui.Text(place.ToString());
                                    ImGui.TableNextColumn();
                                    ImGui.Text($"{looter.Player.NickName} ({looter.Player.MainChar.MainClassType})");
                                    ImGui.TableNextColumn();
                                    ImGui.Text(looter.Reason);
                                    ImGui.TableNextColumn();
                                    ImGui.Text(_session.Rolls[looter.Player].ToString());

                                    //ImGui.Text($"{place}: {looter.Player.NickName} Rule: { looter.Reason} Roll: {_session.Rolls[looter.Player]}");
                                    place++;
                                }
                                ImGui.EndTable();
                            }
                            ImGui.EndTabItem();
                        }
                    }
                    ImGui.EndTabBar();
                    if (ImGui.Button(Localize("Copy Chat Message", "Copy Chat Message")))
                    {
                        string message = "";
                        foreach (var result in _session.Results)
                        {
                            message += $"Result for {result.Key.Item1.Name} # {result.Key.Item2 + 1}\n";
                            message += $"{result.Value[0].Item1.NickName} won by rule: {result.Value[0].Item2}\n";
                        }
                        ImGui.SetClipboardText(message);
                    }
                    ImGui.End();
                }


            }
        }
    }
}
