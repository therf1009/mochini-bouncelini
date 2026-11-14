using System.Collections.Generic;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>
/// Meniu: NERFED > Build Tutorial. Scena "Tutorial", in doua parti:
///
/// 1. Coridorul (2D, stil schita): miscare -> hop -> groapa (cazi, reapari) -> charge launch -> wall bounce.
///    Reperele vin din MochiController: hop ~1.9 sus (treptele de 1.5), charge ~5 sus (marginea de 3.5).
///
/// 2. Camera rotativa (Q / E), la capatul coridorului, cu podeaua in continuarea marginii (sus la 3.5):
///    din fata, un stalp de 6 blocuri te blocheaza, iar iesirea e sus, pe varful lui.
///    Cele 6 blocuri sunt la adancimi diferite: rotit spre dreapta (E), stalpul devine o scara
///    cu trepte de cate 1, pe care urci cu hop pana la iesire.
///      unghiul 0 (fata):   stalp in coloana x 61..62, inalt de 6
///      unghiul 1 (E):      scara de la u 60 la u 66, urca spre dreapta -> iesirea in varf
///      unghiul 2 (spate):  stalp
///      unghiul 3 (Q):      scara in oglinda, dar baza e prinsa sub trepte: rotesti din nou
/// </summary>
public static class TutorialBuilder
{
    const string ScenePath = NerfedEditorUtil.ScenesDir + "/Tutorial.unity";

    static readonly Color Paper = new Color(0.94f, 0.92f, 0.86f);
    static readonly Color Ink = new Color(0.16f, 0.15f, 0.17f);

    // camera rotativa: 8 x 8 celule, de la x 58 si z -4, podeaua (randul 0) intre y 2.5 si 3.5
    const int RoomX = 58, RoomZ = -4, RoomSize = 8;
    const float RoomBaseY = 2.5f;
    const int PillarX = 61;
    const int StairCount = 6;

