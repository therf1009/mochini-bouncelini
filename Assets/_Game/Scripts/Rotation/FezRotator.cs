using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Q / E (sau LB / RB pe gamepad) rotesc lumea la 90 de grade, doar cand mochi-ul e in camera rotativa.
///
/// Regulile din documentul de design:
///  - timpul se opreste pe durata rotirii (tween de 0.4 s)
///  - mochi-ul isi pastreaza pozitia pe ecran, nu in lume
///  - daca dupa rotire ar ramane intr-un bloc, e impins in cel mai apropiat loc liber, pe verticala intai
///  - viteza verticala ramane, cea orizontala se reseteaza (altfel rotirea devine exploit de viteza)
///  - cooldown de 1 secunda, ca rotirea sa nu fie buton de panica
/// </summary>
public class FezRotator : MonoBehaviour
{
    public FezRoom room;
    public Rigidbody2D player;
    [Tooltip("Obiecte ascunse cand nu esti la unghiul 0 (ex. coridorul, care din lateral ar acoperi camera).")]
    public Transform[] hideWhenRotated;

    public float tweenTime = 0.4f;
    public float cooldown = 1f;

    int angle;
    float yaw;
    bool rotating;
    float lastRotateTime = -99f;
    readonly List<Renderer> hidden = new List<Renderer>();

    public int Angle => angle;
    public float Yaw => yaw;
    public bool IsRotating => rotating;

    public event System.Action<int> AngleChanged;

    void Start()
    {
        if (hideWhenRotated != null)
            foreach (Transform t in hideWhenRotated)
                if (t != null) hidden.AddRange(t.GetComponentsInChildren<Renderer>(true));

        var respawn = player != null ? player.GetComponent<Respawn2D>() : null;
        if (respawn != null) respawn.Respawned += () => SnapTo(0);   // checkpoint-urile sunt la unghiul 0
    }

    void Update()
    {
        if (rotating || room == null || player == null) return;

        int dir = ReadDirection();
        if (dir == 0) return;
        if (Time.unscaledTime - lastRotateTime < cooldown) return;
        if (!room.ContainsU(player.position.x, 0.4f)) return;   // rotesti doar in camera rotativa

        StartCoroutine(Rotate(dir));
    }

    static int ReadDirection()
    {
        var kb = Keyboard.current;
        var pad = Gamepad.current;
        if ((kb != null && kb.qKey.wasPressedThisFrame) || (pad != null && pad.leftShoulder.wasPressedThisFrame)) return -1;
        if ((kb != null && kb.eKey.wasPressedThisFrame) || (pad != null && pad.rightShoulder.wasPressedThisFrame)) return 1;
        return 0;
    }

    IEnumerator Rotate(int dir)
    {
        rotating = true;
        lastRotateTime = Time.unscaledTime;
        float previousScale = Time.timeScale > 0f ? Time.timeScale : 1f;
        Time.timeScale = 0f;

        int target = (angle + dir + 4) % 4;
        float fromYaw = angle * 90f;
        float toYaw = fromYaw + dir * 90f;
        if (target != 0) SetHidden(true);   // coridorul dispare inainte sa se roteasca in cadru

        for (float t = 0f; t < tweenTime; t += Time.unscaledDeltaTime)
        {
            yaw = Mathf.Lerp(fromYaw, toYaw, Mathf.SmoothStep(0f, 1f, t / tweenTime));
            yield return null;
        }

        angle = target;
        yaw = angle * 90f;
        room.SetAngle(angle);
        SetHidden(angle != 0);

        Physics2D.SyncTransforms();
        PushOutOfBlocks();
        player.linearVelocity = new Vector2(0f, player.linearVelocity.y);

        Time.timeScale = previousScale;
        rotating = false;
        CameraShake.Shake(0.1f);
        AngleChanged?.Invoke(angle);
    }

    /// <summary>Revine instant la un unghi (ex. la respawn), fara tween.</summary>
    public void SnapTo(int a)
    {
        StopAllCoroutines();
        if (rotating) Time.timeScale = 1f;
        rotating = false;
        angle = ((a % 4) + 4) % 4;
        yaw = angle * 90f;
        if (room != null) room.SetAngle(angle);
        SetHidden(angle != 0);
        AngleChanged?.Invoke(angle);
    }

    void SetHidden(bool hide)
    {
        foreach (var r in hidden)
            if (r != null) r.enabled = !hide;
    }

    void PushOutOfBlocks()
    {
        var col = player.GetComponent<CircleCollider2D>();
        float radius = col != null ? col.radius * 0.95f : 0.45f;
        Vector2 p = player.position;
        if (IsFree(p, radius, col)) return;

        // vertical intai (cel mai apropiat, sus sau jos), apoi orizontal
        for (float d = 0.25f; d <= 8f; d += 0.25f)
        {
            if (IsFree(p + Vector2.up * d, radius, col)) { MoveTo(p + Vector2.up * d); return; }
            if (IsFree(p + Vector2.down * d, radius, col)) { MoveTo(p + Vector2.down * d); return; }
        }
        for (float d = 0.25f; d <= 4f; d += 0.25f)
        {
            if (IsFree(p + Vector2.left * d, radius, col)) { MoveTo(p + Vector2.left * d); return; }
            if (IsFree(p + Vector2.right * d, radius, col)) { MoveTo(p + Vector2.right * d); return; }
        }
    }

    static bool IsFree(Vector2 point, float radius, Collider2D self)
    {
        foreach (var c in Physics2D.OverlapCircleAll(point, radius))
            if (c != self && !c.isTrigger) return false;
        return true;
    }

    void MoveTo(Vector2 point)
    {
        player.position = point;
        player.transform.position = new Vector3(point.x, point.y, player.transform.position.z);
    }
}
