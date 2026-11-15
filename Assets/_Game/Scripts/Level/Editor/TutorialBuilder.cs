using UnityEditor;
using UnityEngine;
using static FezLevelKit;

/// <summary>
/// Meniu: Mochini Bouncelini > Build Tutorial. Scena "Tutorial" = BUILD v0.9 (beta), tema PRIMAVARA.
///
/// Facut cu acelasi kit ca nivelurile (FezLevelKit): o singura camera rotativa (32 x 32, lata, cu loc de respirat intre lectii;
/// camera urmareste si pe orizontala), blocuri pixelate cu iarba, ciresi infloriti, petale care cad, standul QA.
/// Proiectat si verificat pe regula Fez cu fizica reala a lui mochi (simulatorul din scratchpad: drumul exista,
/// Ledge cere W, iesirea cere rotire):
///
///   Meadow (start, x 0..7)  A / D, standul QA     -> Path cu Step_1, Step_2 (x 8..15)  SPACE (hop, +1 rand)
///   groapa (x 16..17, fara podea)                 -> cazi = reapari la felinar
///   Island (x 18..20) -> groapa -> Ledge (x 23..26, +2 randuri) -> doar cu W; peretele Ledge-ului e bun de wall bounce
///   pe Ledge: un stalp de 5 blocuri               -> din fata e un zid; din lateral (Q sau E) e o scara -> iesirea, sus
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
            S("Meadow", 0, 7, -6, 2, 0, 0, "Stone"),
            S("Path", 8, 15, -6, 0, 0, 0, "Stone"),
            S("Step_1", 10, 11, -4, -3, 1, 1, "Wood"),
            S("Step_2", 13, 15, -4, -3, 1, 2, "Wood"),
            // groapa: coloanele 16..17, fara nimic dedesubt
            S("Island", 18, 20, -4, -3, 2, 2, "PlankHung"),
            // groapa: coloanele 21..22 (o lansare cu W + D o trece; cu hop nu ajungi, e cu 2 randuri mai sus)
            S("Ledge", 23, 26, -4, -3, 0, 4, "Brick"),
        };
        // stalpul / scara: aceeasi coloana (i 26, marginea din dreapta a Ledge-ului: pe el raman libere 23-25),
        // fiecare bloc cu un rand mai sus si doua celule mai in spate (din lateral: trepte cu gol intre ele, ca in nivelul 1)
        for (int m = 1; m <= 5; m++)
            p.Add(S("Stair_" + m, 26, 26, -3 + 2 * m, -3 + 2 * m, 4 + m, 4 + m, "Wood"));
        return p.ToArray();
    }

    [MenuItem("Mochini Bouncelini/Build Tutorial")]
    public static void Build()
    {
        if (!UnityEditor.SceneManagement.EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
        var t = Spring();
        var c = Begin(t, 0, -16, 32, Pieces(), new Vector3(1.5f, 1.55f, -3.5f), -6f);

        // ---- decor: casuta in spatele pajistii, ciresi infloriti (doar in spatele drumului), felinare, iedera
        Color plaster = new Color(0.97f, 0.92f, 0.84f), roof = new Color(0.86f, 0.42f, 0.46f), frame = new Color(0.55f, 0.36f, 0.24f), glass = new Color(0.36f, 0.46f, 0.70f);
        DecorBox(c, "CottageWall", new Vector3(0.4f, 1f, 0.6f), new Vector3(2.8f, 3f, 2.3f), plaster, Tex.Stone);
        var roofGo = Cube(c, "CottageRoof", new Vector3(1.6f, 3.0f, 1.45f), new Vector3(1.7f, 1.7f, 1.6f), roof);   // jumatatea de jos ramane in pereti
        roofGo.transform.rotation = Quaternion.Euler(0f, 0f, 45f);
        Cube(c, "CottageDoor", new Vector3(2.0f, 1.6f, 0.57f), new Vector3(0.55f, 1.2f, 0.06f), frame);
        Window(c, new Vector3(1.0f, 2.15f, 0.58f), Vector3.back, frame, glass, 0.5f, 0.5f);
        Vine(c, new Vector3(0.5f, 2.95f, 0.55f), 4, t.capHang);

        Color trunk = new Color(0.42f, 0.30f, 0.26f);
        Color[] blossom = { new Color(1f, 0.74f, 0.84f), new Color(0.98f, 0.62f, 0.76f), new Color(1f, 0.88f, 0.92f) };
        Tree(c, new Vector3(4.6f, 1f, 2.2f), 2.6f, trunk, blossom);
        Tree(c, new Vector3(8.6f, 1f, -0.6f), 2.1f, trunk, blossom);
        Tree(c, new Vector3(12.4f, 1f, 0.2f), 2.8f, trunk, blossom);
        Lantern(c, new Vector3(7.2f, 1f, -2.0f), new Color(1f, 0.85f, 0.55f));
        Lantern(c, new Vector3(20.6f, 3f, -3.3f), new Color(1f, 0.85f, 0.55f));

        // Ledge-ul e un turnulet de caramida: ferestre si iedera pe fata
        Window(c, new Vector3(24.0f, 2.4f, -4.01f), Vector3.back, frame, glass, 0.7f, 0.9f);
        Window(c, new Vector3(25.8f, 2.4f, -4.01f), Vector3.back, frame, glass, 0.7f, 0.9f);
        Vine(c, new Vector3(23.2f, 5f, -4.06f), 5, t.capHang);
        Vine(c, new Vector3(26.8f, 5f, -4.06f), 3, t.capHang);

        // ---- standul (QA te intampina, dupa cativa pasi)
        Stand(c, new Vector3(6.5f, 1f, 0.5f), "Intern_Skibidi", new[]
        {
            "yo. new player detected. welcome to the BETA, no cap.",
            "i'm the intern. i test stuff. today YOU are the stuff. skibidi.",
            "the exit is at the top. bounce up there and secure the bag.",
            "W charges a launch. hold it, let go, ascend. literally.",
            "and if something looks impossible... it's giving 'turn the world'. Q and E. trust the process."
        }, "bounce to the top (no cap)", new Color(0.62f, 0.76f, 0.95f), "propeller,headset");

        // ---- iesirea: pe fata de sus a ultimei trepte (exista doar privita din lateral)
        Vector3 exitPoint = Top(26, 9, 7);
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
        Checkpoint(c, Top(15, 2, -4));
        Checkpoint(c, Top(18, 2, -4));
        Checkpoint(c, Top(23, 4, -4));

        // ---- texte (in fata drumului, fiecare deasupra lectiei lui)
        Prompt(c, "~ how to bounce ~", new Vector3(4.2f, 6.9f, -7.5f), 7f, -2f);
        Prompt(c, "A / D   -   move", new Vector3(2.4f, 4.3f, -7.5f), 4.5f, 2f);
        Prompt(c, "SPACE   -   hop", new Vector3(12.2f, 5.2f, -7.5f), 4.5f, -2f);
        Prompt(c, "fell?  that's fine.\nyou come right back.", new Vector3(16.9f, 0.7f, -7.5f), 3.8f, 2f);
        Prompt(c, "hold W   -   charge\nlet go   -   launch\nsteer with A / D", new Vector3(20.4f, 7.6f, -7.5f), 4.2f, -2f);
        Prompt(c, "launch into a wall\n=   wall bounce", new Vector3(21.9f, 1.2f, -7.5f), 3.4f, 3f);
        Prompt(c, "a wall from the front...\nQ / E   -   turn the world", new Vector3(22.4f, 10.6f, -7.5f), 4.2f, 2f);
        Prompt(c, "the world awaits", exitPoint + new Vector3(0f, 2.3f, 0f), 4.5f, -2f);

        // camera urmareste pe orizontala (tutorialul e lat); din lateral se centreaza pe camera rotativa, ca in niveluri
        Finish(c, ScenePath, 4f, 12f, 6.8f, 10f, 22f);
        Debug.Log("[Mochini] Tutorial (BUILD v0.9, primavara) construit: " + ScenePath +
                  "   |   A/D, SPACE hop, W tinut = charge, Q/E rotire, S vorbesti cu standul.");
    }
}
