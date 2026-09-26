using System;
using System.IO;
using UnityEngine;

// Procedural sound design used by the builder to create real WAV assets (Assets/Audio/Generated).
// All clips are original synthesis — no third-party audio. Replace any of them with freesound /
// OpenGameArt files later by dropping in a clip with the same name.
public static class AudioSynth
{
    public const int SR = 22050;
    static System.Random _rng = new System.Random(20260924);

    static float Noise() => (float)(_rng.NextDouble() * 2.0 - 1.0);
    static float Rand01() => (float)_rng.NextDouble();
    static float[] Buf(float seconds) => new float[(int)(seconds * SR)];
    const float Tau = Mathf.PI * 2f;

    // ── WAV ─────────────────────────────────────────────────────────────────

    public static void WriteWav(string path, float[] data, float peak = 0.85f)
    {
        float max = 1e-6f;
        foreach (float s in data) max = Mathf.Max(max, Mathf.Abs(s));
        float gain = peak / max;

        Directory.CreateDirectory(Path.GetDirectoryName(path));
        using (var fs = new FileStream(path, FileMode.Create))
        using (var bw = new BinaryWriter(fs))
        {
            int byteCount = data.Length * 2;
            bw.Write(new[] { 'R', 'I', 'F', 'F' });
            bw.Write(36 + byteCount);
            bw.Write(new[] { 'W', 'A', 'V', 'E', 'f', 'm', 't', ' ' });
            bw.Write(16);
            bw.Write((short)1);
            bw.Write((short)1);
            bw.Write(SR);
            bw.Write(SR * 2);
            bw.Write((short)2);
            bw.Write((short)16);
            bw.Write(new[] { 'd', 'a', 't', 'a' });
            bw.Write(byteCount);
            foreach (float s in data)
                bw.Write((short)Mathf.Clamp(Mathf.RoundToInt(s * gain * 32767f), -32767, 32767));
        }
    }

    // Cross-fades the tail into the head so a noisy clip loops without a click.
    static float[] MakeLoop(float[] d, float fadeSeconds)
    {
        int n = (int)(fadeSeconds * SR);
        int len = d.Length - n;
        var r = new float[len];
        Array.Copy(d, r, len);
        for (int i = 0; i < n; i++)
        {
            float w = i / (float)n;
            r[i] = d[i] * w + d[len + i] * (1f - w);
        }
        return r;
    }

    // ── helpers ─────────────────────────────────────────────────────────────

    static void Pluck(float[] buf, float startSec, float freq, float dur, float amp, float damp = 0.997f)
    {
        int n = Mathf.Max(2, (int)(SR / freq));
        var d = new float[n];
        float prev = 0f;
        for (int j = 0; j < n; j++) { float x = Noise(); prev = 0.5f * (prev + x); d[j] = prev; }   // softened excitation
        int len = (int)(dur * SR), s0 = (int)(startSec * SR), idx = 0;
        for (int i = 0; i < len && s0 + i < buf.Length; i++)
        {
            float cur = d[idx], nxt = d[(idx + 1) % n];
            d[idx] = damp * 0.5f * (cur + nxt);
            float fade = i < 60 ? i / 60f : 1f;
            buf[s0 + i] += cur * amp * fade;
            idx = (idx + 1) % n;
        }
    }

    static void AddDrum(float[] buf, float startSec, float amp, float baseFreq = 68f)
    {
        int s0 = (int)(startSec * SR);
        float phase = 0f;
        for (int i = 0; i < SR && s0 + i < buf.Length; i++)
        {
            float t = i / (float)SR;
            float f = baseFreq * (1f + 0.7f * Mathf.Exp(-t * 28f));
            phase += Tau * f / SR;
            float body = Mathf.Sin(phase) * Mathf.Exp(-t * 7f);
            float slap = Noise() * Mathf.Exp(-t * 55f) * 0.5f;
            buf[s0 + i] += (body + slap) * amp;
        }
    }

    static void Reverb(float[] buf, float delaySeconds, float feedback, float mix)
    {
        int d = (int)(delaySeconds * SR);
        var copy = (float[])buf.Clone();
        for (int i = d; i < buf.Length; i++)
        {
            copy[i] += copy[i - d] * feedback;
            buf[i] += copy[i - d] * mix;
        }
    }

