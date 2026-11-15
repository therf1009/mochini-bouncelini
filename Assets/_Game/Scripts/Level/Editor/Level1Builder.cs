using UnityEditor;
using UnityEngine;
using static FezLevelKit;

/// <summary>
/// Meniu: Mochini Bouncelini > Levels > Build Level 1 (summer tower). Scena "Level1" = BUILD v1.0, tema VARA.
///
/// Un turn de caramida in stil Fez, intr-o singura camera rotativa (14 x 14). Corpul turnului e decor;
/// solide sunt pervazurile, scarile, altarul, balcoanele. Proiectat si verificat pe regula Fez (rotirea pastreaza
/// pozitia 3D), cu fizica reala a lui mochi:
///
/// ACTUL 1 - "uita-te in jur" (fara dash; fara rotire NU se poate)
///   piata -> scara de pe fata stanga (din fata e o coloana, din lateral o scara) -> balconul din spate-stanga
///   -> privit din lateral, coltul din spate-dreapta e "langa" balcon (de fapt e peste tot turnul)
///   -> fata dreapta -> altarul: sigilat din fata, intri prin latura lui dreapta (unghiul 3).
/// ALTAR - REVERT: dash (moment mare) si usa glitch de langa el se aprinde.
/// ACTUL 2 - dash (fara dash, cel mai sus ajungi la altar)
///   usa glitch (SPACE + SHIFT) -> saritura lunga (W + SHIFT) sau acoperisul altarului (doua drumuri)
///   -> perdeaua glitch de pe fata dreapta -> doua cristale, din coltul din spate-dreapta in cel din fata-stanga
///   -> varful turnului, cu gard glitch: W + SHIFT prin el -> steag.
/// </summary>
public static class Level1Builder
{
    const string ScenePath = NerfedEditorUtil.ScenesDir + "/Level1.unity";

    public static Theme Summer()
    {
        var t = new Theme
        {
            prefix = "L1",
            skyTop = new Color(1f, 0.45f, 0.33f), skyMid = new Color(1f, 0.64f, 0.42f), skyLow = new Color(1f, 0.84f, 0.52f),
            haze = new Color(1f, 0.70f, 0.50f), hazeStrength = 0.55f,
            ambient = new Color(0.60f, 0.50f, 0.46f), sun = new Color(1f, 0.93f, 0.80f), sunIntensity = 1.25f,
            squares = new Color(1f, 0.93f, 0.85f, 0.16f),
            cap = Cap.Grass,
            capA = new Color(0.58f, 0.82f, 0.30f), capB = new Color(0.47f, 0.73f, 0.26f), capHang = new Color(0.40f, 0.64f, 0.22f),
            flowers = new[] { new Color(1f, 0.92f, 0.35f), new Color(1f, 0.55f, 0.70f), new Color(0.95f, 0.95f, 1f), new Color(0.65f, 0.55f, 1f) },
            bodyColor = new Color(0.80f, 0.44f, 0.38f), bodyTex = Tex.Brick,
            particles = Particles.Pollen,
            particleA = new Color(1f, 0.97f, 0.75f, 0.9f), particleB = new Color(1f, 0.85f, 0.55f, 0.7f),
        };
        t.skins["Stone"] = new Skin(new Color(0.84f, 0.73f, 0.60f), Tex.Stone);
        t.skins["Wood"] = new Skin(new Color(0.72f, 0.48f, 0.30f), Tex.Wood);
        t.skins["Plank"] = new Skin(new Color(0.84f, 0.62f, 0.38f), Tex.Plank);
        t.skins["PlankHung"] = new Skin(new Color(0.84f, 0.62f, 0.38f), Tex.Plank, hung: true);
        t.skins["Brick"] = new Skin(new Color(0.74f, 0.38f, 0.33f), Tex.Brick);
        t.skins["Roof"] = new Skin(new Color(0.62f, 0.28f, 0.26f), Tex.Brick);
        t.skins["Gold"] = new Skin(new Color(0.98f, 0.78f, 0.32f), Tex.Block, noCap: true);
        return t;
    }

