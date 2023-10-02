using HimbeertoniRaidTool.Common.Data;

namespace HimbeertoniRaidTool.Plugin.DataExtensions;

public static class DataExtensions
{
    public static string Name(this GameExpansion exp)
    {
        return exp.GameVersion switch
        {
            2 => Localization.Localize("EXP_2", "A Realm Reborn"),
            3 => Localization.Localize("EXP_3", "Heavensward"),
            4 => Localization.Localize("EXP_4", "Stormblood"),
            5 => Localization.Localize("EXP_5", "Shadowbringers"),
            6 => Localization.Localize("EXP_6", "Endwalker"),
            _ => Localization.Localize("Unknown", "Unknown"),
        };
    }
}