using Dalamud.Game.ClientState.Objects.SubKinds;
using Dalamud.Hooking;
using FFXIVClientStructs.Attributes;
using FFXIVClientStructs.FFXIV.Client.Game;
using FFXIVClientStructs.FFXIV.Component.GUI;
using HimbeertoniRaidTool.Data;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace HimbeertoniRaidTool.LootMaster
{
    //Credit and apologies for taking and butchering their code goes to Caraxi https://github.com/Caraxi
    //https://github.com/Caraxi/SimpleTweaksPlugin/blob/main/Tweaks/UiAdjustment/ExamineItemLevel.cs
    internal static unsafe class GearRefresherOnExamine
    {
        private static readonly Hook<CharacterInspectOnRefresh> Hook;
        private static readonly IntPtr HookAddress = Services.SigScanner.ScanText("48 89 5C 24 ?? 57 48 83 EC 20 49 8B D8 48 8B F9 4D 85 C0 0F 84 ?? ?? ?? ?? 85 D2");
        private static readonly IntPtr InventoryManagerAddress = Services.SigScanner.GetStaticAddressFromSig("BA ?? ?? ?? ?? 48 8D 0D ?? ?? ?? ?? E8 ?? ?? ?? ?? 48 8B F8 48 85 C0");
        private static readonly IntPtr getInventoryContainerPtr = Services.SigScanner.ScanText("E8 ?? ?? ?? ?? 8B 55 BB");
        private static readonly IntPtr getContainerSlotPtr = Services.SigScanner.ScanText("E8 ?? ?? ?? ?? 8B 5B 0C");

        private delegate byte CharacterInspectOnRefresh(AtkUnitBase* atkUnitBase, int a2, AtkValue* a3);
        private delegate InventoryContainer* GetInventoryContainer(IntPtr inventoryManager, InventoryType inventoryType);
        private delegate InventoryItem* GetContainerSlot(InventoryContainer* inventoryContainer, int slotId);

        private static readonly GetInventoryContainer _getInventoryContainer;
        private static readonly GetContainerSlot _getContainerSlot;
        public static PlayerCharacter? TargetOverrride = null;
        static GearRefresherOnExamine()
        {
            Hook = new(HookAddress, OnExamineRefresh);
            _getContainerSlot = Marshal.GetDelegateForFunctionPointer<GetContainerSlot>(getContainerSlotPtr);
            _getInventoryContainer = Marshal.GetDelegateForFunctionPointer<GetInventoryContainer>(getInventoryContainerPtr);

        }
        internal static void Enable() => Hook.Enable();
        private static byte OnExamineRefresh(AtkUnitBase* atkUnitBase, int a2, AtkValue* loadingStage)
        {
            byte result = Hook.Original(atkUnitBase, a2, loadingStage);
            if (loadingStage != null && a2 > 0)
            {
                if (loadingStage->UInt == 4)
                {
                    GetItemInfos();
                }
            }
            return result;

        }
        private static void GetItemInfos()
        {
            /*
             * TODO: Get ChracterInfo from Examine Window
            var examineWindow = (AddonCharacterInspect*)Services.GameGui.GetAddonByName("CharacterInspect", 1);
            var compInfo = (AtkUldComponentInfo*)examineWindow->PreviewComponent->UldManager.Objects;
            if (compInfo == null || compInfo->ComponentType != ComponentType.Preview) return;
            var nodeList = examineWindow->PreviewComponent->UldManager.NodeList;
            var node = nodeList[1];
            var text = (AtkTextNode*)node;
            */
            List<Character> chars = new();
            var target = TargetOverrride ?? Helper.TargetChar;
            TargetOverrride = null;
            if (target is null)
                return;
            string name = target.Name.TextValue;
            AvailableClasses targetClass = target.GetClass();
            int level = target.Level;
            foreach (RaidGroup g in LootMaster.RaidGroups)
            {
                Character? c = g.GetCharacter(name);
                if (c is not null)
                    chars.Add(c);
            }
            if (chars.Count == 0)
                return;
            List<GearSet> setsToFill = new();
            foreach (Character c in chars)
            {
                PlayableClass playableClass = c.GetClass(targetClass);
                playableClass.Level = level;
                setsToFill.Add(playableClass.Gear);
            }

            InventoryContainer* container = _getInventoryContainer(InventoryManagerAddress, InventoryType.Examine);
            if (container == null)
                return;
            setsToFill.ForEach(x => x.Clear());
            setsToFill.ForEach(x => x.TimeStamp = DateTime.UtcNow);
            for (int i = 0; i < 13; i++)
            {
                if (i == (int)GearSetSlot.Waist)
                    continue;
                InventoryItem* slot = _getContainerSlot(container, i);
                if (slot->ItemID == 0)
                    continue;
                setsToFill.ForEach(set => set[(GearSetSlot)i] = new(slot->ItemID));
            }
        }
        public static void Dispose()
        {
            Hook.Disable();
            Hook.Dispose();
        }
    }
    [StructLayout(LayoutKind.Explicit, Size = 1280)]
    [Addon("CharacterInspect")]
    public unsafe struct AddonCharacterInspect
    {
        [FieldOffset(0x000)] public AtkUnitBase AtkUnitBase;
        [FieldOffset(0x430)] public AtkComponentBase* PreviewComponent;
    }
}
