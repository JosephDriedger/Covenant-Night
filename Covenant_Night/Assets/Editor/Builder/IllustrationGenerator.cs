using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

// Generates the stylised night-scene illustrations shown on the story panels (flat-colour silhouettes,
// warm torch glows, starry skies). Original, procedural art — replace the PNGs with hand-painted panels
// any time; the story data references sprites by asset.
public static class IllustrationGenerator
{
    const int W = 768, H = 432;

    class Canvas
    {
        public readonly int W, H;               // shadow the story-panel defaults so branding art can use other sizes
        public Color[] px;                      // index = y*W + x, y measured from the TOP (flipped on save)

        public Canvas(int w = IllustrationGenerator.W, int h = IllustrationGenerator.H)
        {
            W = w; H = h;
            px = new Color[w * h];
        }

        public Color At(int x, int y) => px[y * W + x];

        public void Gradient(Color top, Color bottom)
        {
            for (int y = 0; y < H; y++)
            {
                Color c = Color.Lerp(top, bottom, y / (float)(H - 1));
                for (int x = 0; x < W; x++) px[y * W + x] = c;
            }
        }

        public void Blend(int x, int y, Color c)
        {
            if (x < 0 || y < 0 || x >= W || y >= H) return;
            Color d = px[y * W + x];
            px[y * W + x] = new Color(d.r * (1 - c.a) + c.r * c.a, d.g * (1 - c.a) + c.g * c.a, d.b * (1 - c.a) + c.b * c.a, 1f);
        }

        public void Add(int x, int y, Color c, float k)
        {
            if (x < 0 || y < 0 || x >= W || y >= H) return;
            Color d = px[y * W + x];
            px[y * W + x] = new Color(Mathf.Min(1, d.r + c.r * k), Mathf.Min(1, d.g + c.g * k), Mathf.Min(1, d.b + c.b * k), 1f);
        }

        public void Rect(int x0, int y0, int x1, int y1, Color c)
        {
            for (int y = Mathf.Max(0, y0); y < Mathf.Min(H, y1); y++)
                for (int x = Mathf.Max(0, x0); x < Mathf.Min(W, x1); x++) Blend(x, y, c);
        }

        public void Disc(float cx, float cy, float r, Color c)
        {
            for (int y = (int)(cy - r - 1); y <= cy + r + 1; y++)
                for (int x = (int)(cx - r - 1); x <= cx + r + 1; x++)
                {
                    float d = Mathf.Sqrt((x - cx) * (x - cx) + (y - cy) * (y - cy));
                    float a = Mathf.Clamp01(r - d + 0.5f);
                    if (a > 0) Blend(x, y, new Color(c.r, c.g, c.b, c.a * a));
                }
        }

        public void Glow(float cx, float cy, float r, Color c, float strength)
        {
            for (int y = (int)(cy - r); y <= cy + r; y++)
                for (int x = (int)(cx - r); x <= cx + r; x++)
                {
                    float d = Mathf.Sqrt((x - cx) * (x - cx) + (y - cy) * (y - cy)) / r;
                    if (d >= 1f) continue;
                    float k = (1f - d); k *= k;
                    Add(x, y, c, k * strength);
                }
        }

        public void Poly(Vector2[] p, Color c)
        {
            float minY = float.MaxValue, maxY = float.MinValue;
            foreach (var v in p) { minY = Mathf.Min(minY, v.y); maxY = Mathf.Max(maxY, v.y); }
            for (int y = Mathf.Max(0, (int)minY); y <= Mathf.Min(H - 1, (int)maxY); y++)
            {
                var xs = new List<float>();
                for (int i = 0; i < p.Length; i++)
                {
                    Vector2 a = p[i], b = p[(i + 1) % p.Length];
                    if ((a.y <= y && b.y > y) || (b.y <= y && a.y > y))
                        xs.Add(a.x + (y - a.y) / (b.y - a.y) * (b.x - a.x));
                }
                xs.Sort();
                for (int i = 0; i + 1 < xs.Count; i += 2)
                    for (int x = (int)xs[i]; x <= (int)xs[i + 1]; x++) Blend(x, y, c);
            }
        }