    // ── ambience ────────────────────────────────────────────────────────────

    public static float[] Wind()
    {
        float len = 12f;
        var b = Buf(len);
        float lp1 = 0, lp2 = 0;
        for (int i = 0; i < b.Length; i++)
        {
            float t = i / (float)SR;
            float cutoff = 0.02f + 0.014f * Mathf.Sin(Tau * t / 12f) + 0.008f * Mathf.Sin(Tau * 3f * t / 12f);
            float n = Noise();
            lp1 += cutoff * (n - lp1);
            lp2 += cutoff * (lp1 - lp2);
            float amp = 0.55f + 0.45f * Mathf.Sin(Tau * 2f * t / 12f + 1f);
            b[i] = lp2 * amp;
        }
        return MakeLoop(b, 1.2f);
    }

    public static float[] Crickets()
    {
        var b = Buf(7f);
        float[] carriers = { 4200f, 4650f, 3900f };
        float[] periods  = { 1.3f, 1.7f, 2.1f };
        for (int v = 0; v < 3; v++)
        {
            float ph = Rand01() * 6f, phase = 0f;
            for (int i = 0; i < b.Length; i++)
            {
                float t = i / (float)SR;
                float burst = Mathf.Sin(Tau * t / periods[v] + ph) > 0.25f ? 1f : 0f;
                float pulse = Mathf.Sin(Tau * 26f * t) > 0.2f ? 1f : 0f;
                phase += Tau * carriers[v] / SR;
                b[i] += Mathf.Sin(phase) * burst * pulse * 0.2f;
            }
        }
        return MakeLoop(b, 0.5f);
    }

    public static float[] TorchCrackle()
    {
        var b = Buf(6.5f);
        float lp = 0;
        for (int i = 0; i < b.Length; i++)
        {
            lp += 0.08f * (Noise() - lp);
            b[i] = lp * 0.25f;
        }
        int pops = 40;
        for (int p = 0; p < pops; p++)
        {
            int s0 = (int)(Rand01() * (b.Length - 800));
            float a = 0.2f + Rand01() * 0.8f;
            for (int i = 0; i < 500; i++)
                b[s0 + i] += Noise() * a * Mathf.Exp(-i / 60f) * 0.6f;
        }
        return MakeLoop(b, 0.5f);
    }

    // ── ancient-instrument helpers ──────────────────────────────────────────
    // Own random generator and wrap-around writes: the loops stay seamless and the shared sequence used by
    // the other clips is untouched.

    // Kinnor (lyre): a plucked string.
    static void Lyre(float[] buf, System.Random rng, float startSec, float freq, float dur, float amp, float damp = 0.9965f)
    {
        int n = Mathf.Max(2, Mathf.RoundToInt(SR / freq));
        var d = new float[n];
        float prev = 0f;
        for (int j = 0; j < n; j++) { float x = (float)(rng.NextDouble() * 2.0 - 1.0); prev = 0.6f * prev + 0.4f * x; d[j] = prev; }
        int len = (int)(dur * SR), s0 = (int)(startSec * SR), idx = 0;
        for (int i = 0; i < len; i++)
        {
            float cur = d[idx], nxt = d[(idx + 1) % n];
            d[idx] = damp * 0.5f * (cur + nxt);
            buf[(s0 + i) % buf.Length] += cur * amp * (i < 40 ? i / 40f : 1f);
            idx = (idx + 1) % n;
        }
    }

    // Frame drum (tof): a soft skin thump with a little hand slap.
    static void FrameDrum(float[] buf, System.Random rng, float startSec, float amp, float baseFreq = 92f)
    {
        int s0 = (int)(startSec * SR), len = (int)(0.7f * SR);
        float phase = 0f, lp = 0f;
        for (int i = 0; i < len; i++)
        {
            float t = i / (float)SR;
            phase += Tau * baseFreq * (1f + 0.5f * Mathf.Exp(-t * 30f)) / SR;
            lp += 0.25f * ((float)(rng.NextDouble() * 2.0 - 1.0) - lp);
            buf[(s0 + i) % buf.Length] += (Mathf.Sin(phase) * Mathf.Exp(-t * 9f) + lp * Mathf.Exp(-t * 45f) * 0.6f) * amp;
        }
    }

