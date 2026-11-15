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
            if (cooldown <= 0f) { crystal.gameObject.SetActive(true); crystal.localScale = Vector3.one * 0.1f; }
        }
        if (crystal.gameObject.activeSelf)
        {
            crystal.localPosition = baseLocal + Vector3.up * Mathf.Sin(t * 3f + transform.position.x) * 0.12f;
            crystal.localRotation = Quaternion.Euler(45f, t * 140f, 45f);
            crystal.localScale = Vector3.Lerp(crystal.localScale, Vector3.one * 0.55f, 1f - Mathf.Exp(-10f * Time.deltaTime));
        }

        if (!ready || mochi == null || !mochi.dashEnabled || mochi.IsGrounded || mochi.CanAirDash) return;
        if (Vector2.Distance(mochi.transform.position, FezRoom.Screen(transform.position)) > radius) return;

        mochi.RefreshAirDash();
        Fx.Burst(transform.position, color, 16, 5f, 0.14f);
        CameraShake.Shake(0.08f);
        crystal.gameObject.SetActive(false);
        cooldown = respawnTime;
    }
}
