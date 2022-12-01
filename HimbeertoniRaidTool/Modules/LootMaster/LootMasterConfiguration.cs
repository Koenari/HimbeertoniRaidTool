using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Dalamud.Logging;
using Dalamud.Utility;
using HimbeertoniRaidTool.Common;
using HimbeertoniRaidTool.Common.Data;
using HimbeertoniRaidTool.Common.Services;
using HimbeertoniRaidTool.Plugin.DataExtensions;
using HimbeertoniRaidTool.Plugin.Modules.LootMaster;
using HimbeertoniRaidTool.Plugin.UI;
using ImGuiNET;
using Newtonsoft.Json;
using static HimbeertoniRaidTool.Plugin.HrtServices.Localization;

namespace HimbeertoniRaidTool.Plugin.Modules.LootMaster
{
    internal class LootMasterConfiguration : HRTConfiguration<LootMasterConfiguration.ConfigData, LootMasterConfiguration.ConfigUi>
    {
        public override ConfigUi? Ui => _ui;
        private readonly ConfigUi _ui;
        private bool FullyLoaded = false;
        private const int TargetVersion = 1;
        public LootMasterConfiguration(LootMasterModule hrtModule) : base(hrtModule.InternalName, hrtModule.Name)
        {
            _ui = new(this);
        }
        public override void AfterLoad()
        {
            if (FullyLoaded)
                return;
            if (Data.Version > TargetVersion)
            {
                string msg = "Tried loading a configuration from a newer version of the plugin." +
                    "\nTo prevent data loss operation has been stopped.\nYou need to update to use this plugin!";
                PluginLog.LogFatal(msg);
                Services.ChatGui.PrintError($"[HimbeerToniRaidTool]\n{msg}");
                throw new NotSupportedException($"[HimbeerToniRaidTool]\n{msg}");
            }
            if (Data.Version != TargetVersion)
                Upgrade();
            FullyLoaded = true;
        }
        //Still first version no upgrade possible
        private void Upgrade() { }
        internal sealed class ConfigUi : IHrtConfigUi
        {
            private readonly LootMasterConfiguration _config;
            private ConfigData _dataCopy;
            private UiSortableList<LootRule> LootList;
            private static float ScaleFactor => HrtWindow.ScaleFactor;

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
                if (ImGui.BeginTabItem(Localize("Appearance", "Appearance")))
                {
                    ImGui.Checkbox(Localize("Lootmaster:OpenOnLogin", "Open group overview on login"), ref _dataCopy.OpenOnStartup);
                    ImGuiHelper.AddTooltip(Localize("Lootmaster:OpenOnLoginTooltip",
                        "Opens group overview window whean you log in"));
                    ImGui.Checkbox(Localize("Config:Lootmaster:ColoredItemNames", "Color items by item level"), ref _dataCopy.ColoredItemNames);
                    ImGuiHelper.AddTooltip(Localize("Lootmaster:ColoredItemNamesTooltip",
                        "Color items according to the item level"));
                    ImGui.Text($"{Localize("Config:Lootmaster:Colors", "Configured colors")}:");
                    ImGui.NewLine();
                    uint iLvL = _config.Data.SelectedRaidTier.ArmorItemLevel;
                    ImGui.SetCursorPosX(ImGui.GetCursorPosX() + ImGui.CalcTextSize($"{iLvL} > ").X);
                    ImGui.Text($"{Localize("iLvl", "iLvl")} >= {iLvL}");
                    ImGui.SameLine();
                    ImGui.ColorEdit4("##Color0", ref _dataCopy.ItemLevelColors[0]);
                    ImGui.Text($"{iLvL} > {Localize("iLvl", "iLvl")} >= {iLvL - 10}");
                    ImGui.SameLine();
                    ImGui.ColorEdit4("##Color1", ref _dataCopy.ItemLevelColors[1]);
                    ImGui.Text($"{iLvL - 10} > {Localize("iLvl", "iLvl")} >= {iLvL - 20}");
                    ImGui.SameLine();
                    ImGui.ColorEdit4("##Color2", ref _dataCopy.ItemLevelColors[2]);
                    ImGui.Text($"{iLvL - 20} > {Localize("iLvl", "iLvl")}");
                    ImGui.SameLine();
                    ImGui.SetCursorPosX(ImGui.GetCursorPosX() + ImGui.CalcTextSize($" >= {iLvL - 20}").X);
                    ImGui.ColorEdit4("##Color3", ref _dataCopy.ItemLevelColors[3]);
                    ImGui.Separator();
                    ImGui.Text($"{Localize("Config:Lootmaster:Itemformat", "Item format")}:");
                    ImGui.SameLine();
                    string copy = _dataCopy.UserItemFormat;
                    if (ImGui.InputText("##format", ref copy, 50))
                        _dataCopy.UserItemFormat = copy;
                    ImGui.Text($"{Localize("Config:Lootmaster:Itemformat:Available", "Available options")}: {{ilvl}} {{source}} {{slot}}");
                    ImGui.Separator();
                    ImGui.Text($"{Localize("Examples", "Examples")}:");
                    for (int i = 0; i < 4; i++)
                    {
                        (long curiLvL, string source, string slot) = (iLvL - 10 * i, ((ItemSource)i).FriendlyName(), ((GearSetSlot)(i * 2)).FriendlyName());
                        if (_dataCopy.ColoredItemNames)
                            ImGui.TextColored(_dataCopy.ItemLevelColors[i], string.Format(_dataCopy.ItemFormatString + "  ", curiLvL, source, slot));
                        else
                            ImGui.Text(string.Format(_dataCopy.ItemFormatString + "  ", curiLvL, source, slot));
                        ImGui.SameLine();
                    }
                    ImGui.EndTabItem();
                }
                if (ImGui.BeginTabItem("BiS"))
                {
                    ImGui.Checkbox(Localize("Config:Lootmaster:IgnoreMateriaforBis",
                        "Ignore Materia"), ref _dataCopy.IgnoreMateriaForBiS);
                    ImGuiHelper.AddTooltip(Localize("Config:Lootmaster:IgnoreMateriaforBisTooltip",
                        "Ignore Materia when determining if an item is equivalent to BiS"));
                    ImGui.Checkbox(Localize("UpdateBisONStartUp", "Update sets from etro.gg periodically"), ref _dataCopy.UpdateEtroBisOnStartup);
                    ImGui.SetNextItemWidth(150f * ScaleFactor);
                    if (ImGui.InputInt(Localize("BisUpdateInterval", "Update interval (days)"), ref _dataCopy.EtroUpdateIntervalDays))
                        if (_dataCopy.EtroUpdateIntervalDays < 1)
                            _dataCopy.EtroUpdateIntervalDays = 1;
                    ImGui.Text(Localize("DefaultBiSHeading", "Default BiS sets (as etro.gg ID)"));
                    ImGui.TextWrapped(Localize("DefaultBiSDisclaimer",
                        "These sets are used when creating a new characer or adding a new job. These do not affect already created characters and jobs."));
                    var jobs = Enum.GetValues<Job>();
                    Array.Sort(jobs, (a, b) =>
                    {
                        bool aFilled = !_dataCopy.GetDefaultBiS(a).IsNullOrEmpty();
                        bool aOverriden = _dataCopy.BISUserOverride.ContainsKey(a);
                        bool bFilled = !_dataCopy.GetDefaultBiS(b).IsNullOrEmpty();
                        bool bOverriden = _dataCopy.BISUserOverride.ContainsKey(b);
                        if (aOverriden && !bOverriden)
                            return -1;
                        if (!aOverriden && bOverriden)
                            return 1;
                        if (aFilled && !bFilled)
                            return -1;
                        if (!aFilled && bFilled)
                            return 1;
                        return a.ToString().CompareTo(b.ToString());

                    });
                    foreach (var c in jobs)
                    {
                        bool isOverriden = _dataCopy.BISUserOverride.ContainsKey(c);
                        string value = _dataCopy.GetDefaultBiS(c);
                        if (ImGui.InputText(c.ToString(), ref value, 100))
                        {
                            if (value != ConfigData.DefaultBIS[c])
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
                        else
                        {
                            ImGui.SameLine();
                            ImGui.TextDisabled($"({Localize("default", "default")})");
                        }
                    }
                    ImGui.EndTabItem();
                }
                if (ImGui.BeginTabItem("Loot"))
                {
                    ImGui.Text(Localize("LootRuleOrder", "Order in which loot rules should be applied"));
                    LootList.Draw();
                    ImGui.Separator();
                    ImGui.Text(Localize("ConfigRolePriority", "Priority to loot for each role (smaller is higher priority)"));
                    ImGui.Text($"{Localize("Current priority", "Current priority")}: {_dataCopy.RolePriority}");
                    _dataCopy.RolePriority.DrawEdit(ImGui.InputInt);
                    ImGui.EndTabItem();
                }
                ImGui.EndTabBar();
            }

