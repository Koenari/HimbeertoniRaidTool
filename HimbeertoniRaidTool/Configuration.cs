using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Numerics;
using Dalamud.Configuration;
using Dalamud.Logging;
using HimbeertoniRaidTool.Data;
using HimbeertoniRaidTool.UI;
using ImGuiNET;
using Newtonsoft.Json;
using static Dalamud.Localization;
using static HimbeertoniRaidTool.Data.Job;

namespace HimbeertoniRaidTool
{
    [Serializable]
    public class Configuration : IPluginConfiguration
    {
        public delegate void ConfigurationChangedDelegate();
        public event ConfigurationChangedDelegate? ConfigurationChanged;
        [JsonIgnore]
        public bool FullyLoaded { get; private set; } = false;
        [JsonProperty]
        public bool ShowWelcomeWindow = true;
        [JsonIgnore]
        private readonly int TargetVersion = 4;
        public int Version { get; set; } = 4;
        [JsonIgnore]
        private readonly ReadOnlyDictionary<Job, string> OldDefaultBIS =
            new ReadOnlyDictionary<Job, string>(
            new Dictionary<Job, string>
        {
            { AST, "88647808-8a28-477b-b285-687bdcbff2d4" },
            { BLM, "327d090b-2d5a-4c3c-9eb9-8fd42342cce3" },
            { BLU, "3db73aab-2968-4eb7-b392-d524f5a1b783" },
            { BRD, "cec981af-25c7-4ffb-905e-3024411b797a" },
            { DNC, "fd333e44-0f90-42a6-a070-044b332bb54e" },
            { DRG, "8bdd42db-a318-41a0-8903-14efa5e0774b" },
            { DRK, "dda8aef5-41e4-40b6-813c-df306e1f1cee" },
            { GNB, "88fbea7d-3b43-479c-adb8-b87c9d6cb5f9" },
            { MCH, "6b4b1ba5-a821-41a0-b070-b1f50e986f85" },
            { MNK, "841ecfdb-41fe-44b4-8764-b3b08e223f8c" },
            { NIN, "b9876a4d-aba9-48f0-9c03-cb542af46a29" },
            { PLD, "38fe3778-f2c1-4300-99e4-b58a0445e969" },
            { RDM, "80fdec19-1109-4ca2-8172-53d4dda44144" },
            { RPR, "b301e789-96da-42f2-9628-95f68345e35b" },
            { SAM, "3a7c7f45-b715-465d-a377-db458045506a" },
            { SCH, "f1802c19-d766-40f0-b781-f5b965cb964e" },
            { SGE, "3c7d9741-0e74-41d7-9ec4-1b2e7c1673a5" },
            { SMN, "840a5088-23fa-49c5-a12a-3731ca55b4a6" },
            { WAR, "6d0d2d4d-a477-44ea-8002-862eca8ef91d" },
            { WHM, "e78a29e3-1dcf-4e53-bbcf-234f33b2c831" },
        });
        [JsonProperty("DefaultBIS")]
        private Dictionary<Job, string> BISUserOverride { get; set; } = new Dictionary<Job, string>();
        public string GetDefaultBiS(Job c) => BISUserOverride.ContainsKey(c) ? BISUserOverride[c] : CuratedData.DefaultBIS[c];
        public LootRuling LootRuling { get; set; } = new();
        [JsonProperty]
        private RaidGroup? GroupInfo = null;
        [JsonProperty]
        private List<RaidGroup> RaidGroups = new();
        [JsonProperty]
        public bool OpenLootMasterOnStartup = false;
        [JsonProperty]
        public bool LootMasterHideInBattle = true;
        [JsonProperty]
        public int LootmasterUiLastIndex = 0;
        [JsonIgnore]
        public RaidTier SelectedRaidTier => RaidTierOverride ?? CuratedData.CurrentRaidSavage;
        [JsonProperty]
        public RaidTier? RaidTierOverride;
        public void Save()
        {
            if (Version == TargetVersion)
                Services.PluginInterface.SavePluginConfig(this);
            else
                PluginLog.LogError("Configuration Version mismatch. Did not Save!");
        }
        private void Upgrade()
        {
            int oldVersion;
            while (Version < TargetVersion)
            {
                oldVersion = Version;
                DoUpgradeStep();
                //Detect endless loops and "crash gracefully"
                if (Version == oldVersion)
                    throw new Exception("Configuration upgrade ran into an unexpected issue");
            }
            void DoUpgradeStep()
            {
                switch (Version)
                {
                    case 1:
                        RaidGroups.Clear();
                        RaidGroups.Add(GroupInfo ?? new());
                        GroupInfo = null;
                        Version = 2;
                        break;
                    case 2:
                        DataManagement.DataManager.Fill(RaidGroups);
                        RaidGroups = new();
                        Version = 3;
                        Save();
                        break;
                    case 3:
                        foreach (var c in Enum.GetValues<Job>())
                        {
                            if (BISUserOverride.ContainsKey(c) && BISUserOverride[c] == OldDefaultBIS[c])
                                BISUserOverride.Remove(c);
                        }
                        Version = 4;
                        Save();
                        break;
                    default:
                        throw new Exception("Unsupported Version of Configuration");
                }
            }
        }
        internal void AfterLoad()
        {
            if (FullyLoaded)
                return;
            if (Version > TargetVersion)
            {
                string msg = "Tried loading a configuration from a newer version of the plugin." +
                    "\nTo prevent data loss operation has been stopped.\nYou need to update to use this plugin!";
                PluginLog.LogFatal(msg);
                Services.ChatGui.PrintError($"[HimbeerToniRaidTool]\n{msg}");
                throw new NotSupportedException($"[HimbeerToniRaidTool]\n{msg}");
            }
            if (Version != TargetVersion)
                Upgrade();
            if (LootRuling.RuleSet.Count == 0)
            {
                LootRuling.RuleSet.AddRange(
                    new List<LootRule>()
                        {
                            new(LootRuleEnum.BISOverUpgrade),
                            new(LootRuleEnum.ByPosition),
                            new(LootRuleEnum.HighesItemLevelGain),
                            new(LootRuleEnum.LowestItemLevel),
                            new(LootRuleEnum.Random)
                        }
                );
            }
            try
            {
                if (RaidTierOverride is not null)
                    RaidTierOverride = CuratedData.RaidTiers[Array.IndexOf(CuratedData.RaidTiers, RaidTierOverride)];
            }
            catch (Exception) { };
            FullyLoaded = true;
        }