    // Reed flute (ney / halil): a breathy note that swells in and fades out.
    static void ReedNote(float[] buf, System.Random rng, float startSec, float freq, float dur, float amp)
    {
        int s0 = (int)(startSec * SR), len = (int)(dur * SR);
        float lp = 0f;
        for (int i = 0; i < len; i++)
        {
            float t = i / (float)SR;
            float env = Mathf.Clamp01(t / 0.35f) * Mathf.Clamp01((dur - t) / 0.9f);
            float pm = 0.5f * Mathf.Sin(Tau * 5f * t) * Mathf.Clamp01(t / 0.6f);
            float ph = Tau * freq * t + pm;
            lp += 0.2f * ((float)(rng.NextDouble() * 2.0 - 1.0) - lp);
            float s = Mathf.Sin(ph) + 0.35f * Mathf.Sin(2f * ph) + 0.18f * Mathf.Sin(3f * ph) + lp * 0.3f;
            buf[(s0 + i) % buf.Length] += s * env * amp;
        }
    }

    // Tension layer under the calm music: a quiet heartbeat on a frame drum with a tremolo on the lyre, built on a
    // flat second (Eb against D). No sustained tone. The game raises its volume and pitch (so it also quickens)
    // as guards grow suspicious.
    public static float[] Drone()
    {
        float len = 8f;
        var b = Buf(len);
        var rng = new System.Random(19);
        for (int k = 0; k < 10; k++)                                   // lub-dub every 0.8 s
        {
            FrameDrum(b, rng, k * 0.8f, 0.9f, 72f);
            FrameDrum(b, rng, k * 0.8f + 0.22f, 0.55f, 64f);
        }
        float[] fs = { 293.66f, 293.66f, 311.13f, 293.66f, 293.66f, 220f, 311.13f, 293.66f };
        for (int bar = 0; bar < 4; bar++)
            for (int i = 0; i < 8; i++)
                Lyre(b, rng, bar * 2f + i * 0.25f, fs[i], 0.5f, 0.28f + (i % 4 == 0 ? 0.12f : 0f), 0.994f);
        return b;
    }

    // Calm underscore: a slow, contemplative kinnor melody in a Hijaz-like mode (D, Eb, F#, G, A, Bb, C) with a low
    // root on the lyre, soft frame drum and two answering reed notes. 40 beats at 67 bpm.
    public static float[] Music()
    {
        const float beat = 0.9f;
        var b = Buf(40 * beat);
        var rng = new System.Random(73);
        const float D3 = 146.83f, Eb3 = 155.56f, Fs3 = 185f, G3 = 196f, A3 = 220f, Bb3 = 233.08f, C4 = 261.63f, D4 = 293.66f;

        void N(float pos, float f, float ring = 2.4f, float amp = 0.5f) => Lyre(b, rng, pos * beat, f, ring, amp);

        // melody
        N(0, A3, 3.4f); N(2, Bb3); N(3, A3); N(4, G3); N(5, Fs3); N(6, Eb3, 3.4f);
        N(8, D3, 3.4f); N(10, Eb3); N(11, Fs3); N(12, G3); N(13, A3); N(14, Bb3, 3.4f);
        N(16, C4); N(17, Bb3); N(18, A3, 3f); N(20, G3); N(21, Fs3); N(22, Eb3); N(23, Fs3);
        N(24, G3); N(25, A3); N(26, Bb3); N(27, A3); N(28, Fs3, 3.4f); N(30, Eb3, 3f);
        N(32, D4, 3f); N(34, C4); N(35, Bb3); N(36, A3, 3.4f); N(38, Eb3); N(39, D3, 3.4f);

        // low root on the lyre
        for (int k = 0; k < 5; k++) N(k * 8, D3, 3.6f, 0.35f);

        // reed flute answers
        ReedNote(b, rng, 16 * beat, D4, 3f * beat, 0.22f);
        ReedNote(b, rng, 32 * beat, A3, 4f * beat, 0.22f);

        // soft frame drum from the third bar on
        for (int bar = 2; bar < 10; bar++)
        {
            FrameDrum(b, rng, bar * 4 * beat, 0.5f);
            FrameDrum(b, rng, (bar * 4 + 2.5f) * beat, 0.25f);
        }

        Reverb(b, 0.11f, 0.3f, 0.3f);
        return b;
    }