        public void Stars(int count, int seed, int maxY)
        {
            var r = new System.Random(seed);
            for (int i = 0; i < count; i++)
            {
                int x = r.Next(W), y = r.Next(maxY);
                float b = 0.4f + (float)r.NextDouble() * 0.6f;
                Blend(x, y, new Color(1f, 1f, 0.92f, b));
                if (r.NextDouble() < 0.15) { Blend(x + 1, y, new Color(1, 1, 1, b * 0.5f)); Blend(x, y + 1, new Color(1, 1, 1, b * 0.5f)); }
            }
        }

        public void Moon(float cx, float cy, float r)
        {
            Glow(cx, cy, r * 4f, new Color(0.45f, 0.55f, 0.8f), 0.35f);
            Disc(cx, cy, r, new Color(0.92f, 0.94f, 1f, 1f));
            Disc(cx + r * 0.3f, cy - r * 0.1f, r * 0.95f, new Color(0.1f, 0.13f, 0.28f, 0.0f));
        }

        // Ridge line of hills
        public void Hills(int baseY, float amp, int seed, Color c)
        {
            var r = new System.Random(seed);
            float p1 = (float)r.NextDouble() * 6f, p2 = (float)r.NextDouble() * 6f;
            for (int x = 0; x < W; x++)
            {
                float y = baseY + Mathf.Sin(x * 0.011f + p1) * amp + Mathf.Sin(x * 0.027f + p2) * amp * 0.4f;
                for (int yy = (int)y; yy < H; yy++) Blend(x, yy, c);
            }
        }

        public void Skyline(int baseY, int seed, Color c, Color window, int minH, int maxH)
        {
            var r = new System.Random(seed);
            int x = -10;
            while (x < W)
            {
                int w = 30 + r.Next(50), h = minH + r.Next(maxH - minH);
                Rect(x, baseY - h, x + w, H, c);
                if (r.NextDouble() < 0.5) Rect(x + 4, baseY - h - 6, x + w - 4, baseY - h, c);   // flat-roof parapet
                for (int i = 0; i < 3; i++)
                    if (r.NextDouble() < 0.45)
                    {
                        int wx = x + 6 + r.Next(Mathf.Max(1, w - 16)), wy = baseY - h + 10 + r.Next(Mathf.Max(1, h - 22));
                        Rect(wx, wy, wx + 5, wy + 8, window);
                        Glow(wx + 2, wy + 4, 14, window, 0.35f);
                    }
                x += w + r.Next(6);
            }
        }

        public void Figure(float cx, float footY, float height, Color c, bool cloak = true)
        {
            float bodyH = height * 0.62f, headR = height * 0.09f;
            Poly(new[] {
                new Vector2(cx - height * 0.13f, footY - bodyH),
                new Vector2(cx + height * 0.13f, footY - bodyH),
                new Vector2(cx + (cloak ? height * 0.19f : height * 0.12f), footY),
                new Vector2(cx - (cloak ? height * 0.19f : height * 0.12f), footY) }, c);
            Disc(cx, footY - bodyH - headR * 0.9f, headR, c);
        }

        public void Torch(float x, float y, float scale)
        {
            Rect((int)(x - 2 * scale), (int)y, (int)(x + 2 * scale), (int)(y + 46 * scale), new Color(0.12f, 0.08f, 0.05f, 1));
            Glow(x, y - 4 * scale, 90 * scale, new Color(1f, 0.55f, 0.15f), 0.75f);
            Disc(x, y - 4 * scale, 5 * scale, new Color(1f, 0.85f, 0.4f, 1));
        }

        public void Vignette(float strength)
        {
            for (int y = 0; y < H; y++)
                for (int x = 0; x < W; x++)
                {
                    float dx = (x / (float)W - 0.5f) * 2f, dy = (y / (float)H - 0.5f) * 2f;
                    float k = Mathf.Clamp01((dx * dx + dy * dy) - 0.35f) * strength;
                    Color d = px[y * W + x];
                    px[y * W + x] = new Color(d.r * (1 - k), d.g * (1 - k), d.b * (1 - k), 1f);
                }
        }

