using System;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.Audio;

// Creates Assets/Audio/CovenantNight.mixer with Music / Ambience / SFX submixes and three snapshots
// (Calm, Suspicious, Alarmed). Unity has no public API for authoring mixers, so this uses the same
// internal AudioMixerController the mixer window uses (via reflection).
public class MixerRefs
{
    public AudioMixer mixer;
    public AudioMixerGroup master, music, ambience, sfx;
    public AudioMixerSnapshot calm, suspicious, alarmed;
}

public static class MixerBuilder
{
    public const string Path = "Assets/Audio/CovenantNight.mixer";

    public static MixerRefs Build()
    {
        if (AssetDatabase.LoadAssetAtPath<AudioMixer>(Path) != null)
            AssetDatabase.DeleteAsset(Path);      // rebuilt from scratch so snapshots stay deterministic

        var asm  = typeof(EditorApplication).Assembly;
        var ctrlT = asm.GetType("UnityEditor.Audio.AudioMixerController");
        var groupT = asm.GetType("UnityEditor.Audio.AudioMixerGroupController");
        var snapT  = asm.GetType("UnityEditor.Audio.AudioMixerSnapshotController");
        const BindingFlags All = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static;

        object controller = ctrlT.GetMethod("CreateMixerControllerAtPath", All).Invoke(null, new object[] { Path });
        object master = ctrlT.GetProperty("masterGroup", All).GetValue(controller);

        object MakeGroup(string name)
        {
            object g = ctrlT.GetMethod("CreateNewGroup", All).Invoke(controller, new object[] { name, false });
            ctrlT.GetMethod("AddChildToParent", All).Invoke(controller, new object[] { g, master });
            return g;
        }
        object music = MakeGroup("Music");
        object amb   = MakeGroup("Ambience");
        object sfx   = MakeGroup("SFX");

        // Snapshots: the controller starts with one; rename it and clone the others from it.
        var snapsProp = ctrlT.GetProperty("snapshots", All);
        var snaps = (Array)snapsProp.GetValue(controller);
        object calm = snaps.GetValue(0);
        ((UnityEngine.Object)calm).name = "Calm";

        var targetProp = ctrlT.GetProperty("TargetSnapshot", All);
        var clone = ctrlT.GetMethod("CloneNewSnapshotFromTarget", All);
        object CloneFromCalm(string name)
        {
            targetProp?.SetValue(controller, calm);
            object r = clone.Invoke(controller, new object[] { false });
            // The return value isn't reliable across versions: read the snapshot list back and take the newest.
            var list = (Array)snapsProp.GetValue(controller);
            object made = r ?? list.GetValue(list.Length - 1);
            ((UnityEngine.Object)made).name = name;
            return made;
        }
        object susp  = CloneFromCalm("Suspicious");
        object alarm = CloneFromCalm("Alarmed");
        Debug.Log("[Builder] mixer snapshots: " + ((Array)snapsProp.GetValue(controller)).Length);

        var setVol = groupT.GetMethod("SetValueForVolume", All);
        void Vol(object group, object snap, float db) => setVol.Invoke(group, new object[] { controller, snap, db });

        //                    Music  Ambience  SFX
        Vol(music, calm,  -6f);  Vol(amb, calm,   0f);  Vol(sfx, calm,  0f);
        Vol(music, susp,  -3f);  Vol(amb, susp,  -4f);  Vol(sfx, susp,  0f);
        Vol(music, alarm,  1f);  Vol(amb, alarm, -12f); Vol(sfx, alarm, 0f);

        EditorUtility.SetDirty((UnityEngine.Object)controller);
        AssetDatabase.SaveAssets();
        AssetDatabase.ImportAsset(Path, ImportAssetOptions.ForceUpdate);

        var mixer = AssetDatabase.LoadAssetAtPath<AudioMixer>(Path);
        var refs = new MixerRefs
        {
            mixer      = mixer,
            master     = mixer.FindMatchingGroups("Master")[0],
            music      = mixer.FindMatchingGroups("Master/Music")[0],
            ambience   = mixer.FindMatchingGroups("Master/Ambience")[0],
            sfx        = mixer.FindMatchingGroups("Master/SFX")[0],
            calm       = mixer.FindSnapshot("Calm"),
            suspicious = mixer.FindSnapshot("Suspicious"),
            alarmed    = mixer.FindSnapshot("Alarmed"),
        };
        if (refs.calm == null || refs.suspicious == null || refs.alarmed == null)
            throw new Exception("Mixer snapshots were not created correctly.");
        return refs;
    }
}
