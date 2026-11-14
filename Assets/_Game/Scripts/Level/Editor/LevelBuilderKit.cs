using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Piese comune pentru builderele de niveluri 2D (Level1/2/3): scena goala cu lumina, blocuri,
/// texte, checkpoint-uri, mochi, camera, standul cu personaj, tokenul REVERT, steagul de final.
///
/// Repere de inaltime (din MochiController), folosite de toate nivelurile:
///   hop ~1.9 sus si ~4.5 in lungime  |  charge drept in sus ~5.2, in diagonala ~3.4
///   double jump: + ~1.9 din varf     |  dash: ~2.9 pe orizontala, fara sa cada
/// </summary>
public static class LevelBuilderKit
{
    public static readonly Color Ink = new Color(0.16f, 0.15f, 0.17f);

    public static Scene NewScene()
    {
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        var light = new GameObject("Directional Light").AddComponent<Light>();
        light.type = LightType.Directional;
        light.intensity = 1.15f;
        light.shadows = LightShadows.Soft;
        light.transform.rotation = Quaternion.Euler(50f, -30f, 0f);
        return scene;
    }

    /// <summary>Bloc solid dat prin margini: x de la x0 la x1, y de la y0 la y1.</summary>
    public static GameObject Box(Transform parent, Material mat, string name, float x0, float x1, float y0, float y1)
    {
        var go = NerfedEditorUtil.Primitive(PrimitiveType.Cube, name, parent,
            new Vector3((x0 + x1) * 0.5f, (y0 + y1) * 0.5f, 0f), new Vector3(x1 - x0, y1 - y0, 1.6f), mat, keepCollider: false);
        go.AddComponent<BoxCollider2D>();
        return go;
    }

    /// <summary>Text in lume, in fata geometriei, cu leganare lina.</summary>
    public static TextMeshPro Prompt(Transform parent, string text, Vector2 pos, float tilt, Color color, float size = 6f)
    {
        var t = NerfedEditorUtil.WorldText(parent, "Prompt", text, new Vector3(pos.x, pos.y, -1.2f), size, color,
                                           FontStyles.Italic, new Vector2(14f, 4f));
        var holder = t.transform.parent;
        holder.localRotation = Quaternion.Euler(0f, 0f, tilt);
        holder.gameObject.AddComponent<PromptWobble>();
        return t;
    }

    /// <summary>Checkpoint pe sol: pos = (x, inaltimea solului).</summary>
    public static void Checkpoint(Transform parent, Vector2 pos)
    {
        var go = new GameObject("Checkpoint");
        go.transform.SetParent(parent, false);
        go.transform.localPosition = pos;
        go.AddComponent<Checkpoint2D>();
    }

    public static GameObject Mochi(Vector2 pos, float killY)
    {
        Material body = NerfedEditorUtil.Mat("M_Mochi", new Color(0.98f, 0.62f, 0.25f));
        Material eye = NerfedEditorUtil.Mat("M_Eye", new Color(0.10f, 0.10f, 0.12f), unlit: true);

        var go = new GameObject("Mochi");
        go.tag = "Player";
        go.transform.position = pos;

        var rb = go.AddComponent<Rigidbody2D>();
        rb.freezeRotation = true;
        var cc = go.AddComponent<CircleCollider2D>();
        cc.radius = 0.5f;
        cc.sharedMaterial = NerfedEditorUtil.MochiPhysics();

        var visual = NerfedEditorUtil.Primitive(PrimitiveType.Sphere, "Visual", go.transform, Vector3.zero, Vector3.one, body, keepCollider: false);
        NerfedEditorUtil.Primitive(PrimitiveType.Sphere, "EyeL", visual.transform, new Vector3(-0.17f, 0.12f, -0.45f), Vector3.one * 0.12f, eye, keepCollider: false);
        NerfedEditorUtil.Primitive(PrimitiveType.Sphere, "EyeR", visual.transform, new Vector3(0.17f, 0.12f, -0.45f), Vector3.one * 0.12f, eye, keepCollider: false);

        go.AddComponent<MochiController>().visual = visual.transform;
        go.AddComponent<Respawn2D>().killY = killY;
        go.AddComponent<AbilityLoader>();   // dash / double jump castigate deja in nivelurile anterioare
        return go;
    }

    /// <summary>Camera laterala: CameraFollow -> ShakeRig -> Main Camera.</summary>
    public static Camera CameraRig(Transform target, Color background, float minX, float maxX, float minY, float maxY)
    {
        var follow = new GameObject("CameraFollow");
        follow.transform.position = new Vector3(minX, minY, 0f);
        var cf = follow.AddComponent<CameraFollow2D>();
        cf.target = target;
        cf.minX = minX;
        cf.maxX = maxX;
        cf.minY = minY;
        cf.maxY = maxY;

        var shake = new GameObject("ShakeRig");
        shake.transform.SetParent(follow.transform, false);
        shake.AddComponent<CameraShake>();

        var camGo = new GameObject("Main Camera");
        camGo.tag = "MainCamera";
        camGo.transform.SetParent(shake.transform, false);
        camGo.transform.localPosition = new Vector3(0f, 0f, -20f);
        var cam = camGo.AddComponent<Camera>();
        cam.orthographic = true;
        cam.orthographicSize = 6.5f;
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = background;
        cam.nearClipPlane = 0.1f;
        cam.farClipPlane = 100f;
        camGo.AddComponent<AudioListener>();

        cf.Snap();
        return cam;
    }