        // Stone wall with an arched opening (hole shows what was painted before)
        public void GateWall(int x0, int x1, int topY, int baseY, int cx, int halfW, int archTopY, Color stone, Color? doors)
        {
            int archStart = archTopY + halfW;
            for (int y = topY; y < baseY; y++)
                for (int x = x0; x < x1; x++)
                {
                    bool hole = Mathf.Abs(x - cx) < halfW &&
                                (y >= archStart || (x - cx) * (x - cx) + (y - archStart) * (y - archStart) <= halfW * halfW) &&
                                y >= archTopY;
                    if (!hole)
                    {
                        float shade = 0.92f + 0.08f * Mathf.Sin(x * 0.4f) * Mathf.Sin(y * 0.3f);
                        Blend(x, y, new Color(stone.r * shade, stone.g * shade, stone.b * shade, 1));
                    }
                    else if (doors.HasValue)
                    {
                        float plank = (x - cx + halfW) % 22 < 2 ? 0.55f : 1f;
                        Color d = doors.Value;
                        Blend(x, y, new Color(d.r * plank, d.g * plank, d.b * plank, 1));
                    }
                }
        }

        public void Line(Vector2 a, Vector2 b, float thick, Color c)
        {
            Vector2 d = (b - a).normalized, n = new Vector2(-d.y, d.x) * thick * 0.5f, e = d * thick * 0.5f;
            Poly(new[] { a - e + n, b + e + n, b + e - n, a - e - n }, c);
        }

        // Block capitals on a 0.7 x 1 unit cell (y down), stroked with Line; enough for "COVENANT NIGHT".
        static readonly Dictionary<char, Vector2[][]> Glyphs = BuildGlyphs();

        static Vector2[] Arc(float a0, float a1)
        {
            var p = new Vector2[25];
            for (int i = 0; i < p.Length; i++)
            {
                float t = Mathf.Deg2Rad * Mathf.Lerp(a0, a1, i / (p.Length - 1f));
                p[i] = new Vector2(0.35f + 0.35f * Mathf.Cos(t), 0.5f + 0.5f * Mathf.Sin(t));
            }
            return p;
        }

        static Dictionary<char, Vector2[][]> BuildGlyphs()
        {
            Vector2 V(float x, float y) => new Vector2(x, y);
            return new Dictionary<char, Vector2[][]>
            {
                ['C'] = new[] { Arc(-40f, -320f) },
                ['O'] = new[] { Arc(0f, 360f) },
                ['V'] = new[] { new[] { V(0, 0), V(.35f, 1), V(.7f, 0) } },
                ['E'] = new[] { new[] { V(.65f, 0), V(0, 0), V(0, 1), V(.65f, 1) }, new[] { V(0, .5f), V(.5f, .5f) } },
                ['N'] = new[] { new[] { V(0, 1), V(0, 0), V(.7f, 1), V(.7f, 0) } },
                ['A'] = new[] { new[] { V(0, 1), V(.35f, 0), V(.7f, 1) }, new[] { V(.14f, .68f), V(.56f, .68f) } },
                ['T'] = new[] { new[] { V(0, 0), V(.7f, 0) }, new[] { V(.35f, 0), V(.35f, 1) } },
                ['I'] = new[] { new[] { V(.35f, 0), V(.35f, 1) }, new[] { V(.12f, 0), V(.58f, 0) }, new[] { V(.12f, 1), V(.58f, 1) } },
                ['G'] = new[] { Arc(-40f, -360f), new[] { V(.7f, .5f), V(.4f, .5f) } },
                ['H'] = new[] { new[] { V(0, 0), V(0, 1) }, new[] { V(.7f, 0), V(.7f, 1) }, new[] { V(0, .5f), V(.7f, .5f) } },
            };
        }

        // Draws a line of text centred on (cx, cy), scaled so it is `width` pixels wide.
        void TextLine(string s, float cx, float cy, float width, float letterH, Color col)
        {
            float cellW = letterH * 0.7f, gap = letterH * 0.34f;
            float total = s.Length * cellW + (s.Length - 1) * gap;
            float k = width > 0f ? width / total : 1f;
            float x = cx - total * k * 0.5f, thick = letterH * k * 0.085f;
            foreach (char ch in s)
            {
                if (Glyphs.TryGetValue(ch, out var strokes))
                    foreach (var stroke in strokes)
                        for (int i = 0; i + 1 < stroke.Length; i++)
                            Line(new Vector2(x + stroke[i].x * letterH * k, cy - letterH * k * 0.5f + stroke[i].y * letterH * k),
                                 new Vector2(x + stroke[i + 1].x * letterH * k, cy - letterH * k * 0.5f + stroke[i + 1].y * letterH * k),
                                 thick, col);
                x += (cellW + gap) * k;
            }
        }

