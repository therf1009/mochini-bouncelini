using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Bounce Back, refacut ca SCUT (abilitatea existenta a nivelului 3, pastrata in fond: patch-urile se intorc
/// la cel care le-a aruncat si il lovesc). Acum e defensiv:
///  - tii F (sau K, ori X pe gamepad): in jurul lui mochi apare o bula;
///  - orice patch care atinge bula e trimis inapoi, automat, mai repede;
///  - bula consuma energie cat o tii (maxim ~1.4 s); goala, are nevoie de o pauza ca sa se reincarce;
///  - "parry": daca ridici scutul chiar inainte sa te loveasca (primele 0.2 s), patch-ul pleaca si mai tare.
/// Fara scut, un patch te impinge (nu te omoara). Tema jam-ului, la propriu: BOUNCE BACK.
/// Se deblocheaza din tokenul REVERT (mesajul OnAbilityUnlocked).
/// </summary>
[RequireComponent(typeof(MochiController))]
public class BounceBack : MonoBehaviour
{
    public bool unlocked;
    public float radius = 1.35f;
    public float maxEnergy = 1.4f;        // secunde de scut
    public float rechargeRate = 0.8f;     // energie pe secunda cand nu-l tii
    public float emptyPause = 0.6f;       // dupa ce l-ai golit, atata nu se reincarca
    public float parryWindow = 0.2f;
    public Color color = new Color(0.40f, 0.90f, 1f);

    float energy, emptyTimer, upTime, flash;
    bool up;
    MochiController mochi;
    Transform bubble;
    Renderer bubbleRenderer;

    /// <summary>Scutul e ridicat acum?</summary>
    public bool IsUp => up;
    public float Energy01 => energy / maxEnergy;

    void Awake()
    {
        mochi = GetComponent<MochiController>();
        energy = maxEnergy;
    }

    void OnAbilityUnlocked(Ability ability)
    {
        if (ability == Ability.Third) unlocked = true;
    }

    void Update()
    {
        if (!unlocked || Time.timeScale == 0f) { SetUp(false); return; }
        float dt = Time.deltaTime;

        bool held = Held() && emptyTimer <= 0f && energy > 0f;
        SetUp(held);

        if (up)
        {
            upTime += dt;
            energy -= dt;
            if (energy <= 0f) { energy = 0f; emptyTimer = emptyPause; SetUp(false); }
            ReflectNearby();
        }
        else
        {
            upTime = 0f;
            if (emptyTimer > 0f) emptyTimer -= dt;
            else energy = Mathf.Min(maxEnergy, energy + rechargeRate * dt);
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

    /// <summary>Apelat de patch cand ajunge la mochi: daca scutul e sus, il trimite inapoi in loc sa loveasca.</summary>
    public bool TryBlock(PatchProjectile p)
    {
        if (!up || p.unblockable) return false;
        Block(p);
        return true;
    }

    void Block(PatchProjectile p)
    {
        bool parry = upTime <= parryWindow;
        p.Reflect(parry ? 2.2f : 1.6f);
        flash = 1f;
        energy = Mathf.Min(maxEnergy, energy + (parry ? 0.4f : 0.1f));   // parry-ul te rasplateste cu energie
        CameraShake.Shake(parry ? 0.3f : 0.18f);
        Fx.Burst(FezRoom.World(p.ScreenPosition), parry ? Color.white : color, parry ? 22 : 10, parry ? 7f : 4f, 0.13f, 2f);
    }

    void SetUp(bool on)
    {
        if (up == on) return;
        up = on;
        if (on) { EnsureBubble(); bubble.localScale = Vector3.one * 0.3f; }
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
        bubble.gameObject.SetActive(up || flash > 0f);
        if (!bubble.gameObject.activeSelf) return;

        Vector3 center = mochi.visual != null ? mochi.visual.position : transform.position;
        bubble.position = center;
        float target = radius * 2f * (1f + 0.04f * Mathf.Sin(Time.time * 12f)) * (1f + 0.12f * flash);
        bubble.localScale = Vector3.Lerp(bubble.localScale, Vector3.one * target, 1f - Mathf.Exp(-25f * Time.unscaledDeltaTime));

        // cu cat mai putina energie, cu atat bula e mai stearsa si palpaie
        float e = Energy01;
        float a = Mathf.Lerp(0.12f, 0.38f, e) + 0.35f * flash;
        if (e < 0.3f) a *= 0.6f + 0.4f * Mathf.Abs(Mathf.Sin(Time.time * 30f));
        Fx.SetColor(bubbleRenderer, new Color(Mathf.Lerp(color.r, 1f, flash), Mathf.Lerp(color.g, 1f, flash), color.b, a));
    }

    static bool Held()
    {
        var kb = Keyboard.current;
        var pad = Gamepad.current;
        bool key = kb != null && (kb.fKey.isPressed || kb.kKey.isPressed);
        bool button = pad != null && pad.buttonWest.isPressed;
        return key || button;
    }

    void OnDisable() { if (bubble != null) bubble.gameObject.SetActive(false); }
}