    // ---- piesele, exact cum au trecut verificarile (unghiuri, porti, fara scurtaturi)
    static readonly Piece[] Pieces =
    {
        // ACTUL 1
        S("Plaza", 0, 9, -5, 4, 0, 0, "Stone"),
        S("L_Step1", -1, -1, -3, -3, 1, 1, "Wood"),
        S("L_Step2", -1, -1, -1, -1, 2, 2, "Wood"),
        S("L_Step3", -1, -1, 1, 1, 3, 3, "Wood"),
        S("L_Step4", -1, -1, 3, 3, 4, 4, "Wood"),
        S("Balcony_BL", -2, 0, 5, 6, 5, 5, "Plank"),
        S("Ledge_BR", 9, 11, 3, 4, 6, 6, "Plank"),
        S("Right_1", 10, 11, 0, 1, 8, 8, "Wood"),
        S("Right_2", 10, 11, -3, -2, 10, 10, "Wood"),
        // ALTARUL
        S("Shrine", 2, 5, -5, -4, 14, 14, "Gold"),
        S("Shrine_Pillar", 6, 6, -5, -5, 14, 20, "Brick"),
        S("Shrine_Roof", 1, 6, -5, -4, 21, 21, "Roof"),
        S("Door_Sill", 1, 1, -5, -4, 14, 14, "Gold"),
        G("GlitchDoor", 1, 1, -5, -5, 15, 20),
        // ACTUL 2
        S("Porch", -2, 0, -7, -7, 15, 15, "Plank"),
        S("Plank_FL", -2, -1, -7, -7, 18, 18, "PlankHung"),
        S("Corner_FR", 7, 11, -7, -7, 18, 18, "Plank"),
        G("GlitchCurtain_R", 10, 11, -4, -4, 19, 29),
        S("Right_3", 10, 11, -3, -2, 21, 21, "Wood"),
        S("Right_4", 10, 11, 1, 2, 23, 23, "Wood"),
        S("Corner_BR_High", 10, 11, 5, 6, 24, 24, "PlankHung"),
        S("Corner_FL_High", -2, -1, -7, -6, 28, 28, "PlankHung"),
        S("Summit", 3, 6, -2, 1, 31, 31, "Stone"),
        G("SummitFence_L", 2, 2, -2, 1, 32, 37),
        G("SummitFence_R", 7, 7, -2, 1, 32, 37),
    };

