using System.Collections.Generic;
using System.Linq;
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
            if (_session.Results.Count > 0)
                AddChild(new LootResultWindow(_session));
        }
        protected override void Draw()
        {
            ImGui.Text($"{Localize("Group", "Group")}: ");
            ImGui.SameLine();
            if (ImGui.BeginCombo("##RaidGroup", _session.Group.Name))
            {
                foreach (RaidGroup group in Services.HrtDataManager.Groups)
                    if (ImGui.Selectable(group.Name) && group != _session.Group)
                    {
                        ClearChildren();
                        _session.Group = group;
                    }
                ImGui.EndCombo();
            }
            ImGui.SameLine();
            if (ImGuiHelper.Button(Localize("Distribute", "Distribute"),
                Localize("OpenLootResultTooltip", "Opens a window with results of loot distribution according to these rules and current equipment of players"),
                _session.NumLootItems > 0))
                StartLootDistribution();
            ImGui.SameLine();
            if (ImGuiHelper.Button(Localize("Close", "Close"), null))
                Hide();
            if (_session.Group.Type == GroupType.Solo)
                ImGui.TextColored(new Vector4(.9f, 0f, 0f, 1f), Localize("DistributeForSolo", "You have selected a group with only one player!"));
            else if (_session.NumLootItems == 0)
                ImGui.TextColored(new Vector4(.9f, 0f, 0f, 1f), Localize("NoLootSelected", "You have not selected any loot!"));
            else
                ImGui.NewLine();
            //
            if (ImGui.BeginChild("Loot", new Vector2(250 * ScaleFactor, 300 * ScaleFactor), false))
            {
                for (int i = 0; i < _session.Loot.Length; i++)
                {
                    ImGui.Text(_session.Loot[i].item.Name);
                    if (ImGui.InputInt($"##Input{i}", ref _session.Loot[i].count) && _session.Loot[i].count < 0)
                        _session.Loot[i].count = 0;
                }
                ImGui.EndChild();
            }
            ImGui.SameLine();

            if (ImGui.BeginChild("Rules", new Vector2(270 * ScaleFactor, 270 * ScaleFactor), false))
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
                if (_session.Results.Count == 0)
                    ImGui.Text(Localize("No loot", "No loot"));
                foreach (((HrtItem item, int nr), List<(Player player, string reason)> ruling) in _session.Results)
                {
                    if (ruling.Count == 0 || ruling.First().reason == Localize("Greed", "Greed"))
                    {
                        ImGui.BeginDisabled();
                        ImGui.CollapsingHeader($"{item.Name} # {nr + 1}  \n{Localize("Greed only", "Greed only")}");
                        ImGui.EndDisabled();
                        continue;
                    }
                    if (ImGui.CollapsingHeader($"{item.Name} # {nr + 1}  \n " +
                        $"{ruling[0].player.NickName} won" +
                        $"{(ruling.Count > 1 ? $" over {ruling[1].player.NickName} ({ruling[0].reason})" : "")}  "))
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
