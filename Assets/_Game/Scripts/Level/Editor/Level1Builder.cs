using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using static LevelBuilderKit;

/// <summary>
/// Meniu: NERFED > Levels > Build Level 1 (dash). Scena "Level1" = BUILD v1.0.
///
/// stand (discutie, scop) -> platforme cu hop -> zid de 3 cu charge -> REVERT: dash
/// -> GROAPA SUB TAVAN JOS -> steag.
///
/// Blocajul de dash: deasupra marginii e un tavan la 1.2 de sol (mochi are 1), deci nu poti sari,
/// iar groapa de 4 are aterizarea cu 1.5 mai jos. Mers simplu: cazi dupa ~1.8. Dash (~2.9 drept
/// + ~3 cat cazi cei 1.5) trece. Peste tavan nu ajungi: e inalt pana la 16.
/// </summary>
public static class Level1Builder
{
    const string ScenePath = NerfedEditorUtil.ScenesDir + "/Level1.unity";

    [MenuItem("NERFED/Levels/Build Level 1 (dash)")]
    public static void Build()
    {
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
        var scene = NewScene();

        Material ground = NerfedEditorUtil.Mat("M_L1Ground", new Color(0.86f, 0.58f, 0.38f));
        Material npc = NerfedEditorUtil.Mat("M_NpcQA", new Color(0.55f, 0.78f, 0.62f));
        Color background = new Color(0.99f, 0.93f, 0.82f);

        Transform level = new GameObject("Level").transform;

        // ---- start + platforme cu hop (treapta de 1.5, goluri de 2)
        Box(level, ground, "Wall_Start", -3f, -2f, -1f, 12f);
        Box(level, ground, "Floor_A", -2f, 12f, -1f, 0f);
        Box(level, ground, "Plat_1", 14f, 17f, 0.5f, 1.5f);
        Box(level, ground, "Plat_2", 19f, 22f, 2f, 3f);
        Box(level, ground, "Plat_3", 24f, 27f, 3.5f, 4.5f);
        Box(level, ground, "Ledge_B", 29f, 36f, 3.5f, 4.5f);

        // ---- zid de 3 (charge), apoi marginea pe care sta REVERT-ul
        Box(level, ground, "Ledge_C", 36f, 48f, 3.5f, 7.5f);

        // ---- blocajul de dash: tavan jos peste capatul marginii si peste groapa (x 48..52)
        Box(level, ground, "Overhang", 45f, 55f, 8.7f, 16f);
        Box(level, ground, "Ledge_D", 52f, 64f, 5f, 6f);
        Box(level, ground, "Wall_End", 64f, 65f, 6f, 18f);

        // ---- stand, REVERT, steag
        Stand(level, 5f, 0f, "QA_Tester_04", new[]
        {
            "oh. a player. hi. i'm QA, i test things.",
            "bad news: patch 1.0.4 removed your DASH. 'underused', apparently.",
            "good news: a REVERT token is hidden at the top of this build. it rolls the dash back.",
            "grab it, then reach the flag. the gap near the end is... very dash-shaped."
        }, "find the REVERT token", npc, Ink);

        Revert(level, new Vector2(43f, 8.1f), Ability.Dash, "PATCH 1.0.4  -  REVERTED",
            new[] { "Dash restored (SHIFT).", "Rollback due to player backlash." }, "reach the flag", Ink);

        Flag(level, 61f, 6f, 1);

        // ---- texte
        Prompt(level, "~ BUILD v1.0 ~", new Vector2(1f, 6f), -2f, Ink, 8f);
        Prompt(level, "a wall?  charge.", new Vector2(32.5f, 7.5f), 2f, Ink);
        Prompt(level, "SHIFT   -   dash", new Vector2(50f, 7.4f), -2f, Ink, 5f);

        // ---- checkpoint-uri
        Checkpoint(level, new Vector2(1f, 0f));
        Checkpoint(level, new Vector2(10f, 0f));
        Checkpoint(level, new Vector2(30f, 4.5f));
        Checkpoint(level, new Vector2(37.5f, 7.5f));
        Checkpoint(level, new Vector2(44f, 7.5f));
        Checkpoint(level, new Vector2(53.5f, 6f));

        var player = Mochi(new Vector2(0.5f, 0.6f), -5f);
        CameraRig(player.transform, background, 6f, 58f, 3f, 12f);

        Save(scene, ScenePath);
        Debug.Log("[NERFED] Level 1 construit: " + ScenePath + "   |   Play din open world (portalul 1) sau direct din scena.");
    }
}
