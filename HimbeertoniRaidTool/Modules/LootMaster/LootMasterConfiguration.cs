using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using HimbeertoniRaidTool.Data;
using HimbeertoniRaidTool.UI;
using ImGuiNET;
using Newtonsoft.Json;
using static Dalamud.Localization;

namespace HimbeertoniRaidTool.Modules.LootMaster
{
    internal class LootMasterConfiguration : HRTConfiguration<LootMasterConfiguration.ConfigData, LootMasterConfiguration.ConfigUi>
    {
        public bool FullyLoaded { get; private set; } = false;

        public override ConfigUi? Ui => _ui;

        private readonly int TargetVersion = 1;

        private readonly ConfigUi _ui;

        public LootMasterConfiguration(LootMasterModule hrtModule) : base(hrtModule.InternalName, hrtModule.Name)
        {
            _ui = new(this);
        }
        public override void AfterLoad()
        {
            if (Data.Version < TargetVersion)
                Upgrade();
            FullyLoaded = true;
        }
        [Obsolete]
        public void FillFromLegacy(LegacyConfiguration leg)
        {
            Data.BISUserOverride = leg.BISUserOverride;
            Data.LootRuling = leg.LootRuling;
            Data.OpenOnStartup = leg.OpenLootMasterOnStartup;
            Data.HideInBattle = leg.LootMasterHideInBattle;
            Data.LastGroupIndex = leg.LootmasterUiLastIndex;
            Data.RaidTierOverride = leg.RaidTierOverride;
        }
        private void Upgrade() { }


        internal sealed class ConfigUi : IHrtConfigUi
        {
            private readonly LootMasterConfiguration _config;
            private ConfigData _dataCopy;
            private UiSortableList<LootRule> LootList;

            internal ConfigUi(LootMasterConfiguration config)
            {
                _config = config;
                _dataCopy = config.Data.Clone();
                LootList = new(LootRuling.PossibleRules, _dataCopy.LootRuling.RuleSet.Cast<LootRule>());
            }

            public void Cancel()
            {

            }

            public void Draw()
            {
                ImGui.BeginTabBar("LootMaster");
                if (ImGui.BeginTabItem(Localize("General", "General")))
                {
                    ImGui.Checkbox(Localize("Open group overview on startup", "Open group overview on startup"),
                        ref _dataCopy.OpenOnStartup);
                    ImGui.Checkbox(Localize("Hide windows in combat", "Hide windows in combat"),
                        ref _dataCopy.HideInBattle);
                    ImGui.EndTabItem();
                }
                if (ImGui.BeginTabItem("BiS"))
                {
                    if (ImGui.BeginChildFrame(1, new Vector2(400, 400), ImGuiWindowFlags.NoResize))
                    {
                        foreach (var c in Enum.GetValues<Job>())
                        {
                            bool isOverriden = _dataCopy.BISUserOverride.ContainsKey(c);
                            string value = _dataCopy.GetDefaultBiS(c);
                            if (ImGui.InputText(c.ToString(), ref value, 100))
                            {
                                if (value != CuratedData.DefaultBIS[c])
                                {
                                    if (isOverriden)
                                        _dataCopy.BISUserOverride[c] = value;
                                    else
                                        _dataCopy.BISUserOverride.Add(c, value);
                                }
                                else
                                {
                                    if (isOverriden)
                                        _dataCopy.BISUserOverride.Remove(c);
                                }

                            }
                            if (isOverriden)
                            {
                                ImGui.SameLine();
                                if (ImGuiHelper.Button(Dalamud.Interface.FontAwesomeIcon.Undo,
                                    $"Reset{c}", Localize("Reset to default", "Reset to default")))
                                    _dataCopy.BISUserOverride.Remove(c);
                            }
                        }
                        ImGui.EndChildFrame();
                    }
                    ImGui.EndTabItem();
                }
                if (ImGui.BeginTabItem("Loot"))
                {
                    ImGui.Checkbox(Localize("Strict Ruling", "Strict Ruling"), ref _dataCopy.LootRuling.StrictRooling);
                    LootList.Draw();
                    ImGui.EndTabItem();
                }
                ImGui.EndTabBar();
            }

            public void OnHide() { }

            public void OnShow()
            {
                _dataCopy = _config.Data.Clone();
                LootList = new(LootRuling.PossibleRules, _dataCopy.LootRuling.RuleSet.Cast<LootRule>());
            }

            public void Save()
            {
                _dataCopy.LootRuling.RuleSet = LootList.List;
                _config.Data = _dataCopy;
            }
        }
        [JsonObject(MemberSerialization.OptIn)]
        internal sealed class ConfigData
        {
            public int Version { get; set; } = 1;
            [JsonProperty("UserBiS")]
            public Dictionary<Job, string> BISUserOverride = new();
            [JsonProperty]
            public LootRuling LootRuling = new();
            [JsonProperty]
            public bool OpenOnStartup = false;
            [JsonProperty]
            public bool HideInBattle = true;
            [JsonProperty]
            public int LastGroupIndex = 0;
            [JsonProperty("RaidTier")]
            public RaidTier? RaidTierOverride = null;
            [JsonIgnore]
            public RaidTier SelectedRaidTier => RaidTierOverride ?? CuratedData.CurrentRaidSavage;
            public string GetDefaultBiS(Job c) => BISUserOverride.ContainsKey(c) ? BISUserOverride[c] : CuratedData.DefaultBIS.ContainsKey(c) ? CuratedData.DefaultBIS[c] : "";
        }
    }

}
