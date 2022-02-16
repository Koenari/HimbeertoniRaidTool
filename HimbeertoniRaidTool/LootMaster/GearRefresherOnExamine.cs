using Dalamud.Hooking;
using FFXIVClientStructs.FFXIV.Component.GUI;
using FFXIVClientStructs.FFXIV.Client.Game;
using System;
using System.Runtime.InteropServices;
using FFXIVClientStructs.Attributes;
using Dalamud.Logging;

namespace HimbeertoniRaidTool.LootMaster
{
    //Credit and apologies for taking and butchering their code goes to Caraxi https://github.com/Caraxi
    //https://github.com/Caraxi/SimpleTweaksPlugin/blob/main/Tweaks/UiAdjustment/ExamineItemLevel.cs
    internal unsafe class GearRefresherOnExamine : IDisposable
    {
        private LootMaster lootMaster;
        
        private Hook<CharacterInspectOnRefresh> Hook;
        private readonly IntPtr HookAddress = Services.SigScanner.ScanText("48 89 5C 24 ?? 57 48 83 EC 20 49 8B D8 48 8B F9 4D 85 C0 0F 84 ?? ?? ?? ?? 85 D2");
        private readonly IntPtr InventoryManagerAddress = Services.SigScanner.GetStaticAddressFromSig("BA ?? ?? ?? ?? 48 8D 0D ?? ?? ?? ?? E8 ?? ?? ?? ?? 48 8B F8 48 85 C0");
        private readonly IntPtr getInventoryContainerPtr = Services.SigScanner.ScanText("E8 ?? ?? ?? ?? 8B 55 BB");
        private readonly IntPtr getContainerSlotPtr = Services.SigScanner.ScanText("E8 ?? ?? ?? ?? 8B 5B 0C");

        private delegate byte CharacterInspectOnRefresh(AtkUnitBase* atkUnitBase, int a2, AtkValue* a3);
        private delegate InventoryContainer* GetInventoryContainer(IntPtr inventoryManager, InventoryType inventoryType);
        private delegate InventoryItem* GetContainerSlot(InventoryContainer* inventoryContainer, int slotId);
        private GetInventoryContainer _getInventoryContainer;
        private GetContainerSlot _getContainerSlot;
        internal GearRefresherOnExamine(LootMaster lm)
        {
            lootMaster = lm;
            Hook = new(HookAddress, OnExamineRefresh);
            _getContainerSlot = Marshal.GetDelegateForFunctionPointer<GetContainerSlot>(getContainerSlotPtr);
            _getInventoryContainer = Marshal.GetDelegateForFunctionPointer<GetInventoryContainer>(getInventoryContainerPtr);
            Hook.Enable();
        }
        private byte OnExamineRefresh(AtkUnitBase* atkUnitBase, int a2, AtkValue* loadingStage)
        {
            byte result = Hook.Original(atkUnitBase, a2, loadingStage);
            if (loadingStage != null && a2 > 0)
            {
                if (loadingStage->UInt == 4)
                {
                    DoStuff();
                }
            }
            return result;

        }
        private void DoStuff()
        { 
            InventoryContainer* container = _getInventoryContainer(InventoryManagerAddress, InventoryType.Examine);
            if (container == null) return;
            var examineWindow = (AddonCharacterInspect*) Services.GameGui.GetAddonByName("CharacterInspect", 1);
            PluginLog.LogDebug("DoStuff() in Examine Hook");
        }

        public void Dispose()
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
