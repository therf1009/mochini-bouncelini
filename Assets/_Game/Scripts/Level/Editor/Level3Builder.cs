using TMPro;
using UnityEditor;
using UnityEngine;
using static FezLevelKit;

/// <summary>
/// Meniu: Mochini Bouncelini > Levels > Build Level 3 (digital server). Scena "Level3" = BUILD v1.2, ultimul nivel, tema DIGITAL.
///
/// Turnul-server, intr-o camera rotativa (14 x 14). Doua abilitati noi:
///   REVERT 1 (chiar la inceput): WALL STICK - tii SPACE pe un perete: te lipesti; dai drumul: te lansezi in sus, departe.
///   REVERT 2 (inainte de arena): BOUNCE BACK, ca SCUT - tii F: patch-urile se intorc la cine le-a trimis.
/// Doua puturi intre pereti: unul se vede doar din fata (unghiul 0), celalalt doar din dreapta (unghiul 3).
/// Verificat pe regula Fez: fara wall stick nu urci in niciunul (nici cu dash + double jump), fara rotire nu ajungi sus.
/// Sus, pe acoperisul serverului: DIRECTOR.exe, robotelul. Rotirea se blocheaza (firewall), il invingi cu scutul,
/// apoi finalul tragic: lumea se sterge bloc cu bloc -> title screen.
/// </summary>
public static class Level3Builder
{
    const string ScenePath = NerfedEditorUtil.ScenesDir + "/Level3.unity";

    public static Theme Digital()
    {
        var t = new Theme
        {
            prefix = "L3",
            skyTop = new Color(0.06f, 0.05f, 0.16f), skyMid = new Color(0.16f, 0.10f, 0.32f), skyLow = new Color(0.30f, 0.14f, 0.42f),
            haze = new Color(0.12f, 0.10f, 0.28f), hazeStrength = 0.6f,
            ambient = new Color(0.36f, 0.38f, 0.52f), sun = new Color(0.75f, 0.85f, 1f), sunIntensity = 0.95f,
            squares = new Color(0.35f, 0.95f, 1f, 0.07f),
            cap = Cap.Neon, neon = new Color(0.30f, 0.95f, 1f),
            bodyColor = new Color(0.20f, 0.22f, 0.32f), bodyTex = Tex.Circuit,
            particles = Particles.Data,
            particleA = new Color(0.35f, 0.95f, 1f, 0.9f), particleB = new Color(1f, 0.35f, 0.85f, 0.9f),
            ink = new Color(0.80f, 0.95f, 1f),
        };
        t.skins["Floor"] = new Skin(new Color(0.26f, 0.28f, 0.40f), Tex.Block, emissiveEdges: true);
        t.skins["Panel"] = new Skin(new Color(0.24f, 0.26f, 0.36f), Tex.Metal, emissiveEdges: true);
        t.skins["Rack"] = new Skin(new Color(0.18f, 0.20f, 0.30f), Tex.Circuit, emissiveEdges: true);
        t.skins["Gold"] = new Skin(new Color(0.98f, 0.78f, 0.32f), Tex.Block, noCap: true);
        return t;
    }

    static readonly Piece[] Pieces =
    {
        S("Plaza", 0, 9, -5, 4, 0, 0, "Floor"),
        S("WS_Shelf", 7, 9, -7, -6, 3, 3, "Gold"),
        S("Shaft1_Floor", 0, 3, -7, -6, 3, 3, "Panel"),
        S("Shaft1_WallL", -1, -1, -7, -6, 5, 15, "Rack"),
        S("Shaft1_WallR", 4, 4, -7, -6, 5, 15, "Rack"),
        S("Ledge_1", 5, 8, -7, -6, 15, 15, "Panel"),
        S("Shaft2_Floor", 10, 11, -2, 1, 16, 16, "Panel"),
        S("Shaft2_WallL", 10, 11, -3, -3, 18, 27, "Rack"),
        S("Shaft2_WallR", 10, 11, 2, 2, 18, 27, "Rack"),
        S("Ledge_2", 12, 13, 3, 5, 28, 28, "Gold"),   // iese de sub arena, in dreapta
        S("Arena", -2, 11, -7, 6, 30, 30, "Floor"),   // arena mare: 14 x 14
        S("Arena_WallL", -4, -3, -7, 6, 33, 44, "Rack"),
        S("Arena_WallR", 12, 13, -7, 6, 33, 44, "Rack"),
    };

