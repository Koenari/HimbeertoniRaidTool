using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Dalamud.Game.ClientState.Objects.SubKinds;
using Dalamud.Hooking;
using FFXIVClientStructs.FFXIV.Client.Game;
using FFXIVClientStructs.FFXIV.Component.GUI;
using HimbeertoniRaidTool.Data;
using Lumina.Excel.GeneratedSheets;
using XivCommon;

namespace HimbeertoniRaidTool.LootMaster
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
        private static readonly IntPtr RequestCharacterInfoPtr;

        private delegate byte CharacterInspectOnRefresh(AtkUnitBase* atkUnitBase, int a2, AtkValue* a3);
        private delegate InventoryContainer* GetInventoryContainer(IntPtr inventoryManager, InventoryType inventoryType);
        private delegate InventoryItem* GetContainerSlot(InventoryContainer* inventoryContainer, int slotId);
        private delegate long RequestCharInfoDelegate(IntPtr ptr);

        private static readonly GetInventoryContainer? _getInventoryContainer;
        private static readonly GetContainerSlot? _getContainerSlot;
        private static readonly RequestCharInfoDelegate? _requestCharacterInfo;
        private static PlayerCharacter? TargetOverrride = null;
        private static readonly XivCommonBase? XivCommonBase;
        private static readonly bool useXivCommon = true;
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
            if (useXivCommon)
            {
                XivCommonBase = new XivCommonBase();
                CanOpenExamine = true;
            }
            else
            {
                try
                {
                    RequestCharacterInfoPtr = Services.SigScanner.ScanText("40 53 48 83 EC 40 48 8B D9 48 8B 49 10 48 8B 01 FF 90 ?? ?? ?? ?? BA");
                    _requestCharacterInfo = Marshal.GetDelegateForFunctionPointer<RequestCharInfoDelegate>(RequestCharacterInfoPtr);
                    if (_requestCharacterInfo == null)
                    {
                        Dalamud.Logging.PluginLog.LogError("Could not find signature for Examine function");
                        CanOpenExamine = false;
                    }
                    else
                    {
                        //Todo Match to GameVeriosn for automatic disabling on updates
                        CanOpenExamine = true;
                    }
                }
                catch (Exception e)
                {
                    Dalamud.Logging.PluginLog.LogError("Failed to load examine function");
                    Dalamud.Logging.PluginLog.LogError(e.Message);
                    Dalamud.Logging.PluginLog.LogError(e.StackTrace ?? "");
                    CanOpenExamine = false;
                }
            }
        }
        internal static unsafe void RefreshGearInfos(PlayerCharacter? @object)
        {
            if (!CanOpenExamine)
                return;
            if (@object == null)
                return;
            if (useXivCommon)
            {
                if (XivCommonBase is null)
                    return;
                XivCommonBase.Functions.Examine.OpenExamineWindow(@object);
            }
            else
            {
                if (_requestCharacterInfo == null)
                    return;
                uint objectId = @object.ObjectId;
                /*
                 * Not needed anymore given XivCommon works fine again
                 */
                TargetOverrride = @object;
                IntPtr agentModule = (IntPtr)FFXIVClientStructs.FFXIV.Client.System.Framework.Framework.Instance()->GetUiModule()->GetAgentModule();
                IntPtr rciData = agentModule + 0x1A8;
                uint* rawRci = (uint*)rciData;
                rawRci[10] = objectId;
                rawRci[11] = objectId;
                rawRci[12] = objectId;
                rawRci[13] = 0xE0000000;
                rawRci[301] = 0u;
                _requestCharacterInfo(rciData);
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
            PlayerCharacter? target = null;
            if (TargetOverrride is not null)
            {
                if (TargetOverrride.Name.Equals(charNameFromExamine) || TargetOverrride.Name.Equals(charNameFromExamine2))
                    if (TargetOverrride.HomeWorld.GameData == worldFromExamine)
                        target = TargetOverrride;
                TargetOverrride = null;
            }
            else
            {
                target = Helper.TryGetChar(charNameFromExamine, worldFromExamine);
                if (target is null)
                {
                    target = Helper.TryGetChar(charNameFromExamine2, worldFromExamine);
                    charNameFromExamine = charNameFromExamine2;
                }
                if (target is null)
                    return;
                if (target.GetJob() is null)
                    return;
                IntPtr intPtr = Marshal.ReadIntPtr((IntPtr)(void*)FFXIVClientStructs.FFXIV.Client.System.Framework.Framework.Instance()->GetUiModule()->GetAgentModule() + 416);
                var objID = target.ObjectId;
                uint* ptr = (uint*)(void*)intPtr;
                //Do not execute on characters not part of any managed raid group
                if (!DataManagement.DataManager.CharacterExists(target.HomeWorld.Id, target.Name.TextValue))
                    return;
                Character targetChar = new(target.Name.TextValue, target.HomeWorld.Id);

                DataManagement.DataManager.GetManagedCharacter(ref targetChar);
                if (targetChar is null)
                    return;
                Job targetClass = (Job)target.GetJob()!;
                InventoryContainer* container = _getInventoryContainer(InventoryManagerAddress, InventoryType.Examine);
                if (container == null)
                    return;

                //Getting level does not work in level synced content
                if (target.Level > targetChar.GetClass(targetClass).Level)
                    targetChar.GetClass(targetClass).Level = target.Level;
                GearSet setToFill = new GearSet(GearSetManager.HRT, targetChar, targetClass);
                DataManagement.DataManager.GetManagedGearSet(ref setToFill);

                setToFill.Clear();
                setToFill.TimeStamp = DateTime.UtcNow;
                for (int i = 0; i < 13; i++)
                {
                    if (i == (int)GearSetSlot.Waist)
                        continue;
                    InventoryItem* slot = _getContainerSlot(container, i);
                    if (slot->ItemID == 0)
                        continue;
                    setToFill[(GearSetSlot)i] = new(slot->ItemID);
                    setToFill[(GearSetSlot)i].IsHq = slot->Flags.HasFlag(InventoryItem.ItemFlags.HQ);
                    for (int j = 0; j < 5; j++)
                    {
                        if (slot->Materia[j] == 0)
                            break;
                        setToFill[(GearSetSlot)i].Materia.Add(new((MateriaCategory)slot->Materia[j], slot->MateriaGrade[j]));
                    }
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
