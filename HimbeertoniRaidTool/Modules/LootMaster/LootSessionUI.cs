using System.Collections.Generic;
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
        internal LootSessionUI(LootSource lootSource, RaidGroup group, LootRuling lootRuling, RolePriority defaultRolePriority) : base()
        {
            _lootRuling = lootRuling;
            _lootSource = lootSource;
            _session = new(group, _lootRuling, group.RolePriority ?? defaultRolePriority,
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
                    ImGui.Text(_session.Loot[i].item.Name);
                    ImGui.InputInt($"##Input{i}", ref _session.Loot[i].Item2);
                }
                ImGui.EndChild();
            }
            ImGui.SameLine();

            if (ImGui.BeginChild("Rules", new Vector2(270, 270), false))
            {
                ImGui.TextWrapped($"{Localize("Role priority", "Role priority")}:\n{_session.RolePriority}");
                _ruleListUi.Draw();
                if (ImGuiHelper.Button(Localize("Reset to default", "Reset to default"),
                    Localize("OverrideWithDefaults", "Overrides these settings with defaults from configuration")))
                {
                    _ruleListUi = new(LootRuling.PossibleRules, _lootRuling.RuleSet);
                }
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
                WindowFlags = ImGuiWindowFlags.AlwaysAutoResize;
            }
            protected override void Draw()
            {
                foreach (((HrtItem item, int nr), List<(Player player, string reason)> ruling) in _session.Results)
                {
                    if (ImGui.CollapsingHeader($"{item.Name} # {nr + 1}\n {ruling[0].player.NickName} won" +
                        $"{(ruling.Count > 1 ? $" over {ruling[1].player.NickName} ({ruling[0].reason})" : "")}"))
                    {
                        if (ImGui.BeginTable($"LootTable##{item.Name} # {nr + 1}", 3,
                            ImGuiTableFlags.Borders | ImGuiTableFlags.SizingStretchProp))
                        {
                            ImGui.TableSetupColumn(Localize("Pos", "Pos"));
                            ImGui.TableSetupColumn(Localize("Player", "Player"));
                            ImGui.TableSetupColumn(Localize("Rule", "Rule"));
                            ImGui.TableHeadersRow();

                            int place = 1;
                            foreach ((Player player, string reason) in ruling)
                            {
                                ImGui.TableNextColumn();
                                ImGui.Text(place.ToString());
                                ImGui.TableNextColumn();
                                ImGui.Text($"{player.NickName} ({player.MainChar.MainJob})");
                                ImGui.TableNextColumn();
                                ImGui.Text(reason);
                                place++;
                            }
                            ImGui.EndTable();
                        }
                    }
                }
            }
        }
    }
}
