using UnityEngine;

/// <summary>
/// Corpul de fizica al mochi-ului traieste in planul ecranului (u, v). Asta ii tine minte si ADANCIMEA,
/// adica unde e de fapt in lumea 3D, ca in Fez:
///  - cand stai pe un bloc al camerei rotative, adancimea devine a blocului pe care stai
///    (daca mai multe blocuri cad pe ecran in acelasi loc, ramai pe cel de la adancimea ta, altfel pe cel din fata);
///  - in aer, adancimea ramane cea de la ultima aterizare;
///  - la rotire, FezRotator foloseste pozitia 3D de aici: lumea se roteste, iar mochi ramane pe blocul lui.
/// Vizualul sta exact la pozitia 3D, deci poate trece prin spatele lucrurilor (silueta il arata atunci).
/// </summary>
public class FezMochiView : MonoBehaviour
{
    public FezRoom room;
    public FezRotator rotator;
    public Transform visual;
    [Tooltip("Adancimea curenta (negativ = spre camera). Se actualizeaza singura cand stai pe blocurile camerei.")]
    public float depth = -5f;

    MochiController mochi;
    Rigidbody2D rb;
    CircleCollider2D circle;

    /// <summary>Blocul 3D pe care stai acum (valid doar daca HasSupport).</summary>
    public Vector3Int SupportCell { get; private set; }
    public bool HasSupport { get; private set; }

    void Awake()
    {
        mochi = GetComponent<MochiController>();
        rb = GetComponent<Rigidbody2D>();
        circle = GetComponent<CircleCollider2D>();
    }

    int Angle => rotator != null ? rotator.Angle : 0;

    /// <summary>Platforma pe sina pe care stai (setata de RailPlatform cat te cara).</summary>
    public RailPlatform Platform { get; private set; }
    float platformTime;

    /// <summary>Apelat de RailPlatform: stai pe ea, deci ai adancimea ei.</summary>
    public void StandOnPlatform(RailPlatform p, float platformDepth)
    {
        Platform = p;
        platformTime = Time.time;
        depth = platformDepth;
    }

    void FixedUpdate()
    {
        if (rotator != null && rotator.IsRotating) return;
        HasSupport = false;
        if (Platform != null && Time.time - platformTime > 0.1f) Platform = null;
        if (room == null || mochi == null || !mochi.IsGrounded) return;
        if (Platform != null) return;   // platforma ii da adancimea
        float radius = circle != null ? circle.radius : 0.5f;
        Vector2 p = rb.position;
        if (room.TryGetSupport(Angle, p.x, p.y - radius, depth, out var cell))
        {
            SupportCell = cell;
            HasSupport = true;
            depth = room.DepthOf(room.CellCenter(cell), Angle);
        }
    }

    /// <summary>Pozitia 3D a centrului mochi-ului (in cadrul unghiului curent, nu al tween-ului).</summary>
    public Vector3 WorldPosition()
    {
        Vector3 p = rb != null ? (Vector3)rb.position : transform.position;
        return room != null ? room.ToWorld(p.x, p.y, depth, Angle * 90f) : new Vector3(p.x, p.y, depth);
    }

    void LateUpdate()
    {
        if (visual == null) return;
        Vector3 p = transform.position;   // (u, v), deja interpolat de Rigidbody2D
        // + coborarea spre sol cand mochi e turtit (altfel compresia s-ar face in aer)
        float ground = mochi != null ? mochi.VisualGroundOffset : 0f;
        // pozitia 3D sta pe loc in timpul rotirii (se roteste lumea, nu mochi); doar fata se intoarce spre camera
        visual.position = (room != null ? room.ToWorld(p.x, p.y, depth, Angle * 90f) : new Vector3(p.x, p.y, depth))
                          + Vector3.up * ground;
        float lean = mochi != null ? mochi.VisualLean : 0f;
        visual.rotation = Quaternion.Euler(0f, rotator != null ? rotator.Yaw : 0f, 0f) * Quaternion.Euler(0f, 0f, lean);
    }
}
