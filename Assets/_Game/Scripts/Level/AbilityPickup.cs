using System.Collections;
using UnityEngine;

/// <summary>
/// Tokenul REVERT: cand mochi-ul il atinge, primeste abilitatea pe loc (pentru nivelul curent),
/// apare patch note-ul de "revert" si obiectivul se schimba.
/// Abilitatea ramane si dupa nivel abia cand nivelul e terminat (LevelGoal -> GameProgress).
/// Pentru abilitati care nu sunt in MochiController, trimite mesajul OnAbilityUnlocked(Ability) pe mochi.
///
/// Cu `cinematic`, e un moment mare: timpul incetineste, camera se apropie, mochi straluceste, explozie de
/// particule si un banner mare pe mijlocul ecranului (BigBanner), cu tasta noii abilitati.
/// In camerele rotative, atingerea se verifica pe ecran (ca in Fez), din orice unghi.
/// </summary>
public class AbilityPickup : MonoBehaviour
{
    public Ability ability = Ability.Dash;
    public string patchHeader = "PATCH 1.0.4  -  REVERTED";
    public string[] patchLines = { "Dash restored.", "Rollback due to player backlash." };
    public string goalAfter = "reach the flag";
    public float radius = 1f;
    public Transform visual;

    [Header("Momentul mare")]
    public bool cinematic = true;
    public string bannerTitle = "DASH RESTORED";
    public string bannerKey = "SHIFT";
    public Color burstColor = new Color(0.35f, 0.9f, 1f);

    Transform player;
    Vector3 basePos;
    bool taken;

    /// <summary>Anunta ca o abilitate a fost luata (ex. peretii glitch se aprind cand vine dash-ul).</summary>
    public static event System.Action<Ability> Granted;

    void Start()
    {
        var mochi = FindAnyObjectByType<MochiController>();
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

        if (player != null && Vector2.Distance(player.position, FezRoom.Screen(transform.position)) <= radius)
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
            if (ability == Ability.WallStick) mochi.wallStickEnabled = true;
        }
        player.SendMessage("OnAbilityUnlocked", ability, SendMessageOptions.DontRequireReceiver);
        Granted?.Invoke(ability);
        if (ability == Ability.Dash) DashPhase.Awaken();

        if (cinematic) { StartCoroutine(Moment()); return; }

        PatchNoteUI.Show(patchHeader, patchLines);
        if (!string.IsNullOrEmpty(goalAfter)) PatchNoteUI.SetGoal(goalAfter);
        CameraShake.Shake(0.25f);
        gameObject.SetActive(false);
    }

    IEnumerator Moment()
    {
        // tokenul dispare imediat, dar obiectul ramane activ cat ruleaza momentul
        foreach (var r in GetComponentsInChildren<Renderer>()) r.enabled = false;

        bool wasLocked = FezRotator.Locked;
        FezRotator.Locked = true;
        var cam = Camera.main;
        float baseSize = cam != null && cam.orthographic ? cam.orthographicSize : 6.5f;
        Fx.Burst(FezRoom.World(player.position), burstColor, 40, 7f);
        Fx.Glow(player.GetComponent<MochiController>()?.visual, burstColor, 1.4f);
        BigBanner.Show(patchHeader, bannerTitle, bannerKey, 3.2f);
        CameraShake.Shake(0.35f);

        // incetinire + apropiere, apoi revenire lina (in timp real: jocul e aproape oprit)
        float old = Time.timeScale > 0f ? Time.timeScale : 1f;
        for (float t = 0f; t < 1.6f; t += Time.unscaledDeltaTime)
        {
            float k = t < 0.25f ? t / 0.25f : (t > 1.2f ? 1f - (t - 1.2f) / 0.4f : 1f);
            Time.timeScale = Mathf.Lerp(old, 0.15f, k);
            if (cam != null && cam.orthographic) cam.orthographicSize = Mathf.Lerp(baseSize, baseSize * 0.68f, Mathf.SmoothStep(0f, 1f, k));
            yield return null;
        }
        Time.timeScale = old;
        if (cam != null && cam.orthographic) cam.orthographicSize = baseSize;
        FezRotator.Locked = wasLocked;

        PatchNoteUI.Show(patchHeader, patchLines);
        if (!string.IsNullOrEmpty(goalAfter)) PatchNoteUI.SetGoal(goalAfter);
        gameObject.SetActive(false);
    }
}
