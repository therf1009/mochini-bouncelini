using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Mochi-ul in open world (3D, camera de sus, inclinata).
/// WASD/sageti = mers relativ la camera, SPACE = salt, SHIFT = dash (daca e deblocat).
///
/// Abilitatile sunt sloturi oprite implicit; le porneste progresul (AbilityLoader):
///   canDash        -> castigat la nivelul 1 (trece groapa dintre insula 1 si 2)
///   canDoubleJump  -> castigat la nivelul 2 (urca platoul insulei 3)
///
/// Repere (cu valorile implicite):
///   salt simplu: ~1.6 sus, ~4.3 in lungime
///   salt + dash: ~7.5 in lungime
///   double jump: ~3.2 sus
/// </summary>
[RequireComponent(typeof(CharacterController))]
public class OverworldController : MonoBehaviour
{
    [Header("Abilitati (le seteaza progresul)")]
    public bool canDash;
    public bool canDoubleJump;

    [Header("Miscare")]
    public float moveSpeed = 6f;
    public float accel = 50f;
    public float gravity = -25f;
    public float jumpHeight = 1.6f;
    public float coyoteTime = 0.08f;
    public float jumpBuffer = 0.1f;

    [Header("Dash")]
    public float dashSpeed = 18f;
    public float dashTime = 0.18f;
    public float dashCooldown = 0.35f;

    [Header("Respawn")]
    public float killY = -8f;

    [Header("Vizual")]
    public Transform visual;
    public float turnSpeed = 14f;

    CharacterController cc;
    Transform cam;

    Vector3 horizVel;
    float vertVel;
    float lastGroundedTime = -99f;
    float lastJumpPressTime = -99f;
    int airJumpsUsed;
    bool airDashUsed;
    float dashTimer, dashCooldownTimer;
    Vector3 dashDir;
    Vector3 lastSafePos;
    bool wasGrounded;
    float squash;   // >0 turtit, <0 alungit

    public bool IsDashing => dashTimer > 0f;

    void Awake()
    {
        cc = GetComponent<CharacterController>();
        lastSafePos = transform.position;
        if (visual == null && transform.childCount > 0) visual = transform.GetChild(0);
    }

    void Start()
    {
        if (Camera.main != null) cam = Camera.main.transform;
    }

    void Update()
    {
        ReadInput(out Vector2 input, out bool jumpPressed, out bool dashPressed);

        // directia de mers e relativa la camera, proiectata pe sol
        Vector3 fwd = Vector3.forward, right = Vector3.right;
        if (cam != null)
        {
            fwd = Vector3.ProjectOnPlane(cam.forward, Vector3.up).normalized;
            right = Vector3.ProjectOnPlane(cam.right, Vector3.up).normalized;
        }
        Vector3 wish = (fwd * input.y + right * input.x) * moveSpeed;

        bool grounded = cc.isGrounded;
        if (grounded)
        {
            lastGroundedTime = Time.time;
            airJumpsUsed = 0;
            airDashUsed = false;
            if (vertVel < 0f) vertVel = -2f;   // tine-l lipit de sol
            lastSafePos = transform.position;
            if (!wasGrounded) squash = 0.25f;  // aterizare
        }
        wasGrounded = grounded;

        if (jumpPressed) lastJumpPressTime = Time.time;
        dashCooldownTimer -= Time.deltaTime;

        // ---- dash: o data in aer, oricand pe sol (cu cooldown)
        bool dashAllowed = canDash && dashTimer <= 0f && dashCooldownTimer <= 0f && (grounded || !airDashUsed);
        if (dashPressed && dashAllowed)
        {
            dashDir = wish.sqrMagnitude > 0.01f ? wish.normalized : FacingDir(fwd);
            dashTimer = dashTime;
            dashCooldownTimer = dashCooldown;
            if (!grounded) airDashUsed = true;
            squash = -0.3f;
        }

        if (dashTimer > 0f)
        {
            dashTimer -= Time.deltaTime;
            horizVel = dashDir * dashSpeed;
            vertVel = 0f;   // dash-ul e orizontal: nu urca, nu cade
        }
        else
        {
            horizVel = Vector3.MoveTowards(horizVel, wish, accel * Time.deltaTime);

            bool buffered = Time.time - lastJumpPressTime <= jumpBuffer;
            bool coyote = Time.time - lastGroundedTime <= coyoteTime;
            if (buffered && coyote)
            {
                Jump();
                lastGroundedTime = -99f;
            }
            else if (jumpPressed && !coyote && canDoubleJump && airJumpsUsed < 1)
            {
                Jump();
                airJumpsUsed++;
            }

            vertVel += gravity * Time.deltaTime;
        }

        cc.Move((horizVel + Vector3.up * vertVel) * Time.deltaTime);

        // a lovit tavanul -> nu mai urca
        if ((cc.collisionFlags & CollisionFlags.Above) != 0 && vertVel > 0f) vertVel = 0f;

        if (transform.position.y < killY) Respawn();

        UpdateVisual();
    }

