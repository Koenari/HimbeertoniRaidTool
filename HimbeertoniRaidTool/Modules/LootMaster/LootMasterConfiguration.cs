using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using Dalamud.Logging;
using Dalamud.Utility;
using HimbeertoniRaidTool.Common;
using HimbeertoniRaidTool.Common.Data;
using HimbeertoniRaidTool.Plugin.DataExtensions;
using HimbeertoniRaidTool.Plugin.UI;
using ImGuiNET;
using Newtonsoft.Json;
using static HimbeertoniRaidTool.Plugin.Services.Localization;

namespace HimbeertoniRaidTool.Plugin.Modules.LootMaster;

internal class LootMasterConfiguration : HRTConfiguration<LootMasterConfiguration.ConfigData, LootMasterConfiguration.ConfigUi>
{
    public override ConfigUi Ui { get; }

    private bool _fullyLoaded;
    private const int TargetVersion = 1;
    public LootMasterConfiguration(LootMasterModule hrtModule) : base(hrtModule.InternalName, hrtModule.Name)
    {
        Ui = new ConfigUi(this);
    }
    public override void AfterLoad()
    {
        if (_fullyLoaded)
            return;
        if (Data.Version > TargetVersion)
        {
            const string msg = "Tried loading a configuration from a newer version of the plugin." +
                               "\nTo prevent data loss operation has been stopped.\nYou need to update to use this plugin!";
            PluginLog.Fatal(msg);
            ServiceManager.ChatGui.PrintError($"[HimbeerToniRaidTool]\n{msg}");
            throw new NotSupportedException($"[HimbeerToniRaidTool]\n{msg}");
        }
        _fullyLoaded = true;
    }
    internal sealed class ConfigUi : IHrtConfigUi
    {
        private readonly LootMasterConfiguration _config;
        private ConfigData _dataCopy;
        private UiSortableList<LootRule> _lootList;
        private static float ScaleFactor => HrtWindow.ScaleFactor;

