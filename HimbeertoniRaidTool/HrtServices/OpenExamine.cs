using System;
using Dalamud.Game.ClientState.Objects.SubKinds;
using Dalamud.Logging;
using FFXIVClientStructs.FFXIV.Client.UI.Agent;

namespace HimbeertoniRaidTool.Plugin.Modules.LootMaster;
internal static class OpenExamine
{
    internal static unsafe void OpenExamineWindow(PlayerCharacter? @object)
    {
        if (@object is null)
            return;
        try
        {
            AgentInspect.Instance()->ExamineCharacter(@object.ObjectId);
        }
        catch (Exception e)
        {
            PluginLog.Error(e, "Could not inspect character");
        }
    }
}
