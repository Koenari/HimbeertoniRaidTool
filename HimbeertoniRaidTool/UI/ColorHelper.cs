using System.Numerics;

namespace HimbeertoniRaidTool.Plugin.UI;

public static class Colors
{
    public static readonly Vector4 Red = new(0.95f, 0.2f, 0.2f, 1f);
    public static readonly Vector4 Yellow = new(0.95f, 0.95f, 0.2f, 1f);
    public static readonly Vector4 Green = new(0.2f, 0.95f, 0.2f, 1f);

    public static Vector4 RedWood => new(0.4f, 0.15f, 0.15f, 1f);

    public static Vector4 WithAlpha(this Vector4 old, float alpha) =>
        new(old.X, old.Y, old.Z, alpha);

}