    [MenuItem("NERFED/Build Tutorial")]
    public static void Build()
    {
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        Material ink = NerfedEditorUtil.Mat("M_Ink", Ink, unlit: true);
        Material mochiMat = NerfedEditorUtil.Mat("M_Mochi", new Color(0.98f, 0.62f, 0.25f));
        Material exitMat = NerfedEditorUtil.Mat("M_ExitGlow", new Color(0.35f, 0.85f, 0.95f), unlit: true);
        Material roomFloor = NerfedEditorUtil.Mat("M_RoomFloor", new Color(0.32f, 0.30f, 0.35f));
        Material stairMat = NerfedEditorUtil.Mat("M_StairBlock", new Color(0.55f, 0.68f, 0.95f));

        // lumina fixa in lume: cand camera se roteste, fetele cuburilor se lumineaza diferit -> se vede 3D-ul
        var light = new GameObject("Directional Light").AddComponent<Light>();
        light.type = LightType.Directional;
        light.intensity = 1.1f;
        light.transform.rotation = Quaternion.Euler(50f, -30f, 0f);

        // =====================================================================
        // 1. CORIDORUL (se ascunde cand lumea e rotita, altfel ar acoperi camera)
        // =====================================================================
        Transform corridor = new GameObject("Corridor").transform;

        Block(corridor, ink, "Wall_Start", new Vector2(-2.5f, 5f), new Vector2(1f, 12f));
        Block(corridor, ink, "Floor_A", new Vector2(9f, -0.5f), new Vector2(22f, 1f));      // x -2..20
        Block(corridor, ink, "Step_1", new Vector2(12f, 0.75f), new Vector2(2f, 1.5f));
        Block(corridor, ink, "Step_2", new Vector2(15f, 1.5f), new Vector2(2f, 3f));
        // groapa x 20..22.5
        Block(corridor, ink, "Floor_B", new Vector2(26.25f, -0.5f), new Vector2(7.5f, 1f)); // x 22.5..30
        Block(corridor, ink, "Ledge", new Vector2(44f, 1.25f), new Vector2(28f, 4.5f));     // x 30..58, sus la 3.5
        // doi pereti suspendati (treci pe sub ei): loc de exersat wall bounce-ul
        Block(corridor, ink, "Shaft_L", new Vector2(41.5f, 9f), new Vector2(1f, 7f));
        Block(corridor, ink, "Shaft_R", new Vector2(45.5f, 9f), new Vector2(1f, 7f));

        // textele stau IN FATA geometriei (z negativ), ca sa nu fie taiate de blocuri
        Prompt(corridor, "~ how to bounce ~", new Vector3(3f, 6.5f, -1f), -2f, 9f);
        Prompt(corridor, "A / D   -   move", new Vector3(3f, 3.5f, -1f), 3f);
        Prompt(corridor, "SPACE   -   hop", new Vector3(10.5f, 4.8f, -1f), -2.5f);
        Prompt(corridor, "fell?  that's fine.\nyou come right back.", new Vector3(21.2f, 5f, -1f), 2f);
        Prompt(corridor, "hold SPACE   -   charge\nlet go   -   launch", new Vector3(26f, 5.5f, -1f), -3f);
        Prompt(corridor, "launch into a wall\n-   wall bounce   ->", new Vector3(36.5f, 6.5f, -1f), 2f);

        Checkpoint(corridor, new Vector2(18f, 0f));
        Checkpoint(corridor, new Vector2(23.5f, 0f));
        Checkpoint(corridor, new Vector2(31.5f, 3.5f));
        Checkpoint(corridor, new Vector2(RoomX + 0.5f, 3.5f));   // intrarea in camera rotativa

        // =====================================================================
        // 2. CAMERA ROTATIVA
        // =====================================================================
        var roomGo = new GameObject("RotationRoom");   // ramane la origine: fizica e in planul XY al lumii
        var room = roomGo.AddComponent<FezRoom>();
        room.originX = RoomX;
        room.originZ = RoomZ;
        room.size = RoomSize;
        room.baseY = RoomBaseY;
        room.solidCells = RoomCells();

        Transform visuals = new GameObject("RoomVisuals").transform;
        visuals.SetParent(roomGo.transform, false);

        // podeaua: un singur cub vizual de 8 x 1 x 8 (colliderele vin din celule, nu de aici)
        NerfedEditorUtil.Primitive(PrimitiveType.Cube, "Floor", visuals,
            new Vector3(RoomX + RoomSize * 0.5f, RoomBaseY + 0.5f, RoomZ + RoomSize * 0.5f),
            new Vector3(RoomSize, 1f, RoomSize), roomFloor, keepCollider: false);

        // treptele: aceeasi coloana pe X, fiecare la alta adancime
        for (int m = 1; m <= StairCount; m++)
        {
            Vector3Int c = StairCell(m);
            NerfedEditorUtil.Primitive(PrimitiveType.Cube, "Stair_" + m, visuals,
                new Vector3(c.x + 0.5f, RoomBaseY + c.y + 0.5f, c.z + 0.5f), Vector3.one, stairMat, keepCollider: false);
        }

        // iesirea: pe fata de sus a ultimei trepte
        Vector3Int top = StairCell(StairCount);
        Vector3 exitPoint = new Vector3(top.x + 0.5f, RoomBaseY + top.y + 1f, top.z + 0.5f);
        var anchor = new GameObject("ExitAnchor").transform;
        anchor.SetParent(visuals, false);
        anchor.localPosition = exitPoint;
        NerfedEditorUtil.Primitive(PrimitiveType.Cube, "ExitGlow", visuals,
            exitPoint + Vector3.up * 0.75f, new Vector3(0.7f, 1.5f, 0.7f), exitMat, keepCollider: false);

        var exitGo = new GameObject("TutorialExit");
        var exit = exitGo.AddComponent<SceneExit2D>();
        exit.targetScene = "OpenWorld";
        exit.radius = 1f;
        exit.room = room;
        exit.worldAnchor = anchor;

        Prompt(visuals, "Q / E   -   rotate the world\nfind the side where it's stairs",
            new Vector3(RoomX + 1.5f, 6.8f, RoomZ - 0.5f), 2f, 5.5f, faceCamera: true);
        Prompt(visuals, "the world awaits", exitPoint + Vector3.up * 2.2f, -2f, 5f, faceCamera: true);

        // =====================================================================
        // MOCHI, ROTATIA, CAMERA
        // =====================================================================
        GameObject player = CreateMochi(mochiMat, new Vector2(0.5f, 0.6f));
        var body = player.GetComponent<Rigidbody2D>();

        var rotator = new GameObject("FezRotator").AddComponent<FezRotator>();
        rotator.room = room;
        rotator.player = body;
        rotator.hideWhenRotated = new[] { corridor };

        var view = player.AddComponent<FezMochiView>();
        view.room = room;
        view.rotator = rotator;
        view.visual = player.GetComponent<MochiController>().visual;
        view.depth = -(RoomSize * 0.5f + 1f);   // mereu in fata camerei rotative

        var rig = new GameObject("CameraRig");
        var fezCam = rig.AddComponent<FezCamera>();
        fezCam.target = body;
        fezCam.room = room;
        fezCam.rotator = rotator;
        fezCam.minU = 6f;
        fezCam.maxU = room.CenterU;
        fezCam.minV = 3f;
        fezCam.maxV = 12f;

        var shake = new GameObject("ShakeRig");
        shake.transform.SetParent(rig.transform, false);
        shake.AddComponent<CameraShake>();

        var camGo = new GameObject("Main Camera");
        camGo.tag = "MainCamera";
        camGo.transform.SetParent(shake.transform, false);
        camGo.transform.localPosition = new Vector3(0f, 0f, -30f);
        var cam = camGo.AddComponent<Camera>();
        cam.orthographic = true;
        cam.orthographicSize = 6.5f;
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = Paper;
        cam.nearClipPlane = 0.1f;
        cam.farClipPlane = 120f;
        camGo.AddComponent<AudioListener>();

        fezCam.Snap();

        System.IO.Directory.CreateDirectory(NerfedEditorUtil.ScenesDir);
        EditorSceneManager.SaveScene(scene, ScenePath);
        NerfedEditorUtil.AddSceneToBuild(ScenePath);
        AssetDatabase.SaveAssets();

        Debug.Log("[NERFED] Tutorial construit: " + ScenePath +
                  "   |   A/D, SPACE scurt = hop, SPACE tinut = charge, Q/E = rotire (doar in camera de la capat).");
    }