        public class ConfigUI : HrtUI
        {
            private UiSortableList<LootRule> LootList;
            public ConfigUI() : base(false)
            {
                Services.PluginInterface.UiBuilder.OpenConfigUi += Show;
                LootList = new(LootRuling.PossibleRules, HRTPlugin.Configuration.LootRuling.RuleSet.Cast<LootRule>());
            }
            protected override void BeforeDispose()
            {
                Services.PluginInterface.UiBuilder.OpenConfigUi -= Show;
            }
            protected override void OnShow()
            {
                LootList = new(LootRuling.PossibleRules, HRTPlugin.Configuration.LootRuling.RuleSet);
            }
            protected override void Draw()
            {
                ImGui.SetNextWindowSize(new Vector2(450, 500), ImGuiCond.Always);
                if (ImGui.Begin("HimbeerToni Raid Tool Configuration", ref Visible,
                    ImGuiWindowFlags.NoResize | ImGuiWindowFlags.NoCollapse))
                {
                    ImGui.BeginTabBar("Menu");
                    if (ImGui.BeginTabItem(Localize("General", "General")))
                    {
                        ImGui.Checkbox(Localize("Open group overview on startup", "Open group overview on startup"),
                            ref HRTPlugin.Configuration.OpenLootMasterOnStartup);
                        ImGui.Checkbox(Localize("Hide windows in combat", "Hide windows in combat"),
                            ref HRTPlugin.Configuration.LootMasterHideInBattle);
                        ImGui.EndTabItem();
                    }
                    if (ImGui.BeginTabItem("BiS"))
                    {
                        if (ImGui.BeginChildFrame(1, new Vector2(400, 400), ImGuiWindowFlags.NoResize))
                        {
                            foreach (var c in Enum.GetValues<Job>())
                            {
                                bool isOverriden = HRTPlugin.Configuration.BISUserOverride.ContainsKey(c);
                                string value = HRTPlugin.Configuration.GetDefaultBiS(c);
                                if (ImGui.InputText(c.ToString(), ref value, 100))
                                {
                                    if (value != CuratedData.DefaultBIS[c])
                                    {
                                        if (isOverriden)
                                            HRTPlugin.Configuration.BISUserOverride[c] = value;
                                        else
                                            HRTPlugin.Configuration.BISUserOverride.Add(c, value);
                                    }
                                    else
                                    {
                                        if (isOverriden)
                                            HRTPlugin.Configuration.BISUserOverride.Remove(c);
                                    }

                                }
                                if (isOverriden)
                                {
                                    ImGui.SameLine();
                                    if (ImGuiHelper.Button(Dalamud.Interface.FontAwesomeIcon.Undo,
                                        $"Reset{c}", Localize("Reset to default", "Reset to default")))
                                        HRTPlugin.Configuration.BISUserOverride.Remove(c);
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
                    ImGui.EndTabBar();
                    if (ImGuiHelper.Button(Dalamud.Interface.FontAwesomeIcon.Save, "SaveConfig", $"{Localize("Save configuration", "Save configuration")}##Config"))
                    {
                        HRTPlugin.Configuration.LootRuling.RuleSet = LootList.List;
                        HRTPlugin.Configuration.Save();
                        Hide();
                        HRTPlugin.Configuration.ConfigurationChanged?.Invoke();
                    }
                }
                ImGui.End();
            }


        }
    }
}
