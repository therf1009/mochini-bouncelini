using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Peretii glitch ("deprecated"): solizi pentru orice, mai putin pentru mochi in timpul dash-ului.
/// Cine are asemenea collidere le inregistreaza aici (FezRoom pentru celulele glitch, GlitchWall pentru
/// peretii din nivelurile 2D). MochiController le ignora cat dureaza dash-ul.
/// </summary>
public static class DashPhase
{
    static readonly List<Collider2D> colliders = new List<Collider2D>();
    public static IReadOnlyList<Collider2D> Colliders => colliders;

    // fara reincarcarea domeniului la Play (setarea proiectului), staticele raman de la rularea trecuta: le golim
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetStatics() { colliders.Clear(); Awakened = null; }

    public static void Register(Collider2D c)
    {
        if (c != null && !colliders.Contains(c)) colliders.Add(c);
    }

    public static void Unregister(Collider2D c) => colliders.Remove(c);

    public static bool Contains(Collider2D c) => c != null && colliders.Contains(c);

    /// <summary>Mochi e acum (macar partial) in interiorul unui perete glitch activ?</summary>
    public static bool Overlapping(Vector2 point, float radius)
    {
        foreach (var c in colliders)
        {
            if (c == null || !c.isActiveAndEnabled) continue;
            if (c.OverlapPoint(point)) return true;
            Vector2 closest = c.ClosestPoint(point);
            if ((closest - point).sqrMagnitude < radius * radius) return true;
        }
        return false;
    }

    /// <summary>Anunt cand REVERT-ul de dash a fost luat: peretii glitch se aprind (ca sa vezi la ce folosesc).</summary>
    public static event System.Action Awakened;
    public static void Awaken() => Awakened?.Invoke();
}