        // "COVENANT" over "NIGHT" in warm torch gold, with a soft glow behind.
        public void Title(float cx, float cy, float width)
        {
            var gold = new Color(1f, 0.82f, 0.48f, 1f);
            Glow(cx, cy, width * 0.45f, Warm, 0.10f);
            TextLine("COVENANT", cx, cy - 46, width, 62f, gold);
            TextLine("NIGHT", cx, cy + 46, width * 0.61f, 62f, gold);
        }

        public Texture2D ToTexture()
        {
            var t = new Texture2D(W, H, TextureFormat.RGBA32, false);
            var flipped = new Color[W * H];
            for (int y = 0; y < H; y++)
                Array.Copy(px, y * W, flipped, (H - 1 - y) * W, W);
            t.SetPixels(flipped);
            t.Apply();
            return t;
        }
    }

    static readonly Color Night1 = new Color(0.03f, 0.05f, 0.14f), Night2 = new Color(0.11f, 0.13f, 0.27f);
    static readonly Color Warm = new Color(1f, 0.72f, 0.35f);

    static Canvas Sky(int seed)
    {
        var c = new Canvas();
        c.Gradient(Night1, Night2);
        c.Stars(260, seed, 250);
        return c;
    }

    static Canvas Warning()
    {
        var c = Sky(1);
        c.Moon(610, 90, 30);
        c.Hills(300, 12, 3, new Color(0.06f, 0.07f, 0.16f, 1));
        c.Skyline(340, 5, new Color(0.05f, 0.05f, 0.1f, 1), Warm, 60, 150);
        c.Rect(0, 350, W, H, new Color(0.03f, 0.03f, 0.06f, 1));
        c.Glow(190, 330, 140, Warm, 0.6f);
        c.Figure(150, 400, 190, new Color(0.02f, 0.02f, 0.04f, 1));   // servant
        c.Figure(265, 400, 205, new Color(0.03f, 0.03f, 0.07f, 1));   // Jonathan, woken
        c.Disc(178, 315, 9, new Color(1f, 0.9f, 0.55f, 1));           // the servant's lamp
        c.Vignette(0.7f);
        return c;
    }

    static Canvas WakeDavid()
    {
        var c = new Canvas();
        c.Gradient(new Color(0.06f, 0.04f, 0.05f), new Color(0.16f, 0.1f, 0.07f));
        c.Rect(0, 340, W, H, new Color(0.1f, 0.06f, 0.04f, 1));                        // floor
        c.Glow(560, 210, 260, Warm, 0.8f);                                              // lamp
        c.Rect(120, 270, 470, 330, new Color(0.19f, 0.12f, 0.08f, 1));                 // bed
        c.Rect(120, 250, 470, 275, new Color(0.3f, 0.2f, 0.16f, 1));                   // blanket
        c.Disc(160, 258, 24, new Color(0.08f, 0.05f, 0.04f, 1));                       // David sitting up (head)
        c.Figure(215, 300, 130, new Color(0.06f, 0.04f, 0.04f, 1));
        c.Figure(600, 400, 215, new Color(0.02f, 0.02f, 0.04f, 1));                    // Jonathan
        c.Rect(548, 300, 552, 400, new Color(0.15f, 0.1f, 0.07f, 1));                  // lamp stand
        c.Disc(550, 292, 9, new Color(1f, 0.9f, 0.5f, 1));
        // harp silhouette
        c.Poly(new[] { new Vector2(40, 400), new Vector2(46, 260), new Vector2(120, 240), new Vector2(60, 400) }, new Color(0.05f, 0.03f, 0.02f, 1));
        c.Vignette(0.8f);
        return c;
    }

