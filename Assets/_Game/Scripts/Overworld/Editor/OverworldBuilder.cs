using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>
/// Meniu: NERFED > Build Open World. Scena "OpenWorld": trei insule plutitoare.
///
///   Insula A (start)  x -7..7      sus la y 0     -> portalul 1
///   groapa            x 7..12.5    (5.5)          -> doar salt + dash (nivelul 1 da dash)
///   Insula B          x 12.5..24.5 sus la y 0     -> portalul 2
///   faleza            +2.6 pe verticala           -> doar double jump (nivelul 2 il da)
///   Insula C (platou) x 24.5..36.5 sus la y 2.6   -> portalul 3, ultimul nivel
///
/// Portalurile propriu-zise le pune NERFED > Add Portals To Open World, pe locurile PortalSpot_N.
/// </summary>
public static class OverworldBuilder
{
    public const string ScenePath = NerfedEditorUtil.ScenesDir + "/OpenWorld.unity";

    [MenuItem("NERFED/Build Open World")]
    public static void Build()
    {
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        Material grass = NerfedEditorUtil.Mat("M_Grass", new Color(0.56f, 0.76f, 0.38f));
        Material cliff = NerfedEditorUtil.Mat("M_Cliff", new Color(0.48f, 0.40f, 0.33f));
        Material stone = NerfedEditorUtil.Mat("M_Stone", new Color(0.82f, 0.80f, 0.74f));
        Material wood = NerfedEditorUtil.Mat("M_Wood", new Color(0.55f, 0.36f, 0.22f));
        Material leaf = NerfedEditorUtil.Mat("M_Leaf", new Color(0.33f, 0.60f, 0.30f));
        Material roof = NerfedEditorUtil.Mat("M_Roof", new Color(0.82f, 0.35f, 0.28f));
        Material wall = NerfedEditorUtil.Mat("M_HouseWall", new Color(0.96f, 0.88f, 0.72f));
        Material mochi = NerfedEditorUtil.Mat("M_Mochi", new Color(0.98f, 0.62f, 0.25f));
        Material eye = NerfedEditorUtil.Mat("M_Eye", new Color(0.10f, 0.10f, 0.12f), unlit: true);
        Material abyss = NerfedEditorUtil.Mat("M_Abyss", new Color(0.30f, 0.42f, 0.60f), unlit: true);

        var light = new GameObject("Directional Light").AddComponent<Light>();
        light.type = LightType.Directional;
        light.intensity = 1.15f;
        light.shadows = LightShadows.Soft;
        light.transform.rotation = Quaternion.Euler(50f, -35f, 0f);

        Transform world = new GameObject("World").transform;

        // ---- insule
        Island(world, grass, cliff, "Island_A_Start", new Vector3(0f, 0f, 0f), new Vector3(14f, 3f, 14f));
        Island(world, grass, cliff, "Island_B", new Vector3(18.5f, 0f, 0f), new Vector3(12f, 3f, 12f));
        Island(world, grass, cliff, "Island_C_High", new Vector3(30.5f, 2.6f, 0f), new Vector3(12f, 5.6f, 12f));

        // hau departe dedesubt, doar decor (respawn-ul se face inainte sa ajungi la el)
        NerfedEditorUtil.Primitive(PrimitiveType.Cube, "Abyss", world,
            new Vector3(15f, -14f, 0f), new Vector3(140f, 0.5f, 140f), abyss, keepCollider: false);

        // ---- casa de start
        Transform house = new GameObject("House").transform;
        house.SetParent(world, false);
        house.localPosition = new Vector3(-3.5f, 0f, 3f);
        NerfedEditorUtil.Primitive(PrimitiveType.Cube, "Walls", house, new Vector3(0f, 1.1f, 0f), new Vector3(3f, 2.2f, 3f), wall, keepCollider: true);
        var roofGo = NerfedEditorUtil.Primitive(PrimitiveType.Cube, "Roof", house, new Vector3(0f, 2.25f, 0f), new Vector3(2.3f, 2.3f, 3.3f), roof, keepCollider: false);
        roofGo.transform.localRotation = Quaternion.Euler(0f, 0f, 45f);
        NerfedEditorUtil.Primitive(PrimitiveType.Cube, "Door", house, new Vector3(0f, 0.7f, -1.51f), new Vector3(0.8f, 1.4f, 0.05f), wood, keepCollider: false);

        // ---- copaci
        Tree(world, wood, leaf, new Vector3(-5f, 0f, -4f));
        Tree(world, wood, leaf, new Vector3(5f, 0f, -5f));
        Tree(world, wood, leaf, new Vector3(-5.5f, 0f, -0.5f));
        Tree(world, wood, leaf, new Vector3(22f, 0f, -4f));
        Tree(world, wood, leaf, new Vector3(14.5f, 0f, 4.5f));
        Tree(world, wood, leaf, new Vector3(34f, 2.6f, 4f));
        Tree(world, wood, leaf, new Vector3(27f, 2.6f, -4f));

        // ---- locurile portalurilor (pietre plate); portalurile vin in etapa urmatoare
        Spot(world, stone, 1, new Vector3(4f, 0f, 3.5f));
        Spot(world, stone, 2, new Vector3(20f, 0f, 2.5f));
        Spot(world, stone, 3, new Vector3(31f, 2.6f, 1f));

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
        var body = NerfedEditorUtil.Primitive(PrimitiveType.Sphere, "Body", visualRoot, new Vector3(0f, 0.5f, 0f), Vector3.one, mochi, keepCollider: false);
        NerfedEditorUtil.Primitive(PrimitiveType.Sphere, "EyeL", body.transform, new Vector3(-0.17f, 0.12f, 0.45f), Vector3.one * 0.12f, eye, keepCollider: false);
        NerfedEditorUtil.Primitive(PrimitiveType.Sphere, "EyeR", body.transform, new Vector3(0.17f, 0.12f, 0.45f), Vector3.one * 0.12f, eye, keepCollider: false);

        var controller = player.AddComponent<OverworldController>();
        controller.visual = visualRoot;

        // ---- camera
        var camGo = new GameObject("Main Camera");
        camGo.tag = "MainCamera";
        var cam = camGo.AddComponent<Camera>();
        cam.fieldOfView = 35f;
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = new Color(0.62f, 0.80f, 0.92f);
        cam.farClipPlane = 250f;
        camGo.AddComponent<AudioListener>();
        var oc = camGo.AddComponent<OverworldCamera>();
        oc.target = player.transform;
        oc.Snap();

        System.IO.Directory.CreateDirectory(NerfedEditorUtil.ScenesDir);
        EditorSceneManager.SaveScene(scene, ScenePath);
        NerfedEditorUtil.AddSceneToBuild(ScenePath);
        AssetDatabase.SaveAssets();

        Debug.Log("[NERFED] Open world construit: " + ScenePath +
                  "   |   WASD = mers, SPACE = salt. Dash si double jump sunt inca blocate.");
    }

