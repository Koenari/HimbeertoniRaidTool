using System;
using System.Runtime.InteropServices;
using Dalamud.Game.ClientState.Objects.SubKinds;
using Dalamud.Logging;

namespace HimbeertoniRaidTool.Plugin.Modules.LootMaster;
internal static class OpenExamine
{
    internal static bool CanOpen = false;

    private delegate long RequestCharInfoDelegate(IntPtr ptr);

    private static RequestCharInfoDelegate? RequestCharacterInfo { get; }

    static OpenExamine()
    {
        // got this by checking what accesses rciData below
        if (Services.SigScanner.TryScanText("40 53 48 83 EC 40 48 8B D9 48 8B 49 10 48 8B 01 FF 90 ?? ?? ?? ?? BA", out nint rciPtr))
        {
            RequestCharacterInfo = Marshal.GetDelegateForFunctionPointer<RequestCharInfoDelegate>(rciPtr);
        }
        CanOpen = true;
    }
    internal static unsafe void OpenExamineWindow(PlayerCharacter? @object)
    {

        if (!CanOpen || @object is null)
            return;
        try
        {
            OpenExamineWindow(@object.ObjectId);
        }
        catch (Exception e)
        {
            PluginLog.Error(e, "Could not inspect character");
        }
    }
    /// <summary>
    /// Opens the Examine window for the object with the specified ID.
    /// </summary>
    /// <param name="objectId">Object ID to open window for</param>
    /// <exception cref="InvalidOperationException">If the signature for this function could not be found</exception>
    private static unsafe void OpenExamineWindow(uint objectId)
    {
        if (RequestCharacterInfo == null)
        {
            throw new InvalidOperationException("Could not find signature for Examine function");
        }

        // NOTES LAST UPDATED: 6.0

        // offsets and stuff come from the beginning of case 0x2c (around line 621 in IDA)
        // if 29f8 ever changes, I'd just scan for it in old binary and find what it is in the new binary at the same spot
        // 40 55 53 57 41 54 41 55 41 56 48 8D 6C 24
        // offset below is 4C 8B B0 ?? ?? ?? ?? 4D 85 F6 0F 84 ?? ?? ?? ?? 0F B6 83
        IntPtr agentModule = (IntPtr)FFXIVClientStructs.FFXIV.Client.System.Framework.Framework.Instance()->GetUiModule()->GetAgentModule();
        //PluginLog.Debug($"agent: {agentModule:X}");
        IntPtr rciData = Marshal.ReadIntPtr(agentModule + 0x1A8);
        //PluginLog.Debug($"rci: {rciData:X}");
        //return;
        // offsets at sig E8 ?? ?? ?? ?? 33 C0 EB 4C
        // this is called at the end of the 2c case
        uint* raw = (uint*)rciData;
        *(raw + 10) = objectId;
        *(raw + 11) = objectId;
        *(raw + 12) = objectId;
        *(raw + 13) = 0xE0000000;
        *(raw + 301) = 0;

        RequestCharacterInfo(rciData);
    }


}