    static Canvas CityNight(int seed)
    {
        var c = Sky(seed);
        c.Moon(120 + seed * 60 % 500, 80, 24);
        c.Hills(280, 10, seed, new Color(0.07f, 0.08f, 0.17f, 1));
        c.Skyline(360, seed + 7, new Color(0.05f, 0.05f, 0.1f, 1), Warm, 70, 170);
        c.Rect(0, 372, W, H, new Color(0.03f, 0.03f, 0.05f, 1));
        c.Torch(120, 300, 1f);
        c.Torch(640, 310, 1f);
        c.Figure(300, 410, 190, new Color(0.02f, 0.02f, 0.03f, 1));       // Jonathan
        c.Figure(390, 412, 180, new Color(0.03f, 0.03f, 0.04f, 1));       // David
        c.Vignette(0.7f);
        return c;
    }

    static Canvas GateApproach()
    {
        var c = Sky(11);
        c.Moon(650, 70, 26);
        c.Hills(250, 8, 12, new Color(0.06f, 0.07f, 0.15f, 1));
        c.GateWall(60, 708, 130, 400, W / 2, 80, 160, new Color(0.24f, 0.2f, 0.17f, 1), new Color(0.15f, 0.09f, 0.05f, 1));
        c.Rect(0, 400, W, H, new Color(0.04f, 0.04f, 0.06f, 1));
        c.Torch(250, 310, 1.1f);
        c.Torch(520, 310, 1.1f);
        c.Figure(384, 410, 250, new Color(0.02f, 0.02f, 0.03f, 1), true);
        c.Vignette(0.65f);
        return c;
    }

    static Canvas GateBluff()
    {
        var c = Sky(21);
        c.GateWall(0, W, 100, 420, 590, 90, 130, new Color(0.24f, 0.2f, 0.17f, 1), new Color(0.15f, 0.09f, 0.05f, 1));
        c.Rect(0, 400, W, H, new Color(0.04f, 0.04f, 0.06f, 1));
        c.Torch(300, 250, 1.3f);
        c.Figure(430, 420, 330, new Color(0.03f, 0.02f, 0.03f, 1));     // the commander looms
        c.Figure(150, 430, 300, new Color(0.02f, 0.02f, 0.04f, 1));     // Jonathan (foreground)
        // his raised hand with the signet ring catching the light
        c.Rect(190, 265, 214, 275, new Color(0.02f, 0.02f, 0.04f, 1));
        c.Disc(216, 265, 8, new Color(0.03f, 0.03f, 0.05f, 1));
        c.Glow(216, 262, 46, new Color(1f, 0.85f, 0.4f), 1f);
        c.Disc(216, 262, 3, new Color(1f, 1f, 0.8f, 1));
        c.Vignette(0.75f);
        return c;
    }

    static Canvas Farewell(bool withFigures)
    {
        var c = Sky(31);
        c.Moon(380, 110, 34);
        c.Hills(300, 16, 33, new Color(0.05f, 0.06f, 0.14f, 1));
        c.Hills(340, 10, 34, new Color(0.03f, 0.04f, 0.09f, 1));
        if (withFigures) c.Figure(430, 345, 60, new Color(0.02f, 0.02f, 0.04f, 1), true);   // David, far along the road
        // road glow
        c.Poly(new[] { new Vector2(360, 432), new Vector2(430, 335), new Vector2(438, 335), new Vector2(560, 432) }, new Color(0.14f, 0.15f, 0.24f, 0.6f));
        // gate frame in the foreground
        c.GateWall(0, W, 0, H, W / 2, 200, 70, new Color(0.10f, 0.08f, 0.07f, 1), null);
        c.Torch(60, 250, 1.4f);
        c.Torch(708, 250, 1.4f);
        if (withFigures) c.Figure(150, 440, 320, new Color(0.01f, 0.01f, 0.02f, 1));       // Jonathan on the threshold
        c.Vignette(0.7f);
        return c;
    }

