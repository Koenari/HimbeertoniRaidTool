using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using Dalamud.Utility;
using HimbeertoniRaidTool.Common;
using HimbeertoniRaidTool.Common.Data;
using HimbeertoniRaidTool.Common.Security;
using HimbeertoniRaidTool.Plugin.DataExtensions;
using HimbeertoniRaidTool.Plugin.UI;
using ImGuiNET;
using Newtonsoft.Json;
using static HimbeertoniRaidTool.Plugin.Services.Localization;

namespace HimbeertoniRaidTool.Plugin.Modules.LootMaster;

internal class LootMasterConfiguration : HrtConfiguration<LootMasterConfiguration.ConfigData, LootMasterConfiguration.ConfigUi>
{
    public override ConfigUi Ui { get; }

    private bool _fullyLoaded;
    private const int TARGET_VERSION = 2;
    public LootMasterConfiguration(IHrtModule hrtModule) : base(hrtModule.InternalName, hrtModule.Name)
    {
        Ui = new ConfigUi(this);
        
    }
    public override void AfterLoad()
    {
        if (_fullyLoaded)
            return;
        if (Data.Version > TARGET_VERSION)
        {
            const string msg = "Tried loading a configuration from a newer version of the plugin." +
                               "\nTo prevent data loss operation has been stopped.\nYou need to update to use this plugin!";
            ServiceManager.PluginLog.Fatal(msg);
            ServiceManager.ChatGui.PrintError($"[HimbeerToniRaidTool]\n{msg}");
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
            ServiceManager.PluginLog.Fatal(msg);
            ServiceManager.ChatGui.PrintError($"[HimbeerToniRaidTool]\n{msg}");
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
                Data.RaidGroups = ServiceManager.HrtDataManager.Groups;
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
                    "Ignore Materia"), ref _dataCopy.IgnoreMateriaForBiS);
                ImGuiHelper.AddTooltip(Localize("Config:Lootmaster:IgnoreMateriaForBisTooltip",
                    "Ignore Materia when determining if an item is equivalent to BiS"));
                ImGui.Checkbox(Localize("UpdateBisONStartUp", "Update sets from etro.gg periodically"), ref _dataCopy.UpdateEtroBisOnStartup);
                ImGui.SetNextItemWidth(150f * ScaleFactor);
                if (ImGui.InputInt(Localize("BisUpdateInterval", "Update interval (days)"), ref _dataCopy.EtroUpdateIntervalDays))
                    if (_dataCopy.EtroUpdateIntervalDays < 1)
                        _dataCopy.EtroUpdateIntervalDays = 1;
                ImGui.Text(Localize("DefaultBiSHeading", "Default BiS sets (as etro.gg ID)"));
                ImGui.TextWrapped(Localize("DefaultBiSDisclaimer",
                    "These sets are used when creating a new character or adding a new job. These do not affect already created characters and jobs."));
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
    [SuppressMessage("ReSharper", "RedundantDefaultMemberInitializer")]
    internal sealed class ConfigData : IHrtConfigData
    {
        [JsonProperty]
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
            new(0.17f, 0.85f, 0.17f, 1f),
            //10 below
            new(0.5f, 0.83f, 0.72f, 1f),
            //20 below
            new(0.85f, 0.85f, 0.17f, 1f),
            //30 or more below
            new(0.85f, 0.17f, 0.17f, 1f),
        };
        /*
         * BiS
         */
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
                new(LootRuleEnum.BisOverUpgrade),
                new(LootRuleEnum.RolePrio),
                new(LootRuleEnum.DpsGain),
                new(LootRuleEnum.HighestItemLevelGain),
                new(LootRuleEnum.LowestItemLevel),
                new(LootRuleEnum.Random),
            },
        };
        [JsonProperty(ObjectCreationHandling = ObjectCreationHandling.Replace)]
        public RolePriority RolePriority = new()
        {
            { Role.Melee, 0 },
            { Role.Caster, 1 },
            { Role.Ranged, 1 },
            { Role.Tank, 3 },
            { Role.Healer, 4 },
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
        [JsonIgnore]
        public List<RaidGroup> RaidGroups = new();
        [JsonProperty("RaidGroupIds",ObjectCreationHandling = ObjectCreationHandling.Replace)]
        private List<HrtId> _raidGroupIds = new();
        [JsonProperty("RaidTierIndex")]
        public int? RaidTierOverride = null;
        [JsonIgnore]
        public RaidTier SelectedRaidTier => Common.Services.ServiceManager.GameInfo.CurrentExpansion.SavageRaidTiers[RaidTierOverride ?? ^1];
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

        public void AfterLoad()
        {
            RaidGroups.Clear();
            foreach (HrtId id in _raidGroupIds)
            {
                if(ServiceManager.HrtDataManager.RaidGroupDb.TryGet(id,out RaidGroup? group))
                    RaidGroups.Add(group);
            }
        }

        public void BeforeSave()
        {
            _raidGroupIds = RaidGroups.ConvertAll(g => g.LocalId);
        }
    }
}