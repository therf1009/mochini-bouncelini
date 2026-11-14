using UnityEngine;

/// <summary>
/// Ceva care arunca "patch-uri" spre mochi (turela, Directorul). Cand un patch ii e trimis inapoi
/// cu Bounce Back, se intoarce la el si il loveste (OnReflectedHit).
/// </summary>
public abstract class PatchSource : MonoBehaviour
{
    public float hitRadius = 1f;
    public float projectileSpeed = 6f;
    public Material projectileMat;
    public Material reflectedMat;

    public abstract void OnReflectedHit(PatchProjectile projectile);

    protected Transform FindPlayer()
    {
        var mochi = FindFirstObjectByType<MochiController>();
        return mochi != null ? mochi.transform : null;
    }

    protected void Fire(Vector2 from, Vector2 target, float speed)
    {
        Vector2 dir = target - from;
        if (dir.sqrMagnitude < 0.0001f) dir = Vector2.left;
        PatchProjectile.Spawn(this, from, dir.normalized * speed);
    }
}
