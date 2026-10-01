using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

// Minimal TrueType reader: pulls glyph outlines (simple glyphs only) and advances for a character, so the builder
// can paint real font lettering into generated PNGs without needing a GPU or a readable font atlas.
public class TtfOutlines
{
    public readonly int UnitsPerEm;
    readonly byte[] d;
    readonly Dictionary<string, (int off, int len)> tables = new Dictionary<string, (int, int)>();
    readonly int locaFormat, numHMetrics;
    int cmapOff;

    public TtfOutlines(string path)
    {
        d = File.ReadAllBytes(path);
        int n = U16(4);
        for (int i = 0; i < n; i++)
        {
            int r = 12 + i * 16;
            tables[System.Text.Encoding.ASCII.GetString(d, r, 4)] = (I32(r + 8), I32(r + 12));
        }
        UnitsPerEm = U16(tables["head"].off + 18);
        locaFormat = S16(tables["head"].off + 50);
        numHMetrics = U16(tables["hhea"].off + 34);
        FindCmap();
    }

    int U16(int o) => (d[o] << 8) | d[o + 1];
    int S16(int o) => (short)U16(o);
    int I32(int o) => (d[o] << 24) | (d[o + 1] << 16) | (d[o + 2] << 8) | d[o + 3];

    void FindCmap()
    {
        int t = tables["cmap"].off, n = U16(t + 2);
        for (int i = 0; i < n; i++)
        {
            int plat = U16(t + 4 + i * 8), enc = U16(t + 6 + i * 8), off = t + I32(t + 8 + i * 8);
            if (plat == 3 && enc == 1 && U16(off) == 4) { cmapOff = off; return; }
        }
        throw new Exception("TTF has no Windows Unicode BMP cmap (format 4)");
    }

    int GlyphIndex(char ch)
    {
        int o = cmapOff, segX2 = U16(o + 6);
        int endO = o + 14, startO = endO + segX2 + 2, deltaO = startO + segX2, rangeO = deltaO + segX2;
        for (int i = 0; i < segX2 / 2; i++)
        {
            if (ch > U16(endO + i * 2) || ch < U16(startO + i * 2)) continue;
            int range = U16(rangeO + i * 2), delta = U16(deltaO + i * 2);
            if (range == 0) return (ch + delta) & 0xFFFF;
            int g = U16(rangeO + i * 2 + range + (ch - U16(startO + i * 2)) * 2);
            return g == 0 ? 0 : (g + delta) & 0xFFFF;
        }
        return 0;
    }

    public int Advance(char ch)
    {
        int g = GlyphIndex(ch);
        return U16(tables["hmtx"].off + Math.Min(g, numHMetrics - 1) * 4);
    }

    // Contours as flattened polylines in font units (y up).
    public List<Vector2[]> Outline(char ch)
    {
        var result = new List<Vector2[]>();
        int g = GlyphIndex(ch);
        int loca = tables["loca"].off, glyf = tables["glyf"].off;
        int a = locaFormat == 0 ? U16(loca + g * 2) * 2 : I32(loca + g * 4);
        int b = locaFormat == 0 ? U16(loca + g * 2 + 2) * 2 : I32(loca + g * 4 + 4);
        if (a == b) return result;                      // empty glyph (space)

        int p = glyf + a, nc = S16(p);
        if (nc < 0) throw new Exception($"Glyph '{ch}' is composite; not supported");
        p += 10;
        var ends = new int[nc];
        for (int i = 0; i < nc; i++) { ends[i] = U16(p); p += 2; }
        int pts = ends[nc - 1] + 1;
        p += 2 + U16(p);                                // skip instructions

        var flags = new byte[pts];
        for (int i = 0; i < pts;)
        {
            byte f = d[p++];
            flags[i++] = f;
            if ((f & 8) != 0)
                for (int r = d[p++]; r > 0; r--) flags[i++] = f;
        }
        var xs = new int[pts]; var ys = new int[pts];
        int v = 0;
        for (int i = 0; i < pts; i++)
        {
            byte f = flags[i];
            if ((f & 2) != 0) { int dx = d[p++]; v += (f & 16) != 0 ? dx : -dx; }
            else if ((f & 16) == 0) { v += S16(p); p += 2; }
            xs[i] = v;
        }
        v = 0;
        for (int i = 0; i < pts; i++)
        {
            byte f = flags[i];
            if ((f & 4) != 0) { int dy = d[p++]; v += (f & 32) != 0 ? dy : -dy; }
            else if ((f & 32) == 0) { v += S16(p); p += 2; }
            ys[i] = v;
        }

        int start = 0;
        for (int c = 0; c < nc; c++)
        {
            int count = ends[c] - start + 1;
            var P = new Vector2[count]; var on = new bool[count];
            for (int i = 0; i < count; i++) { P[i] = new Vector2(xs[start + i], ys[start + i]); on[i] = (flags[start + i] & 1) != 0; }
            start = ends[c] + 1;
            result.Add(Flatten(P, on));
        }
        return result;
    }

    static Vector2[] Flatten(Vector2[] P, bool[] on)
    {
        int n = P.Length;
        // build the list of on-curve anchors with implied midpoints between consecutive off-curve points
        var seq = new List<(Vector2 p, bool on)>();
        for (int i = 0; i < n; i++)
        {
            var cur = (P[i], on[i]); var nxt = (P[(i + 1) % n], on[(i + 1) % n]);
            seq.Add(cur);
            if (!cur.Item2 && !nxt.Item2) seq.Add(((cur.Item1 + nxt.Item1) * 0.5f, true));
        }
        int s = seq.FindIndex(q => q.on);
        var outp = new List<Vector2>();
        int m = seq.Count;
        Vector2 prev = seq[s].p;
        outp.Add(prev);
        for (int k = 1; k <= m; k++)
        {
            var q = seq[(s + k) % m];
            if (q.on) { outp.Add(q.p); prev = q.p; continue; }
            var end = seq[(s + k + 1) % m].p;
            for (int t = 1; t <= 8; t++)
            {
                float u = t / 8f, w = 1f - u;
                outp.Add(w * w * prev + 2f * w * u * q.p + u * u * end);
            }
            prev = end; k++;
        }
        return outp.ToArray();
    }
}
