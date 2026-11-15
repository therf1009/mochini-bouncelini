using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Tex = FezLevelKit.Tex;

/// <summary>
/// Meniu: Mochini Bouncelini > Build Open World. Scena "OpenWorld": insule plutitoare, in stilul nivelurilor
/// (blocuri pixelate, iarba care atarna, flori, copaci din cuburi, felinare, cer in gradient cu patrate),
/// dar ramane 3D. Fiecare insula are tema nivelului spre care duce portalul ei.
///
///   Insula A (start, VARA)       x -7..7      sus la y 0     -> portalul 1, casa (usa = tutorial)
///   groapa                       x 7..12.5    (5.5)          -> doar salt + dash (nivelul 1 da dash)
///   Insula B (TOAMNA)            x 12.5..24.5 sus la y 0     -> portalul 2
///   faleza                       +2.6 pe verticala           -> doar double jump (nivelul 2 il da)
///   Insula C (DIGITAL, platou)   x 24.5..36.5 sus la y 2.6   -> portalul 3, ultimul nivel
///   Insula garderobei            x -17..-8, legata de start cu un podet
///
/// Coliziunile sunt aceleasi cutii ca inainte (invizibile); tot restul e doar vizual.
/// Portalurile propriu-zise le pune Mochini Bouncelini > Add Portals To Open World, pe locurile PortalSpot_N.
/// </summary>
public static class OverworldBuilder
{
    public const string ScenePath = NerfedEditorUtil.ScenesDir + "/OpenWorld.unity";

    // culorile temelor, luate din nivelurile 1-3
    static readonly Color SkyTop = new Color(1f, 0.45f, 0.33f), SkyMid = new Color(1f, 0.64f, 0.42f), SkyLow = new Color(1f, 0.84f, 0.52f);
    static readonly Color Ink = new Color(0.16f, 0.15f, 0.17f);
    static readonly Color Neon = new Color(0.30f, 0.95f, 1f), Pink = new Color(1f, 0.35f, 0.85f);

    class Look
    {
        public string id;
        public Color capA, capB, capHang, band, body;
        public Tex bandTex = Tex.Stone, bodyTex = Tex.Brick;
        public Color[] flowers = { };
        public bool neon;
    }

    static readonly Look Summer = new Look
    {
        id = "Summer", capA = new Color(0.58f, 0.82f, 0.30f), capB = new Color(0.47f, 0.73f, 0.26f), capHang = new Color(0.40f, 0.64f, 0.22f),
        band = new Color(0.84f, 0.73f, 0.60f), body = new Color(0.74f, 0.38f, 0.33f), bodyTex = Tex.Brick,
        flowers = new[] { new Color(1f, 0.92f, 0.35f), new Color(1f, 0.55f, 0.70f), new Color(0.95f, 0.95f, 1f), new Color(0.65f, 0.55f, 1f) },
    };
    static readonly Look Meadow = new Look
    {
        id = "Meadow", capA = Summer.capA, capB = Summer.capB, capHang = Summer.capHang,
        band = new Color(0.84f, 0.73f, 0.60f), body = new Color(0.80f, 0.70f, 0.58f), bodyTex = Tex.Stone, flowers = Summer.flowers,
    };
    static readonly Look Autumn = new Look
    {
        id = "Autumn", capA = new Color(0.93f, 0.48f, 0.16f), capB = new Color(0.80f, 0.26f, 0.16f), capHang = new Color(0.96f, 0.72f, 0.22f),
        band = new Color(0.58f, 0.43f, 0.30f), body = new Color(0.52f, 0.36f, 0.25f), bodyTex = Tex.Wood,
        flowers = new[] { new Color(0.85f, 0.25f, 0.2f), new Color(0.98f, 0.85f, 0.4f) },
    };
    static readonly Look Digital = new Look
    {
        id = "Digital", capA = new Color(0.26f, 0.28f, 0.40f), capB = new Color(0.22f, 0.24f, 0.36f), capHang = Neon,
        band = new Color(0.24f, 0.26f, 0.36f), bandTex = Tex.Metal, body = new Color(0.20f, 0.22f, 0.32f), bodyTex = Tex.Circuit, neon = true,
    };

    static FezLevelKit.Ctx kit;          // ca sa folosim decorul din kitul nivelurilor (copaci, felinare, ferestre, liane)
    static System.Random rng;

