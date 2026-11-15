using UnityEngine;

public enum Ability
{
    Dash,        // nivelul 1
    DoubleJump,  // nivelul 2
    Third,       // nivelul 3: Bounce Back, acum ca PARRY (click). Jocul se termina acolo.
    WallStick    // nivelul 3: te lipesti de perete tinand SPACE, dai drumul si te lansezi
}

/// <summary>
/// Progresul jucatorului, salvat intre rulari (PlayerPrefs):
/// ce abilitati are, ce niveluri a terminat, prin ce portal a iesit ultima data.
/// </summary>
public static class GameProgress
{
    const string Prefix = "nerfed_";

    public static bool Has(Ability ability) => PlayerPrefs.GetInt(Prefix + "ability_" + ability, 0) == 1;

    public static void Unlock(Ability ability)
    {
        PlayerPrefs.SetInt(Prefix + "ability_" + ability, 1);
        PlayerPrefs.Save();
    }

    public static bool IsLevelDone(int level) => PlayerPrefs.GetInt(Prefix + "level_done_" + level, 0) == 1;

    /// <summary>Abilitatea primita la finalul fiecarui nivel.</summary>
    public static Ability RewardFor(int level)
    {
        switch (level)
        {
            case 1: return Ability.Dash;
            case 2: return Ability.DoubleJump;
            default: return Ability.Third;
        }
    }

    /// <summary>Apelat la finalul unui nivel: il marcheaza terminat si da abilitatea.</summary>
    public static void CompleteLevel(int level)
    {
        PlayerPrefs.SetInt(Prefix + "level_done_" + level, 1);
        Unlock(RewardFor(level));
    }

    public static int LastPortal
    {
        get => PlayerPrefs.GetInt(Prefix + "last_portal", 0);
        set { PlayerPrefs.SetInt(Prefix + "last_portal", value); PlayerPrefs.Save(); }
    }

    /// <summary>Sterge tot: abilitati, niveluri, si flag-ul de prima rulare (revine tutorialul).</summary>
    public static void ResetAll()
    {
        foreach (Ability a in System.Enum.GetValues(typeof(Ability)))
            PlayerPrefs.DeleteKey(Prefix + "ability_" + a);
        for (int i = 1; i <= 3; i++)
            PlayerPrefs.DeleteKey(Prefix + "level_done_" + i);
        PlayerPrefs.DeleteKey(Prefix + "last_portal");
        PlayerPrefs.DeleteKey(Prefix + "played");
        PlayerPrefs.Save();
    }
}