    /// <summary>Treapta m (1..6): mereu in coloana PillarX, la inaltimea m, la adancimea k = RoomZ + 6 - m.
    /// Din fata se suprapun intr-un stalp; din lateral (E) se intind intr-o scara spre dreapta.</summary>
    static Vector3Int StairCell(int m) => new Vector3Int(PillarX, m, RoomZ + StairCount - m);

    static List<Vector3Int> RoomCells()
    {
        var cells = new List<Vector3Int>();
        for (int i = RoomX; i < RoomX + RoomSize; i++)
            for (int k = RoomZ; k < RoomZ + RoomSize; k++)
                cells.Add(new Vector3Int(i, 0, k));     // podeaua
        for (int m = 1; m <= StairCount; m++)
            cells.Add(StairCell(m));
        return cells;
    }

    static void Block(Transform parent, Material mat, string name, Vector2 center, Vector2 size)
    {
        var go = NerfedEditorUtil.Primitive(PrimitiveType.Cube, name, parent,
            new Vector3(center.x, center.y, 0f), new Vector3(size.x, size.y, 1f), mat, keepCollider: false);
        go.AddComponent<BoxCollider2D>();
    }

    static void Prompt(Transform parent, string text, Vector3 pos, float tilt, float size = 6f, bool faceCamera = false)
    {
        var go = new GameObject("Prompt");
        go.transform.SetParent(parent, false);
        var t = go.AddComponent<TextMeshPro>();
        t.text = text;
        t.fontSize = size;
        t.fontStyle = FontStyles.Italic;
        t.color = Ink;
        t.alignment = TextAlignmentOptions.Center;
        t.rectTransform.sizeDelta = new Vector2(14f, 4f);
        go.transform.localPosition = pos;
        go.transform.localRotation = Quaternion.Euler(0f, 0f, tilt);
        go.AddComponent<PromptWobble>().faceCamera = faceCamera;
    }

    static void Checkpoint(Transform parent, Vector2 pos)
    {
        var go = new GameObject("Checkpoint");
        go.transform.SetParent(parent, false);
        go.transform.localPosition = pos;
        go.AddComponent<Checkpoint2D>();
    }

    static GameObject CreateMochi(Material mat, Vector2 pos)
    {
        var go = new GameObject("Mochi");
        go.tag = "Player";
        go.transform.position = pos;

        var rb = go.AddComponent<Rigidbody2D>();
        rb.freezeRotation = true;

        var cc = go.AddComponent<CircleCollider2D>();
        cc.radius = 0.5f;
        cc.sharedMaterial = NerfedEditorUtil.MochiPhysics();

        var visual = NerfedEditorUtil.Primitive(PrimitiveType.Sphere, "Visual", go.transform,
            Vector3.zero, Vector3.one, mat, keepCollider: false);

        var ctrl = go.AddComponent<MochiController>();
        ctrl.visual = visual.transform;

        go.AddComponent<Respawn2D>().killY = -6f;
        return go;
    }
}
