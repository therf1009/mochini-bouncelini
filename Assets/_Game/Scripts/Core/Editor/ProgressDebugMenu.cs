using UnityEditor;
using UnityEngine;

/// <summary>
/// Meniu NERFED > Debug: deblocheaza abilitati fara sa joci nivelurile (care inca nu exista),
/// ca sa verifici ca groapa cere dash si faleza cere double jump. Merge si in Play mode.
/// </summary>
public static class ProgressDebugMenu
{
    [MenuItem("NERFED/Debug/Complete Level 1 (dash)")]
    static void CompleteLevel1() => Complete(1);

    [MenuItem("NERFED/Debug/Complete Level 2 (double jump)")]
    static void CompleteLevel2() => Complete(2);

    [MenuItem("NERFED/Debug/Reset Progress (+ tutorial again)")]
    static void ResetProgress()
    {
        GameProgress.ResetAll();
        Refresh();
        Debug.Log("[NERFED] Progres resetat. Title screen-ul te duce iar in tutorial.");
    }

    static void Complete(int level)
    {
        GameProgress.CompleteLevel(level);
        Refresh();
        Debug.Log("[NERFED] Nivelul " + level + " marcat terminat, abilitate: " + GameProgress.RewardFor(level));
    }

    /// <summary>In Play mode, aplica imediat abilitatile pe mochi.</summary>
    static void Refresh()
    {
        if (!Application.isPlaying) return;
        foreach (var loader in Object.FindObjectsByType<AbilityLoader>(FindObjectsSortMode.None))
            loader.Apply();
    }
}