    // ── stingers & one-shots ────────────────────────────────────────────────

    public static float[] AlertSting()
    {
        var b = Buf(1.4f);
        float[] notes = { 146.83f, 174.61f, 220f, 293.66f };   // D3 F3 A3 D4 – tense staccato string hit
        for (int i = 0; i < b.Length; i++)
        {
            float t = i / (float)SR;
            float env = Mathf.Min(1f, t / 0.008f) * Mathf.Exp(-t * 5.5f);
            float s = 0f;
            foreach (float f in notes)
                for (int h = 1; h <= 7; h++) s += Mathf.Sin(Tau * f * h * t + h) / h;
            b[i] = s * env * 0.25f + Noise() * Mathf.Exp(-t * 60f) * 0.4f;
        }
        return b;
    }

    public static float[] Footstep(string surface, int variant)
    {
        float len = 0.28f;
        var b = Buf(len);
        float lp = 0;
        float pitch = 1f + variant * 0.12f;
        for (int i = 0; i < b.Length; i++)
        {
            float t = i / (float)SR;
            float n = Noise();
            switch (surface)
            {
                case "stone":
                    lp += 0.35f * (n - lp);
                    b[i] = lp * Mathf.Exp(-t * 45f) * 0.9f
                         + Mathf.Sin(Tau * 130f * pitch * t) * Mathf.Exp(-t * 38f) * 0.5f
                         + (i < 40 ? n * 0.6f : 0f);
                    break;
                case "dirt":
                    lp += 0.09f * (n - lp);
                    b[i] = lp * Mathf.Exp(-t * 22f) * 2.2f + Mathf.Sin(Tau * 90f * pitch * t) * Mathf.Exp(-t * 30f) * 0.4f;
                    break;
                default: // wood
                    lp += 0.2f * (n - lp);
                    b[i] = Mathf.Sin(Tau * 190f * pitch * t) * Mathf.Exp(-t * 28f) * 0.8f
                         + Mathf.Sin(Tau * 330f * pitch * t) * Mathf.Exp(-t * 40f) * 0.35f
                         + lp * Mathf.Exp(-t * 60f) * 0.5f;
                    break;
            }
        }
        return b;
    }

    public static float[] StoneClatter()
    {
        var b = Buf(0.7f);
        float[] times = { 0f, 0.11f, 0.19f, 0.24f, 0.27f };
        float[] amps  = { 1f, .6f, .4f, .25f, .15f };
        for (int k = 0; k < times.Length; k++)
        {
            int s0 = (int)(times[k] * SR);
            float f = 1500f + Rand01() * 800f;
            for (int i = 0; i < 1400 && s0 + i < b.Length; i++)
            {
                float t = i / (float)SR;
                b[s0 + i] += (Noise() * 0.5f + Mathf.Sin(Tau * f * t)) * Mathf.Exp(-t * 90f) * amps[k];
            }
        }
        return b;
    }

    public static float[] DogBark()
    {
        var b = Buf(1.4f);
        float[] starts = { 0f, 0.42f, 0.78f };
        foreach (float st in starts)
        {
            int s0 = (int)(st * SR);
            float phase = 0, lp1 = 0, lp2 = 0;
            for (int i = 0; i < (int)(0.22f * SR) && s0 + i < b.Length; i++)
            {
                float t = i / (float)SR;
                float f = Mathf.Lerp(400f, 250f, t / 0.22f);
                phase += Tau * f / SR;
                float saw = (phase / Tau % 1f) * 2f - 1f;
                float src = saw * 0.8f + Noise() * 0.25f;
                lp1 += 0.28f * (src - lp1);
                lp2 += 0.28f * (lp1 - lp2);
                float env = Mathf.Min(1f, t / 0.006f) * Mathf.Exp(-t * 13f);
                b[s0 + i] += lp2 * env * 1.4f;
            }
        }
        return b;
    }