    [MenuItem("Mochini Bouncelini/Build Open World")]
    public static void Build()
    {
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        FezLevelKit.EnsureFxMaterial();
        rng = new System.Random(2024);

        Material mochi = NerfedEditorUtil.Mat("M_Mochi", new Color(0.98f, 0.62f, 0.25f));
        Material eye = NerfedEditorUtil.Mat("M_Eye", new Color(0.10f, 0.10f, 0.12f), unlit: true);

        var light = new GameObject("Directional Light").AddComponent<Light>();
        light.type = LightType.Directional;
        light.intensity = 1.25f;
        light.color = new Color(1f, 0.93f, 0.80f);
        light.shadows = LightShadows.Soft;
        light.shadowStrength = 0.7f;
        light.transform.rotation = Quaternion.Euler(50f, -35f, 0f);
        RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
        RenderSettings.ambientLight = new Color(0.62f, 0.54f, 0.50f);
        RenderSettings.fog = false;

        Transform world = new GameObject("World").transform;
        Transform decor = new GameObject("Decor").transform;
        decor.SetParent(world, false);
        kit = new FezLevelKit.Ctx { theme = new FezLevelKit.Theme { prefix = "OW" }, decor = decor, rng = rng };

        // ---- insule (coliziunea = aceleasi cutii ca inainte)
        Island(world, Summer, "Island_A_Start", new Vector3(0f, 0f, 0f), new Vector3(14f, 3f, 14f));
        Island(world, Autumn, "Island_B", new Vector3(18.5f, 0f, 0f), new Vector3(12f, 3f, 12f));
        Island(world, Digital, "Island_C_High", new Vector3(30.5f, 2.6f, 0f), new Vector3(12f, 5.6f, 12f));
        Island(world, Meadow, "Island_Wardrobe", new Vector3(-12.5f, 0f, -1f), new Vector3(9f, 3f, 9f));

        // ---- casa de start (caramida ca turnul din nivelul 1)
        Transform house = new GameObject("House").transform;
        house.SetParent(world, false);
        house.localPosition = new Vector3(-3.5f, 0f, 3f);
        var walls = NerfedEditorUtil.Primitive(PrimitiveType.Cube, "Walls", house, new Vector3(0f, 1.1f, 0f), new Vector3(3f, 2.2f, 3f),
            FezLevelKit.TexMat("M_OW_HouseWall", new Color(0.80f, 0.44f, 0.38f), FezLevelKit.Texture(Tex.Brick), new Vector2(3f, 2.2f)), keepCollider: true);
        var roofGo = NerfedEditorUtil.Primitive(PrimitiveType.Cube, "Roof", house, new Vector3(0f, 2.25f, 0f), new Vector3(2.3f, 2.3f, 3.3f),
            FezLevelKit.TexMat("M_OW_Roof", new Color(0.62f, 0.28f, 0.26f), FezLevelKit.Texture(Tex.Plank), new Vector2(2.3f, 2.3f)), keepCollider: false);
        roofGo.transform.localRotation = Quaternion.Euler(0f, 0f, 45f);
        NerfedEditorUtil.Primitive(PrimitiveType.Cube, "Chimney", house, new Vector3(0.8f, 3.1f, 0.6f), new Vector3(0.45f, 1f, 0.45f),
            FezLevelKit.TexMat("M_OW_Chimney", new Color(0.74f, 0.38f, 0.33f), FezLevelKit.Texture(Tex.Brick), new Vector2(0.5f, 1f)), keepCollider: false);
        NerfedEditorUtil.Primitive(PrimitiveType.Cube, "Door", house, new Vector3(0f, 0.7f, -1.51f), new Vector3(0.8f, 1.4f, 0.05f),
            FezLevelKit.TexMat("M_OW_Door", new Color(0.72f, 0.48f, 0.30f), FezLevelKit.Texture(Tex.Wood), new Vector2(0.8f, 1.4f)), keepCollider: false);
        Color frame = new Color(0.55f, 0.36f, 0.24f), glass = new Color(0.22f, 0.27f, 0.42f);
        FezLevelKit.Window(kit, house.position + new Vector3(-1.52f, 1.2f, 0f), Vector3.left, frame, glass, 0.7f, 0.8f);
        FezLevelKit.Window(kit, house.position + new Vector3(1.52f, 1.2f, 0f), Vector3.right, frame, glass, 0.7f, 0.8f);
        FezLevelKit.Vine(kit, house.position + new Vector3(-1.2f, 2.15f, -1.56f), 4, Summer.capHang);
        FezLevelKit.Vine(kit, house.position + new Vector3(1.25f, 2.15f, -1.56f), 3, Summer.capHang);

        // usa casei duce inapoi in tutorial (E langa ea)
        Material doorGlow = NerfedEditorUtil.Mat("M_DoorLamp", new Color(1f, 0.85f, 0.45f), unlit: true);
        var lamp = NerfedEditorUtil.Primitive(PrimitiveType.Cube, "DoorLamp", house, new Vector3(0f, 1.62f, -1.6f), new Vector3(0.5f, 0.18f, 0.18f), doorGlow, keepCollider: false);
        var doorPortal = new GameObject("TutorialDoor").transform;
        doorPortal.SetParent(house, false);
        doorPortal.localPosition = new Vector3(0f, 0f, -1.6f);
        var doorText = NerfedEditorUtil.WorldText(doorPortal, "Prompt", "E   -   tutorial", new Vector3(0f, 2.6f, -0.6f), 4f,
                                                  Ink, TMPro.FontStyles.Bold, new Vector2(5f, 1f));
        var door = doorPortal.gameObject.AddComponent<Portal>();
        door.levelIndex = 0;
        door.levelScene = "Tutorial";
        door.interactRadius = 1.8f;
        door.glow = lamp.GetComponent<Renderer>();
        door.prompt = doorText;
        door.promptOffset = new Vector3(0f, 2.6f, -0.6f);
        door.idleColor = new Color(1f, 0.85f, 0.45f);   // lampa calda, nu cyan ca portalurile

        // ---- podet spre garderoba (scanduri, cu frunze care atarna)
        Material plank = FezLevelKit.TexMat("M_OW_Bridge", new Color(0.84f, 0.62f, 0.38f), FezLevelKit.Texture(Tex.Plank), new Vector2(3.2f, 0.3f));
        NerfedEditorUtil.Primitive(PrimitiveType.Cube, "Bridge", world, new Vector3(-7.5f, -0.15f, -1.5f), new Vector3(3.2f, 0.3f, 2.2f), plank, keepCollider: true);
        Material rope = NerfedEditorUtil.Mat("M_Rod", new Color(0.22f, 0.18f, 0.20f));
        foreach (float z in new[] { -2.55f, -0.45f })
        {
            NerfedEditorUtil.Primitive(PrimitiveType.Cube, "Rail", world, new Vector3(-7.5f, 0.55f, z), new Vector3(3.4f, 0.06f, 0.06f), rope, keepCollider: false);
            foreach (float x in new[] { -9.05f, -5.95f })
                NerfedEditorUtil.Primitive(PrimitiveType.Cube, "Post", world, new Vector3(x, 0.3f, z), new Vector3(0.1f, 0.6f, 0.1f), rope, keepCollider: false);
        }

        // ---- garderoba
        var wardrobeGo = new GameObject("SkinWardrobe");
        wardrobeGo.transform.SetParent(world, false);
        wardrobeGo.transform.localPosition = new Vector3(-12.5f, 0f, -0.5f);
        var wardrobe = wardrobeGo.AddComponent<SkinWardrobe>();
        wardrobe.arcRadius = 3.5f;
        wardrobe.arcDegrees = 280f;   // arc larg: incap si skin-urile cu poze
        wardrobe.pedestalMat = FezLevelKit.TexMat("M_OW_Pedestal", new Color(0.86f, 0.80f, 0.70f), FezLevelKit.Texture(Tex.Stone), new Vector2(1f, 0.5f));
        wardrobe.previewMat = NerfedEditorUtil.Mat("M_SkinPreview", Color.white);
        Material signWood = FezLevelKit.TexMat("M_OW_Sign", new Color(0.72f, 0.48f, 0.30f), FezLevelKit.Texture(Tex.Plank), new Vector2(3.6f, 1.1f));
        Material postWood = FezLevelKit.TexMat("M_OW_SignPost", new Color(0.52f, 0.36f, 0.25f), FezLevelKit.Texture(Tex.Wood), new Vector2(0.15f, 2.2f));
        var sign = NerfedEditorUtil.Primitive(PrimitiveType.Cube, "SkinsSign", world, new Vector3(-12.5f, 2.6f, 3.4f), new Vector3(3.6f, 1.1f, 0.15f), signWood, keepCollider: false);
        NerfedEditorUtil.Primitive(PrimitiveType.Cube, "SignPostL", world, new Vector3(-13.9f, 1.1f, 3.45f), new Vector3(0.15f, 2.2f, 0.15f), postWood, keepCollider: false);
        NerfedEditorUtil.Primitive(PrimitiveType.Cube, "SignPostR", world, new Vector3(-11.1f, 1.1f, 3.45f), new Vector3(0.15f, 2.2f, 0.15f), postWood, keepCollider: false);
        NerfedEditorUtil.WorldText(sign.transform, "SignText", "SKINS", new Vector3(0f, 0f, -0.6f), 9f, new Color(1f, 0.95f, 0.85f),
                                   TMPro.FontStyles.Bold, new Vector2(3.4f, 1f)).transform.parent.localScale = new Vector3(1f / 3.6f, 1f / 1.1f, 1f);
        FezLevelKit.Lantern(kit, new Vector3(-16.3f, 0f, -4.9f), new Color(1f, 0.85f, 0.45f));
        FezLevelKit.Lantern(kit, new Vector3(-8.7f, 0f, 2.6f), new Color(1f, 0.85f, 0.45f));

        // ---- decor pe insule: copaci din cuburi (cu trunchi solid), felinare, liane
        Color trunk = new Color(0.45f, 0.30f, 0.22f), barkDark = new Color(0.36f, 0.24f, 0.18f);
        Tree(world, new Vector3(-5f, 0f, -4f), 2.4f, trunk, Summer.capA, Summer.capB, new Color(0.38f, 0.62f, 0.22f));
        Tree(world, new Vector3(5f, 0f, -5f), 2.8f, trunk, Summer.capA, Summer.capB, new Color(0.62f, 0.85f, 0.35f));
        Tree(world, new Vector3(-5.5f, 0f, -0.5f), 2.1f, trunk, Summer.capB, new Color(0.55f, 0.78f, 0.28f));
        Tree(world, new Vector3(22f, 0f, -4f), 2.6f, barkDark, Autumn.capA, Autumn.capB, Autumn.capHang, new Color(0.88f, 0.38f, 0.14f));
        Tree(world, new Vector3(14.5f, 0f, 4.5f), 2.3f, barkDark, Autumn.capA, Autumn.capB, Autumn.capHang);
        Tree(world, new Vector3(23f, 0f, 4.2f), 2.0f, barkDark, Autumn.capB, Autumn.capHang, new Color(0.88f, 0.38f, 0.14f));
        Tree(world, new Vector3(-16.1f, 0f, 2.4f), 2.0f, trunk, Summer.capA, Summer.capB);   // in spate, sa nu acopere piedestalele
        FezLevelKit.Lantern(kit, new Vector3(1.6f, 0f, -1.8f), new Color(1f, 0.85f, 0.45f));
        FezLevelKit.Lantern(kit, new Vector3(6.3f, 0f, 1.2f), new Color(1f, 0.85f, 0.45f));
        FezLevelKit.Lantern(kit, new Vector3(13.3f, 0f, -1.5f), new Color(1f, 0.75f, 0.35f));
        FezLevelKit.Lantern(kit, new Vector3(23.7f, 0f, 0.5f), new Color(1f, 0.75f, 0.35f));
        LeafPiles(world, new Vector3(18.5f, 0f, 0f), 5.5f);
        ServerTowers(world);

        // ---- locurile portalurilor (placi din blocuri); portalurile vin cu Add Portals To Open World
        Spot(world, 1, new Vector3(4f, 0f, 3.5f), new Color(0.84f, 0.73f, 0.60f), Tex.Stone);
        Spot(world, 2, new Vector3(20f, 0f, 2.5f), new Color(0.78f, 0.56f, 0.36f), Tex.Plank);
        Spot(world, 3, new Vector3(31f, 2.6f, 1f), new Color(0.26f, 0.28f, 0.40f), Tex.Metal);

        // ---- mochi
        var player = new GameObject("Player");
        player.tag = "Player";
        player.transform.position = new Vector3(-2f, 0.05f, -3f);
        var cc = player.AddComponent<CharacterController>();
        cc.height = 1f;
        cc.radius = 0.45f;
        cc.center = new Vector3(0f, 0.5f, 0f);
        cc.stepOffset = 0.3f;

        // radacina vizualului la talpi, ca squash-ul sa-l turteasca spre sol
        var visualRoot = new GameObject("VisualRoot").transform;
        visualRoot.SetParent(player.transform, false);
        // corpul se rostogoleste (petele arata rostogolirea); ochii stau pe "Face", care ramane cu fata inainte
        var body = NerfedEditorUtil.Primitive(PrimitiveType.Sphere, "Body", visualRoot, new Vector3(0f, 0.5f, 0f), Vector3.one, mochi, keepCollider: false);
        Material spot = NerfedEditorUtil.Mat("M_MochiSpot", new Color(1f, 0.78f, 0.45f));
        foreach (var sp in new[] { new Vector3(0.3f, 0.32f, -0.25f), new Vector3(-0.35f, -0.1f, -0.3f), new Vector3(0.05f, -0.42f, 0.2f), new Vector3(-0.2f, 0.3f, 0.35f) })
            NerfedEditorUtil.Primitive(PrimitiveType.Sphere, "Spot", body.transform, sp.normalized * 0.47f, Vector3.one * 0.16f, spot, keepCollider: false);
        var face = new GameObject("Face").transform;
        face.SetParent(visualRoot, false);
        face.localPosition = new Vector3(0f, 0.5f, 0f);
        NerfedEditorUtil.Primitive(PrimitiveType.Sphere, "EyeL", face, new Vector3(-0.17f, 0.12f, 0.45f), Vector3.one * 0.12f, eye, keepCollider: false);
        NerfedEditorUtil.Primitive(PrimitiveType.Sphere, "EyeR", face, new Vector3(0.17f, 0.12f, 0.45f), Vector3.one * 0.12f, eye, keepCollider: false);

        var controller = player.AddComponent<OverworldController>();
        controller.visual = visualRoot;
        var skin = player.AddComponent<MochiSkin>();   // skin-ul ales in garderoba
        skin.body = body.GetComponent<Renderer>();
        skin.frontU = 0.75f;   // mochi din open world se uita spre +z (directia de mers)
        var spots = new List<Renderer>();
        foreach (Transform sp in body.transform) if (sp.name == "Spot") spots.Add(sp.GetComponent<Renderer>());
        skin.extras = spots.ToArray();

        // ---- camera + cerul nivelurilor (gradient + patrate in parallax) + polen
        var camGo = new GameObject("Main Camera");
        camGo.tag = "MainCamera";
        var cam = camGo.AddComponent<Camera>();
        cam.fieldOfView = 35f;
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = SkyMid;
        cam.farClipPlane = 260f;
        camGo.AddComponent<AudioListener>();
        var oc = camGo.AddComponent<OverworldCamera>();
        oc.target = player.transform;
        oc.Snap();
        Sky(camGo.transform);
        Pollen(player.transform);

        System.IO.Directory.CreateDirectory(NerfedEditorUtil.ScenesDir);
        EditorSceneManager.SaveScene(scene, ScenePath);
        NerfedEditorUtil.AddSceneToBuild(ScenePath);
        AssetDatabase.SaveAssets();

        Debug.Log("[Mochini] Open world construit: " + ScenePath +
                  "   |   WASD = mers, SPACE = salt. Dash si double jump sunt inca blocate.");
    }

