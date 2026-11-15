using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Tex = FezLevelKit.Tex;

/// <summary>
/// Meniu: Mochini Bouncelini > Add Portals To Open World.
/// Deschide OpenWorld.unity, pune cate un portal pe fiecare PortalSpot_N si
/// adauga AbilityLoader si OverworldSpawn pe mochi. Se poate rula de mai multe ori: inlocuieste portalurile vechi.
///
/// Fiecare portal arata ca nivelul lui:
///   1 - VARA:    arc de caramida, iarba care atarna de pe grinda, flori, iedera pe stalpi, lumina calda
///   2 - TOAMNA:  stalpi de scoarta, coroana de frunze portocalii/rosii peste grinda, frunze cazute, lumina de chihlimbar
///   3 - DIGITAL: metal cu circuite, dungi de neon pe muchii, beculete, cuburi de date care plutesc, lumina cyan
/// </summary>
public static class OverworldPortalsBuilder
{
    static readonly string[] Labels =
    {
        "",
        "E   -   LEVEL 1\nsummer tower  ·  reward: dash",
        "E   -   LEVEL 2\nautumn tree  ·  reward: double jump",
        "E   -   LEVEL 3\nthe server  ·  the end"
    };

    struct Look
    {
        public Color pillar, beam, glowIdle, glowNear;
        public Tex pillarTex, beamTex;
    }

    static readonly Look[] Looks =
    {
        default,
        new Look { pillar = new Color(0.74f, 0.38f, 0.33f), beam = new Color(0.84f, 0.73f, 0.60f), pillarTex = Tex.Brick, beamTex = Tex.Stone,
                   glowIdle = new Color(1f, 0.80f, 0.40f), glowNear = new Color(1f, 0.95f, 0.65f) },
        new Look { pillar = new Color(0.46f, 0.31f, 0.23f), beam = new Color(0.52f, 0.36f, 0.25f), pillarTex = Tex.Wood, beamTex = Tex.Wood,
                   glowIdle = new Color(1f, 0.55f, 0.20f), glowNear = new Color(1f, 0.80f, 0.45f) },
        new Look { pillar = new Color(0.20f, 0.22f, 0.32f), beam = new Color(0.24f, 0.26f, 0.36f), pillarTex = Tex.Circuit, beamTex = Tex.Metal,
                   glowIdle = new Color(0.30f, 0.95f, 1f), glowNear = new Color(1f, 0.40f, 0.90f) },
    };

    [MenuItem("Mochini Bouncelini/Add Portals To Open World")]
    public static void Build()
    {
        string path = OverworldBuilder.ScenePath;
        if (!System.IO.File.Exists(path))
        {
            Debug.LogError("[Mochini] Nu exista " + path + ". Ruleaza intai Mochini Bouncelini > Build Open World.");
            return;
        }
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

        var scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Single);

        for (int i = 1; i <= 3; i++)
        {
            var spot = GameObject.Find("PortalSpot_" + i);
            if (spot == null)
            {
                Debug.LogError("[Mochini] Lipseste PortalSpot_" + i + ". Reconstruieste open world-ul.");
                continue;
            }

            var old = spot.transform.Find("Portal_" + i);
            if (old != null) Object.DestroyImmediate(old.gameObject);

            var root = new GameObject("Portal_" + i).transform;
            root.SetParent(spot.transform, false);
            var look = Looks[i];

            Material pillarM = FezLevelKit.TexMat("M_Portal" + i + "_Pillar", look.pillar, FezLevelKit.Texture(look.pillarTex), new Vector2(0.6f, 3.2f));
            Material beamM = FezLevelKit.TexMat("M_Portal" + i + "_Beam", look.beam, FezLevelKit.Texture(look.beamTex), new Vector2(3.4f, 0.5f));
            NerfedEditorUtil.Primitive(PrimitiveType.Cube, "PillarL", root, new Vector3(-1.3f, 1.6f, 0f), new Vector3(0.6f, 3.2f, 0.6f), pillarM, keepCollider: true);
            NerfedEditorUtil.Primitive(PrimitiveType.Cube, "PillarR", root, new Vector3(1.3f, 1.6f, 0f), new Vector3(0.6f, 3.2f, 0.6f), pillarM, keepCollider: true);
            NerfedEditorUtil.Primitive(PrimitiveType.Cube, "Beam", root, new Vector3(0f, 3.45f, 0f), new Vector3(3.4f, 0.5f, 0.7f), beamM, keepCollider: true);
            Material glowM = NerfedEditorUtil.Mat("M_Portal" + i + "_Glow", look.glowIdle, unlit: true);
            var glowGo = NerfedEditorUtil.Primitive(PrimitiveType.Cube, "Glow", root, new Vector3(0f, 1.6f, 0f), new Vector3(2f, 3.2f, 0.08f), glowM, keepCollider: false);
            glowGo.GetComponent<MeshRenderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;

            switch (i)
            {
                case 1: SummerDecor(root); break;
                case 2: AutumnDecor(root); break;
                case 3: DigitalDecor(root, look.glowIdle); break;
            }

            // deasupra grinzii, in fata ramei
            var tmp = NerfedEditorUtil.WorldText(root, "Prompt", Labels[i], new Vector3(0f, 4.6f, -1.6f), 5f,
                                                 new Color(0.15f, 0.14f, 0.18f), FontStyles.Bold, new Vector2(9f, 3f));

            var portal = root.gameObject.AddComponent<Portal>();
            portal.levelIndex = i;
            portal.levelScene = "Level" + i;
            portal.glow = glowGo.GetComponent<Renderer>();
            portal.prompt = tmp;
            portal.promptOffset = new Vector3(0f, 4.6f, -1.6f);
            portal.idleColor = look.glowIdle;
            portal.nearColor = look.glowNear;
        }

