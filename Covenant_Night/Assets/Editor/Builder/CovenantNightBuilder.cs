using System;
using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

// One-shot generator for the whole game: project settings, audio, illustrations, materials, prefabs,
// story data, five zone scenes (with baked NavMesh) and the Persistent scene.
//
//   Unity menu:  Covenant Night > Build Everything
//   Batch mode:  Unity -batchmode -projectPath <proj> -executeMethod CovenantNightBuilder.BuildAllBatch
//
// Everything it makes is a normal Unity asset / scene that can be edited by hand afterwards.
public static class CovenantNightBuilder
{
    [MenuItem("Covenant Night/Build Everything")]
    public static void BuildAll()
    {
        var log = new System.Text.StringBuilder();
        void Step(string s) { Debug.Log("[Builder] " + s); log.AppendLine(s); }

        LowPoly.ResetSession();
        Step("Registering tags and layers");
        EnsureTagsAndLayers();
        ConfigureProject();

        Step("Synthesising audio");
        var audio = AudioLib.Build();

        Step("Painting story illustrations");
        IllustrationGenerator.GenerateAll("Assets/Art/Story");
        AssetDatabase.Refresh(ImportAssetOptions.ForceUpdate);

        Step("Building AudioMixer (Music / Ambience / SFX, Calm / Suspicious / Alarmed)");
        var mixer = MixerBuilder.Build();

        Step("Building materials");
        var mats = MaterialLibrary.Build();

        Step("Building prefabs");
        var prefabs = PrefabFactory.Build(mats, audio, mixer);

        Step("Building story + checkpoint data");
        var story = StoryBuilder.Build();
        var checkpoints = BuildCheckpoints();
        var post = BuildPostFx();
        var input = AssetDatabase.LoadAssetAtPath<InputActionAsset>("Assets/InputSystem_Actions.inputactions");

        Step("Building zone 1 – The Palace District");
        ZoneBuilders.Zone1(new ZoneKit("Zone1_PalaceDistrict", mats, prefabs, audio, mixer), story);
        Step("Building zone 2 – The Market Quarter");
        ZoneBuilders.Zone2(new ZoneKit("Zone2_MarketQuarter", mats, prefabs, audio, mixer), story);
        Step("Building zone 3 – Potter's Alley");
        ZoneBuilders.Zone3(new ZoneKit("Zone3_PottersAlley", mats, prefabs, audio, mixer), story);
        Step("Building zone 4 – Well Square");
        ZoneBuilders.Zone4(new ZoneKit("Zone4_WellSquare", mats, prefabs, audio, mixer), story);
        Step("Building zone 5 – The Eastern Gate");
        ZoneBuilders.Zone5(new ZoneKit("Zone5_EasternGate", mats, prefabs, audio, mixer), story);

        Step("Building Persistent scene");
        PersistentBuilder.Build(mats, prefabs, audio, mixer, story, input, checkpoints, post);

        Step("Updating build settings");
        var scenes = new EditorBuildSettingsScene[PersistentBuilder.ZoneScenes.Length + 1];
        scenes[0] = new EditorBuildSettingsScene("Assets/Scenes/Persistent.unity", true);
        for (int i = 0; i < PersistentBuilder.ZoneScenes.Length; i++)
            scenes[i + 1] = new EditorBuildSettingsScene($"Assets/Scenes/{PersistentBuilder.ZoneScenes[i]}.unity", true);
        EditorBuildSettings.scenes = scenes;

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Step("DONE");
    }

    // Batch-mode entry point (exits the editor when finished).
    public static void BuildAllBatch()
    {
        int code = 0;
        try { BuildAll(); }
        catch (Exception e) { Debug.LogError("[Builder] FAILED: " + e); code = 1; }
        EditorApplication.Exit(code);
    }

    // ── project setup ───────────────────────────────────────────────────────

    static void EnsureTagsAndLayers()
    {
        var tm = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);