    // =====================================================================================================
    // insule
    // =====================================================================================================

    /// <summary>
    /// Insula: cutia de coliziune (invizibila, exact ca inainte) + vizualul in stilul nivelurilor:
    /// capac de iarba / frunze / neon cu fire care atarna pe margini, o banda de pamant, corpul texturat si
    /// dedesubt cateva trepte tot mai mici (insula plutitoare din blocuri).
    /// </summary>
    static void Island(Transform parent, Look look, string name, Vector3 top, Vector3 size)
    {
        var root = new GameObject(name).transform;
        root.SetParent(parent, false);

        var col = new GameObject("Collider");
        col.transform.SetParent(root, false);
        col.transform.localPosition = new Vector3(top.x, top.y - size.y * 0.5f, top.z);
        col.AddComponent<BoxCollider>().size = size;

        string p = "M_OW_" + look.id + "_";
        float bottom = top.y - size.y;

        // corpul: banda de 1 bloc sub iarba + restul
        Material band = FezLevelKit.TexMat(p + "Band_" + size.x, look.band, FezLevelKit.Texture(look.bandTex), new Vector2(size.x, 1f));
        NerfedEditorUtil.Primitive(PrimitiveType.Cube, "Band", root, new Vector3(top.x, top.y - 0.5f, top.z), new Vector3(size.x, 1f, size.z), band, keepCollider: false);
        float bodyH = size.y - 1f;
        Material bodyM = FezLevelKit.TexMat(p + "Body_" + size.x + "x" + bodyH, look.body, FezLevelKit.Texture(look.bodyTex), new Vector2(size.x, bodyH));
        NerfedEditorUtil.Primitive(PrimitiveType.Cube, "Body", root, new Vector3(top.x, top.y - 1f - bodyH * 0.5f, top.z), new Vector3(size.x, bodyH, size.z), bodyM, keepCollider: false);

        // dedesubt: trepte tot mai mici, putin decalate (forma de insula plutitoare)
        float w = size.x, y = bottom;
        for (int n = 0; n < 3; n++)
        {
            w -= n == 0 ? 2f : 3f;
            if (w < 2f) break;
            float h = n == 2 ? 1f : 1.5f;
            var off = new Vector3(rng.Next(-1, 2) * 0.5f, 0f, rng.Next(-1, 2) * 0.5f);
            Material m = FezLevelKit.TexMat(p + "Under_" + w + "x" + h, look.body, FezLevelKit.Texture(look.bodyTex), new Vector2(w, h));
            NerfedEditorUtil.Primitive(PrimitiveType.Cube, "Under", root, new Vector3(top.x, y - h * 0.5f, top.z) + off, new Vector3(w, h, w), m, keepCollider: false);
            y -= h;
        }

        if (look.neon) NeonTop(root, look, top, size);
        else GrassTop(root, look, top, size);

        // liane care atarna de pe fata dinspre camera
        if (!look.neon)
            for (int n = 0; n < Mathf.RoundToInt(size.x / 3.5f); n++)
            {
                float x = top.x - size.x * 0.5f + 1f + (float)rng.NextDouble() * (size.x - 2f);
                FezLevelKit.Vine(kit, new Vector3(x, top.y, top.z - size.z * 0.5f - 0.06f), 3 + rng.Next(0, 4), look.capHang);
            }
    }