            public void OnHide() { }

            public void OnShow()
            {
                _dataCopy = _config.Data.Clone();
                LootList = new(LootRuling.PossibleRules, _dataCopy.LootRuling.RuleSet);
            }

            public void Save()
            {
                _dataCopy.LootRuling.RuleSet = LootList.List;
                _config.Data = _dataCopy;
            }
        }
        [JsonObject(MemberSerialization = MemberSerialization.OptIn, ItemNullValueHandling = NullValueHandling.Ignore)]
        internal sealed class ConfigData
        {
            /// <summary>
            /// Holds a list of Etro IDs to use as BiS sets if users did not enter a preferred BiS
            /// </summary>
            [JsonIgnore]
            internal static Dictionary<Job, string> DefaultBIS { get; } = new Dictionary<Job, string>
            {
                { Job.AST, "a2201358-04ad-4b07-81e4-003a514f0694" },
                { Job.BLM, "bd1b7a52-5893-4928-9d7c-d47aea22d8d2" },
                { Job.BRD, "2a242f9b-8a41-4d09-9e14-3c8fb08e97e4" },
                { Job.DNC, "fb5976d5-a94c-4052-9092-3c3990fefa76" },
                { Job.DRG, "de153cb0-05e7-4f23-a924-1fc28c7ae8db" },
                { Job.DRK, "9467c373-ba77-4f20-aa76-06c8e6f926b8" },
                { Job.GNB, "1cdcf24b-af97-4d6b-ab88-dcfee79f791c" },
                { Job.MCH, "8a0bdf80-80f5-42e8-b10a-160b0fc2d151" },
                { Job.MNK, "12aff29c-8420-4c28-a3c4-68d03ac5afa3" },
                { Job.NIN, "c0c2ba50-b93a-4d18-8cba-a0ebb0705fed" },
                { Job.PLD, "86b4625f-d8ef-4bb1-92b1-cef8bcce7390" },
                { Job.RDM, "5f972eb8-c3cd-44da-aa73-0fa769957e5b" },
                { Job.RPR, "c293f73b-5c58-4855-b43d-aae55b212611" },
                { Job.SAM, "4356046d-2f05-432a-a98c-632f11098ade" },
                { Job.SCH, "41c65b56-fa08-4c6a-b86b-627fd14d04ff" },
                { Job.SGE, "80bec2f5-8e9e-43fb-adcf-0cd7f7018c02" },
                { Job.SMN, "b3567b2d-5c92-4ba1-a18a-eb91b614e944" },
                { Job.WAR, "f3f765a3-56a5-446e-b1e1-1c7cdd23f24b" },
                { Job.WHM, "da9ef350-7568-4c98-8ecc-959040d9ba3a" },
            };
            public int Version { get; set; } = 1;
            /*
             * Appearance
             */
            [JsonProperty]
            public bool OpenOnStartup = false;
            [JsonProperty]
            public bool IgnoreMateriaForBiS = false;
            [JsonProperty("UserItemFormat")]
            private string _userItemFormat = "{source} {slot}";
            [JsonProperty]
            public bool ColoredItemNames = true;
            [JsonProperty]
            public Vector4[] ItemLevelColors = new Vector4[4]
            {
                //At or above cur max iLvl
                new Vector4(0.17f,0.85f,0.17f,1f),
                //10 below
                new Vector4(0.5f,0.83f,0.72f,1f),
                //20 below
                new Vector4(0.85f,0.85f,0.17f,1f),
                //30 or more below
                new Vector4(0.85f,0.17f,0.17f,1f),
            };
            /*
             * BiS
             */
            [JsonProperty("UserBiS")]
            public Dictionary<Job, string> BISUserOverride = new();
            [JsonProperty]
            public bool UpdateEtroBisOnStartup = false;
            [JsonProperty]
            public int EtroUpdateIntervalDays = 14;
            /*
             * Loot
             */
            [JsonProperty(ObjectCreationHandling = ObjectCreationHandling.Replace)]
            public LootRuling LootRuling = new()
            {
                RuleSet = new List<LootRule>()
                        {
                            new(LootRuleEnum.BISOverUpgrade),
                            new(LootRuleEnum.RolePrio),
                            new(LootRuleEnum.HighesItemLevelGain),
                            new(LootRuleEnum.LowestItemLevel),
                            new(LootRuleEnum.Random)
                        }
            };
            [JsonProperty(ObjectCreationHandling = ObjectCreationHandling.Replace)]
            public RolePriority RolePriority = new()
            {
                { Role.Melee,   0 },
                { Role.Caster,  1 },
                { Role.Ranged,  1 },
                { Role.Tank,    3 },
                { Role.Healer,  4 },
            };
            /*
             * Internal
             */
            [JsonIgnore]
            public string UserItemFormat
            {
                get => _userItemFormat;
                set
                {
                    _userItemFormat = value;
                    ItemFormatStringCache = null;
                }
            }
            [JsonIgnore]
            private string? ItemFormatStringCache;
            [JsonIgnore]
            public string ItemFormatString => ItemFormatStringCache ??= ParseItemFormatString(UserItemFormat);
            [JsonProperty]
            public int LastGroupIndex = 0;
            [JsonProperty("RaidTierIndex")]
            public int? RaidTierOverride = null;
            [JsonIgnore]
            public RaidTier SelectedRaidTier => ServiceManager.GameInfo.CurrentExpansion.SavageRaidTiers[RaidTierOverride ?? ^1];
            public string GetDefaultBiS(Job c) => BISUserOverride.ContainsKey(c) ? BISUserOverride[c] : DefaultBIS.ContainsKey(c) ? DefaultBIS[c] : "";
            private static string ParseItemFormatString(string input)
            {
                List<string> result = new();
                string[] split = input.Replace("}{", "} {").Split(' ');
                foreach (string item in split)
                {
                    switch (item.ToLower())
                    {
                        case "{ilvl}": result.Add("{0}"); break;
                        case "{source}": result.Add("{1}"); break;
                        case "{slot}": result.Add("{2}"); break;
                        default: break;
                    }
                }
                return string.Join(' ', result);
            }
        }
    }

}