    [MenuItem("Mochini Bouncelini/Levels/Build Level 3 (digital server)")]
    public static void Build()
    {
        if (!UnityEditor.SceneManagement.EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
        var t = Digital();
        // camera rotativa de 18 (acelasi centru ca turnul de 14), ca sa incapa arena mare de sus
        var c = Begin(t, -4, -9, 18, Pieces, new Vector3(1.5f, 1.55f, -4.5f), -6f);
        c.player.AddComponent<BounceBack>();   // scutul, blocat pana la al doilea REVERT

        // ---- serverul (decor): corp cu circuite, dungi de neon pe muchii, beculete, cabluri
        DecorBox(c, "ServerCore", new Vector3(3f, 1f, -2f), new Vector3(7f, 30f, 2f), t.bodyColor, Tex.Circuit);
        Color neon = t.neon, pink = new Color(1f, 0.35f, 0.85f);
        foreach (var x in new[] { 2.95f, 7.05f })
            foreach (var z in new[] { -2.05f, 2.05f })
                Cube(c, "NeonEdge", new Vector3(x, 15.5f, z), new Vector3(0.08f, 29f, 0.08f), neon, true);
        var rng = new System.Random(3);
        for (int n = 0; n < 60; n++)
        {
            float y = 2f + (float)rng.NextDouble() * 27f;
            int face = rng.Next(4);
            float a = 3.3f + (float)rng.NextDouble() * 3.4f;
            Vector3 p = face == 0 ? new Vector3(a, y, -2.03f) : face == 1 ? new Vector3(a, y, 2.03f)
                      : face == 2 ? new Vector3(2.97f, y, a - 5f) : new Vector3(7.03f, y, a - 5f);
            Color led = rng.NextDouble() < 0.7 ? neon : (rng.NextDouble() < 0.5 ? pink : new Color(0.4f, 1f, 0.5f));
            Cube(c, "Led", p, new Vector3(0.14f, 0.08f, 0.14f), led, true);
        }
        Color cable = new Color(0.10f, 0.10f, 0.16f);
        Pipe(c, cable, 0.08f, new Vector3(3.4f, 30f, -2.2f), new Vector3(3.4f, 22f, -2.2f), new Vector3(4.6f, 22f, -2.2f), new Vector3(4.6f, 16f, -2.2f));
        Pipe(c, cable, 0.1f, new Vector3(6.6f, 29f, -2.2f), new Vector3(6.6f, 9f, -2.2f));
        Pipe(c, new Color(0.2f, 0.12f, 0.3f), 0.09f, new Vector3(7.2f, 28f, 1.5f), new Vector3(7.2f, 12f, 1.5f), new Vector3(7.2f, 12f, -1.4f), new Vector3(7.2f, 4f, -1.4f));
        // cuburi de date care plutesc in jur (decor)
        for (int n = 0; n < 22; n++)
        {
            var p = new Vector3(-1.5f + (float)rng.NextDouble() * 13f, 2f + (float)rng.NextDouble() * 38f, -6.5f + (float)rng.NextDouble() * 13f);
            if (p.x > 2.5f && p.x < 7.5f && p.z > -2.5f && p.z < 2.5f) continue;
            Cube(c, "DataCube", p, Vector3.one * (0.15f + (float)rng.NextDouble() * 0.25f), rng.NextDouble() < 0.6 ? neon : pink, true);
        }

        // ---- standul (QA, ultima data)
        Stand(c, new Vector3(6.5f, 1f, -4.5f), "QA_Tester_04", new[]
        {
            "you made it to v1.2. the last build.",
            "the Director shipped itself as DIRECTOR.exe. it's up on the server roof, sealing everything.",
            "first REVERT is right here: WALL STICK. hold SPACE on a wall, let go to launch.",
            "the second one is the old Bounce Back - now a parry. click right before a patch hits you and it goes home.",
            "...if you take the Director down, nobody balances this world anymore. i don't know what happens then.",
            "go anyway."
        }, "grab the REVERT, climb the server", new Color(0.55f, 0.78f, 0.62f));

        Revert(c, new Vector3(8.5f, 4.6f, -6f), Ability.WallStick, "PATCH 1.2.0  -  REVERTED",
            new[] { "Wall stick restored (hold SPACE on a wall).", "Let go to launch." },
            "climb the shaft. wall to wall.", "WALL STICK", "hold SPACE on a wall", new Color(0.35f, 0.95f, 1f));
        Revert(c, new Vector3(11f, 29.6f, 4.5f), Ability.Third, "PATCH 1.2.1  -  REVERTED",
            new[] { "Bounce Back restored, as a parry (click).", "Patches return to sender." },
            "the roof. DIRECTOR.exe.", "BOUNCE BACK PARRY", "CLICK", new Color(1f, 0.35f, 0.85f));

        // ---- DIRECTOR.exe
        Material patchMat = NerfedEditorUtil.Mat("M_Patch", new Color(0.95f, 0.40f, 0.30f), unlit: true);
        Material reflectedMat = NerfedEditorUtil.Mat("M_PatchReflected", new Color(0.40f, 0.90f, 1.00f), unlit: true);
        Material hazardMat = NerfedEditorUtil.Mat("M_DataWave", new Color(1f, 0.25f, 0.65f), unlit: true);
        var boss = Robot(c, new Vector3(5f, 38.5f, -1f));   // mai sus: arena are loc de manevra
        boss.projectileMat = patchMat;
        boss.reflectedMat = reflectedMat;
        boss.hazardMat = hazardMat;
        boss.activateY = 31f;
        boss.hitRadius = 1.4f;
        boss.projectileSpeed = 6f;
        boss.sweepWidth = 5f;

        var ending = new GameObject("EndingSequence").AddComponent<EndingSequence>();
        ending.boss = boss;
        ending.crumbleRoot = c.room.transform;   // tot turnul (blocuri + decor) dispare, cub cu cub

        // ---- checkpoint-uri
        Checkpoint(c, Top(8, 3, -7));
        Checkpoint(c, Top(1, 3, -7));
        Checkpoint(c, Top(6, 15, -7));
        Checkpoint(c, Top(10, 16, -1));
        Checkpoint(c, Top(10, 28, 4));
        Checkpoint(c, Top(8, 30, -5));

        // ---- texte
        Prompt(c, "~ BUILD v1.2 ~", new Vector3(3f, 7.2f, -7.5f), 8f, -2f);
        Prompt(c, "hold SPACE on a wall,\nlet go to launch", new Vector3(1.5f, 9f, -8f), 3.8f, 2f);
        Prompt(c, "this shaft only exists\nfrom the side...", new Vector3(8f, 19f, -8f), 3.6f, -2f);
        Prompt(c, "CLICK  -  parry\n(right before it hits)", new Vector3(12.5f, 31.5f, 6.5f), 3.8f, 2f);

        Finish(c, ScenePath, 4f, 40f, 7.5f);
        Debug.Log("[Mochini] Level 3 (serverul digital) construit: " + ScenePath);
    }

    /// <summary>Robotelul: corp, cap cu ecran (ochi + gura), antena cu beculet, brate, propulsor.</summary>
    static DirectorBoss Robot(Ctx c, Vector3 pos)
    {
        Material shell = NerfedEditorUtil.Mat("M_RobotShell", new Color(0.78f, 0.80f, 0.86f));
        Material dark = NerfedEditorUtil.Mat("M_RobotDark", new Color(0.18f, 0.19f, 0.24f));
        Material screenMat = NerfedEditorUtil.Mat("M_RobotScreen", new Color(0.06f, 0.08f, 0.12f), unlit: true);
        Material eyeMat = NerfedEditorUtil.Mat("M_RobotEye", new Color(0.35f, 0.95f, 1f), unlit: true);
        Material glow = NerfedEditorUtil.Mat("M_RobotThruster", new Color(1f, 0.55f, 0.2f), unlit: true);
        Material red = NerfedEditorUtil.Mat("M_RobotLight", new Color(1f, 0.25f, 0.25f), unlit: true);

        var root = new GameObject("DIRECTOR.exe").transform;
        root.SetParent(c.gameplay, false);
        root.position = pos;
        var body = new GameObject("Body").transform; body.SetParent(root, false);

        NerfedEditorUtil.Primitive(PrimitiveType.Cube, "Torso", body, new Vector3(0f, -0.9f, 0f), new Vector3(1.5f, 1.2f, 1.2f), shell, keepCollider: false);
        NerfedEditorUtil.Primitive(PrimitiveType.Cube, "Belt", body, new Vector3(0f, -1.25f, 0f), new Vector3(1.6f, 0.2f, 1.3f), dark, keepCollider: false);
        NerfedEditorUtil.Primitive(PrimitiveType.Cube, "Head", body, new Vector3(0f, 0.55f, 0f), new Vector3(2.2f, 1.6f, 1.5f), shell, keepCollider: false);
        var screen = NerfedEditorUtil.Primitive(PrimitiveType.Cube, "Screen", body, new Vector3(0f, 0.55f, -0.76f), new Vector3(1.8f, 1.2f, 0.05f), screenMat, keepCollider: false);
        var eyeL = NerfedEditorUtil.Primitive(PrimitiveType.Cube, "EyeL", body, new Vector3(-0.42f, 0.7f, -0.8f), new Vector3(0.32f, 0.32f, 0.04f), eyeMat, keepCollider: false);
        var eyeR = NerfedEditorUtil.Primitive(PrimitiveType.Cube, "EyeR", body, new Vector3(0.42f, 0.7f, -0.8f), new Vector3(0.32f, 0.32f, 0.04f), eyeMat, keepCollider: false);
        NerfedEditorUtil.Primitive(PrimitiveType.Cube, "Mouth", body, new Vector3(0f, 0.25f, -0.8f), new Vector3(0.7f, 0.08f, 0.04f), eyeMat, keepCollider: false);
        NerfedEditorUtil.Primitive(PrimitiveType.Cube, "Antenna", body, new Vector3(0.5f, 1.65f, 0f), new Vector3(0.06f, 0.6f, 0.06f), dark, keepCollider: false);
        var bulb = NerfedEditorUtil.Primitive(PrimitiveType.Sphere, "AntennaLight", body, new Vector3(0.5f, 2f, 0f), Vector3.one * 0.22f, red, keepCollider: false);
        NerfedEditorUtil.Primitive(PrimitiveType.Cube, "EarL", body, new Vector3(-1.18f, 0.55f, 0f), new Vector3(0.16f, 0.6f, 0.6f), dark, keepCollider: false);
        NerfedEditorUtil.Primitive(PrimitiveType.Cube, "EarR", body, new Vector3(1.18f, 0.55f, 0f), new Vector3(0.16f, 0.6f, 0.6f), dark, keepCollider: false);

        Transform Arm(string name, float side)
        {
            var pivot = new GameObject(name).transform;
            pivot.SetParent(body, false);
            pivot.localPosition = new Vector3(side * 0.85f, -0.55f, 0f);
            NerfedEditorUtil.Primitive(PrimitiveType.Cube, "Upper", pivot, new Vector3(side * 0.3f, -0.3f, 0f), new Vector3(0.22f, 0.7f, 0.22f), dark, keepCollider: false);
            NerfedEditorUtil.Primitive(PrimitiveType.Cube, "Hand", pivot, new Vector3(side * 0.42f, -0.72f, 0f), new Vector3(0.36f, 0.26f, 0.36f), shell, keepCollider: false);
            return pivot;
        }
        var armL = Arm("ArmL", -1f);
        var armR = Arm("ArmR", 1f);
        var thruster = NerfedEditorUtil.Primitive(PrimitiveType.Cube, "Thruster", body, new Vector3(0f, -1.65f, 0f), new Vector3(0.6f, 0.35f, 0.6f), glow, keepCollider: false);

        foreach (var r in root.GetComponentsInChildren<MeshRenderer>())
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;

        var label = NerfedEditorUtil.WorldText(root, "Name", "DIRECTOR.exe", new Vector3(0f, -2.3f, -0.9f), 3f, c.theme.ink, FontStyles.Bold, new Vector2(4f, 0.6f));
        label.transform.parent.gameObject.AddComponent<PromptWobble>().faceCamera = true;

        var boss = root.gameObject.AddComponent<DirectorBoss>();
        boss.screen = screen.GetComponent<Renderer>();
        boss.eyes = new[] { eyeL.GetComponent<Renderer>(), eyeR.GetComponent<Renderer>() };
        boss.armL = armL; boss.armR = armR;
        boss.antennaLight = bulb.transform;
        boss.thruster = thruster.transform;
        boss.body = body;
        return boss;
    }
}