    /// <summary>Capacul de iarba (sau frunze): celule de 1 x 1 in doua nuante, fire pe margini, flori.</summary>
    static void GrassTop(Transform root, Look look, Vector3 top, Vector3 size)
    {
        string p = "M_OW_" + look.id + "_";
        var cells = CellTexture("T_OW_Cells_" + look.id, look.capA, look.capB);
        Material cap = FezLevelKit.TexMat(p + "Cap_" + size.x, Color.white, cells, new Vector2((size.x + 0.14f) / 16f, (size.z + 0.14f) / 16f));
        NerfedEditorUtil.Primitive(PrimitiveType.Cube, "Cap", root, new Vector3(top.x, top.y - 0.08f, top.z), new Vector3(size.x + 0.14f, 0.2f, size.z + 0.14f), cap, keepCollider: false);

        Material a = NerfedEditorUtil.Mat(p + "CapB", look.capB);
        Material hang = NerfedEditorUtil.Mat(p + "CapHang", look.capHang);
        var strands = new GameObject("Hang").transform;
        strands.SetParent(root, false);
        float x0 = top.x - size.x * 0.5f, z0 = top.z - size.z * 0.5f;
        // fire de iarba care atarna peste margini, ca la blocurile din niveluri (0-2 pe fiecare unitate de margine)
        for (int side = 0; side < 4; side++)
        {
            int len = side < 2 ? Mathf.RoundToInt(size.x) : Mathf.RoundToInt(size.z);
            for (int u = 0; u < len; u++)
            {
                int n = rng.Next(0, 3);
                for (int q = 0; q < n; q++)
                {
                    float along = u + 0.1f + (float)rng.NextDouble() * 0.8f;
                    float l = 0.22f + (float)rng.NextDouble() * 0.38f;
                    Vector3 pos; Vector3 sz;
                    switch (side)
                    {
                        case 0: pos = new Vector3(x0 + along, 0f, z0 - 0.08f); sz = new Vector3(0.13f, l, 0.06f); break;
                        case 1: pos = new Vector3(x0 + along, 0f, z0 + size.z + 0.08f); sz = new Vector3(0.13f, l, 0.06f); break;
                        case 2: pos = new Vector3(x0 - 0.08f, 0f, z0 + along); sz = new Vector3(0.06f, l, 0.13f); break;
                        default: pos = new Vector3(x0 + size.x + 0.08f, 0f, z0 + along); sz = new Vector3(0.06f, l, 0.13f); break;
                    }
                    pos.y = top.y + 0.02f - l * 0.5f;
                    NerfedEditorUtil.Primitive(PrimitiveType.Cube, "Strand", strands, pos, sz, rng.NextDouble() < 0.6 ? hang : a, keepCollider: false);
                }
            }
        }

        // flori mici pe celule (pe langa margini mai multe, in mijloc mai putine: acolo se merge)
        if (look.flowers.Length == 0) return;
        var flowers = new GameObject("Flowers").transform;
        flowers.SetParent(root, false);
        for (int i = 0; i < Mathf.RoundToInt(size.x); i++)
            for (int k = 0; k < Mathf.RoundToInt(size.z); k++)
            {
                if (rng.NextDouble() > 0.16) continue;
                Color fc = look.flowers[rng.Next(look.flowers.Length)];
                Vector3 at = new Vector3(x0 + i + 0.15f + (float)rng.NextDouble() * 0.7f, top.y + 0.02f, z0 + k + 0.15f + (float)rng.NextDouble() * 0.7f);
                if (Occupied(at)) continue;
                NerfedEditorUtil.Primitive(PrimitiveType.Cube, "Stem", flowers, at + Vector3.up * 0.09f, new Vector3(0.04f, 0.18f, 0.04f), hang, keepCollider: false);
                NerfedEditorUtil.Primitive(PrimitiveType.Cube, "Flower", flowers, at + Vector3.up * 0.21f, Vector3.one * 0.13f,
                    NerfedEditorUtil.Mat("M_Flower_" + ColorUtility.ToHtmlStringRGB(fc), fc), keepCollider: false);
            }
    }

