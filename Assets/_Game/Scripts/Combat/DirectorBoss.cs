using System.Collections;
using UnityEngine;

/// <summary>
/// Directorul - boss-ul de la finalul nivelului 3. Pluteste deasupra arenei si, cand mochi-ul intra
/// (x >= activateX), arunca patch-uri spre el. Trei patch-uri trimise inapoi cu Bounce Back il dobor.
/// Dupa fiecare lovitura trage mai des si mai repede. La final anunta Defeated (EndingSequence il asculta).
/// </summary>
public class DirectorBoss : PatchSource
{
    public int maxHealth = 3;
    public float activateX = 44f;
    public float fireInterval = 1.8f;
    public float bobHeight = 1.2f;
    public Renderer screen;          // fata Directorului: se inroseste cand e lovit

    public event System.Action Defeated;

    int health;
    float timer, hitFlash;
    bool active, dead;
    Transform player;
    Vector3 basePos;
    Color screenColor = Color.white;
    MaterialPropertyBlock block;

    public int Health => health;

    void Start()
    {
        health = maxHealth;
        player = FindPlayer();
        basePos = transform.position;
        timer = fireInterval;
        block = new MaterialPropertyBlock();
        if (screen != null && screen.sharedMaterial != null) screenColor = screen.sharedMaterial.color;
    }

    void Update()
    {
        if (dead) return;
        float dt = Time.deltaTime;
        if (dt <= 0f) return;

        transform.position = basePos + Vector3.up * (Mathf.Sin(Time.time * 1.3f) * bobHeight);
        UpdateFlash(dt);

        if (player == null) return;

        if (!active)
        {
            if (player.position.x < activateX) return;
            active = true;
            PatchNoteUI.Show("PATCH 1.2.0  -  FINAL", "The Director is now live.", "Unauthorized players will be rebalanced.");
            PatchNoteUI.SetGoal("bounce the patches back   (F)");
            return;
        }

        timer -= dt;
        if (timer > 0f) return;
        int hitsTaken = maxHealth - health;
        timer = Mathf.Max(0.7f, fireInterval - 0.35f * hitsTaken);
        Fire(transform.position, player.position, projectileSpeed + hitsTaken);
    }

    public override void OnReflectedHit(PatchProjectile projectile)
    {
        if (dead) return;
        health--;
        hitFlash = 0.3f;
        CameraShake.Shake(0.45f);

        if (health > 0)
        {
            int percent = Mathf.RoundToInt(100f * health / maxHealth);
            PatchNoteUI.Show("HOTFIX 1.2." + (maxHealth - health),
                             "Director health reduced to " + percent + "%.", "We are aware of the issue.");
            return;
        }

        dead = true;
        PatchNoteUI.SetGoal("");
        foreach (var p in PatchProjectile.Active.ToArray()) Destroy(p.gameObject);
        StartCoroutine(Fall());
    }

    IEnumerator Fall()
    {
        Vector3 start = transform.position;
        for (float t = 0f; t < 1.4f; t += Time.deltaTime)
        {
            transform.position = start + Vector3.down * (t * t * 5f);
            transform.Rotate(0f, 0f, 120f * Time.deltaTime);
            yield return null;
        }
        Defeated?.Invoke();
        gameObject.SetActive(false);
    }

    void UpdateFlash(float dt)
    {
        if (screen == null) return;
        if (hitFlash > 0f) hitFlash -= dt;
        screen.GetPropertyBlock(block);
        block.SetColor("_BaseColor", hitFlash > 0f ? new Color(0.95f, 0.25f, 0.2f) : screenColor);
        screen.SetPropertyBlock(block);
    }
}
