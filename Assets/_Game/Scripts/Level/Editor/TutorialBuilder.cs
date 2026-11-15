using UnityEditor;
using UnityEngine;
using static FezLevelKit;

/// <summary>
/// Meniu: Mochini Bouncelini > Build Tutorial. Scena "Tutorial" = BUILD v0.9 (beta), tema PRIMAVARA.
///
/// Facut cu acelasi kit ca nivelurile (FezLevelKit): o singura camera rotativa (18 x 18), blocuri pixelate cu iarba,
/// cires inflorit, petale care cad, standul QA. Proiectat si verificat pe regula Fez cu fizica reala a lui mochi
/// (simulatorul din scratchpad: drumul exista, Ledge cere W, iesirea cere rotire):
///
///   Meadow (start)  A / D               -> Step_1, Step_2  SPACE (hop, +1 rand)
///   groapa (2 coloane, fara podea)      -> cazi = reapari la felinar
///   Island -> groapa -> Ledge (+2 r.)   -> doar cu W (charge); peretele Ledge-ului e bun de wall bounce
///   pe Ledge: un stalp de 5 blocuri     -> din fata e un zid; din lateral (Q sau E) e o scara -> iesirea, sus
/// </summary>
public static class TutorialBuilder
{
    const string ScenePath = NerfedEditorUtil.ScenesDir + "/Tutorial.unity";

    public static Theme Spring()
    {
        var t = new Theme
        {
            prefix = "TU",
            skyTop = new Color(0.52f, 0.72f, 0.98f), skyMid = new Color(0.80f, 0.84f, 0.98f), skyLow = new Color(1f, 0.88f, 0.86f),
            haze = new Color(0.86f, 0.88f, 0.98f), hazeStrength = 0.5f,
            ambient = new Color(0.60f, 0.60f, 0.66f), sun = new Color(1f, 0.97f, 0.92f), sunIntensity = 1.2f,
            squares = new Color(1f, 1f, 1f, 0.2f),
            cap = Cap.Grass,
            capA = new Color(0.62f, 0.86f, 0.42f), capB = new Color(0.52f, 0.78f, 0.36f), capHang = new Color(0.44f, 0.70f, 0.32f),
            flowers = new[] { new Color(1f, 0.70f, 0.82f), new Color(1f, 1f, 1f), new Color(1f, 0.92f, 0.45f), new Color(0.75f, 0.70f, 1f) },
            bodyColor = new Color(0.86f, 0.80f, 0.72f), bodyTex = Tex.Stone,
            particles = Particles.Leaves,   // petale de cires
            particleA = new Color(1f, 0.74f, 0.84f, 1f), particleB = new Color(1f, 0.90f, 0.94f, 1f),
        };
        t.skins["Stone"] = new Skin(new Color(0.86f, 0.80f, 0.72f), Tex.Stone);
        t.skins["Wood"] = new Skin(new Color(0.76f, 0.54f, 0.36f), Tex.Wood);
        t.skins["Plank"] = new Skin(new Color(0.86f, 0.66f, 0.44f), Tex.Plank);
        t.skins["PlankHung"] = new Skin(new Color(0.86f, 0.66f, 0.44f), Tex.Plank, hung: true);
        t.skins["Brick"] = new Skin(new Color(0.84f, 0.56f, 0.52f), Tex.Brick);
        return t;
    }

    // ---- piesele, exact cum au trecut verificarile din simulator (tut.py)
    static Piece[] Pieces()
    {
        var p = new System.Collections.Generic.List<Piece>
        {
            S("Meadow", 0, 4, -4, 1, 0, 0, "Stone"),
            S("Step_1", 5, 6, -4, -3, 0, 1, "Wood"),
            S("Step_2", 7, 8, -4, -3, 0, 2, "Wood"),
            // groapa: coloanele 9..10, fara nimic dedesubt
            S("Island", 11, 12, -4, -3, 2, 2, "PlankHung"),
            // groapa: coloanele 13..14 (o lansare cu W + D o trece; cu hop nu ajungi, e cu 2 randuri mai sus)
            S("Ledge", 15, 17, -4, -3, 0, 4, "Brick"),
        };
        // stalpul / scara: aceeasi coloana (i 17, marginea din dreapta: pe Ledge raman libere coloanele 15-16), fiecare bloc cu un rand mai sus si doua celule mai in spate (din lateral: trepte cu gol intre ele, ca in nivelul 1)
        for (int m = 1; m <= 5; m++)
            p.Add(S("Stair_" + m, 17, 17, -3 + 2 * m, -3 + 2 * m, 4 + m, 4 + m, "Wood"));
        return p.ToArray();
    }