    // locuri pe care nu cresc flori (casa, placile portalurilor, garderoba, startul lui mochi): (x, z, raza)
    static readonly Vector3[] Busy =
    {
        new Vector3(-3.5f, 3f, 2.2f), new Vector3(4f, 3.5f, 2f), new Vector3(20f, 2.5f, 2f), new Vector3(31f, 1f, 2f),
        new Vector3(-12.5f, -0.5f, 4.3f), new Vector3(-2f, -3f, 1f),
    };

    static bool Occupied(Vector3 at)
    {
        foreach (var b in Busy)
            if (new Vector2(at.x - b.x, at.z - b.y).magnitude < b.z) return true;
        return false;
    }

    /// <summary>Capacul digital (nivelul 3): placa inchisa, dungi de neon pe margini si pe grila de celule.</summary>
    static void NeonTop(Transform root, Look look, Vector3 top, Vector3 size)
    {
        var cells = CellTexture("T_OW_Cells_" + look.id, look.capA, look.capB);
        Material cap = FezLevelKit.TexMat("M_OW_" + look.id + "_Cap_" + size.x, Color.white, cells, new Vector2(size.x / 16f, size.z / 16f));
        NerfedEditorUtil.Primitive(PrimitiveType.Cube, "Cap", root, new Vector3(top.x, top.y - 0.08f, top.z), new Vector3(size.x + 0.04f, 0.2f, size.z + 0.04f), cap, keepCollider: false);
        Material neon = NerfedEditorUtil.Mat("M_OW_Neon", Neon, unlit: true);
        Material dim = NerfedEditorUtil.Mat("M_OW_NeonDim", Color.Lerp(Neon, Color.black, 0.6f), unlit: true);
        float x0 = top.x - size.x * 0.5f, x1 = top.x + size.x * 0.5f, z0 = top.z - size.z * 0.5f, z1 = top.z + size.z * 0.5f;
        float y = top.y + 0.03f;
        NerfedEditorUtil.Primitive(PrimitiveType.Cube, "NeonEdge", root, new Vector3(top.x, y, z0), new Vector3(size.x + 0.1f, 0.08f, 0.08f), neon, keepCollider: false);
        NerfedEditorUtil.Primitive(PrimitiveType.Cube, "NeonEdge", root, new Vector3(top.x, y, z1), new Vector3(size.x + 0.1f, 0.08f, 0.08f), neon, keepCollider: false);
        NerfedEditorUtil.Primitive(PrimitiveType.Cube, "NeonEdge", root, new Vector3(x0, y, top.z), new Vector3(0.08f, 0.08f, size.z + 0.1f), neon, keepCollider: false);
        NerfedEditorUtil.Primitive(PrimitiveType.Cube, "NeonEdge", root, new Vector3(x1, y, top.z), new Vector3(0.08f, 0.08f, size.z + 0.1f), neon, keepCollider: false);
        // muchiile verticale ale insulei
        foreach (var c in new[] { new Vector3(x0, 0f, z0), new Vector3(x1, 0f, z0), new Vector3(x0, 0f, z1), new Vector3(x1, 0f, z1) })
            NerfedEditorUtil.Primitive(PrimitiveType.Cube, "NeonEdgeV", root, new Vector3(c.x, top.y - size.y * 0.5f, c.z), new Vector3(0.08f, size.y, 0.08f), neon, keepCollider: false);
        // grila slaba la fiecare 2 celule
        for (float x = x0 + 2f; x < x1 - 0.5f; x += 2f)
            NerfedEditorUtil.Primitive(PrimitiveType.Cube, "Grid", root, new Vector3(x, top.y + 0.025f, top.z), new Vector3(0.03f, 0.02f, size.z), dim, keepCollider: false);
        for (float z = z0 + 2f; z < z1 - 0.5f; z += 2f)
            NerfedEditorUtil.Primitive(PrimitiveType.Cube, "Grid", root, new Vector3(top.x, top.y + 0.025f, z), new Vector3(size.x, 0.02f, 0.03f), dim, keepCollider: false);
        // beculete pe fata dinspre camera
        for (int n = 0; n < 14; n++)
        {
            Color led = rng.NextDouble() < 0.7 ? Neon : (rng.NextDouble() < 0.5 ? Pink : new Color(0.4f, 1f, 0.5f));
            var at = new Vector3(x0 + 0.5f + (float)rng.NextDouble() * (size.x - 1f), top.y - 1.3f - (float)rng.NextDouble() * (size.y - 2f), z0 - 0.03f);
            NerfedEditorUtil.Primitive(PrimitiveType.Cube, "Led", root, at, new Vector3(0.12f, 0.12f, 0.04f),
                NerfedEditorUtil.Mat("M_OW_Led_" + ColorUtility.ToHtmlStringRGB(led), led, unlit: true), keepCollider: false);
        }
    }

