using UnityEngine;

public enum Role { Jonathan, David, GuardPatrol, GuardSentry, Commander, Civilian, Saul }

// Builds a stylised low-poly humanoid on named pivots so ProceduralCharacterAnim can animate it:
//   Visual/Rig/Hips/{Skirt, LegL/KneeL, LegR/KneeR, Torso/{Head, ArmL, ArmR, Cloak}}
// Flat-shaded faceted meshes, chunky proportions, big readable silhouettes (not realistic).
public static class HumanoidBuilder
{
    static GameObject Part(Transform parent, string name, Mesh mesh, Material mat, Vector3 pos, Vector3 euler = default, Vector3? scale = null)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.transform.localPosition = pos;
        go.transform.localRotation = Quaternion.Euler(euler);
        go.transform.localScale = scale ?? Vector3.one;
        go.AddComponent<MeshFilter>().sharedMesh = mesh;
        var r = go.AddComponent<MeshRenderer>();
        r.sharedMaterial = mat;
        return go;
    }

    static Transform Pivot(Transform parent, string name, Vector3 pos)
    {
        var t = new GameObject(name).transform;
        t.SetParent(parent, false);
        t.localPosition = pos;
        return t;
    }

    // shared meshes
    static Mesh Skirt()  => LowPoly.Cached("hum_skirt",  () => LowPoly.Frustum(8, 0.40f, 0.25f, 0.88f, -0.82f, true, true));
    static Mesh Hem()    => LowPoly.Cached("hum_hem",    () => LowPoly.Frustum(8, 0.418f, 0.405f, 0.10f, -0.82f, false, false));
    static Mesh Thigh()  => LowPoly.Cached("hum_thigh",  () => LowPoly.Frustum(6, 0.075f, 0.09f, 0.5f, -0.5f));
    static Mesh Shin()   => LowPoly.Cached("hum_shin",   () => LowPoly.Frustum(6, 0.06f, 0.075f, 0.45f, -0.45f));
    static Mesh Foot()   => LowPoly.Cached("hum_foot",   () => LowPoly.Box(new Vector3(0.14f, 0.07f, 0.28f), 1f, new Vector3(0, 0.035f, 0.05f)));
    static Mesh Chest()  => LowPoly.Cached("hum_chest",  () => LowPoly.Frustum(8, 0.21f, 0.27f, 0.52f, 0f, true, true, 1.12f, 0.85f));
    static Mesh Belt()   => LowPoly.Cached("hum_belt",   () => LowPoly.Frustum(8, 0.228f, 0.228f, 0.07f, 0.03f, false, false, 1.12f, 0.85f));
    static Mesh Sleeve() => LowPoly.Cached("hum_sleeve", () => LowPoly.Frustum(6, 0.058f, 0.085f, 0.46f, -0.46f));
    static Mesh Ball()   => LowPoly.Cached("hum_ball",   () => LowPoly.Ico(1f, 1));
    static Mesh Blob()   => LowPoly.Cached("hum_blob",   () => LowPoly.Ico(1f, 0));
    static Mesh Cube1()  => LowPoly.Cached("hum_cube",   () => LowPoly.Box(Vector3.one, 1f));
    static Mesh Shaft()  => LowPoly.Cached("hum_shaft",  () => LowPoly.Frustum(5, 0.022f, 0.022f, 2.3f, -0.6f));
    static Mesh Tip()    => LowPoly.Cached("hum_tip",    () => LowPoly.Frustum(4, 0.055f, 0f, 0.24f, 1.7f));
    static Mesh Disc()   => LowPoly.Cached("hum_disc",   () => LowPoly.Frustum(10, 0.27f, 0.27f, 0.05f, -0.025f));
    static Mesh Jar()    => LowPoly.Cached("hum_jar",    () => LowPoly.Frustum(8, 0.11f, 0.14f, 0.26f, 0f, true, true));
    static Mesh Basket() => LowPoly.Cached("hum_basket", () => LowPoly.Frustum(8, 0.13f, 0.18f, 0.16f, -0.08f, false, true));
    static Mesh Ring()   => LowPoly.Cached("hum_ring",   () => LowPoly.Frustum(10, 0.17f, 0.165f, 0.035f, 0f, false, false));

    public static void Build(Transform visual, Role role, Mats m, int variant = 0)
    {
        bool jon = role == Role.Jonathan, dav = role == Role.David;
        bool saul = role == Role.Saul;
        bool guard = role == Role.GuardPatrol || role == Role.GuardSentry || role == Role.Commander || saul;
        bool cmd = role == Role.Commander;
        bool civ = role == Role.Civilian;
        int v = Mathf.Abs(variant);
        Material[] civRobes = { m.civA, m.civB, m.civC, m.civD, m.civE, m.civF };
        bool elder = civ && v % 6 == 5;

        Material robe = civ ? civRobes[v % 6] : jon ? m.jonRobe : dav ? m.davRobe : saul ? m.clothRed : cmd ? m.cmdBody : role == Role.GuardSentry ? m.guardSentry : m.guardBody;
        Material trim = civ ? (v % 2 == 0 ? m.leather : civRobes[(v + 3) % 6]) : jon ? m.jonSash : dav ? m.davScarf : m.gold;
        Material hair = civ ? (elder ? m.hairGrey : v % 2 == 0 ? m.hairBlack : m.hairBrown) : jon ? m.hairBlack : dav ? m.hairBrown : saul ? m.hairGrey : m.hairBlack;

        var rig = Pivot(visual, "Rig", Vector3.zero);
        rig.localScale = Vector3.one * (cmd || saul ? 1.07f : dav ? 0.98f : civ ? 0.92f + (v % 3) * 0.04f : 1f);

        var hips = Pivot(rig, "Hips", new Vector3(0, 0.98f, 0));

        // robe skirt + hem trim
        Part(hips, "Skirt", Skirt(), robe, Vector3.zero);
        Part(hips, "Hem", Hem(), trim, Vector3.zero);

        // legs: hip pivot -> thigh, knee pivot -> shin, ankle pivot -> foot (feet peek out under the hem; the knee
        // lets a sneak fold properly, and the ankle counter-rotates against the thigh/knee so the sole stays
        // roughly flat on the ground instead of the foot rigidly mirroring the shin)
        foreach (var side in new[] { ("LegL", "KneeL", "AnkleL", -0.13f), ("LegR", "KneeR", "AnkleR", 0.13f) })
        {
            var leg = Pivot(hips, side.Item1, new Vector3(side.Item4, -0.05f, 0));
            Part(leg, "Thigh", Thigh(), m.skin, Vector3.zero);
            var knee = Pivot(leg, side.Item2, new Vector3(0, -0.5f, 0));
            Part(knee, "Shin", Shin(), m.skin, Vector3.zero);
            var ankle = Pivot(knee, side.Item3, new Vector3(0, -0.45f, 0));
            Part(ankle, "Foot", Foot(), m.leather, Vector3.zero);
        }

        // torso
        var torso = Pivot(hips, "Torso", new Vector3(0, 0.05f, 0));
        Part(torso, "Chest", Chest(), robe, Vector3.zero);
        Part(torso, "Belt", Belt(), guard ? m.leather : trim, Vector3.zero);
        if (jon) Part(torso, "Sash", Blob(), m.jonSash, new Vector3(0.1f, 0.3f, 0.02f), new Vector3(0, 0, 25), new Vector3(0.06f, 0.36f, 0.28f));
        if (cmd) Part(torso, "Breastplate", Cube1(), m.gold, new Vector3(0, 0.33f, 0.16f), Vector3.zero, new Vector3(0.36f, 0.34f, 0.06f));
        if (guard && !cmd && !saul) Part(torso, "Pauldrons", Cube1(), m.helmet, new Vector3(0, 0.5f, 0), Vector3.zero, new Vector3(0.66f, 0.07f, 0.34f));
        if (cmd) Part(torso, "Pauldrons", Cube1(), m.gold, new Vector3(0, 0.51f, 0), Vector3.zero, new Vector3(0.72f, 0.09f, 0.36f));

        // head
        var head = Pivot(torso, "Head", new Vector3(0, 0.57f, 0));
        Part(head, "Skull", Ball(), m.skin, new Vector3(0, 0.13f, 0), Vector3.zero, new Vector3(0.135f, 0.16f, 0.145f));
        Part(head, "Nose", Blob(), m.skin, new Vector3(0, 0.115f, 0.145f), Vector3.zero, new Vector3(0.028f, 0.04f, 0.045f));
        Part(head, "EyeL", Blob(), m.hairBlack, new Vector3(-0.05f, 0.155f, 0.128f), Vector3.zero, Vector3.one * 0.016f);
        Part(head, "EyeR", Blob(), m.hairBlack, new Vector3(0.05f, 0.155f, 0.128f), Vector3.zero, Vector3.one * 0.016f);

        if (jon)
        {
            Part(head, "Hair", Ball(), hair, new Vector3(0, 0.185f, -0.02f), Vector3.zero, new Vector3(0.15f, 0.12f, 0.16f));
            Part(head, "Circlet", Ring(), m.gold, new Vector3(0, 0.2f, 0));
        }
        else if (dav)
        {
            Part(head, "Curls", Ball(), hair, new Vector3(0, 0.15f, -0.035f), Vector3.zero, new Vector3(0.155f, 0.15f, 0.16f));
            Part(head, "Scarf", Ball(), m.davScarf, new Vector3(0, 0.215f, -0.01f), Vector3.zero, new Vector3(0.16f, 0.1f, 0.17f));
            Part(head, "ScarfTail", Cube1(), m.davScarf, new Vector3(-0.14f, 0.1f, -0.06f), new Vector3(0, 0, 10), new Vector3(0.05f, 0.24f, 0.09f));
            Part(head, "Beard", Blob(), hair, new Vector3(0, 0.03f, 0.1f), Vector3.zero, new Vector3(0.075f, 0.06f, 0.05f));
        }
        else if (civ)
        {
            Material cloth = civRobes[(v + 2) % 6];
            switch (v % 3)
            {
                case 0:   // headcloth with a hanging tail
                    Part(head, "Hair", Ball(), hair, new Vector3(0, 0.15f, -0.03f), Vector3.zero, new Vector3(0.15f, 0.14f, 0.155f));
                    Part(head, "Headcloth", Ball(), cloth, new Vector3(0, 0.215f, -0.01f), Vector3.zero, new Vector3(0.16f, 0.1f, 0.17f));
                    Part(head, "ClothTail", Cube1(), cloth, new Vector3(0.13f, 0.1f, -0.08f), new Vector3(0, 0, -10), new Vector3(0.05f, 0.26f, 0.09f));
                    break;
                case 1:   // wide straw hat
                    Part(head, "Hair", Ball(), hair, new Vector3(0, 0.16f, -0.02f), Vector3.zero, new Vector3(0.15f, 0.13f, 0.155f));
                    Part(head, "HatBrim", Disc(), m.straw, new Vector3(0, 0.235f, 0), new Vector3(0, 0, 0), new Vector3(0.75f, 0.5f, 0.75f));
                    Part(head, "HatCrown", Ball(), m.straw, new Vector3(0, 0.27f, 0), Vector3.zero, new Vector3(0.11f, 0.08f, 0.11f));
                    break;
                default:  // bare head with a band
                    Part(head, "Hair", Ball(), hair, new Vector3(0, 0.185f, -0.02f), Vector3.zero, new Vector3(0.15f, 0.12f, 0.16f));
                    Part(head, "Band", Ring(), cloth, new Vector3(0, 0.19f, 0), Vector3.zero, new Vector3(0.95f, 1f, 0.95f));
                    break;
            }
            if (v % 2 == 0 || elder) Part(head, "Beard", Blob(), hair, new Vector3(0, 0.03f, 0.1f), Vector3.zero, new Vector3(elder ? 0.09f : 0.07f, elder ? 0.12f : 0.06f, 0.05f));

            // carried goods
            switch (v % 4)
            {
                case 1:   // clay jar balanced on the head
                    Part(head, "Jar", Jar(), m.clay, new Vector3(0, 0.36f, 0), Vector3.zero, Vector3.one);
                    break;
                case 2:   // basket on the left arm
                    break;
                case 3:   // bundle on the back
                    Part(torso, "Bundle", Ball(), m.civE, new Vector3(0, 0.3f, -0.3f), Vector3.zero, new Vector3(0.22f, 0.26f, 0.2f));
                    break;
            }
        }
        else if (saul)
        {
            Part(head, "Hair", Ball(), hair, new Vector3(0, 0.185f, -0.02f), Vector3.zero, new Vector3(0.15f, 0.12f, 0.16f));
            Part(head, "Crown", Ring(), m.gold, new Vector3(0, 0.215f, 0), Vector3.zero, new Vector3(1.15f, 1.15f, 1.15f));
            Part(head, "CrownPoint", Blob(), m.gold, new Vector3(0, 0.26f, 0), Vector3.zero, Vector3.one * 0.025f);
            Part(head, "Beard", Blob(), hair, new Vector3(0, 0.03f, 0.1f), Vector3.zero, new Vector3(0.085f, 0.07f, 0.05f));
        }
        else
        {
            // helmet: bronze dome, rim, nose guard, cheek plates
            float hs = cmd ? 1.06f : 1f;
            Part(head, "Helmet", Ball(), m.helmet, new Vector3(0, 0.2f, -0.005f), Vector3.zero, new Vector3(0.16f, 0.115f, 0.17f) * hs);
            Part(head, "HelmetRim", Ring(), m.helmet, new Vector3(0, 0.155f, 0), Vector3.zero, Vector3.one * 1.08f);
            Part(head, "NoseGuard", Cube1(), m.helmet, new Vector3(0, 0.15f, 0.15f), Vector3.zero, new Vector3(0.028f, 0.11f, 0.02f));
            Part(head, "CheekL", Cube1(), m.helmet, new Vector3(-0.14f, 0.09f, 0.02f), Vector3.zero, new Vector3(0.02f, 0.1f, 0.12f));
            Part(head, "CheekR", Cube1(), m.helmet, new Vector3(0.14f, 0.09f, 0.02f), Vector3.zero, new Vector3(0.02f, 0.1f, 0.12f));
            Part(head, "Beard", Blob(), m.hairBlack, new Vector3(0, 0.03f, 0.1f), Vector3.zero, new Vector3(0.09f, 0.07f, 0.05f));
            if (cmd)
            {
                Part(head, "Crest", Cube1(), m.plume, new Vector3(0, 0.36f, -0.02f), Vector3.zero, new Vector3(0.05f, 0.14f, 0.34f));
                Part(head, "CrestTail", Cube1(), m.plume, new Vector3(0, 0.3f, -0.22f), new Vector3(-30, 0, 0), new Vector3(0.05f, 0.2f, 0.08f));
            }
        }

        // arms (pivot at the shoulder)
        foreach (var side in new[] { ("ArmL", -0.31f), ("ArmR", 0.31f) })
        {
            var arm = Pivot(torso, side.Item1, new Vector3(side.Item2, 0.46f, 0));
            Part(arm, "Sleeve", Sleeve(), robe, Vector3.zero);
            Part(arm, "Cuff", Ring(), trim, new Vector3(0, -0.46f, 0), Vector3.zero, new Vector3(0.36f, 1f, 0.36f));
            Part(arm, "Hand", Ball(), m.skin, new Vector3(0, -0.5f, 0), Vector3.zero, Vector3.one * 0.06f);
        }

        var armR = torso.Find("ArmR");
        var armL = torso.Find("ArmL");

        if (civ)
        {
            if (v % 4 == 2)
            {
                Part(armL, "Basket", Basket(), m.crateWood, new Vector3(-0.02f, -0.62f, 0.12f), Vector3.zero, Vector3.one);
                Part(armL, "BasketGoods", Blob(), m.clothOchre, new Vector3(-0.02f, -0.53f, 0.12f), Vector3.zero, new Vector3(0.16f, 0.07f, 0.16f));
            }
            if (elder)
                Part(armR, "Staff", Shaft(), m.woodDark, new Vector3(0.03f, -0.5f, 0.05f), Vector3.zero, new Vector3(1.4f, 0.78f, 1.4f));
        }
        if (jon)
        {
            Part(armR, "SignetRing", Blob(), m.gold, new Vector3(0.03f, -0.5f, 0.03f), Vector3.zero, Vector3.one * 0.03f);
            Part(hips, "Satchel", Cube1(), m.leather, new Vector3(-0.36f, -0.05f, 0.02f), Vector3.zero, new Vector3(0.1f, 0.22f, 0.22f));
            var cloak = Pivot(torso, "Cloak", new Vector3(0, 0.5f, -0.2f));
            Part(cloak, "CloakMesh", Cube1(), m.jonCloak, new Vector3(0, -0.5f, -0.02f), Vector3.zero, new Vector3(0.62f, 1.0f, 0.05f));
            Part(cloak, "CloakTrim", Cube1(), m.gold, new Vector3(0, -1.0f, -0.02f), Vector3.zero, new Vector3(0.62f, 0.04f, 0.055f));
        }
        if (dav)
        {
            // harp on his back
            var harp = Pivot(torso, "Harp", new Vector3(0, 0.3f, -0.27f));
            harp.localRotation = Quaternion.Euler(0, 0, 8);
            Part(harp, "FrameL", Cube1(), m.gold, new Vector3(-0.2f, 0, 0), Vector3.zero, new Vector3(0.05f, 0.7f, 0.05f));
            Part(harp, "FrameR", Cube1(), m.gold, new Vector3(0.22f, 0.05f, 0), Vector3.zero, new Vector3(0.05f, 0.55f, 0.05f));
            Part(harp, "Neck", Cube1(), m.gold, new Vector3(0.0f, 0.34f, 0), new Vector3(0, 0, -10), new Vector3(0.5f, 0.05f, 0.05f));
            Part(harp, "Strings", Cube1(), m.clothOchre, new Vector3(0.01f, 0.03f, 0), Vector3.zero, new Vector3(0.34f, 0.6f, 0.012f));
            var cloak = Pivot(torso, "Cloak", new Vector3(0, 0.5f, -0.19f));
            Part(cloak, "Mantle", Cube1(), m.davRobe, new Vector3(0, -0.28f, -0.01f), Vector3.zero, new Vector3(0.55f, 0.55f, 0.04f));
        }
        if (guard)
        {
            // spear in the right hand, shield on the left arm
            float len = role == Role.GuardSentry ? 1.15f : 1f;
            var spear = Pivot(armR, "Spear", new Vector3(0.0f, -0.5f, 0.02f));
            spear.localRotation = Quaternion.Euler(0, 0, 0);
            Part(spear, "Shaft", Shaft(), m.spearWood, Vector3.zero, Vector3.zero, new Vector3(1f, len, 1f));
            Part(spear, "Tip", Tip(), m.metal, new Vector3(0, (len - 1f) * 2.3f, 0));
            if (cmd) Part(spear, "Tassel", Blob(), m.plume, new Vector3(0, 1.55f, 0), Vector3.zero, new Vector3(0.05f, 0.09f, 0.05f));
            var shield = Pivot(armL, "Shield", new Vector3(-0.07f, -0.36f, 0.1f));
            Part(shield, "Face", Disc(), role == Role.GuardSentry ? m.guardSentry : cmd ? m.cmdBody : m.guardBody, Vector3.zero, new Vector3(0, 0, 90), new Vector3(1, 1, 1));
            Part(shield, "Rim", Ring(), m.helmet, new Vector3(0.03f, 0, 0), new Vector3(0, 0, 90), new Vector3(1.6f, 1.6f, 1.6f));
            Part(shield, "Boss", Blob(), m.helmet, new Vector3(-0.04f, 0, 0), Vector3.zero, Vector3.one * 0.06f);
            if (cmd)
            {
                var cape = Pivot(torso, "Cloak", new Vector3(0, 0.5f, -0.2f));
                Part(cape, "CapeMesh", Cube1(), m.plume, new Vector3(0, -0.55f, -0.02f), Vector3.zero, new Vector3(0.7f, 1.1f, 0.05f));
                Part(cape, "CapeTrim", Cube1(), m.gold, new Vector3(0, -1.1f, -0.02f), Vector3.zero, new Vector3(0.7f, 0.05f, 0.06f));
            }
        }
    }
}
