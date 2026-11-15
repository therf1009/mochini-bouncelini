using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Un "patch" aruncat spre mochi. Zboara drept, pe ecran (in planul fizicii); daca il atinge, il impinge
/// (fara game over) - sau, daca mochi are scutul ridicat, e trimis inapoi. Se sparge de ziduri.
/// Trimis inapoi, se intoarce la cel care l-a aruncat (prin ziduri, fara sa rateze) si il loveste.
/// In camerele rotative, vizualul sta in fata camerei; cand lumea se roteste, patch-urile se "dezintegreaza".
/// Nu foloseste fizica: distante simple, ca sa nu se amestece cu colliderele nivelului.
/// </summary>
public class PatchProjectile : MonoBehaviour
{
    public static readonly List<PatchProjectile> Active = new List<PatchProjectile>();

    public PatchSource owner;
    public Vector2 velocity;
    public bool reflected;
    [Tooltip("Unda de date a boss-ului: scutul n-o opreste si nu se poate trimite inapoi (sari peste ea).")]
    public bool unblockable;
    public float lifetime = 7f;

    Renderer rend;
    float age;
    Vector2 pos;
    int spawnAngle;
    Transform player;
    Rigidbody2D playerBody;
    Collider2D playerCollider;
    BounceBack shield;

    public Vector2 ScreenPosition => pos;

    public static PatchProjectile Spawn(PatchSource owner, Vector2 position, Vector2 velocity)
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
        go.name = "Patch";
        Destroy(go.GetComponent<Collider>());
        go.transform.localScale = Vector3.one * 0.45f;

        var p = go.AddComponent<PatchProjectile>();
        p.owner = owner;
        p.velocity = velocity;
        p.pos = position;
        p.spawnAngle = FezRoom.Active != null ? FezRoom.Active.Angle : 0;
        p.rend = go.GetComponent<Renderer>();
        p.rend.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        if (owner != null && owner.projectileMat != null) p.rend.sharedMaterial = owner.projectileMat;
        p.Place();
        return p;
    }

    void OnEnable() => Active.Add(this);
    void OnDisable() => Active.Remove(this);

    void Start()
    {
        var mochi = FindAnyObjectByType<MochiController>();
        if (mochi == null) return;
        player = mochi.transform;
        playerBody = mochi.GetComponent<Rigidbody2D>();
        playerCollider = mochi.GetComponent<Collider2D>();
        shield = mochi.GetComponent<BounceBack>();
    }

    void Place() => transform.position = FezRoom.World(pos);

    void Update()
    {
        float dt = Time.deltaTime;
        if (dt <= 0f) { Place(); return; }   // timp oprit (dialog): ramane unde e, si in fata camerei

        // lumea s-a rotit: patch-ul nu mai are unde sa fie
        if (FezRoom.Active != null && FezRoom.Active.Angle != spawnAngle) { Pop(); return; }

        age += dt;
        if (age > lifetime) { Destroy(gameObject); return; }

        if (reflected && owner != null)   // se intoarce la expeditor, fara sa rateze
            velocity = (owner.ScreenPosition - pos).normalized * velocity.magnitude;

        pos += velocity * dt;
        Place();
        transform.Rotate(0f, 0f, 360f * dt);

        if (reflected)
        {
            if (owner != null && Vector2.Distance(pos, owner.ScreenPosition) <= owner.hitRadius)
            {
                owner.OnReflectedHit(this);
                Destroy(gameObject);
            }
            return;
        }

        if (player != null && Vector2.Distance(pos, player.position) < 0.7f)
        {
            if (shield != null && shield.TryBlock(this)) return;
            HitPlayer();
            Destroy(gameObject);
            return;
        }

        foreach (var c in Physics2D.OverlapCircleAll(pos, 0.2f))
        {
            if (c.isTrigger || c == playerCollider) continue;
            Pop();   // s-a spart de zid
            return;
        }
    }

    void Pop()
    {
        Fx.Burst(transform.position, new Color(0.95f, 0.45f, 0.35f), 6, 2.5f, 0.1f, 2f);
        Destroy(gameObject);
    }

    void HitPlayer()
    {
        if (playerBody != null)
        {
            float side = Mathf.Sign(playerBody.position.x - pos.x);
            if (side == 0f) side = 1f;
            playerBody.linearVelocity = new Vector2(side * 9f, 7f);   // impins, nu pedepsit
        }
        CameraShake.Shake(0.3f);
    }

    /// <summary>Bounce Back: patch-ul se intoarce spre cel care l-a aruncat, mai repede.</summary>
    public void Reflect(float speedMultiplier = 1.6f)
    {
        if (reflected || unblockable) return;
        reflected = true;
        velocity = -velocity * speedMultiplier;
        age = 0f;
        if (owner != null && owner.reflectedMat != null) rend.sharedMaterial = owner.reflectedMat;
    }
}