    [MenuItem("Mochini Bouncelini/Levels/Build Level 1 (summer tower)")]
    public static void Build()
    {
        if (!EditorSceneManager_SaveIfUserWants()) return;
        var t = Summer();
        var c = Begin(t, -2, -7, 14, Pieces, new Vector3(1.5f, 1.55f, -4.5f), -6f);

        // ---- corpul turnului (decor): caramida calda, ferestre, tevi, iedera
        DecorBox(c, "TowerBody", new Vector3(3f, 1f, -2f), new Vector3(7f, 31f, 2f), t.bodyColor, Tex.Brick);
        DecorBox(c, "TowerBand", new Vector3(2.85f, 13f, -2.15f), new Vector3(7.15f, 13.5f, 2.15f), new Color(0.88f, 0.70f, 0.55f), Tex.Stone);
        DecorBox(c, "TowerCornice", new Vector3(2.8f, 30.4f, -2.2f), new Vector3(7.2f, 31f, 2.2f), new Color(0.88f, 0.70f, 0.55f), Tex.Stone);
        Color frame = new Color(0.55f, 0.36f, 0.24f), glass = new Color(0.22f, 0.27f, 0.42f);
        foreach (float y in new[] { 4.5f, 9f, 17f, 25.5f })
        {
            Window(c, new Vector3(5f, y, -2.01f), Vector3.back, frame, glass);
            Window(c, new Vector3(5f, y + 2f, 2.01f), Vector3.forward, frame, glass);
            Window(c, new Vector3(2.99f, y + 1f, 0f), Vector3.left, frame, glass);
            Window(c, new Vector3(7.01f, y - 1f, 0f), Vector3.right, frame, glass);
        }
        Color copper = new Color(0.86f, 0.52f, 0.28f), blue = new Color(0.42f, 0.50f, 0.78f), red = new Color(0.72f, 0.32f, 0.32f);
        Pipe(c, copper, 0.11f, new Vector3(6.6f, 1f, -2.25f), new Vector3(6.6f, 7.5f, -2.25f), new Vector3(7.3f, 7.5f, -2.25f), new Vector3(7.3f, 12.5f, -2.25f));
        Pipe(c, blue, 0.13f, new Vector3(3.4f, 22f, -2.25f), new Vector3(3.4f, 26.5f, -2.25f), new Vector3(4.6f, 26.5f, -2.25f), new Vector3(4.6f, 29.5f, -2.25f));
        Pipe(c, red, 0.12f, new Vector3(2.75f, 2f, 1.4f), new Vector3(2.75f, 11f, 1.4f), new Vector3(2.75f, 11f, -0.6f), new Vector3(2.75f, 19f, -0.6f));
        Pipe(c, blue, 0.11f, new Vector3(7.25f, 15f, 1.5f), new Vector3(7.25f, 21f, 1.5f), new Vector3(7.25f, 21f, 0.3f), new Vector3(7.25f, 28f, 0.3f));
        Pipe(c, copper, 0.1f, new Vector3(5.5f, 32f, 2.25f), new Vector3(5.5f, 27f, 2.25f), new Vector3(3.6f, 27f, 2.25f), new Vector3(3.6f, 21f, 2.25f));
        Color ivy = new Color(0.36f, 0.60f, 0.22f);
        Vine(c, new Vector3(-1.9f, 6f, 6.45f), 7, ivy);
        Vine(c, new Vector3(0.9f, 6f, 6.45f), 5, ivy);
        Vine(c, new Vector3(11.9f, 19f, -6.55f), 8, ivy);
        Vine(c, new Vector3(7.1f, 19f, -6.55f), 4, ivy);
        Vine(c, new Vector3(-1.95f, 29f, -6.6f), 6, ivy);
        Vine(c, new Vector3(3.2f, 30.3f, -2.25f), 9, ivy);
        Vine(c, new Vector3(6.8f, 30.3f, 2.25f), 7, ivy);

        // ---- piata: copaci, tufe, felinare
        Color trunk = new Color(0.45f, 0.30f, 0.20f);
        Tree(c, new Vector3(0.6f, 1f, 4.4f), 2.4f, trunk, t.capA, t.capB, new Color(0.38f, 0.62f, 0.22f));
        Tree(c, new Vector3(9.4f, 1f, 4.3f), 2.8f, trunk, t.capA, t.capB, new Color(0.62f, 0.85f, 0.35f));
        Tree(c, new Vector3(9.3f, 1f, -1.5f), 2.1f, trunk, t.capB, new Color(0.55f, 0.78f, 0.28f));
        Lantern(c, new Vector3(0.4f, 1f, -1.2f), new Color(1f, 0.85f, 0.45f));
        Lantern(c, new Vector3(9.6f, 19f, -6.5f), new Color(1f, 0.85f, 0.45f));
        Lantern(c, new Vector3(-1.6f, 16f, -6.5f), new Color(1f, 0.85f, 0.45f));

        // ---- standul (QA te trimite in turn)
        Stand(c, new Vector3(6.5f, 1f, -4.5f), "QA_Tester_04", new[]
        {
            "oh. a player. hi. i'm QA, i test things.",
            "bad news: patch 1.0.4 removed your DASH. 'underused', apparently.",
            "the REVERT token is in the shrine, halfway up the old tower. the shrine is sealed... from the front.",
            "tip: Q and E turn the whole world. this tower is a different tower from every side.",
            "and if a ledge looks like a wall... look at it from somewhere else."
        }, "climb the tower, find the REVERT", new Color(0.55f, 0.78f, 0.62f));

        // ---- REVERT (dash), cu momentul mare
        Revert(c, new Vector3(3.5f, 15.6f, -4.5f), Ability.Dash, "PATCH 1.0.4  -  REVERTED",
            new[] { "Dash restored (SHIFT).", "Rollback due to player backlash." },
            "the glitched door. SPACE, then SHIFT.", "DASH RESTORED", "SHIFT", new Color(0.35f, 0.90f, 1f));

        // ---- cristalele (din coltul din spate-dreapta in cel din fata-stanga)
        Orb(c, new Vector3(-1.5f, 29.5f, -0.5f));
        Orb(c, new Vector3(-1.5f, 29.5f, -3.5f));

        // ---- steagul, pe acoperisul turnului (intre gardurile glitch)
        Flag(c, new Vector3(5f, 32f, -0.5f), 1, new Color(0.98f, 0.62f, 0.25f));

        // ---- checkpoint-uri (felinare): atingi = salveaza pozitia 3D si unghiul
        Checkpoint(c, Top(-1, 5, 5));
        Checkpoint(c, Top(10, 10, -3));
        Checkpoint(c, Top(2, 14, -5));
        Checkpoint(c, Top(-1, 15, -7));
        Checkpoint(c, Top(9, 18, -7));
        Checkpoint(c, Top(11, 21, -2));
        Checkpoint(c, Top(10, 24, 6));
        Checkpoint(c, Top(-2, 28, -6));

        // ---- texte
        Prompt(c, "~ BUILD v1.0 ~", new Vector3(3f, 7.2f, -7.5f), 8f, -2f);
        Prompt(c, "Q / E   -   turn the world", new Vector3(2f, 2.6f, -7f), 4.5f, 2f);
        Prompt(c, "sealed from the front...", new Vector3(4f, 18f, -7.5f), 4f, -2f);
        Prompt(c, "red = deprecated.\ndash goes right through.", new Vector3(-0.5f, 23f, -7.5f), 4f, 2f);
        Prompt(c, "crystals give your dash back", new Vector3(10.5f, 27.5f, 5f), 4f, -2f);
        Prompt(c, "fenced off. how rude.", new Vector3(-0.5f, 31.5f, -7.5f), 4f, 2f);

        Finish(c, ScenePath, 4f, 37f, 6.8f);
        Debug.Log("[Mochini] Level 1 (turnul de vara) construit: " + ScenePath + "   |   Q/E rotire, S vorbesti cu standul.");
    }

    static bool EditorSceneManager_SaveIfUserWants() => UnityEditor.SceneManagement.EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo();
}
