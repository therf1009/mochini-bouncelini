using UnityEngine;

/// <summary>
/// Aspectul peretilor glitch ("deprecated"): rosii-magenta, semi-transparenti, tremura si sar din cand in cand.
/// Pana iei dash-ul sunt stinsi (o "eroare" adormita); cand REVERT-ul de dash e luat, se aprind si pulseaza,
/// ca sa vezi imediat la ce folosesc. Cat timp mochi face dash prin ei, se deschid (mai transparenti).
/// Pus pe radacina vizualului unui perete (copiii = cuburile lui).
/// </summary>
public class GlitchVisual : MonoBehaviour
{
    public Color sleepColor = new Color(0.55f, 0.20f, 0.30f, 0.55f);
    public Color awakeColor = new Color(1f, 0.18f, 0.42f, 0.78f);
    public bool awakeAtStart;

    Renderer[] cells;
    Vector3[] basePos;
    bool awake;
    float jitterTimer, flash;
    MochiController mochi;

    void Start()
    {
        cells = GetComponentsInChildren<Renderer>();
        basePos = new Vector3[cells.Length];
        for (int i = 0; i < cells.Length; i++) basePos[i] = cells[i].transform.localPosition;
        mochi = FindAnyObjectByType<MochiController>();
        awake = awakeAtStart || (mochi != null && mochi.dashEnabled);
        DashPhase.Awakened += OnAwaken;
    }

    void OnDestroy() => DashPhase.Awakened -= OnAwaken;

    void OnAwaken() { awake = true; flash = 1f; }

    void Update()
    {
        float t = Time.time;
        flash = Mathf.Max(0f, flash - Time.deltaTime);
        bool open = mochi != null && mochi.IsDashing;

        Color c = awake ? awakeColor : sleepColor;
        if (awake) c = Color.Lerp(c, Color.white, 0.12f + 0.12f * Mathf.Sin(t * 5f) + flash * 0.6f);
        if (open) c.a *= 0.45f;

        jitterTimer -= Time.deltaTime;
        bool jitter = jitterTimer <= 0f;
        if (jitter) jitterTimer = Random.Range(0.05f, awake ? 0.25f : 0.6f);

        for (int i = 0; i < cells.Length; i++)
        {
            if (cells[i] == null) continue;
            Fx.SetColor(cells[i], c);
            if (jitter)
            {
                bool glitchNow = Random.value < (awake ? 0.25f : 0.08f);
                cells[i].transform.localPosition = basePos[i] + (glitchNow ? new Vector3(Random.Range(-0.08f, 0.08f), 0f, Random.Range(-0.05f, 0.05f)) : Vector3.zero);
            }
        }
    }
}
