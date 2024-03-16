using Dalamud.Game.ClientState.Objects.SubKinds;

namespace HimbeertoniRaidTool.Plugin.Services;

public static class EnumExtensions
{
    public static Job GetJob(this PlayerCharacter target) => (Job)target.ClassJob.Id;
}