    static Canvas Throne()
    {
        var c = new Canvas();
        c.Gradient(new Color(0.09f, 0.02f, 0.03f), new Color(0.2f, 0.06f, 0.05f));
        for (int i = 0; i < 5; i++)
        {
            int px = 60 + i * 160;
            c.Rect(px, 0, px + 34, 420, new Color(0.08f, 0.02f, 0.03f, 1));
        }
        c.Rect(0, 400, W, H, new Color(0.06f, 0.02f, 0.02f, 1));
        c.Glow(384, 250, 300, new Color(1f, 0.3f, 0.15f), 0.6f);
        c.Rect(340, 190, 430, 400, new Color(0.05f, 0.02f, 0.02f, 1));                      // throne
        c.Rect(325, 150, 445, 200, new Color(0.05f, 0.02f, 0.02f, 1));
        c.Figure(385, 330, 190, new Color(0.02f, 0.0f, 0.01f, 1));                          // Saul, seated
        c.Disc(385, 215, 7, new Color(0.8f, 0.6f, 0.2f, 1));                                // crown
        c.Figure(210, 430, 240, new Color(0.01f, 0.01f, 0.02f, 1), false);                  // Jonathan, held
        c.Figure(150, 430, 210, new Color(0.03f, 0.01f, 0.01f, 1), false);                  // guard
        c.Figure(270, 430, 210, new Color(0.03f, 0.01f, 0.01f, 1), false);                  // guard
        c.Vignette(0.85f);
        return c;
    }

    // A guard's lantern throws nested cones (green, amber, red); Jonathan waits just outside the light.
    static Canvas GuardCones()
    {
        var dark = new Color(0.02f, 0.02f, 0.03f, 1);
        var c = Sky(41);
        c.Moon(120, 80, 22);
        c.Hills(290, 10, 42, new Color(0.07f, 0.08f, 0.17f, 1));
        c.Skyline(360, 47, new Color(0.05f, 0.05f, 0.1f, 1), Warm, 70, 170);
        c.Rect(0, 372, W, H, new Color(0.03f, 0.03f, 0.05f, 1));

        void Wedge(float end, Color col) =>
            c.Poly(new[] { new Vector2(232, 312), new Vector2(end, 368), new Vector2(end, 432), new Vector2(250, 432) }, col);
        Wedge(600, new Color(0.25f, 0.85f, 0.35f, 0.20f));
        Wedge(455, new Color(1f, 0.7f, 0.15f, 0.24f));
        Wedge(340, new Color(1f, 0.2f, 0.15f, 0.30f));

        c.Figure(200, 410, 200, dark, false);                                          // guard
        c.Rect(170, 175, 174, 410, new Color(0.05f, 0.04f, 0.04f, 1));                 // spear shaft
        c.Poly(new[] { new Vector2(166, 178), new Vector2(178, 178), new Vector2(172, 152) }, new Color(0.5f, 0.5f, 0.55f, 1));
        c.Glow(232, 312, 70, Warm, 0.9f);
        c.Disc(232, 312, 7, new Color(1f, 0.9f, 0.5f, 1));                             // lantern

        c.Rect(668, 240, W, H, new Color(0.05f, 0.04f, 0.07f, 1));                      // wall he hides beside
        c.Figure(704, 428, 190, new Color(0.01f, 0.01f, 0.02f, 1));                     // Jonathan, just outside the light
        c.Vignette(0.7f);
        return c;
    }

    // David waits in cover by the golden marker while Jonathan lures a guard away with a thrown stone.
    static Canvas LeadDavid()
    {
        var dark = new Color(0.02f, 0.02f, 0.03f, 1);
        var c = Sky(51);
        c.Moon(600, 90, 24);
        c.Hills(290, 10, 52, new Color(0.07f, 0.08f, 0.17f, 1));
        c.Skyline(360, 57, new Color(0.05f, 0.05f, 0.1f, 1), Warm, 70, 170);
        c.Rect(0, 372, W, H, new Color(0.03f, 0.03f, 0.05f, 1));

        c.Figure(140, 400, 130, new Color(0.03f, 0.03f, 0.05f, 1));                     // David, hunched in cover
        c.Rect(50, 352, 250, 432, new Color(0.09f, 0.08f, 0.1f, 1));                    // low wall
        c.Glow(300, 428, 70, new Color(1f, 0.8f, 0.2f), 0.9f);                          // golden marker
        c.Disc(300, 428, 6, new Color(1f, 0.92f, 0.5f, 1));
        c.Figure(370, 425, 215, dark);                                                  // Jonathan

        for (int i = 0; i <= 14; i++)                                                   // the thrown stone's arc
        {
            float t = i / 14f;
            c.Disc(415 + t * 320, Mathf.Lerp(268, 396, t) - 95f * Mathf.Sin(Mathf.PI * t), 3f, new Color(0.9f, 0.9f, 0.85f, 0.35f + 0.5f * t));
        }

        c.Figure(610, 410, 190, dark, false);                                           // the guard turns toward the sound
        c.Rect(585, 175, 589, 410, new Color(0.05f, 0.04f, 0.04f, 1));
        c.Poly(new[] { new Vector2(581, 178), new Vector2(593, 178), new Vector2(587, 152) }, new Color(0.5f, 0.5f, 0.55f, 1));
        c.Glow(636, 318, 60, Warm, 0.8f);
        c.Vignette(0.7f);
        return c;
    }

