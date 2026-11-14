using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using static LevelBuilderKit;

/// <summary>
/// Meniu: NERFED > Levels > Build Level 2 (double jump). Scena "Level2" = BUILD v1.1.
///
/// stand -> groapa sub tavan (dash, abilitatea de la nivelul 1) -> "ventilatii" urcate cu charge
/// -> REVERT: double jump -> ZID DE 6 -> steag.
///
/// Blocajul de double jump: zidul are 6. Charge drept in sus urca ~5.2 (nu ajunge),
/// charge + al doilea salt din varf ~7.1 (ajunge). Hop + al doilea salt ~3.9 (nu ajunge).
/// Fara al doilea perete in apropiere, deci nu se poate urca nici din wall bounce.
/// </summary>
public static class Level2Builder
{
    const string ScenePath = NerfedEditorUtil.ScenesDir + "/Level2.unity";

    [MenuItem("NERFED/Levels/Build Level 2 (double jump)")]
    public static void Build()
    {
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
        var scene = NewScene();

        Material ground = NerfedEditorUtil.Mat("M_L2Ground", new Color(0.36f, 0.56f, 0.76f));
        Material npc = NerfedEditorUtil.Mat("M_NpcCM", new Color(0.92f, 0.55f, 0.70f));
        Color background = new Color(0.86f, 0.92f, 0.98f);

        Transform level = new GameObject("Level").transform;

        // ---- start + groapa sub tavan jos (acelasi tipar ca la nivelul 1: se trece doar cu dash)
        Box(level, ground, "Wall_Start", -3f, -2f, -1f, 14f);
        Box(level, ground, "Floor_A", -2f, 14f, -1f, 0f);
        Box(level, ground, "Overhang", 11f, 20f, 1.2f, 9f);
        Box(level, ground, "Floor_B", 18f, 26f, -2.5f, -1.5f);

        // ---- ventilatii: trepte de 3 (charge), goluri de 2
        Box(level, ground, "Vent_1", 28f, 31f, 0.5f, 1.5f);
        Box(level, ground, "Vent_2", 33f, 36f, 3.5f, 4.5f);
        Box(level, ground, "Ledge_E", 38f, 46f, 3.5f, 4.5f);

        // ---- zidul de 6 (double jump) si marginea de sus
        Box(level, ground, "Ledge_F", 46f, 60f, -2.5f, 10.5f);
        Box(level, ground, "Wall_End", 60f, 61f, 10.5f, 24f);

        // ---- stand, REVERT, steag
        Stand(level, 5f, 0f, "Community_Manager", new[]
        {
            "welcome to v1.1! we listened to all of your feedback.",
            "then patch 1.1.2 removed the DOUBLE JUMP. 'confusing for new players'.",
            "its REVERT is up in the vents. the exit is higher than any single jump.",
            "...charge straight up, then jump again at the top. you didn't hear it from me."
        }, "find the REVERT token", npc, Ink);

        Revert(level, new Vector2(34.5f, 5.1f), Ability.DoubleJump, "PATCH 1.1.2  -  REVERTED",
            new[] { "Double jump restored (SPACE in the air).", "We hear you." }, "climb to the flag", Ink);

        Flag(level, 56f, 10.5f, 2);

        // ---- texte
        Prompt(level, "~ BUILD v1.1 ~", new Vector2(1f, 6f), -2f, Ink, 8f);
        Prompt(level, "SHIFT   -   dash", new Vector2(15.5f, 4.5f), 2f, Ink, 5f);
        Prompt(level, "charge up the vents", new Vector2(30.5f, 6f), -2f, Ink, 5f);
        Prompt(level, "charge straight up,\nthen SPACE again at the top", new Vector2(41.5f, 8.3f), 2f, Ink, 5f);

        // ---- checkpoint-uri
        Checkpoint(level, new Vector2(1f, 0f));
        Checkpoint(level, new Vector2(19.5f, -1.5f));
        Checkpoint(level, new Vector2(29.5f, 1.5f));
        Checkpoint(level, new Vector2(38.5f, 4.5f));
        Checkpoint(level, new Vector2(47.5f, 10.5f));

        var player = Mochi(new Vector2(0.5f, 0.6f), -7f);
        CameraRig(player.transform, background, 6f, 54f, 1f, 14f);

        Save(scene, ScenePath);
        Debug.Log("[NERFED] Level 2 construit: " + ScenePath + "   |   Are nevoie de dash (Debug > Complete Level 1 daca testezi direct).");
    }
}
