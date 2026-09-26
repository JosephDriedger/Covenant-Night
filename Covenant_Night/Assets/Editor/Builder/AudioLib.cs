using System.IO;
using UnityEditor;
using UnityEngine;

// Synthesises every sound with AudioSynth, writes WAVs under Assets/Audio/Generated and loads them back
// as AudioClip assets for wiring into prefabs and scenes.
public class AudioLib
{
    public const string Dir = "Assets/Audio/Generated";

    public AudioClip wind, crickets, torchCrackle, drone, music, sting, clatter, bark, harp, creak, epilogue, pickup;
    public AudioClip[] stepStone = new AudioClip[3];
    public AudioClip[] stepDirt  = new AudioClip[3];
    public AudioClip[] stepWood  = new AudioClip[3];

    public static AudioLib Build()
    {
        Directory.CreateDirectory(Dir);

        void Write(string name, float[] data, float peak = 0.85f) => AudioSynth.WriteWav($"{Dir}/{name}.wav", data, peak);
        Write("wind_loop",          AudioSynth.Wind(),          0.7f);
        Write("crickets_loop",      AudioSynth.Crickets(),      0.5f);
        Write("torch_crackle_loop", AudioSynth.TorchCrackle(),  0.8f);
        Write("tension_drone_loop", AudioSynth.Drone(),         0.6f);
        Write("music_underscore",   AudioSynth.Music(),         0.6f);
        Write("alert_sting",        AudioSynth.AlertSting(),    0.9f);
        Write("stone_clatter",      AudioSynth.StoneClatter(),  0.8f);
        Write("dog_bark",           AudioSynth.DogBark(),       0.85f);
        Write("harp_calm",          AudioSynth.HarpCalm(),      0.8f);
        Write("gate_creak",         AudioSynth.GateCreak(),     0.8f);
        Write("epilogue_phrase",    AudioSynth.EpiloguePhrase(),0.7f);
        Write("pickup",             AudioSynth.Pickup(),        0.7f);
        for (int i = 0; i < 3; i++)
        {
            Write($"step_stone_{i}", AudioSynth.Footstep("stone", i), 0.7f);
            Write($"step_dirt_{i}",  AudioSynth.Footstep("dirt",  i), 0.7f);
            Write($"step_wood_{i}",  AudioSynth.Footstep("wood",  i), 0.7f);
        }

        AssetDatabase.Refresh(ImportAssetOptions.ForceUpdate);

        AudioClip L(string n) => AssetDatabase.LoadAssetAtPath<AudioClip>($"{Dir}/{n}.wav");
        var a = new AudioLib
        {
            wind = L("wind_loop"), crickets = L("crickets_loop"), torchCrackle = L("torch_crackle_loop"),
            drone = L("tension_drone_loop"), music = L("music_underscore"), sting = L("alert_sting"),
            clatter = L("stone_clatter"), bark = L("dog_bark"), harp = L("harp_calm"), creak = L("gate_creak"),
            epilogue = L("epilogue_phrase"), pickup = L("pickup"),
        };
        for (int i = 0; i < 3; i++)
        {
            a.stepStone[i] = L($"step_stone_{i}");
            a.stepDirt[i]  = L($"step_dirt_{i}");
            a.stepWood[i]  = L($"step_wood_{i}");
        }
        return a;
    }
}