    // Emblem used for the app icon (square) and the splash logo (wide): a full moon over the city gate, torch-lit,
    // with Jonathan and David as small silhouettes standing in the opening.
    static Canvas Emblem(int w, int totalH, int seed, bool title = false)
    {
        int band = title ? 230 : 0, h = totalH - band;
        var c = new Canvas(w, totalH);
        c.Gradient(Night1, Night2);
        c.Stars(w * h / 1400, seed, h * 6 / 10);
        float u = h / 512f;
        c.Moon(w * 0.5f, h * 0.30f, 74 * u);

        c.Hills((int)(h * 0.70f), 14 * u, seed + 1, new Color(0.05f, 0.07f, 0.15f, 1f));
        var stone = new Color(0.10f, 0.11f, 0.16f);
        int cx = w / 2, halfW = (int)(70 * u), baseY = (int)(h * 0.93f);
        int archTop = (int)(h * 0.55f);
        c.GateWall(cx - (int)(190 * u), cx + (int)(190 * u), (int)(h * 0.50f), baseY, cx, halfW, archTop, stone, new Color(0.95f, 0.55f, 0.18f));
        c.Glow(cx, baseY - 70 * u, 150 * u, Warm, 0.55f);
        c.Rect(cx - (int)(190 * u), (int)(h * 0.50f) - (int)(8 * u), cx + (int)(190 * u), (int)(h * 0.50f), stone);

        c.Torch(cx - 105 * u, h * 0.60f, 1.1f * u);
        c.Torch(cx + 105 * u, h * 0.60f, 1.1f * u);

        var dark = new Color(0.02f, 0.02f, 0.05f, 1f);
        c.Figure(cx - 22 * u, baseY, 112 * u, dark);
        c.Figure(cx + 24 * u, baseY, 92 * u, dark);
        if (title) c.Rect(0, baseY, w, totalH, new Color(0.02f, 0.02f, 0.05f, 1f));
        c.Vignette(0.75f);
        if (title) c.Title(w / 2f, h + band * 0.5f, w * 0.52f);
        return c;
    }

    // Writes the game icon (square) and splash logo (wide) PNGs.
    public static void GenerateBranding(string folder)
    {
        Directory.CreateDirectory(folder);
        void Save(string name, Canvas c)
        {
            var tex = c.ToTexture();
            File.WriteAllBytes($"{folder}/{name}.png", tex.EncodeToPNG());
            UnityEngine.Object.DestroyImmediate(tex);
        }
        Save("icon", Emblem(512, 512, 11));
        Save("splash_logo", Emblem(1024, 800, 12, true));
    }

    public static Dictionary<string, string> GenerateAll(string folder)
    {
        Directory.CreateDirectory(folder);
        var map = new Dictionary<string, string>();
        void Save(string name, Canvas c)
        {
            var tex = c.ToTexture();
            string path = $"{folder}/{name}.png";
            File.WriteAllBytes(path, tex.EncodeToPNG());
            UnityEngine.Object.DestroyImmediate(tex);
            map[name] = path;
        }
        Save("ill_warning", Warning());
        Save("ill_wake_david", WakeDavid());
        Save("ill_guard_cones", GuardCones());
        Save("ill_lead_david", LeadDavid());
        Save("ill_city_market", CityNight(2));
        Save("ill_city_alley", CityNight(3));
        Save("ill_city_well", CityNight(4));
        Save("ill_gate_approach", GateApproach());
        Save("ill_gate_bluff", GateBluff());
        Save("ill_farewell", Farewell(true));
        Save("ill_empty_gate", Farewell(false));
        Save("ill_throne", Throne());
        return map;
    }
}
