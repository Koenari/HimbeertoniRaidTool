using System.Numerics;
using Dalamud.Interface;
using Dalamud.Interface.Utility.Raii;
using HimbeertoniRaidTool.Plugin.Localization;
using HimbeertoniRaidTool.Plugin.UI;
using ImGuiNET;

namespace HimbeertoniRaidTool.Plugin.Modules.LootMaster.Ui;

internal class LootSessionUi : HrtWindow
{
    private const string RULES_POPUP_ID = "RulesButtonPopup";
    private readonly UiSortableList<LootRule> _ruleListUi;
    private readonly LootSession _session;
    private readonly LootMasterModule _module;
    private LootMasterConfiguration.ConfigData CurConfig => _module.ConfigImpl.Data;

    internal LootSessionUi(LootMasterModule module, InstanceWithLoot lootSource, RaidGroup group) : base(
        module.Services.UiSystem)
    {
        _module = module;
        _session = new LootSession(module, lootSource, group);
        _ruleListUi = new UiSortableList<LootRule>(LootRuling.PossibleRules, CurConfig.LootRuling.RuleSet);

        MinSize = new Vector2(600, 300);
        //Size = new Vector2(1100, 600);
        SizeCondition = ImGuiCond.Appearing;
        Title = string.Format(LootmasterLoc.LootSessionUi_Title, lootSource.Name);
        Flags = ImGuiWindowFlags.AlwaysAutoResize;
        OpenCentered = true;
    }

    public override void Draw()
    {
        //Header
        if (ImGuiHelper.CloseButton())
            Hide();
        ImGui.SameLine();
        ImGui.Text($"{LootmasterLoc.LootsessionUi_txt_state}: {_session.CurrentState.FriendlyName()}");
        ImGui.SameLine();

        if (ImGuiHelper.Button(FontAwesomeIcon.Cogs, "##RulesButton",
                               LootmasterLoc.LootSessionUi_btn_tt_Rules))
            ImGui.OpenPopup(RULES_POPUP_ID);
        using (var popup = ImRaii.Popup(RULES_POPUP_ID))
        {
            if (popup)
            {
                if (ImGuiHelper.CloseButton())
                    ImGui.CloseCurrentPopup();
                DrawRulingOptions();
            }
        }

        using (ImRaii.Disabled(_session.CurrentState >= LootSession.State.LootChosen))
        {
            ImGui.SameLine();
            ImGui.SetNextItemWidth(ScaleFactor * 200f);
            using (var combo = ImRaii.Combo("##RaidGroup", _session.Group.Name))
            {
                if (combo)
                {
                    // ReSharper disable once ForeachCanBePartlyConvertedToQueryUsingAnotherGetEnumerator
                    foreach (var group in _module.ConfigImpl.Data.RaidGroups)
                    {
                        if (ImGui.Selectable(group.Name) && group != _session.Group)
                            _session.Group = group;
                    }
                }
            }

            ImGui.SameLine();
            if (ImGuiHelper.Button(LootmasterLoc.LootSessionUi_btn_Calc, LootmasterLoc.LootSessionUi_btn_tt_Calc))
                _session.Evaluate();
        }
        ImGui.NewLine();

        DrawLootSelection();
        ImGui.NewLine();

        if (_session.CurrentState >= LootSession.State.LootChosen)
            DrawResults();
    }

