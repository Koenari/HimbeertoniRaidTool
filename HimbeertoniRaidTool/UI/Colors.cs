using System.Numerics;

namespace HimbeertoniRaidTool.Plugin.UI;

public static class Colors
{
    public static readonly Vector4 Red = new(0.95f, 0.2f, 0.2f, 1f);
    public static readonly Vector4 Yellow = new(0.95f, 0.95f, 0.2f, 1f);
    public static readonly Vector4 Green = new(0.2f, 0.95f, 0.2f, 1f);
    public static readonly Vector4 RedWood = new(0.4f, 0.15f, 0.15f, 1f);
    public static readonly Vector4 PetrolDark = new(0.0f, 0.4f, 0.45f, 1f);

    public static readonly Vector4 TextGreen = new(0.17f, 0.85f, 0.17f, 1f);
    public static readonly Vector4 TextYellow = new(0.85f, 0.85f, 0.17f, 1f);
    public static readonly Vector4 TextRed = new(0.85f, 0.17f, 0.17f, 1f);
    public static readonly Vector4 TextSoftRed = new(0.85f, 0.27f, 0.27f, 1f);
    public static readonly Vector4 TextWhite = new(1f);
    public static readonly Vector4 TextPetrol = new(0f, 0.75f, 0.83f, 1f);
    public static readonly Vector4 TextLink = new(0.35f, 0.35f, 1f, 1f);


    public static readonly Vector4 Etro = new(0.70f, 0.375f, 0.65f, 1f);
    public static readonly Vector4 TheBalance = new(1.0f, 0.6f, 0f, 1f);
    public static readonly Vector4 XivRaidTool = new(0.85f, 0.24f, 0.6f, 1f);

    public static Vector4 TextColor(this GearSetManager manager) => manager switch
    {

        GearSetManager.Hrt     => XivRaidTool,
        GearSetManager.Etro    => Etro,
        GearSetManager.XivGear => TheBalance,
        _                      => TextWhite,
    };
}