using UnityEngine;

/// <summary>
/// Aplica abilitatile castigate pe controllerul de pe acelasi obiect.
/// Merge si pe mochi-ul din open world, si pe cel din niveluri (double jump).
/// </summary>
public class AbilityLoader : MonoBehaviour
{
    void Start() => Apply();

    public void Apply()
    {
        var overworld = GetComponent<OverworldController>();
        if (overworld != null)
        {
            overworld.canDash = GameProgress.Has(Ability.Dash);
            overworld.canDoubleJump = GameProgress.Has(Ability.DoubleJump);
        }

        var mochi = GetComponent<MochiController>();
        if (mochi != null)
            mochi.doubleBounceEnabled = GameProgress.Has(Ability.DoubleJump);
    }
}