    private void DrawLootSelection()
    {
        const float itemSize = 80f;
        const int itemsPerRow = 7;
        int rows = (int)Math.Ceiling(_session.Loot.Count / (float)itemsPerRow);
        using var disabled = ImRaii.Disabled(_session.CurrentState >= LootSession.State.LootChosen);
        for (int row = 0; row < rows; row++)
        {
            using var id = ImRaii.PushId(row);

            using var table = ImRaii.Table("##LootSelection", itemsPerRow,
                ImGuiTableFlags.NoBordersInBody | ImGuiTableFlags.SizingFixedFit);
            if (!table)
                continue;

            for (int col = 0; col < itemsPerRow; col++)
            {
                if (row * itemsPerRow + col >= _session.Loot.Count)
                {
                    ImGui.TableNextRow();
                    break;
                }

                var item = _session.Loot[row * itemsPerRow + col].item;
                ImGui.TableNextColumn();
                ImGui.SetCursorPosX(ImGui.GetCursorPosX() + 10f * ScaleFactor);
                ImGui.Image(_module.Services.IconCache[item.Icon].ImGuiHandle,
                            Vector2.One * ScaleFactor * (itemSize - 30f));
                if (ImGui.IsItemHovered()) {
                    using var tooltip = ImRaii.Tooltip();
                    item.Draw();
                }
            }

            for (int col = 0; col < itemsPerRow; col++)
            {
                if (row * itemsPerRow + col >= _session.Loot.Count)
                {
                    ImGui.TableNextRow();
                    break;
                }

                (var item, int count) = _session.Loot[row * itemsPerRow + col];
                ImGui.TableNextColumn();
                int count2 = count;
                ImGui.SetNextItemWidth(ScaleFactor * (itemSize - 10f));
                if (ImGui.InputInt($"##Input{item.Id}", ref count2))
                {
                    if (count2 < 0)
                        count2 = 0;
                    _session.Loot[row * itemsPerRow + col] = (item, count2);
                }
            }
        }
    }

    private void DrawRulingOptions()
    {
        //Begin rule section
        var rulesChild =
            ImRaii.Child("##Rules", new Vector2(270 * ScaleFactor, 270 * ScaleFactor), false);
        if (!rulesChild.Success)
            return;
        ImGui.TextWrapped($"{LootmasterLoc.LootSession_text_rolePriority}:\n{_session.RolePriority}");
        if (_ruleListUi.Draw())
            _session.RulingOptions.RuleSet = new List<LootRule>(_ruleListUi.List);
        ImGui.NewLine();
        ImGui.TextWrapped(LootmasterLoc.LootSession_text_rolePriorityHint);
    }

