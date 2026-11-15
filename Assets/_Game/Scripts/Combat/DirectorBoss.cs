using System.Collections;
using UnityEngine;

/// <summary>
/// DIRECTOR.exe - boss-ul final: un robotel cu ecran in loc de fata, care pluteste deasupra arenei (acoperisul
/// serverului). Cand mochi pune piciorul in arena, se "porneste": rotirea lumii se blocheaza ("firewall") si incepe lupta.
///
/// Singurul mod de a-l lovi: patch-urile lui, trimise inapoi cu parry-ul Bounce Back (click, chiar inainte sa te loveasca). Sase lovituri, trei faze:
///   faza 1 - un patch tintit, din cand in cand;
///   faza 2 - evantai de trei patch-uri si se plimba mai repede de colo-colo;
///   faza 3 - rafale + "unde de date" pe podea: pe alea scutul NU le opreste, sari peste ele (sau lipeste-te de perete).
/// Lovit: tresare, scantei, ecranul se inroseste, patch note de "hotfix". Invins: cade, explodeaza, anunta Defeated
/// (EndingSequence asculta). Merge in orice unghi: pozitia si tirul sunt pe ecran (FezRoom.Screen).
/// </summary>
public class DirectorBoss : PatchSource
{
    public int maxHealth = 6;                 // doua lovituri pe faza
    public float invulnerableAfterHit = 1.4f;
    [Tooltip("Lupta incepe cand mochi e mai sus de inaltimea asta (podeaua arenei).")]
    public float activateY = 31f;
    public float fireInterval = 1.7f;
    public float bobHeight = 0.5f;
    public float sweepWidth = 3.5f;
    public Material hazardMat;
    [HideInInspector, System.Obsolete("Nefolosit: lupta porneste dupa inaltime (activateY). Pastrat ca builderul vechi sa compileze.")]
    public float activateX;

    [Header("Piesele robotelului (vizual)")]
    public Renderer screen;          // fata: se inroseste cand e lovit
    public Renderer[] eyes;
    public Transform armL, armR, antennaLight, thruster, body;

    public event System.Action Defeated;

    int health;
    float timer, hitFlash, hazardTimer, t;
    bool active, dead, forcedOnce;
    Transform player;
    MochiController mochi;
    Vector3 basePos;
    Color screenColor = Color.white;

    public int Health => health;

    void Start()
    {
        health = maxHealth;
        player = FindPlayer();
        if (player != null) mochi = player.GetComponent<MochiController>();
        basePos = transform.position;
        timer = fireInterval;
        hazardTimer = 3f;
        if (screen != null && screen.sharedMaterial != null) screenColor = screen.sharedMaterial.color;
        SetEyes(new Color(0.35f, 0.95f, 1f));
    }

    int Phase => Mathf.Clamp(1 + (maxHealth - health) / 2, 1, 3);   // 1, 2, 3
    float invulnerable;

    void Update()
    {
        if (dead) return;
        float dt = Time.deltaTime;
        if (dt <= 0f) return;
        if (invulnerable > 0f) invulnerable -= dt;
        t += dt * (active ? 0.6f + 0.35f * Phase : 0.4f);

        // se plimba pe orizontala ECRANULUI (ca sa mearga din orice unghi), pluteste in sus si in jos
        Vector3 right = FezRotator.Active != null ? FezRoom.Right(FezRotator.Active.Yaw) : Vector3.right;
        float sweep = active ? Mathf.Sin(t) * sweepWidth * (Phase >= 2 ? 1f : 0.6f) : 0f;
        transform.position = basePos + right * sweep + Vector3.up * (Mathf.Sin(Time.time * 1.6f) * bobHeight);
        transform.rotation = Quaternion.Euler(0f, FezRotator.Active != null ? FezRotator.Active.Yaw : 0f, Mathf.Sin(t) * -6f);
        Animate(dt);

        if (player == null) return;

        // in arena se lupta doar din fata sau din spate (din lateral peretii arenei acopera tot).
        // In arena intri din lateral (putul 2), iar dupa un respawn rotirea poate fi blocata: "firewall-ul" te intoarce singur.
        int angle = FezRoom.Active != null ? FezRoom.Active.Angle : 0;
        if (player.position.y >= activateY && (angle % 2) != 0 && FezRotator.Active != null && !FezRotator.Active.IsRotating
            && (mochi == null || mochi.IsGrounded))
        {
            if (!forcedOnce) { PatchNoteUI.Show("FIREWALL", "Camera angle enforced.", "Please face the Director."); forcedOnce = true; }
            FezRotator.Active.ForceRotateToward(0);   // spre fata (unghiul 0), pe drumul cel mai scurt
            return;
        }

        if (!active)
        {
            if (player.position.y < activateY || (angle % 2) != 0) return;
            if (Mathf.Abs(player.position.x - ScreenPosition.x) > 9f) return;
            Activate();
            return;
        }

        timer -= dt;
        if (timer <= 0f) Attack();

        if (Phase >= 3)
        {
            hazardTimer -= dt;
            if (hazardTimer <= 0f) { hazardTimer = 3.2f; FloorWave(); }
        }
    }

