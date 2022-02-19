using System;
using System.Drawing;
using System.Numerics;

namespace HimbeertoniRaidTool.UI
{
    class Helper
    {
        public struct ColorHSVA
        {
            public float H;
            public float S;
            public float V;
            public float A;

            public float R => ToColorARGB.R;
            public float G => ToColorARGB.G;
            public float B => ToColorARGB.B;
            public ColorHSVA(float h, float s, float v, float a) => (H, S, V, A) = (h, s, v, a);
            public ColorHSVA(ColorARGB c) => (H, S, V, A) = (c.H, c.S, c.V, c.A);
            public ColorHSVA(Color c) : this(new ColorARGB(c)) { }
            public Color ToColor => ToColorARGB.ToColor;
            public Vector4 ToVec4 => ToColorARGB.ToVec4;
            public ColorARGB ToColorARGB
            {
                get
                {
                    int hi = Convert.ToInt32(MathF.Floor(H / 60)) % 6;
                    float f = H / 60 - MathF.Floor(H / 60);

                    float p = (1 - S);
                    float q = (1 - f * S);
                    float t = (1 - (1 - f) * S);

                    if (hi == 0)
                        return new(A, V, t, p);
                    else if (hi == 1)
                        return new(A, q, V, p);
                    else if (hi == 2)
                        return new(A, p, V, t);
                    else if (hi == 3)
                        return new(A, p, q, V);
                    else if (hi == 4)
                        return new(A, t, p, V);
                    else
                        return new(A, V, p, q);
                }
            }

        }
        public struct ColorARGB
        {
            public float R;
            public float G;
            public float B;
            public float A;

            public float H => ToHSV.H;
            public float S => ToHSV.S;
            public float V => ToHSV.V;


            public ColorARGB(float a, float r, float g, float b) => (A, R, G, B) = (a, r, g, b);
            public ColorARGB(Color c) => (A, R, G, B) = (c.A / 255.0f, c.R / 255.0f, c.G / 255.0f, c.B / 255.0f);
            public Color ToColor => Color.FromArgb((int)(A * 255), (int)(R * 255), (int)(G * 255), (int)(B * 255));
            public Vector4 ToVec4 => new(R, G, B, A);

            public ColorHSVA ToHSV
            {
                get
                {
                    float max = MathF.Max(R, MathF.Max(G, B));
                    float min = MathF.Min(R, MathF.Min(G, B));

                    float h = ToColor.GetHue();
                    float s = (max == 0) ? 0 : 1f - (1f * min / max);
                    float v = max;
                    return new(h, s, v, A);
                }
            }
        }
        public static Vector4 Vec4(Color c) => new ColorARGB(c).ToVec4;
    }
}
