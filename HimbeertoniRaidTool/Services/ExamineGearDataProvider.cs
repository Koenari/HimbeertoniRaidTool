using Dalamud.Game.ClientState.Objects.SubKinds;
using Dalamud.Hooking;
using Dalamud.Plugin.Services;
using FFXIVClientStructs.FFXIV.Client.Game;
using FFXIVClientStructs.FFXIV.Client.UI;
using FFXIVClientStructs.FFXIV.Client.UI.Agent;
using FFXIVClientStructs.FFXIV.Component.GUI;
using HimbeertoniRaidTool.Plugin.Connectors;
using HimbeertoniRaidTool.Plugin.DataManagement;

namespace HimbeertoniRaidTool.Plugin.Services;

internal class ExamineGearDataProvider : IGearDataProvider
{
    private readonly ILogger _logger;
    private readonly IObjectTable _objectTable;
    private readonly HrtDataManager _hrtDataManager;
    private readonly CharacterInfoService _characterInfoService;
    private readonly ConnectorPool _connectorPool;
    private readonly Hook<AddonCharacterInspect.Delegates.OnRefresh>? _hook;
    private GearDataProviderConfiguration _configuration;

    internal ExamineGearDataProvider(IGameInteropProvider iopProvider, ILogger logger, IObjectTable objectTable,
                                     HrtDataManager hrtDataManager, CharacterInfoService characterInfoService,
                                     ConnectorPool connectorPool)
    {
        _logger = logger;
        _objectTable = objectTable;
        _hrtDataManager = hrtDataManager;
        _characterInfoService = characterInfoService;
        _connectorPool = connectorPool;
        try
        {
            unsafe
            {
                _hook = iopProvider.HookFromSignature<AddonCharacterInspect.Delegates.OnRefresh>(
                    "40 56 57 48 83 EC ?? 49 8B F0 48 8B F9 4D 85 C0 0F 84 ?? ?? ?? ?? 85 D2",
                    OnExamineRefresh);
            }
        }
        catch (Exception e)
        {
            _hook = null;
            _logger.Error(e, "Unable to load examine hook");
        }

    }

    public void Enable(GearDataProviderConfiguration configuration)
    {
        _configuration = configuration;
        _hook?.Enable();
    }
    public void Disable()
    {
        _configuration = GearDataProviderConfiguration.Disabled;
        _hook?.Disable();
    }

    public void Dispose()
    {
        if (_hook is null || _hook.IsDisposed)
            return;
        _hook.Dispose();
    }

    private unsafe bool OnExamineRefresh(AddonCharacterInspect* atkUnitBase, uint a2, AtkValue* loadingStage)
    {
        bool result = _hook!.Original(atkUnitBase, a2, loadingStage);
        if (loadingStage is null || a2 <= 0 || loadingStage->UInt != 3) return result;
        GetItemInfos();
        return result;
    }

    private void GetItemInfos()
    {
        if (!_configuration.Enabled)
            return;
        uint entityId;
        unsafe
        {
            entityId = AgentInspect.Instance()->CurrentEntityId;
        }

        if (_objectTable.SearchByEntityId(entityId) is not IPlayerCharacter
            sourceChar)
        {
            _logger.Error($"Examined character not found in world (eid:{entityId:x8})");
            return;
        }
        _logger.Debug($"Examine character found: {sourceChar.Name}");
        if (!_hrtDataManager.Ready)
        {
            _logger.Error(
                $"Database is busy. Did not update gear for:{sourceChar.Name}@{sourceChar.HomeWorld.Value.Name}");
            return;
        }

        //Do not execute on characters not already known
        if (!_hrtDataManager.CharDb.Search(
                CharacterDb.GetStandardPredicate(0, sourceChar.HomeWorld.RowId, sourceChar.Name.TextValue),
                out var targetChar))
        {
            _logger.Debug($"Did not find character in db:{sourceChar.Name}@{sourceChar.HomeWorld.Value.Name}");
            return;
        }

        //Save characters ContentID if not already known
        if (targetChar.CharId == 0)
            targetChar.CharId =
                Character.CalcCharId(_characterInfoService.GetContentId(sourceChar));


        var targetJob = sourceChar.GetJob();
        if (targetJob.IsCombatJob() && !_configuration.CombatJobsEnabled
         || targetJob.IsDoH() && !_configuration.DoHEnabled
         || targetJob.IsDoL() && !_configuration.DoLEnabled)
            return;
        var targetClass = targetChar[targetJob];
        if (targetClass == null)
        {
            if (!_hrtDataManager.Ready)
            {
                _logger.Error(
                    $"Database is busy. Did not update gear for:{targetChar.Name}@{targetChar.HomeWorld?.Name}");
                return;
            }

            targetClass = targetChar.AddClass(targetJob);
            var defaultBiS = _connectorPool.GetDefaultBiS(targetJob);
            if (_hrtDataManager.GearDb.Search(defaultBiS.Equals, out var existingBis))
                targetClass.CurBis = existingBis;
            else
            {
                targetClass.CurBis = defaultBiS.ToGearSet();
                if (_hrtDataManager.GearDb.TryAdd(targetClass.CurBis)
                 && _connectorPool.TryGetConnector(defaultBiS.Service, out var connector))
                    connector.RequestGearSetUpdate(targetClass.CurBis);
            }
            if (!_hrtDataManager.GearDb.TryAdd(targetClass.CurGear))
            {
                _logger.Error(
                    $"Could not create gearset for new job {targetJob} for {targetChar.Name}@{targetChar.HomeWorld?.Name}");
                return;
            }
        }

        //Getting level does not work in level synced content
        if (sourceChar.Level > targetClass.Level)
            targetClass.Level = sourceChar.Level;
        try
        {
            if (CsHelpers.UpdateGearFromInventoryContainer(InventoryType.Examine, targetClass,
                                                           _configuration.MinILvlDowngrade, _logger, _hrtDataManager))
            {
                _logger.Information($"Updated Gear for: {targetChar.Name} @ {targetChar.HomeWorld?.Name}");
            }
            else
            {
                _logger.Error(
                    $"Something went wrong while updating gear for:{targetChar.Name} @ {targetChar.HomeWorld?.Name}");
            }
        }
        catch (Exception e)
        {
            _logger.Error(e,
                          $"Something went wrong while updating gear for:{targetChar.Name} @ {targetChar.HomeWorld?.Name}");
        }
    }
}