    /// <summary>Standul: tejghea, personaj cu ochi, firma cu numele. Personajul vorbeste cand ajungi langa el.</summary>
    public static NpcStand Stand(Transform parent, float x, float floorY, string speaker, string[] lines, string goal,
                                 Material npcBody, Color promptColor)
    {
        Material wood = NerfedEditorUtil.Mat("M_StandWood", new Color(0.62f, 0.42f, 0.28f));
        Material eye = NerfedEditorUtil.Mat("M_Eye", new Color(0.10f, 0.10f, 0.12f), unlit: true);

        var root = new GameObject("Stand_" + speaker).transform;
        root.SetParent(parent, false);
        root.localPosition = new Vector3(x, floorY + 0.5f, 0f);   // la inaltimea centrului mochi-ului

        // decor in spatele planului mochi-ului (z pozitiv), fara collidere: treci prin fata lui
        NerfedEditorUtil.Primitive(PrimitiveType.Cube, "Counter", root, new Vector3(0f, 0.05f, 1.2f), new Vector3(2.4f, 1.1f, 1f), wood, keepCollider: false);
        NerfedEditorUtil.Primitive(PrimitiveType.Capsule, "Npc", root, new Vector3(0f, 0.8f, 2f), new Vector3(0.9f, 0.8f, 0.9f), npcBody, keepCollider: false);
        NerfedEditorUtil.Primitive(PrimitiveType.Sphere, "EyeL", root, new Vector3(-0.17f, 1.15f, 1.55f), Vector3.one * 0.13f, eye, keepCollider: false);
        NerfedEditorUtil.Primitive(PrimitiveType.Sphere, "EyeR", root, new Vector3(0.17f, 1.15f, 1.55f), Vector3.one * 0.13f, eye, keepCollider: false);
        NerfedEditorUtil.Primitive(PrimitiveType.Cube, "Sign", root, new Vector3(0f, 2.4f, 1.2f), new Vector3(2.8f, 0.7f, 0.1f), wood, keepCollider: false);

        NerfedEditorUtil.WorldText(root, "SignText", speaker, new Vector3(0f, 2.4f, 1.1f), 3.2f, Color.white,
                                   FontStyles.Bold, new Vector2(2.7f, 0.6f));

        var talk = Prompt(root, "E   -   talk", new Vector2(0f, 3.3f), 0f, promptColor, 4.5f);

        var stand = root.gameObject.AddComponent<NpcStand>();
        stand.speaker = speaker;
        stand.lines = lines;
        stand.goalAfter = goal;
        stand.talkPrompt = talk;
        return stand;
    }

    /// <summary>Tokenul REVERT, care se roteste si pluteste.</summary>
    public static AbilityPickup Revert(Transform parent, Vector2 pos, Ability ability, string header, string[] lines,
                                       string goalAfter, Color labelColor)
    {
        Material glow = NerfedEditorUtil.Mat("M_ExitGlow", new Color(0.35f, 0.85f, 0.95f), unlit: true);

        var root = new GameObject("Revert_" + ability).transform;
        root.SetParent(parent, false);
        root.localPosition = pos;
        var token = NerfedEditorUtil.Primitive(PrimitiveType.Cube, "Token", root, Vector3.zero, Vector3.one * 0.55f, glow, keepCollider: false);

        NerfedEditorUtil.WorldText(root, "Label", "REVERT", new Vector3(0f, 0.9f, -1.2f), 3f, labelColor,
                                   FontStyles.Bold, new Vector2(3f, 0.6f));

        var pickup = root.gameObject.AddComponent<AbilityPickup>();
        pickup.ability = ability;
        pickup.patchHeader = header;
        pickup.patchLines = lines;
        pickup.goalAfter = goalAfter;
        pickup.visual = token.transform;
        return pickup;
    }

    /// <summary>Steagul de final: atingi -> nivel terminat -> inapoi in open world.</summary>
    public static LevelGoal Flag(Transform parent, float x, float floorY, int levelIndex)
    {
        Material pole = NerfedEditorUtil.Mat("M_FlagPole", new Color(0.25f, 0.24f, 0.27f));
        Material flag = NerfedEditorUtil.Mat("M_Flag", new Color(0.98f, 0.62f, 0.25f));

        var root = new GameObject("Goal_Level" + levelIndex).transform;
        root.SetParent(parent, false);
        root.localPosition = new Vector3(x, floorY + 0.5f, 0f);
        NerfedEditorUtil.Primitive(PrimitiveType.Cube, "Pole", root, new Vector3(0f, 1.1f, 0.5f), new Vector3(0.15f, 3.2f, 0.15f), pole, keepCollider: false);
        var cloth = NerfedEditorUtil.Primitive(PrimitiveType.Cube, "Flag", root, new Vector3(0.6f, 2.25f, 0.5f), new Vector3(1.1f, 0.7f, 0.08f), flag, keepCollider: false);
        cloth.AddComponent<PromptWobble>().swayAngle = 4f;

        var goal = root.gameObject.AddComponent<LevelGoal>();
        goal.levelIndex = levelIndex;
        return goal;
    }

    public static void Save(Scene scene, string path)
    {
        System.IO.Directory.CreateDirectory(NerfedEditorUtil.ScenesDir);
        EditorSceneManager.SaveScene(scene, path);
        NerfedEditorUtil.AddSceneToBuild(path);
        AssetDatabase.SaveAssets();
    }
}
