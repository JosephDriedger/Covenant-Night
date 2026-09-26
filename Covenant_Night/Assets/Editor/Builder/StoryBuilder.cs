using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

// Creates the StoryBeatData assets: intro (Beats 1–2), zone intros (Beat 3), the gate bluff (Beat 4),
// the farewell (Beat 5), the epilogue (1 Samuel 20:42) and the fail cutscene.
public class StoryAssets
{
    public StoryBeatData intro, zone2, zone3, zone4, zone5;
    public StoryBeatData gateBluff, farewell, epilogue, jonathanCaptured;
    public StoryBeatData[] ZoneEntryBeats => new[] { intro, zone2, zone3, zone4, zone5 };
}

public static class StoryBuilder
{
    const string Dir = "Assets/ScriptableObjects/Story";
    const string ArtDir = "Assets/Art/Story";

    static Sprite Spr(string name)
    {
        string path = $"{ArtDir}/{name}.png";
        var imp = AssetImporter.GetAtPath(path) as TextureImporter;
        if (imp != null && imp.textureType != TextureImporterType.Sprite)
        {
            imp.textureType = TextureImporterType.Sprite;
            imp.spriteImportMode = SpriteImportMode.Single;
            imp.mipmapEnabled = false;
            imp.filterMode = FilterMode.Bilinear;
            imp.textureCompression = TextureImporterCompression.Compressed;
            imp.SaveAndReimport();
        }
        return AssetDatabase.LoadAssetAtPath<Sprite>(path);
    }

    static StoryBeat B(string heading, string text, Sprite s) => new StoryBeat { heading = heading, text = text, sprite = s };

    static StoryBeatData Make(string name, params StoryBeat[] beats)
    {
        string path = $"{Dir}/{name}.asset";
        var so = AssetDatabase.LoadAssetAtPath<StoryBeatData>(path);
        if (so == null)
        {
            so = ScriptableObject.CreateInstance<StoryBeatData>();
            AssetDatabase.CreateAsset(so, path);
        }
        so.beats = beats;
        EditorUtility.SetDirty(so);
        return so;
    }

    public static StoryAssets Build()
    {
        if (!AssetDatabase.IsValidFolder("Assets/ScriptableObjects")) AssetDatabase.CreateFolder("Assets", "ScriptableObjects");
        if (!AssetDatabase.IsValidFolder(Dir)) AssetDatabase.CreateFolder("Assets/ScriptableObjects", "Story");

        var warning  = Spr("ill_warning");
        var wake     = Spr("ill_wake_david");
        var market   = Spr("ill_city_market");
        var alley    = Spr("ill_city_alley");
        var well     = Spr("ill_city_well");
        var approach = Spr("ill_gate_approach");
        var bluff    = Spr("ill_gate_bluff");
        var farewell = Spr("ill_farewell");
        var empty    = Spr("ill_empty_gate");
        var throne   = Spr("ill_throne");
        var cones    = Spr("ill_guard_cones");
        var leadDav  = Spr("ill_lead_david");

        var s = new StoryAssets();

        s.intro = Make("Story_Intro",
            B("The Warning",
              "\"The king's men are gathering, my lord. They come for David at dawn.\"\n\n" +
              "You choose the covenant.", warning),
            B("The Plan",
              "\"The eastern gate. Before the sun.\"\n\n" +
              "David takes up his harp and follows.", wake),
            B("Slip Past the Guards",
              "A guard's lantern cone shows what he sees: green, nothing. Amber, a doubt. Red, and he gives chase.\n\n" +
              "Walk softly. Keep to the shadows and out of torchlight.", cones),
            B("Lead David Out",
              "A stone thrown far off draws a guard from his post.\n\n" +
              "Leave David waiting in cover while you scout, then bring him on to the golden marker.", leadDav));

        s.zone2 = Make("Story_Zone2",
            B("The Market Quarter",
              "Torches and open ground. Slip between the stalls and carts, and through the dark between the fires.", market));
        s.zone3 = Make("Story_Zone3",
            B("Potter's Alley",
              "Sleeping dogs wake the whole quarter if you stray near them. The rooftops offer a way across, but the sky is open.", alley));
        s.zone4 = Make("Story_Zone4",
            B("Well Square",
              "Sentries watch from the rooftops, their sight long and unbroken. Go under a roof, or sneak behind the low terrace wall.", well));
        s.zone5 = Make("Story_Zone5",
            B("The Eastern Gate",
              "The commander is Saul's man. No one sneaks past him. Bring David to the gate, and show him the one thing he dares not refuse.", approach));

        s.gateBluff = Make("Story_GateBluff",
            B("The Bluff",
              "\"By the king's order, no one leaves.\"\n\n" +
              "Jonathan lifts his hand. His father's signet ring catches the flame. The commander steps aside, and the gate groans open.", bluff));

        s.farewell = Make("Story_Farewell",
            B("The Farewell",
              "David passes through into the dark hills. He turns once.", farewell));

        s.epilogue = Make("Story_Epilogue",
            B("Epilogue",
              "\"Go in peace. We have sworn friendship with each other in the name of the Lord. The Lord is witness between you and me forever.\"\n\n" +
              "- 1 Samuel 20:42", empty));

        s.jonathanCaptured = Make("Story_JonathanCaptured",
            B("Before the King",
              "Jonathan stands before Saul. He says nothing.", throne));

        AssetDatabase.SaveAssets();
        return s;
    }
}
