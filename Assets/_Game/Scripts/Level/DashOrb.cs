using UnityEngine;

/// <summary>
/// Cristal de reincarcare: plutieste peste gol. Daca il atingi in aer (PE ECRAN, din orice unghi) dupa ce ai
/// folosit dash-ul, primesti inca un dash. Se sparge si revine dupa cateva secunde.
/// Asa se leaga saririle: dash -> cristal -> dash -> cristal.
/// </summary>
public class DashOrb : MonoBehaviour
{
    public float radius = 0.85f;
    public float respawnTime = 2.5f;
    public Transform crystal;
    public Transform halo;          // sfera transparenta care pulseaza (se vede de departe)
    public float size = 0.55f;
    float sparkle;
    public Color color = new Color(0.45f, 0.95f, 1f);

    MochiController mochi;
    float cooldown;
    Vector3 baseLocal;

    void Start()
    {
        mochi = FindAnyObjectByType<MochiController>();
        if (crystal == null) crystal = transform;
        baseLocal = crystal.localPosition;
    }

    void Update()
    {
        float t = Time.time;
        bool ready = cooldown <= 0f;
        if (!ready)
        {
            cooldown -= Time.deltaTime;
            if (cooldown <= 0f) { crystal.gameObject.SetActive(true); crystal.localScale = Vector3.one * 0.1f; if (halo != null) halo.gameObject.SetActive(true); }
        }
        if (crystal.gameObject.activeSelf)
        {
            crystal.localPosition = baseLocal + Vector3.up * Mathf.Sin(t * 3f + transform.position.x) * 0.12f;
            crystal.localRotation = Quaternion.Euler(45f, t * 140f, 45f);
            crystal.localScale = Vector3.Lerp(crystal.localScale, Vector3.one * size, 1f - Mathf.Exp(-10f * Time.deltaTime));
            if (halo != null) halo.localScale = Vector3.one * (1.5f + 0.25f * Mathf.Sin(t * 4f + transform.position.y));
            // cateva scantei din cand in cand: atrag privirea
            sparkle -= Time.deltaTime;
            if (sparkle <= 0f) { sparkle = 0.35f; Fx.Burst(crystal.position, color, 2, 1.6f, 0.07f, -1f); }   // urca usor
        }

        if (!ready || mochi == null || !mochi.dashEnabled || mochi.IsGrounded || mochi.CanAirDash) return;
        if (Vector2.Distance(mochi.transform.position, FezRoom.Screen(transform.position)) > radius) return;

        mochi.RefreshAirDash();
        Fx.Burst(transform.position, color, 16, 5f, 0.14f);
        CameraShake.Shake(0.08f);
        crystal.gameObject.SetActive(false);
        if (halo != null) halo.gameObject.SetActive(false);
        cooldown = respawnTime;
    }
}
