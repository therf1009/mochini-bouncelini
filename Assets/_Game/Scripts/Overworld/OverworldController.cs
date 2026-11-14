using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Mochi-ul in open world (3D, camera de sus, inclinata).
/// WASD/sageti = mers relativ la camera, SPACE = salt.
///
/// Repere (cu valorile implicite):
///   salt simplu: ~1.6 sus, ~4.3 in lungime
/// </summary>
[RequireComponent(typeof(CharacterController))]
public class OverworldController : MonoBehaviour
{
    [Header("Miscare")]
    public float moveSpeed = 6f;
    public float accel = 50f;
    public float gravity = -25f;
    public float jumpHeight = 1.6f;
    public float coyoteTime = 0.08f;
    public float jumpBuffer = 0.1f;

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
    Vector3 lastSafePos;
    bool wasGrounded;
    float squash;   // >0 turtit, <0 alungit

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
        ReadInput(out Vector2 input, out bool jumpPressed);

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
            if (vertVel < 0f) vertVel = -2f;   // tine-l lipit de sol
            lastSafePos = transform.position;
            if (!wasGrounded) squash = 0.25f;  // aterizare
        }
        wasGrounded = grounded;

        if (jumpPressed) lastJumpPressTime = Time.time;

        horizVel = Vector3.MoveTowards(horizVel, wish, accel * Time.deltaTime);

        bool buffered = Time.time - lastJumpPressTime <= jumpBuffer;
        bool coyote = Time.time - lastGroundedTime <= coyoteTime;
        if (buffered && coyote)
        {
            Jump();
            lastGroundedTime = -99f;
        }

        vertVel += gravity * Time.deltaTime;

        cc.Move((horizVel + Vector3.up * vertVel) * Time.deltaTime);

        // a lovit tavanul -> nu mai urca
        if ((cc.collisionFlags & CollisionFlags.Above) != 0 && vertVel > 0f) vertVel = 0f;

        if (transform.position.y < killY) Respawn();

        UpdateVisual();
    }

    void ReadInput(out Vector2 input, out bool jumpPressed)
    {
        input = Vector2.zero;
        jumpPressed = false;

        var kb = Keyboard.current;
        if (kb != null)
        {
            if (kb.aKey.isPressed || kb.leftArrowKey.isPressed) input.x -= 1f;
            if (kb.dKey.isPressed || kb.rightArrowKey.isPressed) input.x += 1f;
            if (kb.wKey.isPressed || kb.upArrowKey.isPressed) input.y += 1f;
            if (kb.sKey.isPressed || kb.downArrowKey.isPressed) input.y -= 1f;
            jumpPressed = kb.spaceKey.wasPressedThisFrame;
        }

        var pad = Gamepad.current;
        if (pad != null)
        {
            Vector2 stick = pad.leftStick.ReadValue();
            if (stick.sqrMagnitude > 0.04f) input = stick;
            jumpPressed |= pad.buttonSouth.wasPressedThisFrame;
        }

        input = Vector2.ClampMagnitude(input, 1f);
    }

    void Jump()
    {
        vertVel = Mathf.Sqrt(2f * -gravity * jumpHeight);
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
    }

    void UpdateVisual()
    {
        if (visual == null) return;

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
