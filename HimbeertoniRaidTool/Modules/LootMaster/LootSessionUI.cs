using System.Numerics;
using HimbeertoniRaidTool.Data;
using HimbeertoniRaidTool.UI;
using ImGuiNET;
using static Dalamud.Localization;

namespace HimbeertoniRaidTool.Modules.LootMaster
{
    internal class LootSessionUI : HrtUI
    {
        private readonly LootSource _lootSource;
        private readonly LootSession _session;
        private UiSortableList<LootRule> _ruleListUi;
        private readonly LootRuling _lootRuling;
        internal LootSessionUI(LootSource lootSource, RaidGroup group, LootRuling lootRuling) : base()
        {
            _lootRuling = lootRuling;
            _lootSource = lootSource;
            _session = new(group, _lootRuling,
                LootDB.GetPossibleLoot(_lootSource).ConvertAll(x => (x, 0)).ToArray());
            _ruleListUi = new(LootRuling.PossibleRules, _session.RulingOptions.RuleSet);
            Size = new Vector2(550, 370);
            Title = $"{Localize("Loot session for", "Loot session for")} {_lootSource}";
            WindowFlags = ImGuiWindowFlags.NoCollapse | ImGuiWindowFlags.NoResize | ImGuiWindowFlags.NoScrollbar;
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
            if (ImGuiHelper.Button(Localize("Distribute", "Distribute"),
                Localize("OpenLootResultTooltip", "Opens a window with results of loot distribution according to these rules and current equipment of players")))
                StartLootDistribution();
            ImGui.SameLine();
            if (ImGuiHelper.Button(Localize("Close", "Close"), null))
                Hide();
            //
            if (ImGui.BeginChild("Loot", new Vector2(250, 300), false))
            {
                for (int i = 0; i < _session.Loot.Length; i++)
                {
                    ImGui.Text(_session.Loot[i].Item1.Name);
                    ImGui.InputInt($"##Input{i}", ref _session.Loot[i].Item2);
                }
                ImGui.EndChild();
            }
            ImGui.SameLine();

            if (ImGui.BeginChild("Rules", new Vector2(250, 250), false))
            {
                if (ImGuiHelper.Button(Localize("Reset to default", "Reset to default"),
                    Localize("Overrides these settings with defaults from configuration", "Overrides these settings with defaults from configuration")))
                {
                    _ruleListUi = new(LootRuling.PossibleRules, _lootRuling.RuleSet);
                }
                _ruleListUi.Draw();
                ImGui.NewLine();

                ImGui.TextWrapped(Localize("ChangesOnlyForThisLootSesseion",
                    "Changes made here only affect this loot session"));
                ImGui.EndChild();
            }
        }
        private class LootResultWindow : HrtUI
        {
            private readonly LootSession _session;
            public LootResultWindow(LootSession session) : base()
            {
                _session = session;
                Size = new Vector2(450, 310);
                Title = Localize("LootResultTitle", "Loot Results");
                WindowFlags = ImGuiWindowFlags.NoResize;
            }
            protected override void Draw()
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
                        if (ImGui.BeginTable($"LootTable##{result.Key.Item1.Name} # {result.Key.Item2 + 1}", 3,
                            ImGuiTableFlags.Borders | ImGuiTableFlags.SizingStretchProp))
                        {
                            ImGui.TableSetupColumn(Localize("Pos", "Pos"));
                            ImGui.TableSetupColumn(Localize("Player", "Player"));
                            ImGui.TableSetupColumn(Localize("Rule", "Rule"));
                            ImGui.TableHeadersRow();

                            int place = 1;
                            foreach ((Player Player, string Reason) looter in result.Value)
                            {
                                ImGui.TableNextColumn();
                                ImGui.Text(place.ToString());
                                ImGui.TableNextColumn();
                                ImGui.Text($"{looter.Player.NickName} ({looter.Player.MainChar.MainJob})");
                                ImGui.TableNextColumn();
                                ImGui.Text(looter.Reason);

                                //ImGui.Text($"{place}: {looter.Player.NickName} Rule: { looter.Reason} Roll: {_session.Rolls[looter.Player]}");
                                place++;
                            }
                            ImGui.EndTable();
                        }
                        ImGui.EndTabItem();
                    }
                }
                ImGui.EndTabBar();
                if (ImGuiHelper.Button(Localize("Copy Chat Message", "Copy Chat Message"),
                    Localize("CopyLootMessageTooltip", "Copies the results to clipboard. Unfortunately only the first line is sent when pasting to FFXIV chat")))
                {
                    string message = "";
                    foreach (var result in _session.Results)
                    {
                        message += $"Result for {result.Key.Item1.Name} # {result.Key.Item2 + 1}\n";
                        message += $"{result.Value[0].Item1.NickName} won by rule: {result.Value[0].Item2}\n";
                    }
                    ImGui.SetClipboardText(message);
                }
            }
        }
    }
}
