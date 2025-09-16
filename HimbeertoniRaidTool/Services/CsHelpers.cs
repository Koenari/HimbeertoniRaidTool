using Dalamud.Game.ClientState.Objects.SubKinds;
using FFXIVClientStructs.FFXIV.Client.Game;
using FFXIVClientStructs.FFXIV.Client.UI.Agent;
using HimbeertoniRaidTool.Plugin.DataManagement;
using Serilog;

namespace HimbeertoniRaidTool.Plugin.Services;

internal static class CsHelpers
{
    internal static unsafe bool UpdateGearFromInventoryContainer(InventoryType type, PlayableClass targetClass,
                                                                 int noDowngradeBelow, ILogger logger,
                                                                 HrtDataManager dataManager)
    {
        if (type is not (InventoryType.Examine or InventoryType.EquippedItems)) return false;
        var container = InventoryManager.Instance()->GetInventoryContainer(type);
        if (container == null || container->Size < 13) return false;
        var targetGearSet = targetClass.AutoUpdatedGearSet;
        if (targetGearSet.LocalId.IsEmpty)
        {
            dataManager.GearDb.TryAdd(targetGearSet);
        }
        for (int i = 0; i < 13; i++)
        {
            if (i == (int)GearSetSlot.Waist) continue;
            var slot = container->GetInventorySlot(i);
            if (slot->ItemId == 0) continue;
            var oldItem = targetGearSet[(GearSetSlot)i];
            //ToDo: correctly read stats of relics, until then do not override
            if (oldItem.IsRelic() && oldItem.Id == slot->ItemId) continue;
            var newItem = new GearItem(slot->ItemId)
            {
                IsHq = slot->Flags.HasFlag(InventoryItem.ItemFlags.HighQuality),
            };
            if (newItem.ItemLevel < oldItem.ItemLevel && newItem.ItemLevel < noDowngradeBelow)
            {
                logger.Debug("Ignored {GearSetSlot} due to item level", (GearSetSlot)i);
                continue;
            }
            targetGearSet[(GearSetSlot)i] = newItem;
            for (int j = 0; j < 5; j++)
            {
                if (slot->Materia[j] == 0) break;
                targetGearSet[(GearSetSlot)i].AddMateria(new MateriaItem(slot->Materia[j], slot->MateriaGrades[j]));
            }
        }
        targetGearSet.TimeStamp = DateTime.UtcNow;
        return true;
    }
    internal static void SafeguardedOpenExamine(IPlayerCharacter? @object, ILogger logger)
    {
        if (@object is null)
            return;
        try
        {
            unsafe
            {
                AgentInspect.Instance()->ExamineCharacter(@object.EntityId);
            }

        }
        catch (Exception e)
        {
            logger.Error(e, "Could not inspect character {ObjectName}", @object.Name);
        }
    }
}