        var player = Object.FindFirstObjectByType<OverworldController>();
        if (player != null && player.GetComponent<AbilityLoader>() == null)
            player.gameObject.AddComponent<AbilityLoader>();
        if (player != null && player.GetComponent<OverworldSpawn>() == null)
            player.gameObject.AddComponent<OverworldSpawn>();   // la intoarcerea dintr-un nivel: apari la portal

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Debug.Log("[Mochini] Portaluri adaugate in open world. Testeaza blocajele cu Mochini Bouncelini > Debug.");
    }

    static GameObject Box(Transform root, string name, Vector3 pos, Vector3 size, Material m)
    {
        var go = NerfedEditorUtil.Primitive(PrimitiveType.Cube, name, root, pos, size, m, keepCollider: false);
        return go;
    }

    /// <summary>Vara: capac de iarba pe grinda, fire care atarna, flori, iedera pe stalpi.</summary>
    static void SummerDecor(Transform root)
    {
        Color a = new Color(0.58f, 0.82f, 0.30f), b = new Color(0.47f, 0.73f, 0.26f), hang = new Color(0.40f, 0.64f, 0.22f);
        Material ma = NerfedEditorUtil.Mat("M_Portal1_Grass", a), mb = NerfedEditorUtil.Mat("M_Portal1_GrassB", b), mh = NerfedEditorUtil.Mat("M_Portal1_Hang", hang);
        Box(root, "GrassCap", new Vector3(0f, 3.76f, 0f), new Vector3(3.5f, 0.14f, 0.8f), ma);
        var rng = new System.Random(11);
        for (int n = 0; n < 14; n++)
        {
            float x = -1.6f + (float)rng.NextDouble() * 3.2f, l = 0.2f + (float)rng.NextDouble() * 0.45f;
            float z = rng.NextDouble() < 0.5 ? -0.42f : 0.42f;
            Box(root, "Strand", new Vector3(x, 3.7f - l * 0.5f, z), new Vector3(0.12f, l, 0.06f), rng.NextDouble() < 0.6 ? mh : mb);
        }
        Color[] flowers = { new Color(1f, 0.92f, 0.35f), new Color(1f, 0.55f, 0.70f), new Color(0.95f, 0.95f, 1f) };
        for (int n = 0; n < 5; n++)
        {
            Color fc = flowers[n % flowers.Length];
            var at = new Vector3(-1.4f + n * 0.7f, 3.85f, (n % 2 == 0 ? -0.15f : 0.2f));
            Box(root, "Stem", at + Vector3.up * 0.08f, new Vector3(0.04f, 0.16f, 0.04f), mh);
            Box(root, "Flower", at + Vector3.up * 0.2f, Vector3.one * 0.14f, NerfedEditorUtil.Mat("M_Flower_" + ColorUtility.ToHtmlStringRGB(fc), fc));
        }
        // iedera pe stalpi
        foreach (float x in new[] { -1.62f, 1.62f })
            for (int n = 0; n < 6; n++)
                Box(root, "Ivy", new Vector3(x, 3.1f - n * 0.42f, -0.31f + (n % 2) * 0.05f), new Vector3(0.16f, 0.2f, 0.05f), n % 2 == 0 ? ma : mh);
    }

    /// <summary>Toamna: coroana de frunze peste grinda, frunze care atarna si frunze cazute la baza.</summary>
    static void AutumnDecor(Transform root)
    {
        Color[] leaves = { new Color(0.93f, 0.48f, 0.16f), new Color(0.80f, 0.26f, 0.16f), new Color(0.96f, 0.72f, 0.22f), new Color(0.88f, 0.38f, 0.14f) };
        var rng = new System.Random(22);
        for (int n = 0; n < 12; n++)
        {
            Color c = leaves[rng.Next(leaves.Length)];
            var at = new Vector3(-1.7f + (float)rng.NextDouble() * 3.4f, 3.75f + (float)rng.NextDouble() * 0.6f, -0.35f + (float)rng.NextDouble() * 0.7f);
            Box(root, "Leaves", at, Vector3.one * (0.45f + (float)rng.NextDouble() * 0.35f), NerfedEditorUtil.Mat("M_Leaves_" + ColorUtility.ToHtmlStringRGB(c), c));
        }
        for (int n = 0; n < 8; n++)
        {
            Color c = leaves[rng.Next(leaves.Length)];
            var at = new Vector3(-1.6f + (float)rng.NextDouble() * 3.2f, 3.1f - (float)rng.NextDouble() * 0.4f, -0.42f);
            Box(root, "HangLeaf", at, new Vector3(0.2f, 0.16f, 0.04f), NerfedEditorUtil.Mat("M_Leaves_" + ColorUtility.ToHtmlStringRGB(c), c));
        }
        for (int n = 0; n < 10; n++)
        {
            Color c = leaves[rng.Next(leaves.Length)];
            var at = new Vector3(-1.8f + (float)rng.NextDouble() * 3.6f, 0.11f, -1.2f + (float)rng.NextDouble() * 1.2f);
            Box(root, "FallenLeaf", at, new Vector3(0.24f, 0.03f, 0.18f), NerfedEditorUtil.Mat("M_Leaves_" + ColorUtility.ToHtmlStringRGB(c), c))
                .transform.localRotation = Quaternion.Euler(0f, (float)rng.NextDouble() * 90f, 0f);
        }
    }

    /// <summary>Digital: neon pe muchiile stalpilor si ale grinzii, beculete, cuburi de date care plutesc.</summary>
    static void DigitalDecor(Transform root, Color neon)
    {
        Material nm = NerfedEditorUtil.Mat("M_Portal3_Neon", neon, unlit: true);
        Color pink = new Color(1f, 0.35f, 0.85f);
        Material pm = NerfedEditorUtil.Mat("M_Portal3_Pink", pink, unlit: true);
        foreach (float px in new[] { -1.3f, 1.3f })
            foreach (var c in new[] { new Vector2(-0.31f, -0.31f), new Vector2(0.31f, -0.31f), new Vector2(-0.31f, 0.31f), new Vector2(0.31f, 0.31f) })
                Box(root, "NeonEdge", new Vector3(px + c.x, 1.6f, c.y), new Vector3(0.05f, 3.2f, 0.05f), nm);
        Box(root, "NeonBeamTop", new Vector3(0f, 3.71f, -0.36f), new Vector3(3.45f, 0.05f, 0.05f), nm);
        Box(root, "NeonBeamBottom", new Vector3(0f, 3.19f, -0.36f), new Vector3(3.45f, 0.05f, 0.05f), pm);
        var rng = new System.Random(33);
        for (int n = 0; n < 8; n++)
            Box(root, "Led", new Vector3((n < 4 ? -1.3f : 1.3f) + (float)(rng.NextDouble() * 0.3 - 0.15), 0.6f + (n % 4) * 0.65f, -0.32f),
                new Vector3(0.1f, 0.08f, 0.03f), rng.NextDouble() < 0.6 ? nm : pm);
        for (int n = 0; n < 6; n++)
        {
            var go = Box(root, "DataCube", new Vector3(-1.8f + (float)rng.NextDouble() * 3.6f, 4.0f + (float)rng.NextDouble() * 1.2f, -0.4f + (float)rng.NextDouble() * 0.8f),
                Vector3.one * (0.12f + (float)rng.NextDouble() * 0.16f), rng.NextDouble() < 0.6 ? nm : pm);
            go.GetComponent<MeshRenderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        }
    }
}
