using System;
using System.Runtime.InteropServices;
using Dalamud.Game.ClientState.Objects.SubKinds;
using Dalamud.Hooking;
using FFXIVClientStructs.FFXIV.Client.Game;
using FFXIVClientStructs.FFXIV.Component.GUI;
using HimbeertoniRaidTool.Data;
using Lumina.Excel.GeneratedSheets;
using XivCommon;

namespace HimbeertoniRaidTool.Modules.LootMaster
{
    //Credit and apologies for taking and butchering their code goes to Caraxi https://github.com/Caraxi
    //https://github.com/Caraxi/SimpleTweaksPlugin/blob/main/Tweaks/UiAdjustment/ExamineItemLevel.cs
    internal static unsafe class GearRefresherOnExamine
    {
        private static readonly bool HookLoadSuccessful;
        internal static readonly bool CanOpenExamine;
        private static readonly Hook<CharacterInspectOnRefresh>? Hook;
        private static readonly IntPtr HookAddress;
        private static readonly IntPtr InventoryManagerAddress;
        private static readonly IntPtr getInventoryContainerPtr;
        private static readonly IntPtr getContainerSlotPtr;

        private delegate byte CharacterInspectOnRefresh(AtkUnitBase* atkUnitBase, int a2, AtkValue* a3);
        private delegate InventoryContainer* GetInventoryContainer(IntPtr inventoryManager, InventoryType inventoryType);
        private delegate InventoryItem* GetContainerSlot(InventoryContainer* inventoryContainer, int slotId);

        private static readonly GetInventoryContainer? _getInventoryContainer;
        private static readonly GetContainerSlot? _getContainerSlot;
        private static readonly XivCommonBase? XivCommonBase;
        static GearRefresherOnExamine()
        {
            try
            {
                HookAddress = Services.SigScanner.ScanText("48 89 5C 24 ?? 57 48 83 EC 20 49 8B D8 48 8B F9 4D 85 C0 0F 84 ?? ?? ?? ?? 85 D2");
                InventoryManagerAddress = Services.SigScanner.GetStaticAddressFromSig("BA ?? ?? ?? ?? 48 8D 0D ?? ?? ?? ?? E8 ?? ?? ?? ?? 48 8B F8 48 85 C0");
                getInventoryContainerPtr = Services.SigScanner.ScanText("E8 ?? ?? ?? ?? 8B 55 AB");
                getContainerSlotPtr = Services.SigScanner.ScanText("E8 ?? ?? ?? ?? 8B 5B 0C");

                Hook = Hook<CharacterInspectOnRefresh>.FromAddress(HookAddress, OnExamineRefresh);
                _getContainerSlot = Marshal.GetDelegateForFunctionPointer<GetContainerSlot>(getContainerSlotPtr);
                _getInventoryContainer = Marshal.GetDelegateForFunctionPointer<GetInventoryContainer>(getInventoryContainerPtr);
                HookLoadSuccessful = true;
            }
            catch (Exception e)
            {
                Dalamud.Logging.PluginLog.LogError("Failed to hook into examine window");
                Dalamud.Logging.PluginLog.LogError(e.Message);
                Dalamud.Logging.PluginLog.LogError(e.StackTrace ?? "");
                HookLoadSuccessful = false;
            }
            XivCommonBase = new XivCommonBase();
            CanOpenExamine = true;
        }
        internal static unsafe void RefreshGearInfos(PlayerCharacter? @object)
        {
            if (!CanOpenExamine || @object is null || XivCommonBase is null)
                return;
            XivCommonBase.Functions.Examine.OpenExamineWindow(@object);
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
            if (!HookLoadSuccessful || _getInventoryContainer is null || _getContainerSlot is null)
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
                worldFromExamine = Helper.TryGetWorldByName(examineWindow->UldManager.NodeList[57]->GetAsAtkTextNode()->NodeText.ToString());

            }
            catch (Exception)
            {
                return;
            }
            //Make sure examine window correspods to intended character and character info is fetchable
            if (!Helper.TryGetChar(out PlayerCharacter? target, charNameFromExamine, worldFromExamine))
            {
                if (Helper.TryGetChar(out target, charNameFromExamine2, worldFromExamine))
                    charNameFromExamine = charNameFromExamine2;
                else
                    return;
            }
            if (target is null)
                return;
            if (!target.TryGetJob(out Job targetClass))
                return;
            //Do not execute on characters not part of any managed raid group
            if (!Services.HrtDataManager.CharacterExists(target.HomeWorld.Id, target.Name.TextValue))
                return;
            Character targetChar = new(target.Name.TextValue, target.HomeWorld.Id);

            if (!Services.HrtDataManager.GetManagedCharacter(ref targetChar, false))
                return;
            if (targetChar is null)
                return;
            var container = _getInventoryContainer(InventoryManagerAddress, InventoryType.Examine);
            if (container == null)
                return;

            //Getting level does not work in level synced content
            if (target.Level > targetChar.GetClass(targetClass).Level)
                targetChar.GetClass(targetClass).Level = target.Level;
            GearSet setToFill = new(GearSetManager.HRT, targetChar, targetClass);
            Services.HrtDataManager.GetManagedGearSet(ref setToFill);

            setToFill.Clear();
            setToFill.TimeStamp = DateTime.UtcNow;
            for (int i = 0; i < 13; i++)
            {
                if (i == (int)GearSetSlot.Waist)
                    continue;
                var slot = _getContainerSlot(container, i);
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
