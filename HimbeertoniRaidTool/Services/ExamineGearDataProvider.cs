using Dalamud.Game.ClientState.Objects.SubKinds;
using Dalamud.Hooking;
using Dalamud.Plugin.Services;
using FFXIVClientStructs.FFXIV.Client.Game;
using FFXIVClientStructs.FFXIV.Client.UI.Agent;
using FFXIVClientStructs.FFXIV.Component.GUI;

namespace HimbeertoniRaidTool.Plugin.Services;

internal class ExamineGearDataProvider : IGearDataProvider
{
    private readonly Hook<CharacterInspectOnRefresh>? _hook;

    private GearDataProviderConfiguration _configuration;

    internal ExamineGearDataProvider(IGameInteropProvider iopProvider)
    {
        try
        {
            unsafe
            {
                _hook = iopProvider.HookFromSignature<CharacterInspectOnRefresh>(
                    "48 89 5C 24 ?? 57 48 83 EC 20 49 8B D8 48 8B F9 4D 85 C0 0F 84 ?? ?? ?? ?? 85 D2",
                    OnExamineRefresh);
            }
        }
        catch (Exception e)
        {
            _hook = null;
            ServiceManager.Logger.Error(e, "Unable to load examine hook");
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

    private unsafe byte OnExamineRefresh(AtkUnitBase* atkUnitBase, int a2, AtkValue* loadingStage)
    {
        byte result = _hook!.Original(atkUnitBase, a2, loadingStage);
        if (loadingStage is null || a2 <= 0 || loadingStage->UInt != 3) return result;
        GetItemInfos();
        return result;
    }

    private void GetItemInfos()
    {
        if (!_configuration.Enabled)
            return;
        uint objId;
        unsafe
        {
            objId = AgentInspect.Instance()->CurrentObjectID;
        }
        if (ServiceManager.ObjectTable.SearchById(objId) is not PlayerCharacter
            sourceChar) return;
        ServiceManager.Logger.Debug("Examine character found");
        if (!ServiceManager.HrtDataManager.Ready)
        {
            ServiceManager.Logger.Error(
                $"Database is busy. Did not update gear for:{sourceChar.Name}@{sourceChar.HomeWorld.GameData?.Name}");
            return;
        }

        //Do not execute on characters not already known
        if (!ServiceManager.HrtDataManager.CharDb.SearchCharacter(sourceChar.HomeWorld.Id, sourceChar.Name.TextValue,
                                                                  out Character? targetChar))
        {
            ServiceManager.Logger.Debug(
                $"Did not find character in db:{sourceChar.Name}@{sourceChar.HomeWorld.GameData?.Name}");
            return;
        }

        //Save characters ContentID if not already known
        if (targetChar.CharId == 0)
            targetChar.CharId =
                Character.CalcCharId(ServiceManager.CharacterInfoService.GetContentId(sourceChar));


        Job targetJob = sourceChar.GetJob();
        if (targetJob.IsCombatJob() && !_configuration.CombatJobsEnabled
         || targetJob.IsDoH() && !_configuration.DoHEnabled
         || targetJob.IsDoL() && !_configuration.DoLEnabled)
            return;
        PlayableClass? targetClass = targetChar[targetJob];
        if (targetClass == null)
        {
            if (!ServiceManager.HrtDataManager.Ready)
            {
                ServiceManager.Logger.Error(
                    $"Database is busy. Did not update gear for:{targetChar.Name}@{targetChar.HomeWorld?.Name}");
                return;
            }

            targetClass = targetChar.AddClass(targetJob);
            string bisEtroId = ServiceManager.ConnectorPool.EtroConnector.GetDefaultBiS(targetJob);
            if (ServiceManager.HrtDataManager.GearDb.TryGetSetByEtroId(bisEtroId, out GearSet? existingBis))
                targetClass.CurBis = existingBis;
            else
            {
                targetClass.CurBis.EtroId = bisEtroId;
                if (ServiceManager.HrtDataManager.GearDb.TryAdd(targetClass.CurBis))
                    ServiceManager.ConnectorPool.EtroConnector.GetGearSetAsync(targetClass.CurBis);
            }
            if (!ServiceManager.HrtDataManager.GearDb.TryAdd(targetClass.CurGear))
            {
                ServiceManager.Logger.Error(
                    $"Could not create gearset for new job {targetJob} for {targetChar.Name}@{targetChar.HomeWorld?.Name}");
                return;
            }
        }

        //Getting level does not work in level synced content
        if (sourceChar.Level > targetClass.Level)
            targetClass.Level = sourceChar.Level;
        try
        {
            CsHelpers.UpdateGearFromInventoryContainer(InventoryType.Examine, targetClass,
                                                       _configuration.MinILvlDowngrade);
            ServiceManager.Logger.Information($"Updated Gear for: {targetChar.Name} @ {targetChar.HomeWorld?.Name}");
        }
        catch (Exception e)
        {
            ServiceManager.Logger.Error(e,
                                        $"Something went wrong while updating gear for:{targetChar.Name} @ {targetChar.HomeWorld?.Name}");
        }
    }

    private unsafe delegate byte CharacterInspectOnRefresh(AtkUnitBase* atkUnitBase, int a2, AtkValue* a3);
}