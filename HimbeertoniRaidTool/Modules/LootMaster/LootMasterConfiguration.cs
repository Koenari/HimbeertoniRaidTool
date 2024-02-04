using System.Numerics;
using HimbeertoniRaidTool.Common;
using HimbeertoniRaidTool.Common.Data;
using HimbeertoniRaidTool.Common.Security;
using HimbeertoniRaidTool.Plugin.Localization;
using HimbeertoniRaidTool.Plugin.UI;
using ImGuiNET;
using Newtonsoft.Json;
using ServiceManager = HimbeertoniRaidTool.Common.Services.ServiceManager;

namespace HimbeertoniRaidTool.Plugin.Modules.LootMaster;

internal class LootMasterConfiguration : HrtConfiguration<LootMasterConfiguration.ConfigData>, IHrtConfiguration
{
    private const int TARGET_VERSION = 2;

    private bool _fullyLoaded;
    public LootMasterConfiguration(IHrtModule hrtModule) : base(hrtModule.InternalName, hrtModule.Name)
    {
        Ui = new ConfigUi(this);

    }
    public override ConfigUi Ui { get; }
    public override void AfterLoad()
    {
        if (_fullyLoaded)
            return;
        if (Data.Version > TARGET_VERSION)
        {
            const string msg = "Tried loading a configuration from a newer version of the plugin." +
                               "\nTo prevent data loss operation has been stopped.\nYou need to update to use this plugin!";
            Services.ServiceManager.PluginLog.Fatal(msg);
            Services.ServiceManager.Chat.PrintError($"[HimbeerToniRaidTool]\n{msg}");
            throw new NotSupportedException($"[HimbeerToniRaidTool]\n{msg}");
        }
        Upgrade();
        _fullyLoaded = true;
    }

    private void Upgrade()
    {
        while (Data.Version < TARGET_VERSION)
        {
            int oldVersion = Data.Version;
            DoUpgradeStep();
            if (Data.Version > oldVersion)
                continue;
            string msg = $"Error upgrading Lootmaster configuration from version {oldVersion}";
            Services.ServiceManager.PluginLog.Fatal(msg);
            Services.ServiceManager.Chat.PrintError($"[HimbeerToniRaidTool]\n{msg}");
            throw new InvalidOperationException(msg);


        }
    }

    private void DoUpgradeStep()
    {
        switch (Data.Version)
        {
            case 1:
                Data.RaidGroups.Clear();
#pragma warning disable CS0612
                Data.RaidGroups = Services.ServiceManager.HrtDataManager.Groups;
#pragma warning restore CS0612
                Data.Version = 2;
                break;
        }
    }

    internal sealed class ConfigUi : IHrtConfigUi
    {
        private readonly LootMasterConfiguration _config;
        private ConfigData _dataCopy;
        private UiSortableList<LootRule> _lootList;

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
            ImGui.BeginTabBar("##LootMaster");
            if (ImGui.BeginTabItem(CoreLoc.Config_Appearance))
            {
                ImGui.Checkbox(LootmasterLoc.Config_input_OpenOnLogin, ref _dataCopy.OpenOnStartup);
                ImGuiHelper.AddTooltip(LootmasterLoc.Config_input_OpenOnLogin_tt);
                ImGui.Checkbox(LootmasterLoc.Config_input_IgnoreMateriaForBis,
                               ref _dataCopy.IgnoreMateriaForBiS);
                ImGuiHelper.AddTooltip(LootmasterLoc.Config_Lootmaster_IgnoreMateriaForBisTooltip);
                ImGui.Checkbox(LootmasterLoc.Config_Lootmaster_IconInGroupOverview,
                               ref _dataCopy.ShowIconInGroupOverview);
                ImGui.Checkbox(LootmasterLoc.Config_Lootmaster_ColoredItemNames, ref _dataCopy.ColoredItemNames);
                ImGuiHelper.AddTooltip(LootmasterLoc.Lootmaster_ColoredItemNamesTooltip);
                ImGui.BeginDisabled(!_dataCopy.ColoredItemNames);
                ImGui.Text(LootmasterLoc.Config_Lootmaster_Colors);
                ImGui.NewLine();
                uint iLvL = _config.Data.SelectedRaidTier.ArmorItemLevel;
                ImGui.SetCursorPosX(ImGui.GetCursorPosX() + ImGui.CalcTextSize($"{iLvL} > ").X);
                ImGui.Text($"{GeneralLoc.iLvl} >= {iLvL}");
                ImGui.SameLine();
                ImGui.ColorEdit4("##Color0", ref _dataCopy.ItemLevelColors[0]);
                ImGui.Text($"{iLvL} > {GeneralLoc.iLvl} >= {iLvL - 10}");
                ImGui.SameLine();
                ImGui.ColorEdit4("##Color1", ref _dataCopy.ItemLevelColors[1]);
                ImGui.Text($"{iLvL - 10} > {GeneralLoc.iLvl} >= {iLvL - 20}");
                ImGui.SameLine();
                ImGui.ColorEdit4("##Color2", ref _dataCopy.ItemLevelColors[2]);
                ImGui.Text($"{iLvL - 20} > {GeneralLoc.iLvl}");
                ImGui.SameLine();
                ImGui.SetCursorPosX(ImGui.GetCursorPosX() + ImGui.CalcTextSize($" >= {iLvL - 20}").X);
                ImGui.ColorEdit4("##Color3", ref _dataCopy.ItemLevelColors[3]);
                ImGui.EndDisabled();
                ImGui.Separator();
                ImGui.Text(LootmasterLoc.Config_Lootmaster_ItemFormat);
                ImGui.SameLine();
                string copy = _dataCopy.UserItemFormat;
                if (ImGui.InputText("##format", ref copy, 50))
                    _dataCopy.UserItemFormat = copy;
                ImGui.Text(
                    $"{LootmasterLoc.Config_Lootmaster_ItemFormat_Available}: {{ilvl}} {{source}} {{slot}}");
                ImGui.Separator();
                ImGui.Text(LootmasterLoc.Config_hdg_Examples);
                for (int i = 0; i < 4; i++)
                {
                    (long curiLvL, string source, string slot) = (iLvL - 10 * i, ((ItemSource)i).FriendlyName(),
                        ((GearSetSlot)(i * 2)).FriendlyName());
                    if (_dataCopy.ColoredItemNames)
                        ImGui.TextColored(_dataCopy.ItemLevelColors[i],
                                          string.Format(_dataCopy.ItemFormatString + "  ", curiLvL, source, slot));
                    else
                        ImGui.Text(string.Format(_dataCopy.ItemFormatString + "  ", curiLvL, source, slot));
                    ImGui.SameLine();
                }
                ImGui.EndTabItem();
            }
            if (ImGui.BeginTabItem("Loot"))
            {
                ImGui.Text(LootmasterLoc.LootRuleOrder);
                _lootList.Draw();
                ImGui.Separator();
                ImGui.Text(LootmasterLoc.ConfigRolePriority);
                ImGui.Text($"{LootmasterLoc.Current_priority}: {_dataCopy.RolePriority}");
                _dataCopy.RolePriority.DrawEdit(ImGui.InputInt);
                ImGui.EndTabItem();
            }
            ImGui.EndTabBar();
        }