    void ReadInput(out Vector2 input, out bool jumpPressed, out bool dashPressed)
    {
        input = Vector2.zero;
        jumpPressed = false;
        dashPressed = false;

        var kb = Keyboard.current;
        if (kb != null)
        {
            if (kb.aKey.isPressed || kb.leftArrowKey.isPressed) input.x -= 1f;
            if (kb.dKey.isPressed || kb.rightArrowKey.isPressed) input.x += 1f;
            if (kb.wKey.isPressed || kb.upArrowKey.isPressed) input.y += 1f;
            if (kb.sKey.isPressed || kb.downArrowKey.isPressed) input.y -= 1f;
            jumpPressed = kb.spaceKey.wasPressedThisFrame;
            dashPressed = kb.leftShiftKey.wasPressedThisFrame || kb.rightShiftKey.wasPressedThisFrame;
        }

        var pad = Gamepad.current;
        if (pad != null)
        {
            Vector2 stick = pad.leftStick.ReadValue();
            if (stick.sqrMagnitude > 0.04f) input = stick;
            jumpPressed |= pad.buttonSouth.wasPressedThisFrame;
            dashPressed |= pad.buttonWest.wasPressedThisFrame || pad.rightShoulder.wasPressedThisFrame;
        }

        input = Vector2.ClampMagnitude(input, 1f);
    }

    void Jump()
    {
        vertVel = Mathf.Sqrt(2f * -gravity * jumpHeight);
        GameAudio.Sfx("jump", 0.6f, cc.isGrounded ? 1f : 1.25f);
        lastJumpPressTime = -99f;
        squash = -0.22f;
    }

    void Respawn()
    {
        cc.enabled = false;
        transform.position = lastSafePos + Vector3.up * 0.5f;
        cc.enabled = true;
        horizVel = Vector3.zero;
        vertVel = 0f;
        dashTimer = 0f;
    }

    Vector3 FacingDir(Vector3 fallback)
    {
        if (visual == null) return fallback;
        Vector3 f = Vector3.ProjectOnPlane(visual.forward, Vector3.up);
        return f.sqrMagnitude > 0.01f ? f.normalized : fallback;
    }

    // --- rostogolire (corpul se rostogoleste, ochii raman in fata) si "idle" viu ---
    Transform bodyPart, facePart;
    float owStep; bool owAlt;
    float roll, idleTime, lookYaw, lookTarget, lookTimer = 2f;
    Vector3 faceBase;

    void UpdateVisual()
    {
        if (visual == null) return;
        float dt = Time.deltaTime;
        if (bodyPart == null) { bodyPart = visual.Find("Body"); facePart = visual.Find("Face"); if (facePart != null) faceBase = facePart.localPosition; }

        // corpul e o minge: se rostogoleste exact cat a mers (unghi = distanta / raza)
        float speed = new Vector3(horizVel.x, 0f, horizVel.z).magnitude;
        roll += speed * dt / 0.5f * Mathf.Rad2Deg;
        if (MochiSkins.CurrentIsImage) roll = 0f;   // cu o poza pe el nu se rostogoleste: fata ramane in fata
        bool idle = speed < 0.2f && cc.isGrounded;
        if (cc.isGrounded && speed > 1.5f)
        {
            owStep -= dt;
            if (owStep <= 0f) { owStep = 0.34f; owAlt = !owAlt; GameAudio.Sfx(owAlt ? "step_a" : "step_b", 0.2f, 1f, 0.08f); }
        }
        else owStep = 0.05f;
        idleTime = idle ? idleTime + dt : 0f;
        float settle = Mathf.Clamp01(idleTime * 1.5f);
        // pe loc: se leagana usor inainte-inapoi, ca o minge care nu sta niciodata chiar locului
        float rock = Mathf.Sin(Time.time * 1.4f) * 9f * settle;
        if (bodyPart != null) bodyPart.localRotation = Quaternion.Euler(roll + rock, 0f, Mathf.Sin(Time.time * 0.9f) * 4f * settle);

        // fata: respira odata cu corpul si, din cand in cand, se uita in jur
        if (idle)
        {
            lookTimer -= dt;
            if (lookTimer <= 0f) { lookTimer = Random.Range(1.8f, 4f); lookTarget = Random.value < 0.4f ? 0f : Random.Range(-35f, 35f); }
        }
        else lookTarget = 0f;
        lookYaw = Mathf.Lerp(lookYaw, lookTarget, 1f - Mathf.Exp(-6f * dt));
        if (facePart != null)
        {
            facePart.localRotation = Quaternion.Euler(Mathf.Sin(Time.time * 1.4f) * 3f * settle, lookYaw, 0f);
            facePart.localPosition = faceBase + Vector3.up * (Mathf.Sin(Time.time * 2.4f) * 0.025f * settle);
        }
        if (idle) squash = Mathf.Max(squash, 0.03f * (0.5f + 0.5f * Mathf.Sin(Time.time * 2.4f)));

        Vector3 flat = new Vector3(horizVel.x, 0f, horizVel.z);
        if (flat.sqrMagnitude > 0.1f)
        {
            Quaternion target = Quaternion.LookRotation(flat);
            visual.rotation = Quaternion.Slerp(visual.rotation, target, 1f - Mathf.Exp(-turnSpeed * Time.deltaTime));
        }

        squash = Mathf.MoveTowards(squash, 0f, 2.5f * Time.deltaTime);
        float sx = 1f + squash;
        float sy = 1f - squash;
        visual.localScale = new Vector3(sx, sy, sx);
    }
}