    /// <summary>Textura 16 x 16 cu celule in doua nuante (alese la intamplare, cu seed fix), cate un pixel pe bloc.</summary>
    static Texture2D CellTexture(string name, Color a, Color b)
    {
        var tex = FezLevelKit.LoadTex(name);
        if (tex != null) return tex;
        tex = new Texture2D(16, 16, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point, name = name };
        var r = new System.Random(name.GetHashCode() & 0xffff);
        for (int y = 0; y < 16; y++)
            for (int x = 0; x < 16; x++)
                tex.SetPixel(x, y, r.NextDouble() < 0.5 ? a : b);
        tex.Apply();
        FezLevelKit.SaveTex(tex, name);
        return tex;
    }

    // =====================================================================================================
    // decor
    // =====================================================================================================

    /// <summary>Copac din cuburi (ca in niveluri) + un trunchi invizibil solid, ca mochi sa nu treaca prin el.</summary>
    static void Tree(Transform world, Vector3 basePos, float height, Color trunk, params Color[] leaves)
    {
        FezLevelKit.Tree(kit, basePos, height, trunk, leaves);
        var col = new GameObject("TreeCollider");
        col.transform.SetParent(world, false);
        col.transform.position = basePos + Vector3.up * height * 0.5f;
        col.AddComponent<BoxCollider>().size = new Vector3(0.4f, height, 0.4f);
    }

