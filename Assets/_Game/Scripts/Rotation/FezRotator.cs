using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Q / E (sau LB / RB pe gamepad) rotesc lumea la 90 de grade, doar cand mochi-ul e in camera rotativa.
///
/// Regulile din documentul de design:
///  - timpul se opreste pe durata rotirii (tween de 0.4 s)
///  - ca in Fez: mochi-ul isi pastreaza pozitia IN LUME. Daca stai pe un bloc, dupa rotire stai tot pe el
///    (doar ca blocul e acum in alt loc pe ecran)
///  - daca dupa rotire ai intra intr-un bloc, rotirea se refuza (tresarire mica + evenimentul Denied)
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
    public float cooldown = 0.6f;

    /// <summary>Rotatorul din scena (unul pe scena).</summary>
    public static FezRotator Active { get; private set; }
    /// <summary>Blocheaza rotirea (ex. in timpul unei scene cinematice).</summary>
    public static bool Locked;

    // fara reincarcarea domeniului la Play (setarea proiectului), staticele raman de la rularea trecuta: le golim
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetStatics() { Active = null; Locked = false; }

    int angle;
    float yaw;
    bool rotating;
    float lastRotateTime = -99f;
    readonly List<Renderer> hidden = new List<Renderer>();

    public int Angle => angle;
    public float Yaw => yaw;
    public bool IsRotating => rotating;

    public event System.Action<int> AngleChanged;

    void Awake() { Active = this; Locked = false; }
    void OnDestroy() { if (Active == this) Active = null; }

    void Start()
    {
        if (hideWhenRotated != null)
            foreach (Transform t in hideWhenRotated)
                if (t != null) hidden.AddRange(t.GetComponentsInChildren<Renderer>(true));

        var respawn = player != null ? player.GetComponent<Respawn2D>() : null;
        if (respawn != null) respawn.Respawned += () => OnRespawn(respawn);
    }

    /// <summary>La respawn: revii la unghiul si la pozitia 3D a checkpoint-ului (FezCheckpoint), altfel la unghiul 0.</summary>
    void OnRespawn(Respawn2D respawn)
    {
        if (!respawn.hasWorldCheckpoint || room == null) { SnapTo(0); return; }
        SnapTo(respawn.checkpointAngle);
        Vector3 w = respawn.checkpointWorld;
        MoveTo(room.ToPhysics(w, angle));
        if (view == null) view = player.GetComponent<FezMochiView>();
        if (view != null) view.depth = room.DepthOf(w, angle);
        Physics2D.SyncTransforms();
        PushOutOfBlocks();
    }

    void OnDisable()
    {
        if (rotating) { Time.timeScale = 1f; rotating = false; }
    }

    void Update()
    {
        if (rotating || room == null || player == null || Locked) return;
        if (Time.timeScale == 0f || DialogueUI.IsOpen || DialogueUI.JustClosed) return;   // nu rotesti in dialog

        int dir = ReadDirection();
        if (dir == 0) return;
        if (Time.unscaledTime - lastRotateTime < cooldown) return;
        if (!room.ContainsU(player.position.x, 0.4f)) return;   // rotesti doar in camera rotativa

        // unde ajunge mochi dupa rotire: aceeasi pozitie 3D, vazuta din noul unghi
        int target = (angle + dir + 4) % 4;
        Vector3 world = TargetWorldPosition();
        if (!SafeWorldPosition(target, ref world))
        {
            // ai intra intr-un bloc: nu rotesti (o tresarire mica, ca sa simti ca lumea "nu vrea")
            lastRotateTime = Time.unscaledTime - cooldown + 0.25f;
            CameraShake.Shake(0.06f);
            Denied?.Invoke(dir);
            GameAudio.Sfx("rotate", 0.35f, 0.7f);
            return;
        }

        StartCoroutine(Rotate(dir, world));
    }

    /// <summary>Rotire ceruta de joc (ex. boss-ul: "firewall-ul" te intoarce cu fata la el). Merge si cand rotirea e blocata
    /// pentru jucator (Locked). Un pas de 90 de grade spre unghiul dorit, pe drumul cel mai scurt; false = nu s-a putut acum.</summary>
    public bool ForceRotateToward(int target)
    {
        if (rotating || room == null || player == null) return false;
        int diff = ((target - angle) % 4 + 4) % 4;
        if (diff == 0) return false;
        int dir = diff == 3 ? -1 : 1;
        int next = (angle + dir + 4) % 4;
        Vector3 world = TargetWorldPosition();
        if (!SafeWorldPosition(next, ref world)) return false;
        StartCoroutine(Rotate(dir, world));
        return true;
    }

    bool FreeAt(int target, Vector3 world) =>
        !room.Overlaps(target, new Vector2(room.ToPhysics(world, target).x, player.position.y), PlayerRadius() * 0.9f);

    /// <summary>
    /// Unde poate sta mochi dupa rotire fara sa fie intr-un bloc. Regula: RAMAI PE PLATFORMA TA.
    ///  1) acelasi loc 3D, daca e liber;
    ///  2) pe sol: cel mai apropiat bloc liber DE PE ACEEASI PLATFORMA (blocurile lipite de cel pe care stai, la aceeasi inaltime);
    ///     mochi aluneca acolo cat dureaza rotirea;
    ///  3) pe o platforma pe sina: alt punct de pe ea;
    ///  in aer: doar acelasi loc (o deplasare ar putea trece prin pereti subtiri).
    /// Daca nimic nu e liber, rotirea se refuza (nu te bagam niciodata intr-un bloc si nu te dam jos de pe platforma).
    /// </summary>
    bool SafeWorldPosition(int target, ref Vector3 world)
    {
        if (FreeAt(target, world)) return true;
        Vector3 start = world;

        if (view != null && view.Platform != null)
        {
            Vector3 c = view.Platform.Center, half = view.Platform.Size * 0.5f;
            Vector3 best = world; float bestD = float.MaxValue;
            for (float x = -half.x + 0.3f; x <= half.x - 0.3f + 1e-3f; x += 0.25f)
                for (float z = -half.z + 0.3f; z <= half.z - 0.3f + 1e-3f; z += 0.25f)
                {
                    var p = new Vector3(c.x + x, world.y, c.z + z);
                    float d = (p - start).sqrMagnitude;
                    if (d < bestD && FreeAt(target, p)) { best = p; bestD = d; }
                }
            if (bestD < float.MaxValue) { world = best; return true; }
            return false;
        }

        if (view != null && view.HasSupport)
        {
            Vector3Int bestCell = default; float bestD = float.MaxValue;
            foreach (var cell in room.PlatformCells(view.SupportCell, 64))
            {
                var p = new Vector3(cell.x + 0.5f, world.y, cell.z + 0.5f);
                float d = (p - start).sqrMagnitude;
                if (d < bestD && FreeAt(target, p)) { bestCell = cell; bestD = d; }
            }
            if (bestD < float.MaxValue) { world = new Vector3(bestCell.x + 0.5f, world.y, bestCell.z + 0.5f); return true; }
            return false;
        }

        // in aer nu mutam nimic: o deplasare ar putea trece prin pereti subtiri (ex. usa glitch a altarului)
        return false;
    }

    /// <summary>Rotirea a fost refuzata (te-ar fi bagat intr-un bloc). Parametrul = directia ceruta.</summary>
    public event System.Action<int> Denied;

    float PlayerRadius()
    {
        var c = player.GetComponent<CircleCollider2D>();
        return c != null ? c.radius : 0.5f;
    }

    /// <summary>Pozitia 3D a mochi-ului. Daca sta pe un bloc, il tinem deasupra blocului (nu pe margine),
    /// ca dupa rotire sa fie tot pe el - ca in Fez, pleci cu tot cu platforma.</summary>
    Vector3 TargetWorldPosition()
    {
        if (view == null) view = player.GetComponent<FezMochiView>();
        if (view == null)
            return room.ToWorld(player.position.x, player.position.y, 0f, angle * 90f);

        Vector3 world = view.WorldPosition();
        if (view.Platform != null)
        {
            // pe o platforma pe sina: ramai deasupra ei dupa rotire
            Vector3 c = view.Platform.Center, half = view.Platform.Size * 0.5f;
            world.x = Mathf.Clamp(world.x, c.x - half.x + 0.1f, c.x + half.x - 0.1f);
            world.z = Mathf.Clamp(world.z, c.z - half.z + 0.1f, c.z + half.z - 0.1f);
        }
        else if (view.HasSupport)
        {
            Vector3Int c = view.SupportCell;
            world.x = Mathf.Clamp(world.x, c.x + 0.1f, c.x + 0.9f);
            world.z = Mathf.Clamp(world.z, c.z + 0.1f, c.z + 0.9f);
        }
        return world;
    }

    FezMochiView view;

    static int ReadDirection()
    {
        var kb = Keyboard.current;
        var pad = Gamepad.current;
        if ((kb != null && kb.qKey.wasPressedThisFrame) || (pad != null && pad.leftShoulder.wasPressedThisFrame)) return -1;
        if ((kb != null && kb.eKey.wasPressedThisFrame) || (pad != null && pad.rightShoulder.wasPressedThisFrame)) return 1;
        return 0;
    }

    IEnumerator Rotate(int dir, Vector3 world)
    {
        rotating = true;
        GameAudio.Sfx("rotate", 0.7f, dir > 0 ? 1.06f : 0.94f);
        lastRotateTime = Time.unscaledTime;
        float previousScale = Time.timeScale > 0f ? Time.timeScale : 1f;
        Time.timeScale = 0f;

        int target = (angle + dir + 4) % 4;
        // mochi sta pe loc in lume (pe blocul lui) cat se roteste camera
        if (view != null) view.depth = room.DepthOf(world, angle);
        MoveTo(new Vector2(room.ToPhysics(world, angle).x, player.position.y));
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

        // aceeasi pozitie 3D, citita din noul unghi: alta coloana pe ecran, alta adancime, acelasi bloc sub picioare
        MoveTo(new Vector2(room.ToPhysics(world, angle).x, player.position.y));
        if (view != null) view.depth = room.DepthOf(world, angle);

        Physics2D.SyncTransforms();
        PushOutOfBlocks();   // plasa de siguranta (nu ar trebui sa mai fie nevoie: rotirea se refuza inainte)
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
