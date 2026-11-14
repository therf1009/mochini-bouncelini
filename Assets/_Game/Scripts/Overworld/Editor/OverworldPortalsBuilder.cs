using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>
/// Meniu: NERFED > Add Portals To Open World.
/// Deschide OpenWorld.unity, pune cate un portal pe fiecare PortalSpot_N si
/// adauga AbilityLoader si OverworldSpawn pe mochi. Se poate rula de mai multe ori: inlocuieste portalurile vechi.
/// </summary>
public static class OverworldPortalsBuilder
{
    static readonly string[] Labels =
    {
        "",
        "E   -   LEVEL 1\nreward: dash",
        "E   -   LEVEL 2\nreward: double jump",
        "E   -   LEVEL 3\nthe end"
    };

    [MenuItem("NERFED/Add Portals To Open World")]
    public static void Build()
    {
        string path = OverworldBuilder.ScenePath;
        if (!System.IO.File.Exists(path))
        {
            Debug.LogError("[NERFED] Nu exista " + path + ". Ruleaza intai NERFED > Build Open World.");
            return;
        }
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

        var scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Single);

        Material frame = NerfedEditorUtil.Mat("M_PortalFrame", new Color(0.30f, 0.27f, 0.35f));
        Material glow = NerfedEditorUtil.Mat("M_PortalGlow", new Color(0.35f, 0.85f, 0.95f), unlit: true);

        for (int i = 1; i <= 3; i++)
        {
            var spot = GameObject.Find("PortalSpot_" + i);
            if (spot == null)
            {
                Debug.LogError("[NERFED] Lipseste PortalSpot_" + i + ". Reconstruieste open world-ul.");
                continue;
            }

            var old = spot.transform.Find("Portal_" + i);
            if (old != null) Object.DestroyImmediate(old.gameObject);

            var root = new GameObject("Portal_" + i).transform;
            root.SetParent(spot.transform, false);

            NerfedEditorUtil.Primitive(PrimitiveType.Cube, "PillarL", root, new Vector3(-1.2f, 1.5f, 0f), new Vector3(0.4f, 3f, 0.4f), frame, keepCollider: true);
            NerfedEditorUtil.Primitive(PrimitiveType.Cube, "PillarR", root, new Vector3(1.2f, 1.5f, 0f), new Vector3(0.4f, 3f, 0.4f), frame, keepCollider: true);
            NerfedEditorUtil.Primitive(PrimitiveType.Cube, "Beam", root, new Vector3(0f, 3.1f, 0f), new Vector3(2.8f, 0.4f, 0.5f), frame, keepCollider: true);
            var glowGo = NerfedEditorUtil.Primitive(PrimitiveType.Cube, "Glow", root, new Vector3(0f, 1.45f, 0f), new Vector3(2f, 2.9f, 0.08f), glow, keepCollider: false);

            // deasupra grinzii, in fata ramei
            var tmp = NerfedEditorUtil.WorldText(root, "Prompt", Labels[i], new Vector3(0f, 4.2f, -1.6f), 5f,
                                                 new Color(0.15f, 0.14f, 0.18f), FontStyles.Bold, new Vector2(8f, 3f));

            var portal = root.gameObject.AddComponent<Portal>();
            portal.levelIndex = i;
            portal.levelScene = "Level" + i;
            portal.glow = glowGo.GetComponent<Renderer>();
            portal.prompt = tmp;
        }

        var player = Object.FindFirstObjectByType<OverworldController>();
        if (player != null && player.GetComponent<AbilityLoader>() == null)
            player.gameObject.AddComponent<AbilityLoader>();
        if (player != null && player.GetComponent<OverworldSpawn>() == null)
            player.gameObject.AddComponent<OverworldSpawn>();   // la intoarcerea dintr-un nivel: apari la portal

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Debug.Log("[NERFED] Portaluri adaugate in open world. Testeaza blocajele cu NERFED > Debug.");
    }
}