    /// <summary>Gramezi de frunze pe insula de toamna.</summary>
    static void LeafPiles(Transform world, Vector3 center, float radius)
    {
        var root = new GameObject("LeafPiles").transform;
        root.SetParent(world, false);
        Color[] cs = { Autumn.capA, Autumn.capB, Autumn.capHang, new Color(0.88f, 0.38f, 0.14f) };
        for (int n = 0; n < 40; n++)
        {
            Color c = cs[rng.Next(cs.Length)];
            var at = center + new Vector3(((float)rng.NextDouble() * 2f - 1f) * radius, 0.04f, ((float)rng.NextDouble() * 2f - 1f) * radius);
            NerfedEditorUtil.Primitive(PrimitiveType.Cube, "Leaf", root, at, new Vector3(0.28f, 0.04f, 0.2f),
                NerfedEditorUtil.Mat("M_Leaves_" + ColorUtility.ToHtmlStringRGB(c), c), keepCollider: false)
                .transform.localRotation = Quaternion.Euler(0f, (float)rng.NextDouble() * 90f, 0f);
        }
    }

    /// <summary>Platoul digital: doua turnuri de server cu neon si cuburi de date care plutesc.</summary>
    static void ServerTowers(Transform world)
    {
        var root = new GameObject("Servers").transform;
        root.SetParent(world, false);
        Material neon = NerfedEditorUtil.Mat("M_OW_Neon", Neon, unlit: true);
        foreach (var basePos in new[] { new Vector3(27f, 2.6f, -4f), new Vector3(34f, 2.6f, 4f) })
        {
            float h = 3.2f;
            Material m = FezLevelKit.TexMat("M_OW_ServerRack", new Color(0.18f, 0.20f, 0.30f), FezLevelKit.Texture(Tex.Circuit), new Vector2(1.2f, h));
            NerfedEditorUtil.Primitive(PrimitiveType.Cube, "Rack", root, basePos + Vector3.up * h * 0.5f, new Vector3(1.2f, h, 1.2f), m, keepCollider: true);
            foreach (var c in new[] { new Vector3(-0.6f, 0f, -0.6f), new Vector3(0.6f, 0f, -0.6f), new Vector3(-0.6f, 0f, 0.6f), new Vector3(0.6f, 0f, 0.6f) })
                NerfedEditorUtil.Primitive(PrimitiveType.Cube, "NeonEdge", root, basePos + c + Vector3.up * h * 0.5f, new Vector3(0.07f, h, 0.07f), neon, keepCollider: false);
            for (int n = 0; n < 6; n++)
            {
                Color led = rng.NextDouble() < 0.6 ? Neon : Pink;
                NerfedEditorUtil.Primitive(PrimitiveType.Cube, "Led", root, basePos + new Vector3(-0.3f + (n % 3) * 0.3f, 0.6f + n / 3 * 1.2f, -0.62f),
                    new Vector3(0.12f, 0.08f, 0.03f), NerfedEditorUtil.Mat("M_OW_Led_" + ColorUtility.ToHtmlStringRGB(led), led, unlit: true), keepCollider: false);
            }
        }
        for (int n = 0; n < 16; n++)
        {
            Color c = rng.NextDouble() < 0.6 ? Neon : Pink;
            var p = new Vector3(25f + (float)rng.NextDouble() * 11f, 4.2f + (float)rng.NextDouble() * 3.5f, -5f + (float)rng.NextDouble() * 10f);
            NerfedEditorUtil.Primitive(PrimitiveType.Cube, "DataCube", root, p, Vector3.one * (0.15f + (float)rng.NextDouble() * 0.25f),
                NerfedEditorUtil.Mat("M_OW_Data_" + ColorUtility.ToHtmlStringRGB(c), c, unlit: true), keepCollider: false)
                .GetComponent<MeshRenderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        }
    }

