using System.Diagnostics.CodeAnalysis;
using Dalamud.Game.ClientState.Objects.SubKinds;
using Dalamud.Plugin.Services;
using FFXIVClientStructs.FFXIV.Client.Game;
using HimbeertoniRaidTool.Plugin.DataManagement;

namespace HimbeertoniRaidTool.Plugin.Services;

internal class OwnCharacterDataProvider : IGearDataProvider
{
    private readonly IClientState _clientState;
    private readonly IFramework _framework;
    private readonly TimeSpan _timeBetweenGearUpdates = new(0, 5, 0);
    private readonly TimeSpan _timeBetweenWalletUpdates = new(30 * TimeSpan.TicksPerSecond);

    private readonly HashSet<Currency> _trackedCurrencies =
    [
        Currency.Gil,
        Currency.TomestoneOfCausality,
        Currency.TomestoneOfComedy,
    ];
    private GearDataProviderConfiguration _config = GearDataProviderConfiguration.Disabled;
    private Character? _curChar;

    private bool _disposed;
    private uint _lastSeenJob;
    private IPlayerCharacter? _self;

    private TimeSpan _timeSinceLastGearUpdate;
    private TimeSpan _timeSinceLastWalletUpdate;
    public OwnCharacterDataProvider(IClientState clientState, IFramework framework)
    {
        _clientState = clientState;
        _framework = framework;
        _clientState.Login += OnLogin;
        _clientState.Logout += OnLogout;
        _framework.Update += OnFrameworkUpdate;
    }
    public void Enable(GearDataProviderConfiguration config)
    {
        if (_disposed) return;
        _config = config;
        //Ensure character is set up regardless when this is enabled
        if (_clientState.IsLoggedIn)
            OnLogin();
    }
    public void Disable() => _config = GearDataProviderConfiguration.Disabled;
    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        _clientState.Login -= OnLogin;
        _clientState.Logout -= OnLogout;
        _framework.Update -= OnFrameworkUpdate;
        OnLogout(0, 0);
    }
    private void OnLogin() => GetChar(out _curChar, out _self);
    private void OnLogout(int type, int code)
    {
        _curChar = null;
        _self = null;
    }
    private void OnFrameworkUpdate(IFramework framework)
    {
        if (_disposed || !_config.Enabled) return;
        _timeSinceLastWalletUpdate += framework.UpdateDelta;
        _timeSinceLastGearUpdate += framework.UpdateDelta;
        if (_timeSinceLastWalletUpdate > _timeBetweenWalletUpdates)
            UpdateWallet();
        if (_timeSinceLastGearUpdate > _timeBetweenGearUpdates || _lastSeenJob != (_self?.ClassJob.RowId ?? 0))
            UpdateGear();
        _lastSeenJob = _self?.ClassJob.RowId ?? 0;
    }
    private static void GetChar([NotNullWhen(true)] out Character? target,
                                [NotNullWhen(true)] out IPlayerCharacter? source)
    {
        target = null;
        source = ServiceManager.ClientState.LocalPlayer;
        if (source == null) return;

        ulong charId = Character.CalcCharId(ServiceManager.ClientState.LocalContentId);

        if (!ServiceManager.HrtDataManager.CharDb.Search(
                CharacterDb.GetStandardPredicate(charId, source.HomeWorld.RowId, source.Name.TextValue),
                out target)) return;
        if (target.CharId == 0)
            target.CharId = charId;
    }

    private unsafe void UpdateWallet()
    {
        if (_curChar == null) return;
        var container = InventoryManager.Instance()->GetInventoryContainer(InventoryType.Currency);
        for (int i = 0; i < container->Size; i++)
        {
            InventoryItem item = container->Items[i];
            var type = (Currency)item.ItemId;
            if (_trackedCurrencies.Contains(type))
            {
                _curChar.Wallet[type] = item.Quantity;
            }
        }
        _timeSinceLastWalletUpdate = TimeSpan.Zero;
    }
    private void UpdateGear()
    {
        if (_curChar == null || _self == null) return;
        Job job = _self.GetJob();
        if (job.IsCombatJob() && !_config.CombatJobsEnabled) return;
        if (job.IsDoH() && !_config.DoHEnabled) return;
        if (job.IsDoL() && !_config.DoLEnabled) return;
        PlayableClass targetClass = _curChar[job] ?? _curChar.AddClass(job);
        if (targetClass.Level < _self.Level) targetClass.Level = _self.Level;
        CsHelpers.UpdateGearFromInventoryContainer(InventoryType.EquippedItems, targetClass, _config.MinILvlDowngrade);
        _timeSinceLastGearUpdate = TimeSpan.Zero;
    }
}