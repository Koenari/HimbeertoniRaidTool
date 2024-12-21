using Dalamud.Game.ClientState.Objects.SubKinds;
using FFXIVClientStructs.FFXIV.Client.Game;
using FFXIVClientStructs.FFXIV.Client.UI.Agent;

namespace HimbeertoniRaidTool.Plugin.Services;

internal static class CsHelpers
{
    internal static unsafe bool UpdateGearFromInventoryContainer(InventoryType type, PlayableClass targetClass,
                                                                 int noDowngradeBelow)
    {
        if (type is not (InventoryType.Examine or InventoryType.EquippedItems)) return false;
        var container = InventoryManager.Instance()->GetInventoryContainer(type);
        if (container == null || container->Size < 13) return false;
        var targetGearSet = targetClass.AutoUpdatedGearSet;
        if (targetGearSet.LocalId.IsEmpty)
        {
            ServiceManager.HrtDataManager.GearDb.TryAdd(targetGearSet);
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
                ServiceManager.Logger.Debug($"Ignored {(GearSetSlot)i} due to item level");
                continue;
            }
            targetGearSet[(GearSetSlot)i] = newItem;
            for (int j = 0; j < 5; j++)
            {
                if (slot->Materia[j] == 0) break;
                targetGearSet[(GearSetSlot)i].AddMateria(new MateriaItem(
                                                             (MateriaCategory)slot->Materia[j],
                                                             (MateriaLevel)slot->MateriaGrades[j]));
            }
        }
        targetGearSet.TimeStamp = DateTime.UtcNow;
        return true;
    }
    internal static void SafeguardedOpenExamine(IPlayerCharacter? @object)
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
            ServiceManager.Logger.Error(e, $"Could not inspect character {@object.Name}");
        }
    }
}