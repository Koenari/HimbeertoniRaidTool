using Dalamud.Game.ClientState.Objects.SubKinds;
using HimbeertoniRaidTool.Common.Data;

namespace HimbeertoniRaidTool.Plugin.Services;

public static class EnumExtensions
{
    public static Job GetJob(this PlayerCharacter target) => (Job)target.ClassJob.Id;
}