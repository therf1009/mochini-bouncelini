using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Un "patch" aruncat spre mochi. Zboara drept; daca il atinge, il impinge (fara game over).
/// Se sparge de ziduri. Trimis inapoi cu Bounce Back, se intoarce la cel care l-a aruncat
/// (prin ziduri, fara sa rateze) si il loveste.
/// Nu foloseste fizica: distante simple, ca sa nu se amestece cu colliderele nivelului.
/// </summary>
public class PatchProjectile : MonoBehaviour
{
    public static readonly List<PatchProjectile> Active = new List<PatchProjectile>();

    public PatchSource owner;
    public Vector2 velocity;
    public bool reflected;
    public float lifetime = 7f;

    Renderer rend;
    float age;
    Transform player;
    Rigidbody2D playerBody;
    Collider2D playerCollider;

    public static PatchProjectile Spawn(PatchSource owner, Vector2 position, Vector2 velocity)
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
        go.name = "Patch";
        Destroy(go.GetComponent<Collider>());
        go.transform.position = new Vector3(position.x, position.y, -0.9f);   // in fata blocurilor
        go.transform.localScale = Vector3.one * 0.45f;

        var p = go.AddComponent<PatchProjectile>();
        p.owner = owner;
        p.velocity = velocity;
        p.rend = go.GetComponent<Renderer>();
        if (owner != null && owner.projectileMat != null) p.rend.sharedMaterial = owner.projectileMat;
        return p;
    }

    void OnEnable() => Active.Add(this);
    void OnDisable() => Active.Remove(this);

    void Start()
    {
        var mochi = FindFirstObjectByType<MochiController>();
        if (mochi == null) return;
        player = mochi.transform;
        playerBody = mochi.GetComponent<Rigidbody2D>();
        playerCollider = mochi.GetComponent<Collider2D>();
    }

    void Update()
    {
        float dt = Time.deltaTime;
        if (dt <= 0f) return;   // timp oprit (dialog, rotire)

        age += dt;
        if (age > lifetime) { Destroy(gameObject); return; }

        if (reflected && owner != null)   // se intoarce la expeditor, fara sa rateze
            velocity = ((Vector2)owner.transform.position - (Vector2)transform.position).normalized * velocity.magnitude;

        Vector2 pos = (Vector2)transform.position + velocity * dt;
        transform.position = new Vector3(pos.x, pos.y, transform.position.z);
        transform.Rotate(0f, 0f, 360f * dt);

        if (reflected)
        {
            if (owner != null && Vector2.Distance(pos, owner.transform.position) <= owner.hitRadius)
            {
                owner.OnReflectedHit(this);
                Destroy(gameObject);
            }
            return;
        }

        if (player != null && Vector2.Distance(pos, player.position) < 0.7f)
        {
            HitPlayer();
            Destroy(gameObject);
            return;
        }

        foreach (var c in Physics2D.OverlapCircleAll(pos, 0.2f))
        {
            if (c.isTrigger || c == playerCollider) continue;
            Destroy(gameObject);   // s-a spart de zid
            return;
        }
    }

    void HitPlayer()
    {
        if (playerBody != null)
        {
            float side = Mathf.Sign(playerBody.position.x - transform.position.x);
            if (side == 0f) side = 1f;
            playerBody.linearVelocity = new Vector2(side * 9f, 7f);   // impins, nu pedepsit
        }
        CameraShake.Shake(0.3f);
    }

    /// <summary>Bounce Back: patch-ul se intoarce spre cel care l-a aruncat, mai repede.</summary>
    public void Reflect()
    {
        if (reflected) return;
        reflected = true;
        velocity = -velocity * 1.6f;
        age = 0f;
        if (owner != null && owner.reflectedMat != null) rend.sharedMaterial = owner.reflectedMat;
    }
}