    public static float[] HarpCalm()
    {
        var b = Buf(5f);
        float[] notes = { 293.66f, 370.0f, 440f, 587.33f, 440f, 370f, 293.66f };   // D major arpeggio
        float[] times = { 0f, 0.4f, 0.8f, 1.2f, 1.9f, 2.4f, 3.0f };
        for (int k = 0; k < notes.Length; k++) Pluck(b, times[k], notes[k], 2.2f, 0.7f, 0.9985f);
        Reverb(b, 0.11f, 0.35f, 0.3f);
        return b;
    }

    public static float[] GateCreak()
    {
        var b = Buf(3.6f);
        float phase = 0, lp = 0;
        for (int i = 0; i < b.Length; i++)
        {
            float t = i / (float)SR;
            float f = 78f + 26f * Mathf.Sin(Tau * 0.6f * t) - 10f * t + Noise() * 6f;
            phase += Tau * f / SR;
            float s = Mathf.Sin(phase) + 0.5f * Mathf.Sin(2.02f * phase) + 0.3f * Mathf.Sin(3.1f * phase);
            s = (float)Math.Tanh(s * 2.2f);
            lp += 0.2f * (Noise() - lp);
            float am = 0.6f + 0.4f * Mathf.Sin(Tau * 11f * t + lp * 6f);
            float env = Mathf.Min(1f, t / 0.3f) * Mathf.Min(1f, (3.6f - t) / 0.5f);
            b[i] = s * am * env * 0.6f + lp * 0.4f * env;
        }
        return b;
    }

    // A single melodic phrase over the farewell (1 Samuel 20:42 epilogue)
    public static float[] EpiloguePhrase()
    {
        var b = Buf(14f);
        float[] f  = { 293.66f, 349.23f, 440f, 392f, 349.23f, 329.63f, 293.66f };
        float[] st = { 0.5f, 2.3f, 4.1f, 6.4f, 8.0f, 9.7f, 11.0f };
        float[] du = { 2.4f, 2.4f, 3.0f, 2.2f, 2.2f, 2.2f, 3.0f };
        for (int k = 0; k < f.Length; k++)
        {
            int s0 = (int)(st[k] * SR), n = (int)(du[k] * SR);
            for (int i = 0; i < n && s0 + i < b.Length; i++)
            {
                float t = i / (float)SR;
                float env = Mathf.Min(1f, t / 0.35f) * Mathf.Min(1f, (du[k] - t) / 1.0f);
                float vib = 1f + 0.006f * Mathf.Sin(Tau * 5f * t) * Mathf.Min(1f, t);
                float s = Mathf.Sin(Tau * f[k] * vib * t)
                        + 0.4f * Mathf.Sin(Tau * 2f * f[k] * vib * t)
                        + 0.18f * Mathf.Sin(Tau * 3f * f[k] * vib * t);
                b[s0 + i] += s * env * 0.3f;
            }
        }
        for (int i = 0; i < b.Length; i++)
        {
            float t = i / (float)SR;
            float env = Mathf.Min(1f, t / 2f) * Mathf.Min(1f, (14f - t) / 2.5f);
            b[i] += (Mathf.Sin(Tau * 73.42f * t) * 0.16f + Mathf.Sin(Tau * 110f * t) * 0.1f) * env;
        }
        Reverb(b, 0.17f, 0.45f, 0.4f);
        return b;
    }

    public static float[] Pickup()
    {
        var b = Buf(0.35f);
        for (int i = 0; i < b.Length; i++)
        {
            float t = i / (float)SR;
            b[i] = (Mathf.Sin(Tau * 700f * t) + 0.5f * Mathf.Sin(Tau * 1050f * t)) * Mathf.Exp(-t * 12f) * 0.5f
                 + Noise() * Mathf.Exp(-t * 80f) * 0.3f;
        }
        return b;
    }
}
