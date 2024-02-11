using Dalamud.Game.ClientState.Objects.SubKinds;
using FFXIVClientStructs.FFXIV.Client.Game;
using FFXIVClientStructs.FFXIV.Client.UI.Agent;
using HimbeertoniRaidTool.Common.Data;

namespace HimbeertoniRaidTool.Plugin.Services;

internal static class CsHelpers
{
    internal static unsafe void UpdateGearFromInventoryContainer(InventoryType type, PlayableClass targetClass,
                                                                 int noDowngradeBelow)
    {
        if (type is not (InventoryType.Examine or InventoryType.EquippedItems)) return;
        var container = InventoryManager.Instance()->GetInventoryContainer(type);
        if (container == null || container->Size < 13) return;
        GearSet targetGearSet = targetClass.AutoUpdatedGearSet;
        if (targetGearSet.LocalId.IsEmpty)
        {
            ServiceManager.HrtDataManager.GearDb.TryAdd(targetGearSet);
        }
        for (int i = 0; i < 13; i++)
        {
            if (i == (int)GearSetSlot.Waist) continue;
            var slot = container->GetInventorySlot(i);
            if (slot->ItemID == 0) continue;
            GearItem oldItem = targetGearSet[(GearSetSlot)i];
            //ToDo: correctly read stats of relics, until then do not override
            if (oldItem.IsRelic() && oldItem.Id == slot->ItemID) continue;
            var newItem = new GearItem(slot->ItemID)
            {
                IsHq = slot->Flags.HasFlag(InventoryItem.ItemFlags.HQ),
            };
            if (newItem.ItemLevel < oldItem.ItemLevel && newItem.ItemLevel < noDowngradeBelow) continue;
            targetGearSet[(GearSetSlot)i] = newItem;
            for (int j = 0; j < 5; j++)
            {
                if (slot->Materia[j] == 0) break;
                targetGearSet[(GearSetSlot)i].AddMateria(new HrtMateria(
                                                             (MateriaCategory)slot->Materia[j],
                                                             (MateriaLevel)slot->MateriaGrade[j]));
            }
        }
        targetGearSet.TimeStamp = DateTime.UtcNow;
    }
    internal static void SafeguardedOpenExamine(PlayerCharacter? @object)
    {
        if (@object is null)
            return;
        try
        {
            unsafe
            {
                AgentInspect.Instance()->ExamineCharacter(@object.ObjectId);
            }

        }
        catch (Exception e)
        {
            ServiceManager.PluginLog.Error(e, $"Could not inspect character {@object.Name}");
        }
    }
}