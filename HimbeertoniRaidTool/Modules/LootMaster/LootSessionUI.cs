using System.Numerics;
using HimbeertoniRaidTool.Data;
using HimbeertoniRaidTool.UI;
using ImGuiNET;
using static HimbeertoniRaidTool.HrtServices.Localization;

namespace HimbeertoniRaidTool.Modules.LootMaster
{
    internal class LootSessionUI : HrtWindow
    {
        private readonly LootSession _session;
        private readonly UiSortableList<LootRule> _ruleListUi;
        private readonly LootResultWindow _resultWindow;
        internal LootSessionUI(IHrtModule module, InstanceWithLoot lootSource, RaidGroup group, LootRuling lootRuling, RolePriority defaultRolePriority) : base()
        {
            _session = new(group, lootRuling, defaultRolePriority,lootSource);
            _ruleListUi = new(LootRuling.PossibleRules, lootRuling.RuleSet);
            _resultWindow = new LootResultWindow(_session);
            module.WindowSystem.AddWindow(_resultWindow);
            Size = new Vector2(550, 370);
            Title = $"{Localize("Loot session for", "Loot session for")} {lootSource.Name}";
            Flags = ImGuiWindowFlags.NoCollapse | ImGuiWindowFlags.NoScrollbar;
            OpenCentered = true;
        }
        private void StartLootDistribution()
        {
            _session.EvaluateAll(true);
            _resultWindow.Show();
        }
        public override void Draw()
        {
            ImGui.Text($"{Localize("Group", "Group")}: ");
            ImGui.SameLine();
            if (ImGui.BeginCombo("##RaidGroup", _session.Group.Name))
            {
                foreach (RaidGroup group in Services.HrtDataManager.Groups)
                    if (ImGui.Selectable(group.Name) && group != _session.Group)
                    {
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
            if (ImGuiHelper.CloseButton())
                Hide();
            if (_session.NumLootItems == 0)
                ImGui.TextColored(new Vector4(.9f, 0f, 0f, 1f), Localize("NoLootSelected", "You have not selected any loot!"));
            else if (_session.Group.Type == GroupType.Solo)
                ImGui.TextColored(new Vector4(.9f, .9f, 0f, 1f), Localize("DistributeForSolo", "You have selected a group with only one player!"));
            else
                ImGui.NewLine();
            //Begin Loot selection section
            if (ImGui.BeginChild("Loot", new Vector2(250 * ScaleFactor, 300 * ScaleFactor), false))
            {
                foreach ((HrtItem item, int count) in _session.Loot)
                {
                    ImGui.Text(item.Name);
                    int count2 = count;
                    if (ImGui.InputInt($"##Input{item.ID}", ref count2))
                    {
                        if (count2 < 0)
                            count2 = 0;
                        _session.Loot[item] = count2;
                    }
                }
                ImGui.EndChild();
            }
            ImGui.SameLine();
            //Begin rule section
            if (ImGui.BeginChild("Rules", new Vector2(270 * ScaleFactor, 270 * ScaleFactor), false))
            {
                ImGui.TextWrapped($"{Localize("Role priority", "Role priority")}:\n{_session.RolePriority}");
                if (_ruleListUi.Draw())
                    _session.RulingOptions.RuleSet = _ruleListUi.List;
                ImGui.NewLine();
                ImGui.TextWrapped(Localize("ChangesOnlyForThisLootSesseion",
                    "Changes made here only affect this loot session"));
                ImGui.EndChild();
            }
        }
        private class LootResultWindow : HrtWindow
        {
            private readonly LootSession _session;
            public LootResultWindow(LootSession session) : base(false)
            {
                _session = session;
                MinSize = new Vector2(500, 250);
                Title = Localize("LootResultTitle", "Loot Results");
                Flags = ImGuiWindowFlags.AlwaysAutoResize;
                OpenCentered = true;
            }
            public override void Draw()
            {
                if (_session.Results.Count == 0)
                    ImGui.Text(Localize("No loot", "No loot"));
                foreach (((HrtItem item, int nr), LootResults results) in _session.Results)
                {
                    if (ImGui.CollapsingHeader($"{item.Name} # {nr + 1}  \n " +
                        ((results.Count > 0 && results[0].Category == LootCategory.Need) ? $"{results[0].Player.NickName} ({results[0].AplicableJob.Job}) won" +
                        $"{((results.Count > 1 & results[1].Category == LootCategory.Need) ? $" over {results[1].Player.NickName} ({results[1].AplicableJob.Job}) " : "")}({results[0].DecidingFactor(results[1])})  "
                        : $"{Localize("Greed only", "Greed only")}")
                        ))
                    {
                        if (ImGui.BeginTable($"LootTable##{item.Name} # {nr + 1}", 4 + _session.RulingOptions.RuleSet.Count,
                            ImGuiTableFlags.Borders | ImGuiTableFlags.SizingFixedFit | ImGuiTableFlags.RowBg))
                        {
                            ImGui.TableSetupColumn(Localize("Pos", "Pos"));
                            ImGui.TableSetupColumn(Localize("Player", "Player"));
                            ImGui.TableSetupColumn(Localize("Needed items", "Needed items"));
                            ImGui.TableSetupColumn(Localize("Rule", "Rule"));
                            foreach (LootRule rule in _session.RulingOptions.RuleSet)
                                ImGui.TableSetupColumn(rule.Name);
                            ImGui.TableHeadersRow();

                            int place = 1;
                            LootRule lastRule = LootRuling.Default;
                            for (int i = 0; i < results.Count; i++)
                            {
                                bool isLast = i == results.Count - 1;
                                var singleResult = results[i];
                                ImGui.TableNextColumn();
                                ImGui.Text(place.ToString());
                                ImGui.TableNextColumn();
                                ImGui.Text($"{singleResult.Player.NickName} ({singleResult.AplicableJob})");
                                ImGui.TableNextColumn();
                                foreach (var neededItem in singleResult.NeededItems)
                                {
                                    ImGui.Text(neededItem.Name);
                                    if (ImGui.IsItemHovered())
                                    {
                                        ImGui.BeginTooltip();
                                        neededItem.Draw();
                                        ImGui.EndTooltip();
                                    }
                                }
                                ImGui.TableNextColumn();
                                if (singleResult.Category == LootCategory.Need)
                                {
                                    var decidingFactor = isLast ? LootRuling.Default
                                        : (results[i + 1].Category > LootCategory.Need ? new(LootRuleEnum.NeedGreed) : singleResult.DecidingFactor(results[i + 1]));
                                    ImGui.Text(decidingFactor.Name);
                                    foreach (LootRule rule in _session.RulingOptions.RuleSet)
                                    {
                                        ImGui.TableNextColumn();
                                        string toPrint = singleResult.EvaluatedRules.TryGetValue(rule, out (int _, string val) a) ? a.val : "";
                                        if (rule == decidingFactor && rule == lastRule)
                                            ImGui.TextColored(Colors.Yellow, toPrint);
                                        else if (rule == decidingFactor)
                                            ImGui.TextColored(Colors.Green, toPrint);
                                        else if (rule == lastRule)
                                            ImGui.TextColored(Colors.Red, toPrint);
                                        else
                                            ImGui.Text(toPrint);
                                    }
                                    lastRule = decidingFactor;
                                }
                                else
                                {
                                    ImGui.Text(singleResult.Category.ToString());
                                    foreach (LootRule rule in _session.RulingOptions.RuleSet)
                                    {
                                        ImGui.TableNextColumn();
                                        ImGui.Text("-");
                                    }

                                }

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
