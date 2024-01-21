using Dalamud.Game.ClientState.Objects.SubKinds;
using Dalamud.Plugin.Services;
using FFXIVClientStructs.FFXIV.Client.Game;
using HimbeertoniRaidTool.Common.Data;
using HimbeertoniRaidTool.Plugin.DataExtensions;
using System.Diagnostics.CodeAnalysis;

namespace HimbeertoniRaidTool.Plugin.Services;

internal static class OwnCharacterDataProvider
{
    private static Character? _curChar = null;
    private static PlayerCharacter? _self = null;
    private static Configuration _config = new(false, false, false, false);
    private static readonly HashSet<Currency> _trackedCurrencies = new()
    {
        Currency.Gil,
        Currency.TomestoneOfCausality,
        Currency.TomestoneOfComedy,
    };
    private static TimeSpan _timeSinceLastWalletUpdate;
    private static readonly TimeSpan _timeBetweenWalletUpdates = new(30 * TimeSpan.TicksPerSecond);

    private static TimeSpan _timeSinceLastGearUpdate;
    private static readonly TimeSpan _timeBetweenGearUpdates = new(0, 5, 0);
    private static uint _lastSeenJob = 0;

    private static bool _initialized = false;
    public static void Initialize(IClientState clientState, IFramework framework)
    {
        if (_initialized) return;
        _initialized = true;
        clientState.Login += OnLogin;
        clientState.Logout += OnLogout;
        if (clientState.IsLoggedIn)
            OnLogin();
        framework.Update += OnFrameworkUpdate;
    }
    internal static void SetConfig(Configuration config) => _config = config;
    internal static void Destroy(IClientState clientState, IFramework framework)
    {
        if (!_initialized) return;
        _initialized = false;
        clientState.Login -= OnLogin;
        clientState.Logout -= OnLogout;
        framework.Update -= OnFrameworkUpdate;
        OnLogout();
    }
    private static void OnLogin() => GetChar(out _curChar, out _self);
    private static void OnLogout()
    {
        _curChar = null;
        _self = null;
    }
    private static void OnFrameworkUpdate(IFramework framework)
    {
        if (!_initialized || !_config.Enabled) return;
        _timeSinceLastWalletUpdate += framework.UpdateDelta;
        _timeSinceLastGearUpdate += framework.UpdateDelta;
        if (_timeSinceLastWalletUpdate > _timeBetweenWalletUpdates)
            UpdateWallet();
        if (_timeSinceLastGearUpdate > _timeBetweenGearUpdates || _lastSeenJob != (_self?.ClassJob.Id ?? 0))
            UpdateGear();
        _lastSeenJob = _self?.ClassJob.Id ?? 0;
    }
    private static bool GetChar([NotNullWhen(true)] out Character? target,
        [NotNullWhen(true)] out PlayerCharacter? source)
    {
        target = null;
        source = ServiceManager.ClientState.LocalPlayer;
        if (source == null)
            return false;

        ulong charId = Character.CalcCharId(ServiceManager.ClientState.LocalContentId);
        if (ServiceManager.HrtDataManager.CharDb.TryGetCharacterByCharId(charId, out target))
            return true;
        if (ServiceManager.HrtDataManager.CharDb.SearchCharacter(source.HomeWorld.Id, source.Name.TextValue,
                out target))
        {
            target.CharId = charId;
            return true;
        }
        return false;
    }

    private static unsafe void UpdateWallet()
    {
        if (_curChar == null) return;
        var container = InventoryManager.Instance()->GetInventoryContainer(InventoryType.Currency);
        for (int i = 0; i < container->Size; i++)
        {
            InventoryItem item = container->Items[i];
            var type = (Currency)item.ItemID;
            if (_trackedCurrencies.Contains(type))
            {
                _curChar.Wallet[type] = item.Quantity;
            }
        }
        _timeSinceLastWalletUpdate = TimeSpan.Zero;
    }
    private static void UpdateGear()
    {
        if (_curChar == null || _self == null) return;
        Job job = _self.GetJob();
        if (job.IsCombatJob() && !_config.CombatJobsEnabled) return;
        if (job.IsDoH() && !_config.DoHEnabled) return;
        if (job.IsDoL() && !_config.DoLEnabled) return;
        PlayableClass targetClass = _curChar[job] ?? _curChar.AddClass(job);
        if (targetClass.Level < _self.Level) targetClass.Level = _self.Level;
        CsHelpers.UpdateGearFromInventoryContainer(InventoryType.EquippedItems, targetClass);
        _timeSinceLastGearUpdate = TimeSpan.Zero;
    }

    public readonly struct Configuration
    {
        public readonly bool Enabled;
        public readonly bool CombatJobsEnabled;
        public readonly bool DoHEnabled;
        public readonly bool DoLEnabled;

        public Configuration(bool enabled, bool combat, bool doh, bool dol)
        {
            Enabled = enabled;
            CombatJobsEnabled = combat;
            DoHEnabled = doh;
            DoLEnabled = dol;
        }
    }

}