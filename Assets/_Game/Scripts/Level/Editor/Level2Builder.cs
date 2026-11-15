using UnityEditor;
using UnityEngine;
using static FezLevelKit;

/// <summary>
/// Meniu: Mochini Bouncelini > Levels > Build Level 2 (autumn tree). Scena "Level2" = BUILD v1.1, tema TOAMNA.
///
/// Un copac urias intr-o camera rotativa (16 x 16): trunchiul e decor, crengile sunt platforme, iar trei
/// platforme merg pe sine (ca in video-ul de referinta) - si conteaza din ce unghi le privesti:
///   sina 1 merge IN ADANCIME si in sus: din fata pare un lift, din lateral aluneca pe diagonala;
///   sina 2 e un lift vertical pe fata dreapta;
///   sina 3 merge pe X, in spatele trunchiului: din fata trece prin spatele lui, din lateral pare nemiscata.
/// Verificat pe regula Fez cu fizica reala: fara sine si fara rotire NU ajungi la altar.
///
/// ALTAR - REVERT: double jump (moment mare).
/// ACTUL 2 - patru "porti" de +6 randuri: imposibile fara al doilea salt (W ajunge la +4, W + rotire + dash la +5),
/// cu el (W tinut, SPACE in varf) ajungi. Sus, o scurtatura pentru curajosi: double jump + dash peste sina 3.
/// </summary>
public static class Level2Builder
{
    const string ScenePath = NerfedEditorUtil.ScenesDir + "/Level2.unity";

    public static Theme Autumn()
    {
        var t = new Theme
        {
            prefix = "L2",
            skyTop = new Color(0.52f, 0.34f, 0.52f), skyMid = new Color(0.96f, 0.56f, 0.40f), skyLow = new Color(1f, 0.80f, 0.55f),
            haze = new Color(0.92f, 0.60f, 0.50f), hazeStrength = 0.55f,
            ambient = new Color(0.58f, 0.46f, 0.44f), sun = new Color(1f, 0.82f, 0.62f), sunIntensity = 1.15f,
            sunEuler = new Vector3(44f, -32f, 0f),
            squares = new Color(1f, 0.85f, 0.75f, 0.13f),
            cap = Cap.Leaves,
            capA = new Color(0.93f, 0.48f, 0.16f), capB = new Color(0.80f, 0.26f, 0.16f), capHang = new Color(0.96f, 0.72f, 0.22f),
            flowers = new[] { new Color(0.85f, 0.25f, 0.2f), new Color(0.98f, 0.85f, 0.4f) },
            bodyColor = new Color(0.46f, 0.31f, 0.23f), bodyTex = Tex.Wood,
            particles = Particles.Leaves,
            particleA = new Color(0.95f, 0.50f, 0.15f, 1f), particleB = new Color(0.85f, 0.22f, 0.15f, 1f),
            ink = new Color(0.20f, 0.14f, 0.16f),
        };
        t.skins["Dirt"] = new Skin(new Color(0.58f, 0.43f, 0.30f), Tex.Stone);
        t.skins["Bark"] = new Skin(new Color(0.52f, 0.36f, 0.25f), Tex.Wood);
        t.skins["Plank"] = new Skin(new Color(0.78f, 0.56f, 0.36f), Tex.Plank);
        t.skins["PlankHung"] = new Skin(new Color(0.78f, 0.56f, 0.36f), Tex.Plank, hung: true);
        t.skins["Gold"] = new Skin(new Color(0.98f, 0.78f, 0.32f), Tex.Block, noCap: true);
        return t;
    }

    // ---- piesele statice (verificate); sinele sunt construite separat, cu Rail()
    static readonly Piece[] Pieces =
    {
        S("Plaza", -1, 10, -6, 5, 0, 0, "Dirt"),
        S("Stump_L", -3, -2, -8, -7, 3, 3, "Bark"),
        S("Ledge_BL", -3, -1, 6, 7, 9, 9, "PlankHung"),
        S("Branch_BR", 9, 12, 6, 7, 9, 9, "Bark"),
        S("Branch_Shrine", 7, 10, -7, -6, 16, 16, "Gold"),
        S("Twig_1", 1, 3, -8, -7, 22, 22, "Bark"),
        S("Twig_2", -3, -2, -3, -1, 28, 28, "Bark"),
        S("Branch_Top", 10, 12, 3, 4, 36, 36, "Bark"),
        S("Summit", 3, 6, -2, 1, 42, 42, "Plank"),
    };

