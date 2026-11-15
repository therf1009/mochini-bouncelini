using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Bounce Back, ca PARRY (abilitatea de la nivelul 3; in fond aceeasi: patch-urile se intorc la cine le-a aruncat).
///  - CLICK (stanga; sau X pe gamepad): o bula apare in jurul lui mochi pentru o clipa (activeWindow);
///  - orice patch care atinge bula in clipa aia e trimis inapoi; daca l-ai prins chiar la inceput ("perfect"),
///    pleaca si mai tare si scutul se reincarca imediat;
///  - dupa fiecare click urmeaza o mica pauza (cooldown): nu merge sa tot apesi.
/// Fara scut, un patch te impinge (nu te omoara). Tema jam-ului, la propriu: BOUNCE BACK.
/// Se deblocheaza din tokenul REVERT (mesajul OnAbilityUnlocked).
/// </summary>
[RequireComponent(typeof(MochiController))]
public class BounceBack : MonoBehaviour
{
    public bool unlocked;
    public float radius = 1.35f;
    public float activeWindow = 0.3f;     // cat sta bula ridicata dupa click
    public float perfectWindow = 0.12f;   // prins in primele 0.12 s = parry perfect
    public float cooldown = 0.45f;        // dupa fereastra, atat pana poti da iar click
    public Color color = new Color(0.40f, 0.90f, 1f);

    float upTimer, cooldownTimer, flash, sinceClick;
    bool perfectHit;
    MochiController mochi;
    Transform bubble;
    Renderer bubbleRenderer;

    /// <summary>Bula e ridicata acum?</summary>
    public bool IsUp => upTimer > 0f;
    /// <summary>0..1: cat de gata e scutul (1 = poti da click).</summary>
    public float Ready01 => cooldownTimer <= 0f && upTimer <= 0f ? 1f : 0f;

    void Awake() => mochi = GetComponent<MochiController>();

    void OnAbilityUnlocked(Ability ability)
    {
        if (ability == Ability.Third) unlocked = true;
    }

    void Update()
    {
        if (!unlocked || Time.timeScale == 0f) return;
        float dt = Time.deltaTime;

        if (upTimer > 0f)
        {
            upTimer -= dt;
            sinceClick += dt;
            ReflectNearby();
            if (upTimer <= 0f) cooldownTimer = perfectHit ? 0.05f : cooldown;   // parry perfect: gata imediat
        }
        else if (cooldownTimer > 0f) cooldownTimer -= dt;
        else if (Clicked())
        {
            upTimer = activeWindow;
            sinceClick = 0f;
            perfectHit = false;
            EnsureBubble();
            bubble.localScale = Vector3.one * 0.4f;
            ReflectNearby();
        }
        flash = Mathf.Max(0f, flash - dt * 4f);
    }

    void LateUpdate() => UpdateBubble();

    void ReflectNearby()
    {
        foreach (var p in PatchProjectile.Active.ToArray())
        {
            if (p.reflected || p.unblockable) continue;
            if (Vector2.Distance(p.ScreenPosition, transform.position) > radius + 0.25f) continue;
            Block(p);
        }
    }

    /// <summary>Apelat de patch cand ajunge la mochi: daca bula e ridicata, il trimite inapoi in loc sa loveasca.</summary>
    public bool TryBlock(PatchProjectile p)
    {
        if (upTimer <= 0f || p.unblockable) return false;
        Block(p);
        return true;
    }

    void Block(PatchProjectile p)
    {
        bool perfect = sinceClick <= perfectWindow;
        if (perfect) perfectHit = true;
        p.Reflect(perfect ? 2.2f : 1.6f);
        GameAudio.Sfx("parry", perfect ? 1f : 0.75f, perfect ? 1.12f : 1f);
        flash = 1f;
        CameraShake.Shake(perfect ? 0.3f : 0.18f);
        Fx.Burst(FezRoom.World(p.ScreenPosition), perfect ? Color.white : color, perfect ? 22 : 10, perfect ? 7f : 4f, 0.13f, 2f);
    }

    void EnsureBubble()
    {
        if (bubble != null) return;
        var go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        go.name = "ShieldBubble";
        Destroy(go.GetComponent<Collider>());
        bubble = go.transform;
        bubbleRenderer = go.GetComponent<Renderer>();
        bubbleRenderer.sharedMaterial = Fx.Material;
        bubbleRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
    }

    void UpdateBubble()
    {
        if (bubble == null) return;
        bool show = upTimer > 0f || flash > 0f;
        bubble.gameObject.SetActive(show);
        if (!show) return;

        Vector3 center = mochi.visual != null ? mochi.visual.position : transform.position;
        bubble.position = center;
        // apare cu un "pop", apoi se stinge pe masura ce trece fereastra
        float life = activeWindow > 0f ? Mathf.Clamp01(upTimer / activeWindow) : 0f;
        float target = radius * 2f * (1f + 0.1f * flash);
        bubble.localScale = Vector3.Lerp(bubble.localScale, Vector3.one * target, 1f - Mathf.Exp(-40f * Time.unscaledDeltaTime));
        float a = 0.12f + 0.3f * life + 0.35f * flash;
        Fx.SetColor(bubbleRenderer, new Color(Mathf.Lerp(color.r, 1f, flash), Mathf.Lerp(color.g, 1f, flash), color.b, a));
    }

    static bool Clicked()
    {
        var mouse = Mouse.current;
        var pad = Gamepad.current;
        bool click = mouse != null && mouse.leftButton.wasPressedThisFrame;
        bool button = pad != null && pad.buttonWest.wasPressedThisFrame;
        return click || button;
    }

    void OnDisable() { if (bubble != null) bubble.gameObject.SetActive(false); }
}
