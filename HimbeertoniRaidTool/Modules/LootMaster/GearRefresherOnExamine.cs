using System;
using System.Linq;
using Dalamud.Game.ClientState.Objects.SubKinds;
using Dalamud.Hooking;
using Dalamud.Logging;
using FFXIVClientStructs.FFXIV.Client.Game;
using FFXIVClientStructs.FFXIV.Component.GUI;
using HimbeertoniRaidTool.Data;
using Lumina.Excel;
using Lumina.Excel.GeneratedSheets;
using XivCommon;

namespace HimbeertoniRaidTool.Modules.LootMaster
{
    //Inspired by aka Copied from
    //https://github.com/Caraxi/SimpleTweaksPlugin/blob/main/Tweaks/UiAdjustment/ExamineItemLevel.cs
    internal static unsafe class GearRefresherOnExamine
    {
        private static readonly bool HookLoadSuccessful;
        internal static readonly bool CanOpenExamine;
        private static readonly Hook<CharacterInspectOnRefresh>? Hook;
        private static readonly IntPtr HookAddress;
        private static readonly ExcelSheet<World>? WorldSheet;

        private delegate byte CharacterInspectOnRefresh(AtkUnitBase* atkUnitBase, int a2, AtkValue* a3);
        private static readonly XivCommonBase? XivCommonBase;
        static GearRefresherOnExamine()
        {
            try
            {
                HookAddress = Services.SigScanner.ScanText("48 89 5C 24 ?? 57 48 83 EC 20 49 8B D8 48 8B F9 4D 85 C0 0F 84 ?? ?? ?? ?? 85 D2");
                Hook = Hook<CharacterInspectOnRefresh>.FromAddress(HookAddress, OnExamineRefresh);
                HookLoadSuccessful = true;
            }
            catch (Exception e)
            {
                PluginLog.LogError(e, "Failed to hook into examine window");
                HookLoadSuccessful = false;
            }
            XivCommonBase = new XivCommonBase();
            CanOpenExamine = true;
            WorldSheet = Services.DataManager.GetExcelSheet<World>();
        }
        internal static unsafe void RefreshGearInfos(PlayerCharacter? @object)
        {
            if (!CanOpenExamine || @object is null || XivCommonBase is null)
                return;
            try
            {
                XivCommonBase.Functions.Examine.OpenExamineWindow(@object);
            }
            catch (Exception e)
            {
                PluginLog.Error(e, "Could not inspect character");
            }
        }
        internal static void Enable()
        {
            if (HookLoadSuccessful && Hook is not null) Hook.Enable();
        }

        private static byte OnExamineRefresh(AtkUnitBase* atkUnitBase, int a2, AtkValue* loadingStage)
        {
            byte result = Hook!.Original(atkUnitBase, a2, loadingStage);
            if (loadingStage != null && a2 > 0)
            {
                if (loadingStage->UInt == 4)
                {
                    GetItemInfos(atkUnitBase);
                }
            }
            return result;

        }
        private static void GetItemInfos(AtkUnitBase* examineWindow)
        {
            if (!HookLoadSuccessful)
                return;
            //Get Chracter Information from examine window
            //There are two possible fields for name/title depending on their order
            string charNameFromExamine = "";
            string charNameFromExamine2 = "";
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
                PluginLog.Debug(e, "Exception while reading name / world from examine window");
                return;
            }
            if (worldFromExamine is null)
                return;
            //Make sure examine window correspods to intended character and character info is fetchable
            if (!Services.CharacterInfoService.TryGetChar(out PlayerCharacter? target, charNameFromExamine, worldFromExamine)
                && !Services.CharacterInfoService.TryGetChar(out target, charNameFromExamine2, worldFromExamine))
            {
                PluginLog.Debug($"Name + World from examine window didn't match any character in the area: " +
                    $"name1 {charNameFromExamine}, name 2 {charNameFromExamine2}, wolrd {worldFromExamine?.Name}");
                return;
            }
            //Do not execute on characters not part of any managed raid group
            if (!Services.HrtDataManager.CharacterExists(target.HomeWorld.Id, target.Name.TextValue))
                return;
            Character targetChar = new(target.Name.TextValue, target.HomeWorld.Id);

            if (!Services.HrtDataManager.GetManagedCharacter(ref targetChar, false))
            {
                PluginLog.Error($"Internal database error. Did not update gear for:{targetChar.Name}@{targetChar.HomeWorld?.Name}");
                return;
            }

            //Start getting Infos from Game
            Job targetClass = target.GetJob();
            //Getting level does not work in level synced content
            if (target.Level > targetChar[targetClass].Level)
                targetChar[targetClass].Level = target.Level;
            try
            {
                InventoryContainer* container = InventoryManager.Instance()->GetInventoryContainer(InventoryType.Examine);
                GearSet setToFill = new(GearSetManager.HRT, targetChar, targetClass);
                Services.HrtDataManager.GetManagedGearSet(ref setToFill);
                for (int i = 0; i < 13; i++)
                {
                    if (i == (int)GearSetSlot.Waist)
                        continue;
                    InventoryItem* slot = container->GetInventorySlot(i);
                    if (slot->ItemID == 0)
                        continue;
                    setToFill[(GearSetSlot)i] = new(slot->ItemID)
                    {
                        IsHq = slot->Flags.HasFlag(InventoryItem.ItemFlags.HQ)
                    };
                    for (int j = 0; j < 5; j++)
                    {
                        if (slot->Materia[j] == 0)
                            break;
                        setToFill[(GearSetSlot)i].Materia.Add(new((MateriaCategory)slot->Materia[j], slot->MateriaGrade[j]));
                    }
                }
                setToFill.TimeStamp = DateTime.UtcNow;
            }
            catch (Exception e)
            {
                PluginLog.Error(e, $"Something went wrong getting gear for:{targetChar.Name}");
            }
        }
        public static void Dispose()
        {
            if (Hook is not null)
            {
                Hook.Disable();
                Hook.Dispose();
            }
            XivCommonBase?.Dispose();
        }
    }
}
