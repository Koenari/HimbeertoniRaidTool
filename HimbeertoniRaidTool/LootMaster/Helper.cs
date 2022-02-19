using HimbeertoniRaidTool.Data;
using System;
using System.Drawing;
using System.Numerics;

namespace HimbeertoniRaidTool.LootMaster
{
    class Helper
    {
        


        public static Dalamud.Game.ClientState.Objects.Types.Character? Target => (Dalamud.Game.ClientState.Objects.Types.Character?)Services.TargetManager.Target;
        public static AvailableClasses TargetClass => Enum.Parse<AvailableClasses>(Target!.ClassJob.GameData!.Abbreviation.RawString, true);
    }
}
