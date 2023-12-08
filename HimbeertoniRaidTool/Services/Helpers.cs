using FFXIVClientStructs.FFXIV.Client.Game;
using HimbeertoniRaidTool.Common.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HimbeertoniRaidTool.Plugin.Services;

internal class Helpers
{
    internal static unsafe void UpdateGearFromInventoryContainer(InventoryType type, PlayableClass targetClass)
    {
        if (type is not (InventoryType.Examine or InventoryType.EquippedItems)) return;
        var container = InventoryManager.Instance()->GetInventoryContainer(type);
        if (container == null || container->Size < 13) return;
        for (int i = 0; i < 13; i++)
        {
            if (i == (int)GearSetSlot.Waist) continue;
            var slot = container->GetInventorySlot(i);
            if (slot->ItemID == 0) continue;
            targetClass.Gear[(GearSetSlot)i] = new GearItem(slot->ItemID)
            {
                IsHq = slot->Flags.HasFlag(InventoryItem.ItemFlags.HQ),
            };
            for (int j = 0; j < 5; j++)
            {
                if (slot->Materia[j] == 0) break;
                targetClass.Gear[(GearSetSlot)i].AddMateria(new HrtMateria((MateriaCategory)slot->Materia[j],
                    (MateriaLevel)slot->MateriaGrade[j]));
            }
        }
        targetClass.Gear.TimeStamp = DateTime.UtcNow;
    }
}