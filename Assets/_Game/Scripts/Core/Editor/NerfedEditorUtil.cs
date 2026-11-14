using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Unelte comune pentru builderele din meniul NERFED: materiale, scene in build, primitive.
/// </summary>
public static class NerfedEditorUtil
{
    public const string MaterialsDir = "Assets/_Game/Art/Materials";
    public const string ScenesDir = "Assets/_Game/Scenes";

    /// <summary>Incarca materialul daca exista, altfel il creeaza (URP Lit sau Unlit).</summary>
    public static Material Mat(string name, Color color, bool unlit = false)
    {
        string path = MaterialsDir + "/" + name + ".mat";
        var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (mat != null) return mat;

        Shader shader = Shader.Find(unlit ? "Universal Render Pipeline/Unlit" : "Universal Render Pipeline/Lit");
        if (shader == null) shader = Shader.Find(unlit ? "Unlit/Color" : "Standard");

        mat = new Material(shader) { name = name };
        if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", color);
        mat.color = color;
        if (mat.HasProperty("_Smoothness")) mat.SetFloat("_Smoothness", 0.15f);

        System.IO.Directory.CreateDirectory(MaterialsDir);
        AssetDatabase.CreateAsset(mat, path);
        return mat;
    }

    /// <summary>Materialul fizic al mochi-ului: fara frecare, fara bounce (wall bounce-ul e in cod).</summary>
    public static PhysicsMaterial2D MochiPhysics()
    {
        const string path = MaterialsDir + "/PM_Mochi.physicsMaterial2D";
        var pm = AssetDatabase.LoadAssetAtPath<PhysicsMaterial2D>(path);
        if (pm != null) return pm;

        pm = new PhysicsMaterial2D("PM_Mochi") { friction = 0f, bounciness = 0f };
        System.IO.Directory.CreateDirectory(MaterialsDir);
        AssetDatabase.CreateAsset(pm, path);
        return pm;
    }

    /// <summary>Adauga scena la sfarsitul listei din Build Settings, fara sa le stearga pe celelalte.</summary>
    public static void AddSceneToBuild(string scenePath)
    {
        var list = new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
        foreach (var s in list)
            if (s.path == scenePath) return;
        list.Add(new EditorBuildSettingsScene(scenePath, true));
        EditorBuildSettings.scenes = list.ToArray();
    }

    /// <summary>Primitiva cu material; pozitia e LOCALA fata de parinte.</summary>
    public static GameObject Primitive(PrimitiveType type, string name, Transform parent,
                                       Vector3 localPos, Vector3 scale, Material mat, bool keepCollider)
    {
        var go = GameObject.CreatePrimitive(type);
        go.name = name;
        if (parent != null) go.transform.SetParent(parent, false);
        go.transform.localPosition = localPos;
        go.transform.localScale = scale;
        go.GetComponent<MeshRenderer>().sharedMaterial = mat;
        if (!keepCollider) Object.DestroyImmediate(go.GetComponent<Collider>());
        return go;
    }
}
