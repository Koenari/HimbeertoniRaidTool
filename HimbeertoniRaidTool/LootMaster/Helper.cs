using HimbeertoniRaidTool.Data;
using System;
using System.Numerics;

namespace HimbeertoniRaidTool.LootMaster
{
    public class Color
    {
        public static Vector4 White => new(1.0f, 1.0f, 1.0f, 1.0f);
        public static Vector4 Black => new(0.0f, 0.0f, 0.0f, 1.0f);
        public static Vector4 Red => new(1.0f, 0.0f, 0.0f, 1.0f);
        public static Vector4 Green => new(0.0f, 1.0f, 0.0f, 1.0f);
        public static Vector4 Blue => new(0.0f, 0.0f, 1.0f, 1.0f);
        public static Vector4 Yellow => new(1.0f, 1.0f, 0.0f, 1.0f);
        public static Vector4 Cyan => new(0.0f, 1.0f, 1.0f, 1.0f);
        public static Vector4 Magenta => new(1.0f, 0.0f, 1.0f, 1.0f);
        public static Vector4 Pink => new(1.0f, 0.412f, 0.706f, 1.0f);
        public static Vector4 BabyBlue => new(0.537f, 0.812f, 0.941f, 1.0f);
        public static Vector4 Purple => new(0.749f, 0.0f, 1.0f, 1.0f);

        public static Vector4 WithAlpha(Vector4 c, float alpha) => new(c.X, c.Y, c.Z, alpha);
    }
    class Helper
    {
        
        public static Dalamud.Game.ClientState.Objects.Types.Character? Target => (Dalamud.Game.ClientState.Objects.Types.Character?)Services.TargetManager.Target;
        public static AvailableClasses TargetClass => Enum.Parse<AvailableClasses>(Target!.ClassJob.GameData!.Abbreviation.RawString, true);
    }
}