    void Activate()
    {
        active = true;
        FezRotator.Locked = true;
        PatchNoteUI.Show("PATCH 1.2.0  -  FINAL", "DIRECTOR.exe is now live.", "Rotation disabled by firewall.");
        PatchNoteUI.SetGoal("click right before a patch hits you");
        BigBanner.Show("PATCH 1.2.0  -  FINAL", "DIRECTOR.exe", "CLICK  =  parry", 2.2f);
        GameAudio.PlayMusic("Boss");   // muzica epica a luptei
        CameraShake.Shake(0.3f);
    }

    void Attack()
    {
        int phase = Phase;
        Vector2 me = ScreenPosition;
        Vector2 target = player.position;
        float speed = projectileSpeed + (phase - 1) * 1.2f;
        if (phase == 1)
        {
            timer = fireInterval;
            Fire(me, target, speed);
        }
        else if (phase == 2)
        {
            timer = fireInterval + 0.2f;
            Vector2 dir = (target - me).normalized;
            for (int n = -1; n <= 1; n++)
                PatchProjectile.Spawn(this, me, Rotate(dir, n * 16f) * speed);
        }
        else
        {
            timer = fireInterval * 0.8f;
            StartCoroutine(Burst(me, speed));
        }
        Recoil(0.15f);
    }

    IEnumerator Burst(Vector2 from, float speed)
    {
        for (int n = 0; n < 2 && !dead; n++)
        {
            if (player != null) Fire(ScreenPosition, player.position, speed + 1f);
            yield return new WaitForSeconds(0.28f);
        }
    }

    /// <summary>Unda de date pe podea: vine de pe o margine a arenei spre cealalta; scutul nu o opreste, sari peste ea.</summary>
    void FloorWave()
    {
        if (player == null) return;
        float floorV = activateY + 0.35f;   // chiar deasupra podelei (nu in ea, altfel se sparge)
        float dir = Random.value < 0.5f ? 1f : -1f;
        float u = ScreenPosition.x - dir * 7f;
        if (FezRoom.Active != null) u = dir > 0f ? FezRoom.Active.MinU + 1.5f : FezRoom.Active.MaxU - 1.5f;   // de la marginea arenei
        var p = PatchProjectile.Spawn(this, new Vector2(u, floorV), new Vector2(dir * 7.5f, 0f));
        p.unblockable = true;
        p.lifetime = 3f;
        p.transform.localScale = new Vector3(0.9f, 0.45f, 0.45f);
        if (hazardMat != null) p.GetComponent<Renderer>().sharedMaterial = hazardMat;
        CameraShake.Shake(0.1f);
    }

    static Vector2 Rotate(Vector2 v, float deg)
    {
        float r = deg * Mathf.Deg2Rad, c = Mathf.Cos(r), s = Mathf.Sin(r);
        return new Vector2(v.x * c - v.y * s, v.x * s + v.y * c);
    }