    /// <summary>Insula = corp de faleza cu collider + strat subtire de iarba deasupra (doar vizual).</summary>
    static void Island(Transform parent, Material grass, Material cliff, string name, Vector3 top, Vector3 size)
    {
        var root = new GameObject(name).transform;
        root.SetParent(parent, false);
        NerfedEditorUtil.Primitive(PrimitiveType.Cube, "Body", root,
            new Vector3(top.x, top.y - size.y * 0.5f, top.z), size, cliff, keepCollider: true);
        NerfedEditorUtil.Primitive(PrimitiveType.Cube, "Grass", root,
            new Vector3(top.x, top.y - 0.09f, top.z), new Vector3(size.x + 0.1f, 0.2f, size.z + 0.1f), grass, keepCollider: false);
    }

    static void Tree(Transform parent, Material wood, Material leaf, Vector3 basePos)
    {
        var root = new GameObject("Tree").transform;
        root.SetParent(parent, false);
        root.localPosition = basePos;
        NerfedEditorUtil.Primitive(PrimitiveType.Cylinder, "Trunk", root, new Vector3(0f, 0.8f, 0f), new Vector3(0.35f, 0.8f, 0.35f), wood, keepCollider: true);
        NerfedEditorUtil.Primitive(PrimitiveType.Sphere, "Canopy", root, new Vector3(0f, 2.1f, 0f), Vector3.one * 1.6f, leaf, keepCollider: false);
    }

    static void Spot(Transform parent, Material stone, int index, Vector3 pos)
    {
        var spot = new GameObject("PortalSpot_" + index).transform;
        spot.SetParent(parent, false);
        spot.localPosition = pos;
        NerfedEditorUtil.Primitive(PrimitiveType.Cylinder, "Pad", spot, new Vector3(0f, 0.04f, 0f), new Vector3(2.8f, 0.04f, 2.8f), stone, keepCollider: false);
    }
}
