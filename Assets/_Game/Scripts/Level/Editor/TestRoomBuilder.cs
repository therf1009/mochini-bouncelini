using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;


/// Nota pe care o vom folosi mai tarziu: fizica 2D ignora complet pozitia pe Z,
/// deci colliderele cuburilor sunt deja "colapsate in adancime" — exact
/// comportamentul de care avem nevoie pentru rotatia in stil Fez.
public static class TestRoomBuilder
{
    const string ScenePath = "Assets/_Game/Scenes/TestRoom.unity";
    const string MatPath = "Assets/_Game/Art/Materials/M_White.mat";
    const string PhysMatPath = "Assets/_Game/Art/Materials/PM_Mochi.physicsMaterial2D";

    [MenuItem("Mochini Bouncelini/Build Test Room")]
    public static void Build()
    {
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        Material white = GetOrCreateWhiteMaterial();
        PhysicsMaterial2D slippery = GetOrCreatePhysicsMaterial();

        CreateLight();
        CreateCamera();
        CreateRoom(white);
        CreatePlayer(white, slippery);

        System.IO.Directory.CreateDirectory("Assets/_Game/Scenes");
        EditorSceneManager.SaveScene(scene, ScenePath);

        // adauga scena fara sa le stearga pe celelalte (title, tutorial, open world)
        NerfedEditorUtil.AddSceneToBuild(ScenePath);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log("[Mochini] Camera de test construita: " + ScenePath +
                  "   |   A/D sau sageti = deplasare, SPACE = hop, W tinut = charge launch.");
    }

    // ---------------------------------------------------------------- resurse

    static Material GetOrCreateWhiteMaterial()
    {
        var existing = AssetDatabase.LoadAssetAtPath<Material>(MatPath);
        if (existing != null) return existing;

        Shader shader = Shader.Find("Universal Render Pipeline/Lit");
        if (shader == null) shader = Shader.Find("Standard");

        var mat = new Material(shader);
        mat.name = "M_White";
        mat.color = new Color(0.93f, 0.93f, 0.92f);
        if (mat.HasProperty("_Smoothness")) mat.SetFloat("_Smoothness", 0.1f);

        System.IO.Directory.CreateDirectory("Assets/_Game/Art/Materials");
        AssetDatabase.CreateAsset(mat, MatPath);
        return mat;
    }

    static PhysicsMaterial2D GetOrCreatePhysicsMaterial()
    {
        var existing = AssetDatabase.LoadAssetAtPath<PhysicsMaterial2D>(PhysMatPath);
        if (existing != null) return existing;

        var pm = new PhysicsMaterial2D("PM_Mochi")
        {
            friction = 0f,      // wall bounce-ul e manual; frecarea doar l-ar lipi de perete
            bounciness = 0f
        };

        System.IO.Directory.CreateDirectory("Assets/_Game/Art/Materials");
        AssetDatabase.CreateAsset(pm, PhysMatPath);
        return pm;
    }

    // ---------------------------------------------------------------- scena

    static void CreateLight()
    {
        var go = new GameObject("Directional Light");
        var l = go.AddComponent<Light>();
        l.type = LightType.Directional;
        l.intensity = 1.1f;
        l.shadows = LightShadows.Soft;
        go.transform.rotation = Quaternion.Euler(50f, -30f, 0f);
    }

    static void CreateCamera()
    {
        var rig = new GameObject("CameraRig");
        rig.transform.position = new Vector3(0f, 6f, 0f);
        rig.AddComponent<CameraShake>();

        var camGo = new GameObject("Main Camera");
        camGo.tag = "MainCamera";
        camGo.transform.SetParent(rig.transform, false);
        camGo.transform.localPosition = new Vector3(0f, 0f, -20f);

        var cam = camGo.AddComponent<Camera>();
        cam.orthographic = true;
        cam.orthographicSize = 8f;
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = new Color(0.14f, 0.14f, 0.16f);
        cam.nearClipPlane = 0.1f;
        cam.farClipPlane = 100f;

        camGo.AddComponent<AudioListener>();
    }

    static void CreateRoom(Material mat)
    {
        var room = new GameObject("Room");

        // podea, tavan, pereti
        Block(room, mat, "Floor", new Vector3(0f, -0.5f, 0f), new Vector3(26f, 1f, 2f));
        Block(room, mat, "Ceiling", new Vector3(0f, 14.5f, 0f), new Vector3(26f, 1f, 2f));
        Block(room, mat, "Wall_L", new Vector3(-13f, 7f, 0f), new Vector3(1f, 16f, 2f));
        Block(room, mat, "Wall_R", new Vector3(13f, 7f, 0f), new Vector3(1f, 16f, 2f));

        // platforme — inaltimi alese ca sa testeze fiecare verb
        Block(room, mat, "Plat_Hop_1", new Vector3(-8f, 1.5f, 0f), new Vector3(4f, 1f, 2f));
        Block(room, mat, "Plat_Hop_2", new Vector3(-3f, 3.0f, 0f), new Vector3(3f, 1f, 2f));
        Block(room, mat, "Plat_Launch", new Vector3(5f, 5.5f, 0f), new Vector3(5f, 1f, 2f));
        Block(room, mat, "Plat_High", new Vector3(-6f, 8.5f, 0f), new Vector3(4f, 1f, 2f));

        // coloana centrala: culoar ingust pentru wall bounce
        Block(room, mat, "Pillar", new Vector3(1f, 9f, 0f), new Vector3(1f, 10f, 2f));

        // cuburi unitare, ca referinta de scara pentru grid
        for (int i = 0; i < 3; i++)
            Block(room, mat, "Cube_" + i, new Vector3(9f + i, 0.5f + i, 0f), Vector3.one);
    }

    static void Block(GameObject parent, Material mat, string name, Vector3 pos, Vector3 size)
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
        go.name = name;
        go.transform.SetParent(parent.transform, false);
        go.transform.position = pos;
        go.transform.localScale = size;
        go.GetComponent<MeshRenderer>().sharedMaterial = mat;

        // fizica 3D afara, fizica 2D inauntru
        Object.DestroyImmediate(go.GetComponent<BoxCollider>());
        go.AddComponent<BoxCollider2D>();
    }

    static void CreatePlayer(Material mat, PhysicsMaterial2D physMat)
    {
        var go = new GameObject("Mochi");
        go.transform.position = new Vector3(-10f, 2f, 0f);

        var rb = go.AddComponent<Rigidbody2D>();
        rb.freezeRotation = true;

        var cc = go.AddComponent<CircleCollider2D>();
        cc.radius = 0.5f;
        cc.sharedMaterial = physMat;

        var visual = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        visual.name = "Visual";
        visual.transform.SetParent(go.transform, false);
        visual.transform.localScale = Vector3.one;
        visual.GetComponent<MeshRenderer>().sharedMaterial = mat;
        Object.DestroyImmediate(visual.GetComponent<SphereCollider>());

        go.AddComponent<MochiSkin>().body = visual.GetComponent<Renderer>();   // skin-ul ales in garderoba
        var ctrl = go.AddComponent<MochiController>();
        ctrl.visual = visual.transform;
    }
}