        var tags = tm.FindProperty("tags");
        bool has = false;
        for (int i = 0; i < tags.arraySize; i++) if (tags.GetArrayElementAtIndex(i).stringValue == GameLayers.DavidTag) has = true;
        if (!has)
        {
            tags.InsertArrayElementAtIndex(tags.arraySize);
            tags.GetArrayElementAtIndex(tags.arraySize - 1).stringValue = GameLayers.DavidTag;
        }

        var layers = tm.FindProperty("layers");
        void Set(int idx, string name) => layers.GetArrayElementAtIndex(idx).stringValue = name;
        Set(GameLayers.Characters, "Characters");
        Set(GameLayers.Walls, "Walls");
        Set(GameLayers.Guards, "Guards");
        Set(GameLayers.GroundStone, "GroundStone");
        Set(GameLayers.GroundDirt, "GroundDirt");
        Set(GameLayers.GroundWood, "GroundWood");
        Set(GameLayers.Roof, "Roof");
        Set(GameLayers.Civilians, "Civilians");
        tm.ApplyModifiedProperties();
        AssetDatabase.SaveAssets();
    }

    static void ConfigureProject()
    {
        PlayerSettings.companyName = "Covenant Night";
        PlayerSettings.productName = "Covenant Night";
        PlayerSettings.defaultScreenWidth = 1280;
        PlayerSettings.defaultScreenHeight = 720;
        PlayerSettings.fullScreenMode = FullScreenMode.Windowed;
        PlayerSettings.resizableWindow = true;

        // URP: allow shadows from torch point lights, sensible shadow distance
        foreach (var path in new[] { "Assets/Settings/PC_RPAsset.asset", "Assets/Settings/Mobile_RPAsset.asset" })
        {
            var asset = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(path);
            if (asset == null) continue;
            var so = new SerializedObject(asset);
            void Bool(string n, bool v) { var p = so.FindProperty(n); if (p != null) p.boolValue = v; }
            void Float(string n, float v) { var p = so.FindProperty(n); if (p != null) p.floatValue = v; }
            void Int(string n, int v) { var p = so.FindProperty(n); if (p != null) p.intValue = v; }
            Bool("m_AdditionalLightShadowsSupported", true);
            Bool("m_MainLightShadowsSupported", true);
            Float("m_ShadowDistance", 55f);
            Int("m_AdditionalLightsShadowmapResolution", 1024);
            Int("m_MainLightShadowmapResolution", 2048);
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(asset);
        }
        AssetDatabase.SaveAssets();
    }

    static CheckpointData[] BuildCheckpoints()
    {
        const string dir = "Assets/ScriptableObjects/Checkpoints";
        if (!AssetDatabase.IsValidFolder("Assets/ScriptableObjects")) AssetDatabase.CreateFolder("Assets", "ScriptableObjects");
        if (!AssetDatabase.IsValidFolder(dir)) AssetDatabase.CreateFolder("Assets/ScriptableObjects", "Checkpoints");

        var result = new CheckpointData[PersistentBuilder.ZoneScenes.Length];
        for (int i = 0; i < result.Length; i++)
        {
            string path = $"{dir}/Zone{i + 1}_Checkpoint.asset";
            var cp = AssetDatabase.LoadAssetAtPath<CheckpointData>(path);
            if (cp == null)
            {
                cp = ScriptableObject.CreateInstance<CheckpointData>();
                AssetDatabase.CreateAsset(cp, path);
            }
            result[i] = cp;
        }
        return result;
    }

    static VolumeProfile BuildPostFx()
    {
        const string path = "Assets/Settings/CovenantNight_PostFX.asset";
        if (AssetDatabase.LoadAssetAtPath<VolumeProfile>(path) != null) AssetDatabase.DeleteAsset(path);

        var profile = ScriptableObject.CreateInstance<VolumeProfile>();
        AssetDatabase.CreateAsset(profile, path);

        // Scope: Bloom + Vignette only, plus the colour adjustment the GDD allows for unifying borrowed-asset palettes.
        var bloom = profile.Add<Bloom>(true);
        bloom.threshold.Override(1.0f);
        bloom.intensity.Override(0.6f);
        bloom.scatter.Override(0.65f);
        bloom.tint.Override(new Color(1f, 0.85f, 0.65f));

        var vig = profile.Add<Vignette>(true);
        vig.intensity.Override(0.32f);
        vig.smoothness.Override(0.45f);
        vig.color.Override(Color.black);

        var ca = profile.Add<ColorAdjustments>(true);
        ca.postExposure.Override(0.25f);
        ca.contrast.Override(10f);
        ca.saturation.Override(0f);
        ca.colorFilter.Override(new Color(0.96f, 0.97f, 1f));

        foreach (var c in profile.components) AssetDatabase.AddObjectToAsset(c, profile);
        EditorUtility.SetDirty(profile);
        AssetDatabase.SaveAssets();
        return profile;
    }

    // ── player builds ───────────────────────────────────────────────────────
    //
    //   Windows / macOS / Linux build with the standard Unity modules. Consoles (PlayStation, Xbox, Nintendo
    //   Switch) need the platform holder's licensed SDK and the matching Unity module installed; the game itself
    //   has no PC-only dependencies (gamepad-complete input, safe-area UI, console frame-rate tuning) so
    //   nothing in the project has to change — see docs/PLATFORMS.md.

    [MenuItem("Covenant Night/Build Windows Player")]
    public static void BuildWindowsPlayer() => BuildFor(BuildTarget.StandaloneWindows64, "Windows", "CovenantNight.exe");

    [MenuItem("Covenant Night/Build macOS Player")]
    public static void BuildMacPlayer() => BuildFor(BuildTarget.StandaloneOSX, "macOS", "CovenantNight.app");

    [MenuItem("Covenant Night/Build Linux Player")]
    public static void BuildLinuxPlayer() => BuildFor(BuildTarget.StandaloneLinux64, "Linux", "CovenantNight.x86_64");

    // Builds any target by enum name, e.g.  -executeMethod CovenantNightBuilder.BuildTargetBatch -cnTarget PS5
    public static void BuildTargetBatch()
    {
        string name = "StandaloneWindows64";
        var args = Environment.GetCommandLineArgs();
        for (int i = 0; i < args.Length - 1; i++) if (args[i] == "-cnTarget") name = args[i + 1];
        int code = 0;
        try
        {
            if (!Enum.TryParse(name, true, out BuildTarget target)) throw new Exception($"Unknown build target '{name}'.");
            BuildFor(target, name, target == BuildTarget.StandaloneWindows64 ? "CovenantNight.exe" : "CovenantNight");
        }
        catch (Exception e) { Debug.LogError("[Builder] Player build FAILED: " + e.Message); code = 1; }
        EditorApplication.Exit(code);
    }

    public static void BuildWindowsPlayerBatch()
    {
        int code = 0;
        try { BuildWindowsPlayer(); }
        catch (Exception e) { Debug.LogError("[Builder] Player build FAILED: " + e); code = 1; }
        EditorApplication.Exit(code);
    }

    static void BuildFor(BuildTarget target, string folder, string exeName)
    {
        var group = BuildPipeline.GetBuildTargetGroup(target);
        if (!BuildPipeline.IsBuildTargetSupported(group, target))
            throw new Exception($"The {target} build module is not installed for this editor (Unity Hub > Installs > Add modules). " +
                                "Console targets additionally need the platform holder's SDK.");

        string outDir = $"Builds/{folder}";
        Directory.CreateDirectory(outDir);
        var opts = new BuildPlayerOptions
        {
            scenes = Array.ConvertAll(EditorBuildSettings.scenes, s => s.path),
            locationPathName = $"{outDir}/{exeName}",
            target = target,
            options = BuildOptions.None,
        };
        var report = BuildPipeline.BuildPlayer(opts);
        Debug.Log($"[Builder] {target} build: {report.summary.result}, {report.summary.totalSize / (1024 * 1024)} MB, {report.summary.totalErrors} errors");
        if (report.summary.result != BuildResult.Succeeded) throw new Exception($"{target} build failed.");
        if (File.Exists("../CREDITS.txt")) File.Copy("../CREDITS.txt", $"{outDir}/CREDITS.txt", true);
    }
}
