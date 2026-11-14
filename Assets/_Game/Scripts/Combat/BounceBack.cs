using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// A treia abilitate (nivelul 3): F (sau X pe gamepad) trimite inapoi orice patch aflat aproape.
/// Patch-ul se intoarce la cel care l-a aruncat si il loveste. Tema jam-ului, la propriu: BOUNCE BACK.
/// Se deblocheaza din tokenul REVERT (mesajul OnAbilityUnlocked).
/// </summary>
[RequireComponent(typeof(MochiController))]
public class BounceBack : MonoBehaviour
{
    public bool unlocked;
    public float radius = 2.4f;
    public float cooldown = 0.25f;

    float lastUse = -99f;

    void OnAbilityUnlocked(Ability ability)
    {
        if (ability == Ability.Third) unlocked = true;
    }

    void Update()
    {
        if (!unlocked || Time.timeScale == 0f || !Pressed()) return;
        if (Time.time - lastUse < cooldown) return;
        lastUse = Time.time;

        bool any = false;
        foreach (var p in PatchProjectile.Active.ToArray())
        {
            if (p.reflected) continue;
            if (Vector2.Distance(p.transform.position, transform.position) > radius) continue;
            p.Reflect();
            any = true;
        }
        CameraShake.Shake(any ? 0.25f : 0.05f);
    }

    static bool Pressed()
    {
        var kb = Keyboard.current;
        var pad = Gamepad.current;
        bool key = kb != null && (kb.fKey.wasPressedThisFrame || kb.kKey.wasPressedThisFrame);
        bool button = pad != null && pad.buttonWest.wasPressedThisFrame;
        return key || button;
    }
}
