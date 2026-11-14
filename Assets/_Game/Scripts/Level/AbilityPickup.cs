using UnityEngine;

/// <summary>
/// Tokenul REVERT: cand mochi-ul il atinge, primeste abilitatea pe loc (pentru nivelul curent),
/// apare patch note-ul de "revert" si obiectivul se schimba.
/// Abilitatea ramane si dupa nivel abia cand nivelul e terminat (LevelGoal -> GameProgress).
/// Pentru abilitati care nu sunt in MochiController, trimite mesajul OnAbilityUnlocked(Ability) pe mochi.
/// </summary>
public class AbilityPickup : MonoBehaviour
{
    public Ability ability = Ability.Dash;
    public string patchHeader = "PATCH 1.0.4  -  REVERTED";
    public string[] patchLines = { "Dash restored.", "Rollback due to player backlash." };
    public string goalAfter = "reach the flag";
    public float radius = 1f;
    public Transform visual;

    Transform player;
    Vector3 basePos;
    bool taken;

    void Start()
    {
        var mochi = FindFirstObjectByType<MochiController>();
        if (mochi != null) player = mochi.transform;
        if (visual == null) visual = transform;
        basePos = visual.localPosition;
    }

    void Update()
    {
        if (taken) return;

        float t = Time.time;
        visual.localPosition = basePos + Vector3.up * (Mathf.Sin(t * 2.5f) * 0.15f);
        visual.localRotation = Quaternion.Euler(0f, t * 120f, 45f);

        if (player != null && Vector2.Distance(player.position, transform.position) <= radius)
            Grant();
    }

    void Grant()
    {
        taken = true;

        var mochi = player.GetComponent<MochiController>();
        if (mochi != null)
        {
            if (ability == Ability.Dash) mochi.dashEnabled = true;
            if (ability == Ability.DoubleJump) mochi.doubleBounceEnabled = true;
        }
        player.SendMessage("OnAbilityUnlocked", ability, SendMessageOptions.DontRequireReceiver);

        PatchNoteUI.Show(patchHeader, patchLines);
        if (!string.IsNullOrEmpty(goalAfter)) PatchNoteUI.SetGoal(goalAfter);
        CameraShake.Shake(0.25f);
        gameObject.SetActive(false);
    }
}
