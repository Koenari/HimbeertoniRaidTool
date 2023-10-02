using Dalamud.Game.ClientState.Objects.SubKinds;
using Dalamud.Game.ClientState.Party;
using Dalamud.Hooking;
using Dalamud.Utility.Signatures;
using FFXIVClientStructs.FFXIV.Client.Game;
using FFXIVClientStructs.FFXIV.Client.UI.Agent;
using FFXIVClientStructs.FFXIV.Component.GUI;
using HimbeertoniRaidTool.Common.Data;
using HimbeertoniRaidTool.Plugin.DataExtensions;
using Lumina.Excel;
using Lumina.Excel.GeneratedSheets;

namespace HimbeertoniRaidTool.Plugin.Services;

//Inspired by aka Copied from
//https://github.com/Caraxi/SimpleTweaksPlugin/blob/main/Tweaks/UiAdjustment/ExamineItemLevel.cs
internal unsafe class GearRefresher
{
    public static GearRefresher Instance => InstanceImpl.Value;
    private static readonly Lazy<GearRefresher> InstanceImpl = new(() => new GearRefresher());
    private bool HookLoadSuccessful => _hook is not null;

    [Signature("48 89 5C 24 ?? 57 48 83 EC 20 49 8B D8 48 8B F9 4D 85 C0 0F 84 ?? ?? ?? ?? 85 D2",
        DetourName = nameof(OnExamineRefresh))]
    private readonly Hook<CharacterInspectOnRefresh>? _hook = null;

    private static readonly ExcelSheet<World>? WorldSheet;

    private delegate byte CharacterInspectOnRefresh(AtkUnitBase* atkUnitBase, int a2, AtkValue* a3);

    static GearRefresher()
    {
        WorldSheet = ServiceManager.DataManager.GetExcelSheet<World>();
    }


    internal void Enable()
    {
        ServiceManager.GameInteropProvider.InitializeFromAttributes(this);
        if (_hook is null)
        {
            ServiceManager.PluginLog.Error("Failed to hook into examine window");
            return;
        }

        _hook.Enable();
    }

    internal static void RefreshGearInfos(PlayerCharacter? @object)
    {
        if (@object is null)
            return;
        try
        {
            AgentInspect.Instance()->ExamineCharacter(@object.ObjectId);
        }
        catch (Exception e)
        {
            ServiceManager.PluginLog.Error(e, $"Could not inspect character {@object.Name}");
        }
    }


    private byte OnExamineRefresh(AtkUnitBase* atkUnitBase, int a2, AtkValue* loadingStage)
    {
        byte result = _hook!.Original(atkUnitBase, a2, loadingStage);
        if (loadingStage is null || a2 <= 0 || loadingStage->UInt != 4) return result;
        GetItemInfos(atkUnitBase);
        return result;
    }

    private void GetItemInfos(AtkUnitBase* examineWindow)
    {
        if (!HookLoadSuccessful)
            return;
        //Get Character Information from examine window
        //There are two possible fields for name/title depending on their order
        string charNameFromExamine;
        string charNameFromExamine2;
        World? worldFromExamine;
        try
        {
            charNameFromExamine = examineWindow->UldManager.NodeList[60]->GetAsAtkTextNode()->NodeText.ToString();
            charNameFromExamine2 = examineWindow->UldManager.NodeList[59]->GetAsAtkTextNode()->NodeText.ToString();
            string worldString = examineWindow->UldManager.NodeList[57]->GetAsAtkTextNode()->NodeText.ToString();
            worldFromExamine = WorldSheet?.FirstOrDefault(x => x?.Name.RawString == worldString, null);
        }
        catch (Exception e)
        {
            ServiceManager.PluginLog.Error(e, "Exception while reading name / world from examine window");
            return;
        }

        if (worldFromExamine is null)
            return;
        //Make sure examine window corresponds to intended character and character info is fetchable
        if (!ServiceManager.CharacterInfoService.TryGetChar(out PlayerCharacter? target, charNameFromExamine,
                worldFromExamine)
            && !ServiceManager.CharacterInfoService.TryGetChar(out target, charNameFromExamine2, worldFromExamine))
        {
            ServiceManager.PluginLog.Error(
                $"Name + World from examine window didn't match any character in the area: " +
                $"Name1 {charNameFromExamine}, Name2 {charNameFromExamine2}, World {worldFromExamine?.Name}");
            return;
        }

        if (!ServiceManager.HrtDataManager.Ready)
        {
            ServiceManager.PluginLog.Error(
                $"Database is busy. Did not update gear for:{target.Name}@{target.HomeWorld.GameData?.Name}");
            return;
        }

        //Do not execute on characters not already known
        if (!ServiceManager.HrtDataManager.CharDB.SearchCharacter(target.HomeWorld.Id, target.Name.TextValue,
                out Character? targetChar))
        {
            ServiceManager.PluginLog.Debug(
                $"Did not find character in db:{target.Name}@{target.HomeWorld.GameData?.Name}");
            return;
        }

        //Save characters ContentID if not already known
        if (targetChar.CharID == 0)
        {
            PartyMember? p = ServiceManager.PartyList.FirstOrDefault(p => p?.ObjectId == target.ObjectId, null);
            if (p != null) targetChar.CharID = Character.CalcCharID(p.ContentId);
        }

        Job targetJob = target.GetJob();
        if (!targetJob.IsCombatJob())
            return;
        PlayableClass? targetClass = targetChar[targetJob];
        if (targetClass == null)
        {
            if (!ServiceManager.HrtDataManager.Ready)
            {
                ServiceManager.PluginLog.Error(
                    $"Database is busy. Did not update gear for:{targetChar.Name}@{targetChar.HomeWorld?.Name}");
                return;
            }

            targetClass = targetChar.AddClass(targetJob);
            ServiceManager.HrtDataManager.GearDB.AddSet(targetClass.Gear);
            ServiceManager.HrtDataManager.GearDB.AddSet(targetClass.BIS);
        }

        //Getting level does not work in level synced content
        if (target.Level > targetClass.Level)
            targetClass.Level = target.Level;
        var container = InventoryManager.Instance()->GetInventoryContainer(InventoryType.Examine);
        UpdateGear(container, targetClass);
        ServiceManager.PluginLog.Information($"Updated Gear for: {targetChar.Name} @ {targetChar.HomeWorld?.Name}");
    }

    internal static void UpdateGear(InventoryContainer* container, PlayableClass targetClass)
    {
        try
        {
            for (int i = 0; i < 13; i++)
            {
                if (i == (int)GearSetSlot.Waist)
                    continue;
                var slot = container->GetInventorySlot(i);
                if (slot->ItemID == 0)
                    continue;
                targetClass.Gear[(GearSetSlot)i] = new GearItem(slot->ItemID)
                {
                    IsHq = slot->Flags.HasFlag(InventoryItem.ItemFlags.HQ),
                };
                for (int j = 0; j < 5; j++)
                {
                    if (slot->Materia[j] == 0)
                        break;
                    targetClass.Gear[(GearSetSlot)i].AddMateria(new HrtMateria((MateriaCategory)slot->Materia[j],
                        (MateriaLevel)slot->MateriaGrade[j]));
                }
            }

            targetClass.Gear.TimeStamp = DateTime.UtcNow;
        }
        catch (Exception e)
        {
            ServiceManager.PluginLog.Error(e, $"Something went wrong getting gear for:{targetClass.Parent?.Name}");
        }
    }

    internal void Dispose()
    {
        if (_hook is null || _hook.IsDisposed)
            return;
        _hook.Dispose();
    }
}