using UnityEngine;

/// <summary>
/// Ceva care arunca "patch-uri" spre mochi (turela, boss-ul). Cand un patch ii e trimis inapoi
/// cu scutul Bounce Back, se intoarce la el si il loveste (OnReflectedHit).
/// Pozitia pe ecran (ScreenPosition) tine cont de rotirea lumii, deci merge si in camerele rotative.
/// </summary>
public abstract class PatchSource : MonoBehaviour
{
    public float hitRadius = 1f;
    public float projectileSpeed = 6f;
    public Material projectileMat;
    public Material reflectedMat;

    public abstract void OnReflectedHit(PatchProjectile projectile);

    /// <summary>Unde e sursa pe ecran (planul fizicii), la unghiul curent.</summary>
    public virtual Vector2 ScreenPosition => FezRoom.Screen(transform.position);

    protected Transform FindPlayer()
    {
        var mochi = FindAnyObjectByType<MochiController>();
        return mochi != null ? mochi.transform : null;
    }

    protected void Fire(Vector2 from, Vector2 target, float speed)
    {
        Vector2 dir = target - from;
        if (dir.sqrMagnitude < 0.0001f) dir = Vector2.left;
        PatchProjectile.Spawn(this, from, dir.normalized * speed);
    }
}
