using System;
using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using System.Security.Cryptography;
using System.Text;
using ColorHelper;
using Dalamud.Game.ClientState.Objects.SubKinds;
using HimbeertoniRaidTool.Data;
using Lumina.Excel;
using Lumina.Excel.GeneratedSheets;
using Newtonsoft.Json;

namespace HimbeertoniRaidTool
{
    public static class Helper
    {
        private static readonly ExcelSheet<World>? WorldSheet = Services.DataManager.GetExcelSheet<World>();
        public static World? TryGetWorldByName(string name)
        {
            if (WorldSheet == null)
                return null;
            foreach (var row in WorldSheet)
                if (row.Name == name)
                    return row;
            return null;
        }
        public static bool TryGetChar([NotNullWhen(returnValue:true)] out PlayerCharacter? result, string name, World? w = null)
        {
            result = null;
            if (name == null)
                return false;
            if (name.Equals(TargetChar?.Name.TextValue))
                if (w is null || TargetChar?.HomeWorld.GameData?.RowId == w.RowId)
                {
                    result = TargetChar;
                    return true;
                }

            if (name.Equals(Self?.Name.TextValue))
                if (w is null || Self!.HomeWorld.GameData?.RowId == w.RowId)
                {
                    result = Self;
                    return true;
                }
            foreach (var obj in Services.ObjectTable)
                if (obj.GetType().IsAssignableTo(typeof(PlayerCharacter)) && name.Equals(obj?.Name.TextValue))
                    if (w is null || ((PlayerCharacter)obj).HomeWorld.GameData?.RowId == w.RowId)
                    {
                        result = (PlayerCharacter)obj;
                        return true;
                    }
            return false;
        }
        public static PlayerCharacter? TargetChar
        {
            get
            {
                var _targetCopy = Services.TargetManager.Target;
                if (_targetCopy == null)
                    return null;
                else if (!_targetCopy.GetType().IsAssignableTo(typeof(PlayerCharacter)))
                    return null;
                return (PlayerCharacter)_targetCopy;
            }
        }
        public static bool TryGetJob(this PlayerCharacter target, out Job result) =>
            Enum.TryParse(target.ClassJob.GameData?.Abbreviation.RawString, true, out result);

        public static PlayerCharacter? Self => Services.ClientState.LocalPlayer;
        private static readonly Vector4[] ColorCache = new Vector4[4]
        {
            HRTColorConversions.Vec4(ColorName.Green.ToHsv().Saturation(0.8f).Value(0.85f)),
            HRTColorConversions.Vec4(ColorName.Aquamarine.ToHsv().Saturation(0.8f).Value(0.85f)),
            HRTColorConversions.Vec4(ColorName.Yellow.ToHsv().Saturation(0.8f).Value(0.85f)),
            HRTColorConversions.Vec4(ColorName.Red.ToHsv().Saturation(0.8f).Value(0.85f)),
        };
        public static Vector4 ILevelColor(GearItem item, uint maxItemLevel) => (maxItemLevel - (int)item.ItemLevel) switch
        {
            <= 0 => ColorCache[0],
            <= 10 => ColorCache[1],
            <= 20 => ColorCache[2],
            _ => ColorCache[3],
        };
    }
    public static class HRTExtensions
    {
        public static T Clone<T>(this T source)
            => JsonConvert.DeserializeObject<T>(JsonConvert.SerializeObject(source))!;
        public static int ConsistentHash(this string obj)
        {
            SHA512 alg = SHA512.Create();
            byte[] sha = alg.ComputeHash(Encoding.UTF8.GetBytes(obj));
            return sha[0] + 256 * sha[1] + 256 * 256 * sha[2] + 256 * 256 * 256 * sha[2];
        }
    }


}
namespace ColorHelper
{
    public static class HRTColorConversions
    {
        public static Vector4 Vec4(ColorName name) => Vec4(name.ToRgb());
        public static Vector4 Vec4(ColorName name, float alpha) => Vec4(name.ToRgb(), alpha);
        public static Vector4 Vec4(HSV hsv) => Vec4(hsv, 1f);
        public static Vector4 Vec4(HSV hsv, float alpha) => Vec4(ColorConverter.HsvToRgb(hsv), alpha);
        public static Vector4 Vec4(RGB c) => Vec4(c, 1f);
        public static Vector4 Vec4(RGB c, float alpha) => new(c.R / 100f, c.G / 100f, c.B / 100f, alpha);
    }
    public static class ColorExtensions
    {
        public static HSV Saturation(this HSV hsv, float sat)
        {
            hsv.S = (byte)(hsv.S * sat);
            if (hsv.S < 0)
                hsv.S = 0;
            if (hsv.S > 100)
                hsv.S = 100;
            return hsv;
        }
        public static HSV Value(this HSV hsv, float val)
        {
            hsv.V = (byte)(hsv.V * val);
            if (hsv.V < 0)
                hsv.V = 0;
            if (hsv.V > 100)
                hsv.V = 100;
            return hsv;
        }

    }
}
