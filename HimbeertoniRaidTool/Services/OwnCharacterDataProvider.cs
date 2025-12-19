using Dalamud.Plugin.Services;
using FFXIVClientStructs.FFXIV.Client.Game;
using HimbeertoniRaidTool.Common.Extensions;
using HimbeertoniRaidTool.Plugin.DataManagement;
using Serilog;

namespace HimbeertoniRaidTool.Plugin.Services;

internal class OwnCharacterDataProvider : IGearDataProvider
{
    private readonly IPlayerState _playerState;
    private readonly IClientState _clientState;
    private readonly IFramework _framework;
    private readonly ILogger _logger;
    private readonly HrtDataManager _hrtDataManager;
    private readonly TimeSpan _timeBetweenGearUpdates = TimeSpan.FromMinutes(5);
    private readonly TimeSpan _timeBetweenWalletUpdates = TimeSpan.FromSeconds(30);

    private GearDataProviderConfiguration _config = GearDataProviderConfiguration.Disabled;
    private Character? _curChar;
    private ulong _curCharContentId;

    private bool _disposed;

    private TimeSpan _timeSinceLastGearUpdate;
    private TimeSpan _timeSinceLastWalletUpdate;
    public OwnCharacterDataProvider(IPlayerState playerState, IClientState clientState, IFramework framework,
                                    ILogger logger,
                                    HrtDataManager hrtDataManager)
    {
        _playerState = playerState;
        _clientState = clientState;
        _framework = framework;
        _logger = logger;
        _hrtDataManager = hrtDataManager;
        _timeSinceLastGearUpdate = _timeBetweenGearUpdates;
        _timeSinceLastWalletUpdate = _timeBetweenWalletUpdates;
        _clientState.ClassJobChanged += UpdateJobAndGear;
        _clientState.LevelChanged += UpdateJobAndGear;
        _framework.Update += OnFrameworkUpdate;
    }
    public void Enable(GearDataProviderConfiguration config)
    {
        if (_disposed) return;
        _config = config;
    }
    public void Disable() => _config = GearDataProviderConfiguration.Disabled;
    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        _clientState.ClassJobChanged -= UpdateJobAndGear;
        _clientState.LevelChanged -= UpdateJobAndGear;
        _framework.Update -= OnFrameworkUpdate;
    }
    private void OnFrameworkUpdate(IFramework framework)
    {
        if (_disposed || !_config.Enabled || !_playerState.IsLoaded) return;
        _timeSinceLastWalletUpdate += framework.UpdateDelta;
        _timeSinceLastGearUpdate += framework.UpdateDelta;
        if (_timeSinceLastWalletUpdate < _timeBetweenWalletUpdates) return;
        UpdateChar();
        UpdateWallet();
        if (_timeSinceLastGearUpdate > _timeBetweenGearUpdates)
            UpdateJobAndGear(_playerState.ClassJob.RowId);

    }
    private void UpdateChar()
    {
        if (!_playerState.IsLoaded)
        {
            _curChar = null;
            _curCharContentId = 0;
            return;
        }
        if (_curCharContentId == _playerState.ContentId) return;

        _curChar = null;
        _curCharContentId = 0;
        ulong charId = Character.CalcCharId(_playerState.ContentId);
        if (!_hrtDataManager.GetTable<Character>().Search(
                CharacterDb.GetStandardPredicate(charId, _playerState.HomeWorld.RowId, _playerState.CharacterName),
                out _curChar)) return;
        if (_curChar.CharId == 0)
            _curChar.CharId = charId;
        _curChar.HomeWorldId = _playerState.HomeWorld.RowId;
        _curChar.Name = _playerState.CharacterName;
        _curCharContentId = _playerState.ContentId;
    }

    private unsafe void UpdateWallet()
    {
        if (_curChar == null) return;
        _logger.Debug("UpdateWallet for {charName}", _curChar.Name);
        var container = InventoryManager.Instance()->GetInventoryContainer(InventoryType.Currency);
        for (int i = 0; i < container->Size; i++)
        {
            var item = container->Items[i];
            var type = (Currency)item.ItemId;
            _curChar.Wallet[type] = item.Quantity;
        }
        _timeSinceLastWalletUpdate = TimeSpan.Zero;
    }

    private void UpdateJobAndGear(uint rawJob) => UpdateJobAndGear(rawJob, 0);

    private void UpdateJobAndGear(uint rawJob, uint rawLevel)
    {
        if (!_playerState.IsLoaded || _curChar == null) return;
        var job = (Job)rawJob;
        int level = rawLevel == 0 ? _playerState.Level : (int)rawLevel;
        if (job.IsCombatJob() && !_config.CombatJobsEnabled) return;
        if (job.IsDoH() && !_config.DoHEnabled) return;
        if (job.IsDoL() && !_config.DoLEnabled) return;
        _logger.Debug("UpdateJobAndGear: {Job} {Level}", job, level);
        var targetClass = _curChar[job] ?? _curChar.AddClass(job);
        if (targetClass.Level < level) targetClass.Level = level;
        CsHelpers.UpdateGearFromInventoryContainer(InventoryType.EquippedItems, targetClass, _config.MinILvlDowngrade,
                                                   _logger, _hrtDataManager);
        _timeSinceLastGearUpdate = TimeSpan.Zero;
    }
}