using UnityEngine;

/// <summary>
/// Platforma pe sina (ca in video-ul de referinta): merge incolo si incoace intre doua puncte 3D, pe o tija vizibila.
/// E gandita pentru rotire: o platforma care se misca IN ADANCIME pare nemiscata din fata (doar trece prin spatele
/// lucrurilor), dar din lateral se plimba stanga-dreapta. Invers pentru cele care merg pe X. Pe verticala, din orice unghi.
///
/// Fizica: un corp cinematic in planul ecranului, cu latimea proiectiei la unghiul curent. Mochi e carat
/// (pozitia lui se muta odata cu platforma), iar adancimea lui devine a platformei, ca la rotire sa ramana pe ea.
/// </summary>
public class RailPlatform : MonoBehaviour
{
    public Vector3 pointA;
    public Vector3 pointB;
    [Tooltip("Marimea in lume (x, y, z) a platformei.")]
    public Vector3 size = new Vector3(2f, 0.5f, 1f);
    public float speed = 2.2f;
    public float pauseAtEnds = 0.6f;
    [Range(0f, 1f)] public float startPhase;
    public Transform visual;

    Rigidbody2D body;
    BoxCollider2D box;
    MochiController mochi;
    Rigidbody2D mochiBody;
    FezMochiView mochiView;
    float t, dir = 1f, pause;
    Vector2 lastScreen;
    bool hasLast;

    public Vector3 Center { get; private set; }
    public Vector3 Size => size;

    void Start()
    {
        var go = new GameObject("RailPhysics_" + name);
        body = go.AddComponent<Rigidbody2D>();
        body.bodyType = RigidbodyType2D.Kinematic;
        body.interpolation = RigidbodyInterpolation2D.Interpolate;
        box = go.AddComponent<BoxCollider2D>();

        mochi = FindAnyObjectByType<MochiController>();
        if (mochi != null)
        {
            mochiBody = mochi.GetComponent<Rigidbody2D>();
            mochiView = mochi.GetComponent<FezMochiView>();
        }
        t = startPhase;
        Center = Vector3.Lerp(pointA, pointB, t);
        Place(true);
    }

    void FixedUpdate()
    {
        if (pause > 0f) pause -= Time.fixedDeltaTime;
        else
        {
            float len = Mathf.Max(0.01f, Vector3.Distance(pointA, pointB));
            t += dir * speed * Time.fixedDeltaTime / len;
            if (t >= 1f) { t = 1f; dir = -1f; pause = pauseAtEnds; }
            if (t <= 0f) { t = 0f; dir = 1f; pause = pauseAtEnds; }
        }
        // usor in si din capete (se simte ca un mecanism, nu ca un robot)
        Center = Vector3.Lerp(pointA, pointB, Mathf.SmoothStep(0f, 1f, t));
        Place(false);
    }

    void LateUpdate()
    {
        if (visual != null) visual.position = Center;
    }

    void Place(bool snap)
    {
        float yaw = FezRotator.Active != null ? FezRotator.Active.Angle * 90f : 0f;
        Vector3 right = FezRoom.Right(yaw);
        float width = Mathf.Abs(size.x * right.x) + Mathf.Abs(size.z * right.z);
        box.size = new Vector2(width, size.y);
        Vector2 screen = FezRoom.Screen(Center);

        Vector2 delta = hasLast ? screen - lastScreen : Vector2.zero;
        // daca s-a rotit lumea, proiectia sare: nu e miscare, nu caram nimic, si o mutam direct (nu "zboara")
        bool jumped = delta.sqrMagnitude > 1f;
        if (jumped) delta = Vector2.zero;

        if (snap || jumped) { body.position = screen; body.linearVelocity = Vector2.zero; }
        else body.linearVelocity = (screen - body.position) / Time.fixedDeltaTime;   // prin viteza: mochi stie ca sta pe ceva care urca
        if (!snap) Carry(screen, width, delta);
        lastScreen = screen;
        hasLast = true;
    }

    void Carry(Vector2 screen, float width, Vector2 delta)
    {
        if (mochi == null || mochiBody == null || !mochi.IsGrounded) return;
        Vector2 m = mochiBody.position;
        float top = screen.y + size.y * 0.5f;
        float feet = m.y - 0.5f;
        if (Mathf.Abs(m.x - screen.x) > width * 0.5f + 0.4f) return;
        if (feet < top - 0.2f || feet > top + 0.3f) return;

        mochiBody.position = m + delta;
        if (mochiView != null && FezRoom.Active != null)
            mochiView.StandOnPlatform(this, FezRoom.Active.DepthOf(Center, FezRoom.Active.Angle));
    }

    void OnDrawGizmos()
    {
        Gizmos.color = new Color(1f, 0.7f, 0.2f);
        Gizmos.DrawLine(pointA, pointB);
        Gizmos.DrawWireCube(pointA, size);
        Gizmos.DrawWireCube(pointB, size);
    }
}