        public void OnHide() { }

        public void OnShow()
        {
            _config.Data.BeforeSave();
            _dataCopy = _config.Data.Clone();
            _dataCopy.AfterLoad();
            _lootList = new UiSortableList<LootRule>(LootRuling.PossibleRules, _dataCopy.LootRuling.RuleSet);
        }

        public void Save()
        {
            _dataCopy.LootRuling.RuleSet = new List<LootRule>(_lootList.List);
            _dataCopy.BeforeSave();
            _config.Data = _dataCopy;
            _config.Data.AfterLoad();
        }
    }

    [JsonObject(MemberSerialization = MemberSerialization.OptIn, ItemNullValueHandling = NullValueHandling.Ignore)]
    internal sealed class ConfigData : IHrtConfigData
    {
        [JsonIgnore]
        private string? _itemFormatStringCache;
        [JsonProperty("RaidGroupIds", ObjectCreationHandling = ObjectCreationHandling.Replace)]
        private List<HrtId> _raidGroupIds = new();
        [JsonProperty("UserItemFormat")]
        private string _userItemFormat = "{source} {slot}";
        [JsonProperty]
        public bool ColoredItemNames = true;
        [JsonProperty]
        public bool IgnoreMateriaForBiS;
        [JsonProperty]
        public Vector4[] ItemLevelColors =
        {
            //At or above cur max iLvl
            new(0.17f, 0.85f, 0.17f, 1f),
            //10 below
            new(0.5f, 0.83f, 0.72f, 1f),
            //20 below
            new(0.85f, 0.85f, 0.17f, 1f),
            //30 or more below
            new(0.85f, 0.17f, 0.17f, 1f),
        };
        [JsonProperty]
        public int LastGroupIndex;
        /*
         * Loot
         */
        [JsonProperty(ObjectCreationHandling = ObjectCreationHandling.Replace)]
        public LootRuling LootRuling = new()
        {
            RuleSet = new List<LootRule>
            {
                new(LootRuleEnum.BisOverUpgrade),
                new(LootRuleEnum.RolePrio),
                new(LootRuleEnum.DpsGain),
                new(LootRuleEnum.HighestItemLevelGain),
                new(LootRuleEnum.LowestItemLevel),
                new(LootRuleEnum.Random),
            },
        };
        /*
         * Appearance
         */
        [JsonProperty]
        public bool OpenOnStartup;
        [JsonIgnore]
        public List<RaidGroup> RaidGroups = new();
        [JsonProperty("RaidTierIndex")]
        public int? RaidTierOverride;
        [JsonProperty(ObjectCreationHandling = ObjectCreationHandling.Replace)]
        public RolePriority RolePriority = new()
        {
            { Role.Melee, 0 },
            { Role.Caster, 1 },
            { Role.Ranged, 1 },
            { Role.Tank, 3 },
            { Role.Healer, 4 },
        };
        [JsonProperty]
        public bool ShowIconInGroupOverview;
        [JsonProperty]
        public int Version { get; set; } = 1;
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
        public string ItemFormatString => _itemFormatStringCache ??= ParseItemFormatString(UserItemFormat);
        [JsonIgnore]
        public RaidTier SelectedRaidTier =>
            ServiceManager.GameInfo.CurrentExpansion.SavageRaidTiers[RaidTierOverride ?? ^1];

        public void AfterLoad()
        {
            RaidGroups.Clear();
            foreach (HrtId id in _raidGroupIds)
            {
                if (Services.ServiceManager.HrtDataManager.RaidGroupDb.TryGet(id, out RaidGroup? group))
                    RaidGroups.Add(group);
            }
        }

        public void BeforeSave() => _raidGroupIds = RaidGroups.ConvertAll(g => g.LocalId);
        private static string ParseItemFormatString(string input)
        {
            List<string> result = new();
            string[] split = input.Replace("}{", "} {").Split(' ');
            foreach (string item in split)
            {
                switch (item.ToLower())
                {
                    case "{ilvl}":
                        result.Add("{0}");
                        break;
                    case "{source}":
                        result.Add("{1}");
                        break;
                    case "{slot}":
                        result.Add("{2}");
                        break;
                }
            }
            return string.Join(' ', result);
        }
    }
}