    public override void OnReflectedHit(PatchProjectile projectile)
    {
        if (dead || !active || invulnerable > 0f) return;
        health--;
        invulnerable = invulnerableAfterHit;
        hitFlash = 0.45f;
        CameraShake.Shake(0.5f);
        Fx.Burst(transform.position, new Color(1f, 0.85f, 0.3f), 30, 8f, 0.16f);
        Recoil(0.6f);

        if (health > 0)
        {
            int percent = Mathf.RoundToInt(100f * health / maxHealth);
            PatchNoteUI.Show("HOTFIX 1.2." + (maxHealth - health),
                             "DIRECTOR.exe health reduced to " + percent + "%.", Phase == 3 ? "Floor waves enabled. Jump." : "We are aware of the issue.");
            SetEyes(Phase == 1 ? new Color(0.35f, 0.95f, 1f) : Phase == 2 ? new Color(1f, 0.85f, 0.3f) : new Color(1f, 0.3f, 0.25f));
            foreach (var p in PatchProjectile.Active.ToArray()) Destroy(p.gameObject);   // toate: o lovitura = o lovitura
            timer = 1.2f;
            return;
        }

        dead = true;
        PatchNoteUI.SetGoal("");
        GameAudio.StopMusic();
        GameAudio.Sfx("boss_defeat", 1f, 1f, 0f);
        foreach (var p in PatchProjectile.Active.ToArray()) Destroy(p.gameObject);
        BigBanner.Show("DIRECTOR.exe", "PROCESS TERMINATED", "", 2.5f);
        StartCoroutine(Fall());
    }

    float recoil;
    void Recoil(float amount) => recoil = Mathf.Max(recoil, amount);

    IEnumerator Fall()
    {
        Vector3 start = transform.position;
        for (float k = 0f; k < 1.6f; k += Time.deltaTime)
        {
            transform.position = start + Vector3.down * (k * k * 4f) + Random.insideUnitSphere * 0.08f;
            transform.Rotate(0f, 0f, 160f * Time.deltaTime);
            if (Random.value < 0.25f) Fx.Burst(transform.position, new Color(1f, 0.6f, 0.2f), 4, 4f, 0.12f);
            yield return null;
        }
        Fx.Burst(transform.position, new Color(1f, 0.85f, 0.4f), 60, 10f, 0.2f);
        CameraShake.Shake(0.7f);
        Defeated?.Invoke();
        gameObject.SetActive(false);
    }

    void Animate(float dt)
    {
        float time = Time.time;
        if (armL != null) armL.localRotation = Quaternion.Euler(0f, 0f, 20f + Mathf.Sin(time * 3f) * 18f + recoil * 60f);
        if (armR != null) armR.localRotation = Quaternion.Euler(0f, 0f, -20f - Mathf.Sin(time * 3f + 1f) * 18f - recoil * 60f);
        if (body != null) body.localScale = new Vector3(1f + recoil * 0.25f, 1f - recoil * 0.2f, 1f);
        recoil = Mathf.Max(0f, recoil - dt * 2.5f);
        if (antennaLight != null) antennaLight.gameObject.SetActive(Mathf.Repeat(time, 0.9f) < 0.45f);
        if (thruster != null) thruster.localScale = new Vector3(0.6f, 0.35f + 0.12f * Mathf.Sin(time * 25f), 0.6f);
        // clipeste din cand in cand
        if (eyes != null)
            foreach (var e in eyes)
                if (e != null) e.transform.localScale = new Vector3(e.transform.localScale.x, Mathf.Repeat(time, 3.1f) < 0.12f ? 0.03f : 0.32f, e.transform.localScale.z);

        if (screen == null) return;
        if (hitFlash > 0f) hitFlash -= dt;
        Fx.SetColor(screen, hitFlash > 0f ? new Color(0.95f, 0.25f, 0.2f) : screenColor);
    }

    void SetEyes(Color c)
    {
        if (eyes == null) return;
        foreach (var e in eyes) if (e != null) Fx.SetColor(e, c);
    }

    void OnDisable() { if (dead) FezRotator.Locked = true; }
}
