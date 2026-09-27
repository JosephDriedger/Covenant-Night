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

    // Optional lore scrolls, purely narrative, found off the critical path in a zone.
    public StoryBeatData loreLedger, loreMarket, lorePotter, loreWell, loreSoldier;
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
              "Before midnight, a servant wakes Jonathan. \"The king's men are gathering,\" he whispers. \"They mean to take David at dawn.\"\n\n" +
              "Jonathan has sworn a covenant with David, and he intends to keep it.", warning),
            B("The Plan",
              "Jonathan hurries to David's room and wakes him. \"We leave by the eastern gate,\" he says, \"before the sun is up.\"\n\n" +
              "David takes up his harp and follows.", wake),
            B("Slipping Past the Guards",
              "Jonathan has trained beside these men and knows how they watch. The glow of a guard's lantern shows what he can see. " +
              "Green means he sees nothing, amber means he suspects something, and red means he is coming.\n\n" +
              "So Jonathan keeps to the shadows, stays out of torchlight and walks softly.", cones),
            B("Leading David Out",
              "A stone thrown into the dark will draw a guard away from his post.\n\n" +
              "David waits in cover while Jonathan scouts ahead. When the way is clear, Jonathan brings him on to the next golden marker.", leadDav));

        s.zone2 = Make("Story_Zone2",
            B("The Market Quarter",
              "Beyond the palace wall, the market square lies open under torchlight. Jonathan finds cover behind the stalls and carts, and keeps to the dark between the fires.", market));
        s.zone3 = Make("Story_Zone3",
            B("Potter's Alley",
              "Dogs sleep in their pens along the narrow alley, and they will wake the whole quarter if Jonathan strays too close. The rooftops offer another way across, but there is no cover under the open sky.", alley));
        s.zone4 = Make("Story_Zone4",
            B("Well Square",
              "Sentries watch from the rooftops, and very little escapes them. Jonathan can pass beneath the roofs of the houses, or slip along the terrace behind its low wall.", well));
        s.zone5 = Make("Story_Zone5",
            B("The Eastern Gate",
              "The gate commander serves Saul, and no one sneaks past him. Jonathan must bring David to the gate and show the commander the one thing he dares not refuse.", approach));

        s.gateBluff = Make("Story_GateBluff",
            B("The Bluff",
              "\"By order of the king, no one leaves Gibeah tonight,\" the commander says.\n\n" +
              "Jonathan raises his hand, and his father's signet ring catches the torchlight. The commander studies it, then steps aside, and the great gate groans open.", bluff));

        s.farewell = Make("Story_Farewell",
            B("The Farewell",
              "David passes through the gate and into the dark hills. He turns back once.\n\n" +
              "Jonathan stays at the threshold and watches until his friend is out of sight.", farewell));

        s.epilogue = Make("Story_Epilogue",
            B("Epilogue",
              "\"Go in peace. We have sworn friendship with each other in the name of the Lord. The Lord is witness between you and me forever.\"\n\n" +
              "1 Samuel 20:42", empty));

        s.jonathanCaptured = Make("Story_JonathanCaptured",
            B("Before the King",
              "Saul's guards drag Jonathan before the throne. \"Where is the son of Jesse?\" the king demands.\n\n" +
              "Jonathan lifts his chin and says nothing.", throne));

        // Lore scrolls: short, optional flavor notes found off the critical path, no gameplay effect.
        s.loreLedger = Make("Story_LoreLedger",
            B("A Steward's Ledger",
              "\"...grain for the household, and a portion set aside for the son of Jesse, per the king's table, until further notice.\"\n\n" +
              "The entry has been crossed out and rewritten twice.", null));
        s.loreMarket = Make("Story_LoreMarket",
            B("A Merchant's Complaint",
              "\"Curfew again tonight. Guards at every corner asking after travelers. Bad for business, worse for sleep. " +
              "I have half a mind to close the stall until the king finds whoever he's looking for.\"", null));
        s.lorePotter = Make("Story_LorePotter",
            B("A Potter's Prayer",
              "\"Lord, keep the dogs quiet and the kiln hot. A man's pots are the only thing in Gibeah that hold still these days.\"", null));
        s.loreWell = Make("Story_LoreWell",
            B("Well Water Rights",
              "\"By order of the king's household: water may be drawn from the square only between the second and fourth watch. " +
              "Sentries are instructed to note who comes and goes.\"", null));
        s.loreSoldier = Make("Story_LoreSoldier",
            B("A Soldier's Letter",
              "\"...tell mother I am well and the extra watches pay double. I do not know what we are watching for, only that the captain is short-tempered about it. " +
              "I will be home for the barley harvest, God willing.\"", null));

        AssetDatabase.SaveAssets();
        return s;
    }
}
