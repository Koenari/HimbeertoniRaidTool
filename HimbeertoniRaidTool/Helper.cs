﻿using HimbeertoniRaidTool.Data;
using Newtonsoft.Json;
using System;
using System.Numerics;

namespace HimbeertoniRaidTool
{
    public static class Helper
    {
        public static Dalamud.Game.ClientState.Objects.Types.Character? Target => (Dalamud.Game.ClientState.Objects.Types.Character?)Services.TargetManager.Target;
        public static AvailableClasses TargetClass => Enum.Parse<AvailableClasses>(Target!.ClassJob.GameData!.Abbreviation.RawString, true);
    }
    public static class HRTExtensions
    {
        public static T Clone<T>(this T source)
        {
            var serialized = JsonConvert.SerializeObject(source);
            return JsonConvert.DeserializeObject<T>(serialized)!;
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