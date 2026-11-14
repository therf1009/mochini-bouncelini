using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using static LevelBuilderKit;

/// <summary>
/// Meniu: NERFED > Levels > Build Level 3 (the end). Scena "Level3" = BUILD v1.2, ultimul nivel.
///
/// stand -> groapa sub tavan (dash) -> zid de 6 (double jump) -> REVERT: Bounce Back
/// -> turela care incuie zidul "deprecated" (ii trimiti un patch inapoi) -> arena Directorului
/// -> trei patch-uri trimise inapoi -> finalul tragic -> title screen.
///
/// Zidul cu turela are 9.5 deasupra marginii: peste el nu se trece nici cu charge + double jump (~7.1).
/// Arena e din blocuri de 1x1 unite intr-un CompositeCollider2D, ca finalul sa le stearga pe rand.
/// </summary>
public static class Level3Builder
{
    const string ScenePath = NerfedEditorUtil.ScenesDir + "/Level3.unity";

    [MenuItem("NERFED/Levels/Build Level 3 (the end)")]
    public static void Build()
    {
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
        var scene = NewScene();

        Material ground = NerfedEditorUtil.Mat("M_L3Ground", new Color(0.42f, 0.44f, 0.50f));
        Material npc = NerfedEditorUtil.Mat("M_NpcQA", new Color(0.55f, 0.78f, 0.62f));
        Material gateMat = NerfedEditorUtil.Mat("M_Gate", new Color(0.75f, 0.30f, 0.30f));
        Material turretMat = NerfedEditorUtil.Mat("M_Turret", new Color(0.30f, 0.30f, 0.34f));
        Material directorMat = NerfedEditorUtil.Mat("M_Director", new Color(0.20f, 0.21f, 0.24f));
        Material screenMat = NerfedEditorUtil.Mat("M_DirectorScreen", new Color(0.85f, 0.88f, 0.92f), unlit: true);
        Material redEye = NerfedEditorUtil.Mat("M_EyeRed", new Color(0.95f, 0.25f, 0.20f), unlit: true);
        Material patchMat = NerfedEditorUtil.Mat("M_Patch", new Color(0.95f, 0.40f, 0.30f), unlit: true);
        Material reflectedMat = NerfedEditorUtil.Mat("M_PatchReflected", new Color(0.40f, 0.90f, 1.00f), unlit: true);
        Color background = new Color(0.16f, 0.17f, 0.20f);
        Color light = new Color(0.90f, 0.90f, 0.92f);

        Transform level = new GameObject("Level").transform;

        // ---- start, groapa sub tavan (dash), zid de 6 (double jump)
        Box(level, ground, "Wall_Start", -3f, -2f, -1f, 14f);
        Box(level, ground, "Floor_A", -2f, 12f, -1f, 0f);
        Box(level, ground, "Overhang", 9f, 18f, 1.2f, 9f);
        Box(level, ground, "Floor_B", 16f, 26f, -2.5f, -1.5f);
        Box(level, ground, "Ledge_C", 26f, 41f, -2.5f, 4.5f);

        // ---- zidul "deprecated", incuiat de turela de pe fata lui
        var gate = Box(level, gateMat, "Gate", 39f, 40f, 4.5f, 14f).AddComponent<BreakableGate>();

        var turretRoot = new GameObject("PatchTurret").transform;
        turretRoot.SetParent(level, false);
        turretRoot.localPosition = new Vector3(38.4f, 9f, 0f);
        NerfedEditorUtil.Primitive(PrimitiveType.Cube, "Body", turretRoot, Vector3.zero, new Vector3(0.8f, 0.8f, 1f), turretMat, keepCollider: false);
        NerfedEditorUtil.Primitive(PrimitiveType.Cube, "Barrel", turretRoot, new Vector3(-0.5f, 0f, 0f), new Vector3(0.5f, 0.25f, 0.25f), turretMat, keepCollider: false);
        var muzzle = new GameObject("Muzzle").transform;
        muzzle.SetParent(turretRoot, false);
        muzzle.localPosition = new Vector3(-0.8f, 0f, 0f);
        var turret = turretRoot.gameObject.AddComponent<PatchTurret>();
        turret.gate = gate;
        turret.muzzle = muzzle;
        turret.hitRadius = 1f;
        turret.projectileSpeed = 6f;
        turret.projectileMat = patchMat;
        turret.reflectedMat = reflectedMat;

        // ---- arena: blocuri de 1x1 unite intr-un singur collider (finalul le sterge pe rand)
        var arena = new GameObject("Arena");
        arena.AddComponent<Rigidbody2D>().bodyType = RigidbodyType2D.Static;
        arena.AddComponent<CompositeCollider2D>().geometryType = CompositeCollider2D.GeometryType.Polygons;
        for (int x = 41; x < 69; x++) Tile(arena.transform, ground, x, 3.5f);       // podeaua, sus la 4.5
        for (int y = 0; y < 11; y++) Tile(arena.transform, ground, 69, 4.5f + y);   // peretele din dreapta

        // ---- Directorul
        var bossRoot = new GameObject("Director").transform;
        bossRoot.localPosition = new Vector3(63f, 10.5f, 0f);
        NerfedEditorUtil.Primitive(PrimitiveType.Cube, "Body", bossRoot, Vector3.zero, new Vector3(3f, 3f, 1.2f), directorMat, keepCollider: false);
        var screen = NerfedEditorUtil.Primitive(PrimitiveType.Cube, "Screen", bossRoot, new Vector3(0f, 0.25f, -0.66f), new Vector3(2.4f, 1.6f, 0.1f), screenMat, keepCollider: false);
        NerfedEditorUtil.Primitive(PrimitiveType.Cube, "EyeL", bossRoot, new Vector3(-0.55f, 0.4f, -0.72f), new Vector3(0.35f, 0.35f, 0.1f), redEye, keepCollider: false);
        NerfedEditorUtil.Primitive(PrimitiveType.Cube, "EyeR", bossRoot, new Vector3(0.55f, 0.4f, -0.72f), new Vector3(0.35f, 0.35f, 0.1f), redEye, keepCollider: false);
        var nameGo = new GameObject("Name");
        nameGo.transform.SetParent(bossRoot, false);
        var bossName = nameGo.AddComponent<TextMeshPro>();
        bossName.text = "DIRECTOR";
        bossName.fontSize = 3f;
        bossName.fontStyle = FontStyles.Bold;
        bossName.color = light;
        bossName.alignment = TextAlignmentOptions.Center;
        bossName.rectTransform.sizeDelta = new Vector2(3f, 0.6f);
        nameGo.transform.localPosition = new Vector3(0f, -1.1f, -0.7f);

        var boss = bossRoot.gameObject.AddComponent<DirectorBoss>();
        boss.activateX = 44f;
        boss.hitRadius = 1.8f;
        boss.projectileSpeed = 6f;
        boss.projectileMat = patchMat;
        boss.reflectedMat = reflectedMat;
        boss.screen = screen.GetComponent<Renderer>();

        var ending = new GameObject("EndingSequence").AddComponent<EndingSequence>();
        ending.boss = boss;
        ending.crumbleRoot = arena.transform;

        // ---- stand, REVERT
        Stand(level, 5f, 0f, "QA_Tester_04", new[]
        {
            "you made it to v1.2. the last build.",
            "the Director is shipping the final patch. it seals everything. no more nerfs. no more reverts.",
            "the last REVERT is just ahead. it lets you bounce a patch back at whoever sent it.",
            "...if you take the Director down, nobody balances this world anymore. i don't know what happens then.",
            "go anyway."
        }, "find the last REVERT", npc, light);

        Revert(level, new Vector2(29f, 5.1f), Ability.Third, "PATCH 1.2.0  -  REVERTED",
            new[] { "Bounce Back restored (F near a patch).", "Returning patches to sender is now supported." },
            "break the turret's lock", light);

        // ---- texte
        Prompt(level, "~ BUILD v1.2 ~", new Vector2(1f, 6f), -2f, light, 8f);
        Prompt(level, "SHIFT   -   dash", new Vector2(13.5f, 4.5f), 2f, light, 5f);
        Prompt(level, "charge straight up,\nthen SPACE again at the top", new Vector2(21f, 6.5f), -2f, light, 5f);
        Prompt(level, "F   -   bounce a patch back", new Vector2(33f, 8f), 2f, light, 5f);

        // ---- checkpoint-uri
        Checkpoint(level, new Vector2(1f, 0f));
        Checkpoint(level, new Vector2(19.5f, -1.5f));
        Checkpoint(level, new Vector2(27.5f, 4.5f));
        Checkpoint(level, new Vector2(42.5f, 4.5f));

        // ---- mochi (cu Bounce Back, blocat pana la REVERT) si camera
        var player = Mochi(new Vector2(0.5f, 0.6f), -8f);
        player.AddComponent<BounceBack>();
        CameraRig(player.transform, background, 6f, 60f, 1f, 13f);

        Save(scene, ScenePath);
        Debug.Log("[NERFED] Level 3 construit: " + ScenePath + "   |   Are nevoie de dash + double jump (Debug > Complete Level 1/2).");
    }

    static void Tile(Transform parent, Material mat, float x, float y)
    {
        var go = NerfedEditorUtil.Primitive(PrimitiveType.Cube, "Tile", parent,
            new Vector3(x + 0.5f, y + 0.5f, 0f), new Vector3(1f, 1f, 1.6f), mat, keepCollider: false);
        go.AddComponent<BoxCollider2D>().compositeOperation = Collider2D.CompositeOperation.Merge;
    }
}