    [MenuItem("Mochini Bouncelini/Levels/Build Level 2 (autumn tree)")]
    public static void Build()
    {
        if (!UnityEditor.SceneManagement.EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
        var t = Autumn();
        var c = Begin(t, -3, -8, 16, Pieces, new Vector3(1.5f, 1.55f, -5.5f), -6f);

        // ---- sinele (centrul platformei, varful ei = randul piesei + 1)
        var plank = new Vector3(2f, 0.5f, 2f);
        Rail(c, "Rail_1_DepthLift", new Vector3(-2f, 3.75f, -5f), new Vector3(-2f, 7.75f, 5f), plank, 2.0f, 0f);
        Rail(c, "Rail_2_Lift", new Vector3(12f, 9.75f, 3f), new Vector3(12f, 15.75f, 3f), plank, 1.8f, 0.5f);
        Rail(c, "Rail_3_BehindTrunk", new Vector3(-1f, 30.75f, 7f), new Vector3(10f, 30.75f, 7f), plank, 2.4f, 0f);

        // ---- trunchiul (decor), crengi de legatura spre platforme, coroana
        Color bark = t.bodyColor, barkDark = new Color(0.36f, 0.24f, 0.18f);
        DecorBox(c, "Trunk", new Vector3(3f, 0f, -2f), new Vector3(7f, 42f, 2f), bark, Tex.Wood);
        DecorBox(c, "Roots_F", new Vector3(2.2f, 0f, -2.8f), new Vector3(7.8f, 1.6f, 2.8f), barkDark, Tex.Wood);
        Cube(c, "Bough", new Vector3(8.6f, 16.3f, -4f), new Vector3(0.5f, 0.5f, 4f), barkDark);
        Cube(c, "Bough", new Vector3(2.4f, 22.3f, -4.9f), new Vector3(0.5f, 0.5f, 5.8f), barkDark);
        Cube(c, "Bough", new Vector3(0.4f, 28.3f, -1f), new Vector3(5.2f, 0.5f, 0.5f), barkDark);
        Cube(c, "Bough", new Vector3(9.3f, 36.3f, 3f), new Vector3(4.6f, 0.5f, 0.5f), barkDark);
        Cube(c, "Bough", new Vector3(10.5f, 9.3f, 4.5f), new Vector3(0.5f, 0.5f, 5f), barkDark);
        // coroana: blocuri mari de frunze, DEASUPRA varfului (ca un acoperis), ca sa nu ascunda steagul si pe mochi
        var rng = new System.Random(5);
        Color[] leaves = { t.capA, t.capB, t.capHang, new Color(0.88f, 0.38f, 0.14f) };
        for (int n = 0; n < 26; n++)
        {
            float a = n * 0.95f;
            float r = 1.5f + (float)rng.NextDouble() * 5f;
            var p = new Vector3(5f + Mathf.Cos(a) * r, 48f + (float)rng.NextDouble() * 3.5f, Mathf.Sin(a) * r);
            float s = 1.6f + (float)rng.NextDouble() * 1.6f;
            Cube(c, "Canopy", p, new Vector3(s, s * 0.8f, s), leaves[rng.Next(leaves.Length)]);
        }
        // scorbura, ciuperci pe trunchi, felinare
        Cube(c, "Hollow", new Vector3(5f, 5.5f, -2.02f), new Vector3(1.4f, 2f, 0.05f), new Color(0.15f, 0.10f, 0.10f), true);
        foreach (var m in new[] { new Vector3(3f, 12f, -2.1f), new Vector3(7.1f, 19f, -0.5f), new Vector3(3.5f, 33f, 2.1f) })
        {
            Cube(c, "MushroomCap", m + Vector3.up * 0.15f, new Vector3(0.7f, 0.2f, 0.5f), new Color(0.85f, 0.25f, 0.2f));
            Cube(c, "MushroomStem", m, new Vector3(0.18f, 0.3f, 0.18f), new Color(0.95f, 0.90f, 0.80f));
        }
        Lantern(c, new Vector3(9.6f, 17f, -6.5f), new Color(1f, 0.75f, 0.35f));
        Lantern(c, new Vector3(1.4f, 23f, -7.6f), new Color(1f, 0.75f, 0.35f));
        Lantern(c, new Vector3(11.6f, 37f, 3.5f), new Color(1f, 0.75f, 0.35f));
        Lantern(c, new Vector3(9.5f, 1f, -5.6f), new Color(1f, 0.75f, 0.35f));
        Vine(c, new Vector3(-2.9f, 9f, 7.95f), 6, new Color(0.62f, 0.32f, 0.15f));
        Vine(c, new Vector3(12.9f, 9f, 7.95f), 5, new Color(0.62f, 0.32f, 0.15f));

        // ---- piata: dovleci, copacei, gramezi de frunze
        Color pumpkin = new Color(0.95f, 0.52f, 0.12f), stem = new Color(0.35f, 0.45f, 0.18f);
        foreach (var p in new[] { new Vector3(0.2f, 1.3f, -3.6f), new Vector3(0.9f, 1.25f, -2.9f), new Vector3(8.4f, 1.3f, 3.8f), new Vector3(9.6f, 1.35f, -0.6f) })
        {
            Cube(c, "Pumpkin", p, new Vector3(0.7f, 0.55f, 0.6f), pumpkin);
            Cube(c, "PumpkinStem", p + Vector3.up * 0.33f, new Vector3(0.1f, 0.18f, 0.1f), stem);
        }
        Tree(c, new Vector3(-0.5f, 1f, 4.5f), 2.2f, barkDark, leaves);
        Tree(c, new Vector3(10.2f, 1f, -4.6f), 2.6f, barkDark, leaves);

        // ---- standul (Community Manager-ul de la v1.1)
        Stand(c, new Vector3(6.5f, 1f, -5.5f), "Ohio_Comm_Mgr", new[]
        {
            "WELCOME TO v1.1!!! we listened to ALL of your feedback. we read none of it.",
            "then patch 1.1.2 removed the DOUBLE JUMP. 'confusing for new players'. it's giving Ohio.",
            "its REVERT is up in the old tree. the platforms there ride on rails. very demure. very mindful.",
            "a rail that goes INTO the screen looks frozen. it's not. turn the world and watch it. real.",
            "S to talk to me again. you won't. nobody does. i'm fine. this is fine."
        }, "ride the rails up the tree", new Color(0.92f, 0.55f, 0.70f), "cap,mustache");

        Revert(c, new Vector3(9f, 17.6f, -6f), Ability.DoubleJump, "PATCH 1.1.2  -  REVERTED",
            new[] { "Double jump restored (SPACE in the air).", "We hear you." },
            "climb to the top of the tree", "DOUBLE JUMP RESTORED", "SPACE  x2", new Color(1f, 0.75f, 0.35f));

        Flag(c, new Vector3(5f, 43f, -1f), 2, new Color(0.85f, 0.25f, 0.2f));

        Checkpoint(c, Top(-2, 3, -7));
        Checkpoint(c, Top(-2, 9, 7));
        Checkpoint(c, Top(10, 9, 6));
        Checkpoint(c, Top(8, 16, -7));
        Checkpoint(c, Top(2, 22, -8));
        Checkpoint(c, Top(-2, 28, -2));
        Checkpoint(c, Top(11, 36, 3));

        Prompt(c, "~ BUILD v1.1 ~", new Vector3(2.5f, 7.6f, -8.5f), 8f, -2f);
        // trunchiul continua putin peste varf, ca o tulpina in coroana
        DecorBox(c, "TrunkTop", new Vector3(4.2f, 43f, -0.8f), new Vector3(5.8f, 49f, 0.8f), bark, Tex.Wood);
        Prompt(c, "a rail that goes INTO the screen\nlooks like a lift. turn the world.", new Vector3(-1.5f, 6.5f, -8.5f), 3.8f, 2f);
        Prompt(c, "hold W, launch straight up,\nthen SPACE at the top", new Vector3(5f, 20.5f, -8.5f), 4f, -2f);
        Prompt(c, "brave? double jump + dash.\nwise? the rail.", new Vector3(2f, 33.5f, -8.5f), 3.8f, 2f);

        Finish(c, ScenePath, 4f, 44f, 7f);
        Debug.Log("[Mochini] Level 2 (copacul de toamna) construit: " + ScenePath);
    }
}