    private void DrawResults()
    {
        //Header 
        ImGui.Text(string.Format(LootmasterLoc.LootSession_heading_results, _session.Instance.Name,
                                 _session.Group.Name));
        ImGui.SameLine();
        if (ImGuiHelper.Button(LootmasterLoc.LootUi_Results_btn_Abort,
                               LootmasterLoc.LootUi_Results_btn_tt_Abort,
                               _session.CurrentState < LootSession.State.DistributionStarted))
            _session.RevertToChooseLoot();
        ImGui.Separator();
        //Guaranteed LuminaItem
        ImGui.Text(LootmasterLoc.LootUi_Results_hdg_GuaranteedItems);
        if (_session.GuaranteedLoot.Count == 0)
            ImGui.Text(GeneralLoc.CommonTerms_None);
        foreach ((var item, bool awarded) in _session.GuaranteedLoot)
        {
            using (ImRaii.Group())
            {
                ImGui.Image(_module.Services.IconCache[item.Icon].ImGuiHandle,
                    Vector2.One * ImGui.GetTextLineHeightWithSpacing());
                ImGui.SameLine();
                ImGui.Text(item.Name);
            }
            if (ImGui.IsItemHovered())
            {
                using var tooltip = ImRaii.Tooltip();
                item.Draw();
            }

            ImGui.SameLine();
            if (ImGuiHelper.Button($"{LootmasterLoc.LootResultWindow_Button_AwardGuaranteed}##{item.Id}",
                                   LootmasterLoc.LootResultWindow_Button_AwardGuaranteed_Tooltip, !awarded))
                _session.AwardGuaranteedLoot(item);
        }

        //Possible Items
        ImGui.NewLine();
        ImGui.Text(LootmasterLoc.LootUi_Results_PossibleItems);
        ImGui.Separator();
        if (_session.Results.Count == 0)
            ImGui.Text(GeneralLoc.CommonTerms_None);
        foreach (((var item, int nr), var results) in _session.Results)
        {
            using var id = ImRaii.PushId($"{item.Id}##{nr}");
            if (!ImGui.CollapsingHeader($"{item.Name} # {nr + 1}  \n {results.ShortResult}",
                    ImGuiTreeNodeFlags.DefaultOpen))
                continue;

            using var table = ImRaii.Table("LootTable", 4 + this._session.RulingOptions.ActiveRules.Count(),
                ImGuiTableFlags.Borders | ImGuiTableFlags.SizingFixedFit | ImGuiTableFlags.RowBg);
            if (!table)
                continue;

            ImGui.TableSetupColumn(LootmasterLoc.LootSessionUi_resultTable_Col_Pos);
            ImGui.TableSetupColumn(Player.DataTypeName.Capitalized());
            ImGui.TableSetupColumn(LootmasterLoc.LootUi_Results_hdg_NeededItems);
            ImGui.TableSetupColumn(LootmasterLoc.LootUI_Results_Rule);
            foreach (var rule in this._session.RulingOptions.ActiveRules)
            {
                ImGui.TableSetupColumn(rule.Name);
            }
            ImGui.TableHeadersRow();

            int place = 1;
            var lastRule = LootRuling.Default;
            for (int i = 0; i < results.Count; i++)
            {
                var singleResult = results[i];
                if (singleResult.ShouldIgnore) continue;
                var nextResult = i + 1 < results.Count ? results[i + 1] : null;
                ImGui.TableNextColumn();
                ImGui.Text(place.ToString());
                ImGui.TableNextColumn();
                ImGui.Text($"{singleResult.Player.NickName} ({singleResult.ApplicableJob})");
                ImGui.TableNextColumn();
                foreach (var neededItem in singleResult.NeededItems)
                {
                    ImGui.Text(neededItem.Name);
                    if (ImGui.IsItemHovered())
                    {
                        using var tooltip = ImRaii.Tooltip();
                        neededItem.Draw();
                    }

                    if (!results.IsAwarded
                        || results.AwardedIdx == i && neededItem.Equals(results[i].AwardedItem)) {
                        ImGui.SameLine();
                        if (neededItem.Slots.Count() > 1)
                        {
                            if (ImGuiHelper.Button(FontAwesomeIcon.Check, $"##Award##{i}##{neededItem.Id}",
                                    $"{LootmasterLoc.LootUi_results_btn_tt_Award} ({GeneralLoc.CommonTerms_Right_Abbrev})",
                                    !results.IsAwarded))
                                this._session.AwardItem((item, nr), neededItem, i);
                            ImGui.SameLine();
                            if (ImGuiHelper.Button(FontAwesomeIcon.Check, $"##Award##{i}##{neededItem.Id}",
                                    $"{LootmasterLoc.LootUi_results_btn_tt_Award} ({GeneralLoc.CommonTerms_Left_Abbrev}",
                                    !results.IsAwarded))
                                this._session.AwardItem((item, nr), neededItem, i, true);
                        }
                        else
                        {
                            if (ImGuiHelper.Button(FontAwesomeIcon.Check, $"##Award##{i}##{neededItem.Id}",
                                    $"{LootmasterLoc.LootUi_results_btn_tt_Award}",
                                    !results.IsAwarded))
                                this._session.AwardItem((item, nr), neededItem, i);
                        }
                    }
                }

                ImGui.TableNextColumn();
                if (singleResult.Category == LootCategory.Need) {
                    var decidingFactor = singleResult.DecidingFactor(nextResult);
                    ImGui.Text(decidingFactor.Name);
                    foreach (var rule in this._session.RulingOptions.ActiveRules)
                    {
                        ImGui.TableNextColumn();
                        string toPrint =
                            singleResult.EvaluatedRules.TryGetValue(rule, out (float _, string val) a)
                                ? a.val
                                : "";
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
                    ImGui.Text(singleResult.Category.FriendlyName());
                    foreach (var _ in this._session.RulingOptions.ActiveRules) {
                        ImGui.TableNextColumn();
                        ImGui.Text("-");
                    }
                }

                place++;
            }
        }
    }
}