        internal ConfigUi(LootMasterConfiguration config)
        {
            _config = config;
            _dataCopy = config.Data.Clone();
            _lootList = new UiSortableList<LootRule>(LootRuling.PossibleRules, _dataCopy.LootRuling.RuleSet);
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
                    "Opens group overview window when you log in"));
                ImGui.Checkbox(Localize("Config:Lootmaster:IconInGroupOverview", "Show item icon in group overview"),
                    ref _dataCopy.ShowIconInGroupOverview);
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
                ImGui.Text($"{Localize("Config:Lootmaster:ItemFormat", "Item format")}:");
                ImGui.SameLine();
                string copy = _dataCopy.UserItemFormat;
                if (ImGui.InputText("##format", ref copy, 50))
                    _dataCopy.UserItemFormat = copy;
                ImGui.Text($"{Localize("Config:Lootmaster:ItemFormat:Available", "Available options")}: {{ilvl}} {{source}} {{slot}}");
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
                ImGui.Checkbox(Localize("Config:Lootmaster:IgnoreMateriaForBis",
                    "ShouldIgnore Materia"), ref _dataCopy.IgnoreMateriaForBiS);
                ImGuiHelper.AddTooltip(Localize("Config:Lootmaster:IgnoreMateriaForBisTooltip",
                    "ShouldIgnore Materia when determining if an item is equivalent to BiS"));
                ImGui.Checkbox(Localize("UpdateBisONStartUp", "Update sets from etro.gg periodically"), ref _dataCopy.UpdateEtroBisOnStartup);
                ImGui.SetNextItemWidth(150f * ScaleFactor);
                if (ImGui.InputInt(Localize("BisUpdateInterval", "Update interval (days)"), ref _dataCopy.EtroUpdateIntervalDays))
                    if (_dataCopy.EtroUpdateIntervalDays < 1)
                        _dataCopy.EtroUpdateIntervalDays = 1;
                ImGui.Text(Localize("DefaultBiSHeading", "Default BiS sets (as etro.gg ID)"));
                ImGui.TextWrapped(Localize("DefaultBiSDisclaimer",
                    "These sets are used when creating a new character or adding a new job. These do not affect already created characters and jobs."));
                var jobs = Enum.GetValues<Job>().Where(j => j.IsCombatJob()).ToArray();
                Array.Sort(jobs, (a, b) =>
                {
                    bool aFilled = !_dataCopy.GetDefaultBiS(a).IsNullOrEmpty();
                    bool aOverriden = _dataCopy.BisUserOverride.ContainsKey(a);
                    bool bFilled = !_dataCopy.GetDefaultBiS(b).IsNullOrEmpty();
                    bool bOverriden = _dataCopy.BisUserOverride.ContainsKey(b);
                    if (aOverriden && !bOverriden)
                        return -1;
                    if (!aOverriden && bOverriden)
                        return 1;
                    if (aFilled && !bFilled)
                        return -1;
                    if (!aFilled && bFilled)
                        return 1;
                    return string.Compare(a.ToString(), b.ToString(), StringComparison.Ordinal);

                });
                foreach (Job c in jobs)
                {
                    bool isOverriden = _dataCopy.BisUserOverride.ContainsKey(c);
                    string value = _dataCopy.GetDefaultBiS(c);
                    if (ImGui.InputText(c.ToString(), ref value, 100))
                    {
                        if (value != ConfigData.DefaultBis[c])
                        {
                            if (isOverriden)
                                _dataCopy.BisUserOverride[c] = value;
                            else
                                _dataCopy.BisUserOverride.Add(c, value);
                        }
                        else
                        {
                            if (isOverriden)
                                _dataCopy.BisUserOverride.Remove(c);
                        }

                    }
                    if (isOverriden)
                    {
                        ImGui.SameLine();
                        if (ImGuiHelper.Button(Dalamud.Interface.FontAwesomeIcon.Undo,
                            $"Reset{c}", Localize("Reset to default", "Reset to default")))
                            _dataCopy.BisUserOverride.Remove(c);
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
                _lootList.Draw();
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
            _lootList = new UiSortableList<LootRule>(LootRuling.PossibleRules, _dataCopy.LootRuling.RuleSet);
        }

        public void Save()
        {
            _dataCopy.LootRuling.RuleSet = new List<LootRule>(_lootList.List);
            _config.Data = _dataCopy;
        }
    }
    [JsonObject(MemberSerialization = MemberSerialization.OptIn, ItemNullValueHandling = NullValueHandling.Ignore)]
    [SuppressMessage("ReSharper", "RedundantDefaultMemberInitializer")]
    internal sealed class ConfigData
    {
        /// <summary>
        /// Holds a list of Etro IDs to use as BiS sets if users did not enter a preferred BiS
        /// </summary>
        [JsonIgnore]
        internal static Dictionary<Job, string> DefaultBis { get; } = new Dictionary<Job, string>
        {
            { Job.AST, "83845599-bb32-4539-b829-971501856d7e" },
            { Job.BLM, "1d113f03-16e3-4a47-83a9-c3366a0fff84" },
            { Job.BRD, "f2426d1e-2da8-4151-bf52-74ca67b5f4a2" },
            { Job.DNC, "50746158-5be1-4972-82f4-84a577f4bcce" },
            { Job.DRG, "8a907f52-75a4-4085-9deb-6a63ffa2abd8" },
            { Job.DRK, "dcd2eb34-7c43-4840-a17b-2eb790f19cf4" },
            { Job.GNB, "1dee5389-9906-4690-88b7-55419a342932" },
            { Job.MCH, "0001cd0d-ee54-4b85-8bb6-8ed79e9e7745" },
            { Job.MNK, "" },
            { Job.NIN, "6556da3a-4514-439e-b4f4-07e0ccc85e93" },
            { Job.PLD, "5d279513-f339-402c-8343-fa910e65a4d4" },
            { Job.RDM, "6d7a091d-52f5-49ec-9b2e-d7b1d4c45733" },
            { Job.RPR, "3c8ec7ad-ccfc-42ce-a129-13bd032e2220" },
            { Job.SAM, "d4b6bfc6-a82f-4732-8e55-7c13e094fc1d" },
            { Job.SCH, "d7b63d98-5c7f-4b3a-bc0c-f99eb049a8d4" },
            { Job.SGE, "efc239cb-6371-4d1e-b645-8dd7600575b5" },
            { Job.SMN, "66f5ec54-c062-467f-811f-5e77a90c7aba" },
            { Job.WAR, "1103c082-1c80-4bf3-bb56-83734971d5ea" },
            { Job.WHM, "aee5c1f4-5e59-47fb-88ff-3eeffbef6231" },
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
        public bool ShowIconInGroupOverview = false;
        [JsonProperty]
        public Vector4[] ItemLevelColors = new Vector4[]
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
        public Dictionary<Job, string> BisUserOverride = new();
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
                        new(LootRuleEnum.DPSGain),
                        new(LootRuleEnum.HighestItemLevelGain),
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
                _itemFormatStringCache = null;
            }
        }
        [JsonIgnore]
        private string? _itemFormatStringCache;
        [JsonIgnore]
        public string ItemFormatString => _itemFormatStringCache ??= ParseItemFormatString(UserItemFormat);
        [JsonProperty]
        public int LastGroupIndex = 0;
        [JsonProperty("RaidTierIndex")]
        public int? RaidTierOverride = null;
        [JsonIgnore]
        public RaidTier SelectedRaidTier => Common.Services.ServiceManager.GameInfo.CurrentExpansion.SavageRaidTiers[RaidTierOverride ?? ^1];
        public string GetDefaultBiS(Job c) => BisUserOverride.TryGetValue(c, out string? userOverride)
            ? userOverride
            : DefaultBis.TryGetValue(c, out string? defaultBis) ? defaultBis : "";
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
                }
            }
            return string.Join(' ', result);
        }
    }
}
