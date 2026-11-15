using UnityEngine;

/// <summary>
/// Turela de patch-uri: cand mochi-ul e in raza, arunca periodic patch-uri spre el.
/// E "lacatul" unui zid: un patch trimis inapoi o distruge, iar zidul se sparge odata cu ea.
/// </summary>
public class PatchTurret : PatchSource
{
    public float fireInterval = 1.6f;
    public float range = 9f;
    public Transform muzzle;
    public BreakableGate gate;
    public string goalAfter = "reach the Director";

    Transform player;
    float timer;
    bool dead;

    void Start()
    {
        player = FindPlayer();
        timer = fireInterval;
    }

    void Update()
    {
        if (dead || player == null || Time.deltaTime <= 0f) return;
        if (Vector2.Distance(player.position, ScreenPosition) > range) return;

        timer -= Time.deltaTime;
        if (timer > 0f) return;
        timer = fireInterval;

        Vector2 from = muzzle != null ? FezRoom.Screen(muzzle.position) : ScreenPosition;
        Fire(from, player.position, projectileSpeed);
    }

    public override void OnReflectedHit(PatchProjectile projectile)
    {
        if (dead) return;
        dead = true;
        CameraShake.Shake(0.4f);
        PatchNoteUI.Show("HOTFIX 1.2.0a", "Removed: patch turret.", "Removed: deprecated wall.");
        if (!string.IsNullOrEmpty(goalAfter)) PatchNoteUI.SetGoal(goalAfter);
        if (gate != null) gate.Break();
        gameObject.SetActive(false);
    }
}