    [MenuItem("Mochini Bouncelini/Build Tutorial")]
    public static void Build()
    {
        if (!UnityEditor.SceneManagement.EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
        var t = Spring();
        var c = Begin(t, 0, -9, 18, Pieces(), new Vector3(1.5f, 1.55f, -3.5f), -6f);

        // ---- decor: casuta din spatele pajistii, ciresi infloriti, felinare, iedera
        Color plaster = new Color(0.97f, 0.92f, 0.84f), roof = new Color(0.86f, 0.42f, 0.46f), frame = new Color(0.55f, 0.36f, 0.24f), glass = new Color(0.36f, 0.46f, 0.70f);
        DecorBox(c, "CottageWall", new Vector3(0.2f, 1f, 0.3f), new Vector3(2.4f, 2.9f, 1.9f), plaster, Tex.Stone);
        var roofGo = Cube(c, "CottageRoof", new Vector3(1.3f, 2.9f, 1.1f), new Vector3(1.55f, 1.55f, 1.5f), roof);   // jumatatea de jos ramane in pereti
        roofGo.transform.rotation = Quaternion.Euler(0f, 0f, 45f);
        Cube(c, "CottageDoor", new Vector3(1.6f, 1.6f, 0.27f), new Vector3(0.55f, 1.2f, 0.06f), frame);
        Window(c, new Vector3(0.8f, 2.1f, 0.28f), Vector3.back, frame, glass, 0.5f, 0.5f);
        Vine(c, new Vector3(0.3f, 2.85f, 0.25f), 4, t.capHang);

        Color trunk = new Color(0.42f, 0.30f, 0.26f);
        Color[] blossom = { new Color(1f, 0.74f, 0.84f), new Color(0.98f, 0.62f, 0.76f), new Color(1f, 0.88f, 0.92f) };
        Tree(c, new Vector3(4.2f, 1f, 1.3f), 2.5f, trunk, blossom);
        Tree(c, new Vector3(-0.2f, 1f, -3.9f), 1.9f, trunk, blossom);
        Lantern(c, new Vector3(4.6f, 1f, -2.4f), new Color(1f, 0.85f, 0.55f));
        Lantern(c, new Vector3(12.6f, 3f, -3.3f), new Color(1f, 0.85f, 0.55f));

        // Ledge-ul e un turnulet de caramida: ferestre si iedera pe fata
        Window(c, new Vector3(16.5f, 2.4f, -4.01f), Vector3.back, frame, glass, 0.7f, 0.9f);
        Vine(c, new Vector3(15.2f, 5f, -4.06f), 5, t.capHang);
        Vine(c, new Vector3(17.8f, 5f, -4.06f), 3, t.capHang);

        // ---- standul (QA te intampina)
        Stand(c, new Vector3(3.5f, 1f, 0.5f), "QA_Tester_00", new[]
        {
            "oh hey. you're new. welcome to the BETA.",
            "i'm QA. i test things. today, you're the thing.",
            "the exit is at the top. bounce your way up there.",
            "and if something looks impossible... turn the world. Q and E."
        }, "bounce to the top", new Color(0.62f, 0.76f, 0.95f));

        // ---- iesirea: pe fata de sus a ultimei trepte (exista doar privita din lateral)
        Vector3 exitPoint = Top(17, 9, 7);
        var anchor = new GameObject("ExitAnchor").transform;
        anchor.SetParent(c.gameplay, false);
        anchor.position = exitPoint;
        Cube(c, "ExitGlow", exitPoint + Vector3.up * 0.75f, new Vector3(0.7f, 1.5f, 0.7f), new Color(0.35f, 0.85f, 0.95f), true)
            .GetComponent<MeshRenderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        var exit = new GameObject("TutorialExit").AddComponent<SceneExit2D>();
        exit.transform.SetParent(c.gameplay, false);
        exit.targetScene = "OpenWorld";
        exit.radius = 1f;
        exit.room = c.room;
        exit.worldAnchor = anchor;

        // ---- checkpoint-uri (felinare)
        Checkpoint(c, Top(8, 2, -4));
        Checkpoint(c, Top(11, 2, -4));
        Checkpoint(c, Top(15, 4, -4));

        // ---- texte (in fata, ca sa nu fie acoperite)
        Prompt(c, "~ how to bounce ~", new Vector3(3.2f, 6.8f, -6.5f), 7f, -2f);
        Prompt(c, "A / D   -   move", new Vector3(0.6f, 5.3f, -6.5f), 4.5f, 2f);
        Prompt(c, "SPACE   -   hop", new Vector3(6.8f, 4.6f, -6.5f), 4.5f, -2f);
        Prompt(c, "fell?  that's fine.\nyou come right back.", new Vector3(10f, 0.6f, -6.5f), 3.8f, 2f);
        Prompt(c, "hold W   -   charge\nlet go   -   launch\nsteer with A / D", new Vector3(12.6f, 7.3f, -6.5f), 4.2f, -2f);
        Prompt(c, "launch into a wall\n=   wall bounce", new Vector3(13.9f, 0.9f, -6.5f), 3.4f, 3f);
        Prompt(c, "a wall from the front...\nQ / E   -   turn the world", new Vector3(12.4f, 10.4f, -6.5f), 4.2f, 2f);
        Prompt(c, "the world awaits", exitPoint + new Vector3(0f, 2.3f, 0f), 4.5f, -2f);

        Finish(c, ScenePath, 4f, 12f, 6.8f);
        Debug.Log("[Mochini] Tutorial (BUILD v0.9, primavara) construit: " + ScenePath +
                  "   |   A/D, SPACE hop, W tinut = charge, Q/E rotire, S vorbesti cu standul.");
    }
}