    static void Spot(Transform parent, int index, Vector3 pos, Color color, Tex tex)
    {
        var spot = new GameObject("PortalSpot_" + index).transform;
        spot.SetParent(parent, false);
        spot.localPosition = pos;
        Material m = FezLevelKit.TexMat("M_OW_Pad_" + index, color, FezLevelKit.Texture(tex), new Vector2(3f, 3f));
        NerfedEditorUtil.Primitive(PrimitiveType.Cube, "Pad", spot, new Vector3(0f, 0.05f, 0f), new Vector3(3f, 0.1f, 3f), m, keepCollider: false);
    }

    /// <summary>Cerul nivelului 1: gradient + patrate mari decolorate in parallax, copii ai camerei.</summary>
    static void Sky(Transform cam)
    {
        const string name = "T_OW_Sky";
        var tex = FezLevelKit.LoadTex(name);
        if (tex == null)
        {
            tex = new Texture2D(2, 64, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp, name = name };
            for (int y = 0; y < 64; y++)
            {
                float k = y / 63f;
                Color c = k < 0.5f ? Color.Lerp(SkyLow, SkyMid, k * 2f) : Color.Lerp(SkyMid, SkyTop, (k - 0.5f) * 2f);
                tex.SetPixel(0, y, c); tex.SetPixel(1, y, c);
            }
            tex.Apply();
            FezLevelKit.SaveTex(tex, name);
        }
        Material m = FezLevelKit.TexMat("M_OW_Sky", Color.white, tex, Vector2.one, unlit: true);
        var quad = NerfedEditorUtil.Primitive(PrimitiveType.Quad, "SkyGradient", cam, new Vector3(0f, 0f, 240f), new Vector3(330f, 160f, 1f), m, keepCollider: false);
        quad.GetComponent<MeshRenderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;

        var holder = new GameObject("SkySquares").transform;
        holder.SetParent(cam, false);
        holder.localPosition = new Vector3(0f, 0f, 180f);
        Material sq = FezLevelKit.FxMaterial("M_OW_SkySquare", new Color(1f, 0.93f, 0.85f, 0.16f));
        var r = new System.Random(77);
        for (int n = 0; n < 18; n++)
        {
            float s = 12f + (float)r.NextDouble() * 26f;
            var q = NerfedEditorUtil.Primitive(PrimitiveType.Quad, "Square", holder,
                new Vector3((float)r.NextDouble() * 200f - 100f, (float)r.NextDouble() * 110f - 55f, n * 0.2f),
                new Vector3(s, s * (0.8f + (float)r.NextDouble() * 0.5f), 1f), sq, keepCollider: false);
            q.GetComponent<MeshRenderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        }
        var par = holder.gameObject.AddComponent<SkyParallax>();
        par.wrapWidth = 200f;
        par.wrapHeight = 110f;
        par.verticalFactor = 0.5f;
        par.drift = 0.3f;
    }

    /// <summary>Polen care pluteste in jurul lui mochi (ca in nivelul 1).</summary>
    static void Pollen(Transform follow)
    {
        var go = new GameObject("Ambient_Pollen");
        go.transform.SetParent(follow, false);
        go.transform.localPosition = new Vector3(0f, 2f, 0f);
        var ps = go.AddComponent<ParticleSystem>();
        var main = ps.main;
        main.loop = true;
        main.prewarm = true;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.maxParticles = 200;
        main.startColor = new ParticleSystem.MinMaxGradient(new Color(1f, 0.97f, 0.75f, 0.9f), new Color(1f, 0.85f, 0.55f, 0.7f));
        main.startLifetime = 10f; main.startSpeed = 0.15f; main.startSize = new ParticleSystem.MinMaxCurve(0.06f, 0.14f);
        main.gravityModifier = -0.004f;
        var em = ps.emission; em.rateOverTime = 16f;
        var shape = ps.shape; shape.shapeType = ParticleSystemShapeType.Box; shape.scale = new Vector3(28f, 8f, 20f);
        var noise = ps.noise; noise.enabled = true; noise.strength = 0.4f; noise.frequency = 0.2f;
        var col = ps.colorOverLifetime;
        col.enabled = true;
        var g = new Gradient();
        g.SetKeys(new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                  new[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(1f, 0.2f), new GradientAlphaKey(1f, 0.8f), new GradientAlphaKey(0f, 1f) });
        col.color = g;
        var r = go.GetComponent<ParticleSystemRenderer>();
        r.renderMode = ParticleSystemRenderMode.Billboard;
        r.sharedMaterial = FezLevelKit.ParticleMaterial(true);
        r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
    }
}
