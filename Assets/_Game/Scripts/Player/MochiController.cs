using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Miscarea mochi-ului: hop precis, charge launch si wall bounce.
/// Traieste mereu in planul camerei (Rigidbody2D), adancimea nu conteaza —
/// exact ca in Fez. Verbele sunt bool-uri separate pentru ca Directorul
/// le va putea scoate pe rand in timpul jocului.
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(CircleCollider2D))]
public class MochiController : MonoBehaviour
{
    [Header("Verbe — Directorul le poate dezactiva")]
    public bool hopEnabled = true;
    public bool chargeLaunchEnabled = true;
    public bool wallBounceEnabled = true;
    public bool softLandingEnabled = true;
    public bool doubleBounceEnabled = false;  // al doilea impuls in aer. IMPLICIT OPRIT — se aprinde ca abilitate la nivelul 2.

    [Header("Deplasare")]
    public float moveSpeed = 7f;
    public float groundAccel = 90f;
    public float groundDecel = 70f;
    public float airAccel = 32f;

    [Header("Hop")]
    public float hopVelocity = 11f;
    public float gravityUp = 3.2f;
    public float gravityDown = 4.6f;
    public float coyoteTime = 0.10f;
    public float jumpBuffer = 0.12f;

    [Header("Charge Launch")]
    public float chargeStartDelay = 0.14f;
    public float maxChargeTime = 0.55f;
    public float minLaunchSpeed = 13f;
    public float maxLaunchSpeed = 18f;
    [Range(0f, 1f)] public float chargeMoveDamp = 0.25f;
    [Range(0f, 2f)] public float launchHorizontalBias = 0.72f;

    [Header("Wall Bounce")]
    [Range(0f, 1f)] public float wallBounciness = 0.82f;
    public float minWallBounceSpeed = 6f;
    public float wallBounceLift = 2.5f;
    public float wallBounceControlLock = 0.2f;   // cat timp dupa bounce nu poti trage inapoi spre perete

    [Header("Detectie")]
    public LayerMask solidMask = ~0;
    public float groundCheckDistance = 0.10f;

    [Header("Feel")]
    public Transform visual;
    public float squashAmount = 0.32f;
    public float squashRecovery = 9f;
    public float landShakeScale = 0.35f;
    public float hitStopDuration = 0.045f;

    // --- stare interna ---
    Rigidbody2D rb;
    CircleCollider2D col;

    float moveInput;
    bool jumpHeld, jumpPressedThisFrame, jumpReleasedThisFrame;

    float lastGroundedTime = -99f;
    float lastJumpPressedTime = -99f;
    float holdTime;
    bool isCharging;
    bool isGrounded;
    int jumpsUsed;         // cate salturi s-au folosit de la ultima aterizare

    float squash;          // >0 turtit, <0 alungit
    float hitStopTimer;
    float airControlLock;  // >0 imediat dupa wall bounce
    Vector2 preStepVelocity;   // viteza cu care intra in pasul de fizica (pentru wall bounce)

    public bool IsGrounded => isGrounded;
    public bool IsCharging => isCharging;
    public float ChargeRatio => Mathf.Clamp01((holdTime - chargeStartDelay) / Mathf.Max(0.01f, maxChargeTime));
    int maxJumps => doubleBounceEnabled ? 2 : 1;   // 1 de sol + 1 in aer = maxim dublu

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        col = GetComponent<CircleCollider2D>();
        rb.freezeRotation = true;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        rb.interpolation = RigidbodyInterpolation2D.Interpolate;
        if (visual == null && transform.childCount > 0) visual = transform.GetChild(0);
    }

    void Update()
    {
        ReadInput();

        if (jumpPressedThisFrame) lastJumpPressedTime = Time.time;

        // Charge: tine apasat cat timp esti pe sol
        if (chargeLaunchEnabled && jumpHeld && isGrounded)
        {
            holdTime += Time.deltaTime;
            if (holdTime >= chargeStartDelay) isCharging = true;
        }

        if (jumpReleasedThisFrame)
        {
            if (isCharging) { Launch(); jumpsUsed = Mathf.Max(jumpsUsed, 1); }
            else if (hopEnabled && CanGroundHop()) Hop();
            holdTime = 0f;
            isCharging = false;
        }

        // Hop de sol din buffer (apasare scurta, inainte sa inceapa charge-ul)
        if (!isCharging && hopEnabled && jumpHeld && holdTime < chargeStartDelay && CanGroundHop())
            Hop();

        // Double bounce: al doilea impuls in aer, pe apasare. Plafon dur = maxim dublu salt.
        if (hopEnabled && doubleBounceEnabled && jumpPressedThisFrame
            && !isGrounded && !CanGroundHop() && jumpsUsed < maxJumps)
            AirHop();

        UpdateVisual();
    }

    void FixedUpdate()
    {
        if (airControlLock > 0f) airControlLock -= Time.fixedDeltaTime;

        if (hitStopTimer > 0f)
        {
            hitStopTimer -= Time.fixedDeltaTime;
            preStepVelocity = rb.linearVelocity;
            return;
        }

        GroundCheck();
        ApplyHorizontal();
        ApplyGravity();

        // retinuta ACUM, inainte de pasul de fizica: dupa ciocnire viteza spre perete e deja ~0
        preStepVelocity = rb.linearVelocity;
    }

    void ReadInput()
    {
        var kb = Keyboard.current;
        if (kb == null) { moveInput = 0f; return; }

        float x = 0f;
        if (kb.aKey.isPressed || kb.leftArrowKey.isPressed) x -= 1f;
        if (kb.dKey.isPressed || kb.rightArrowKey.isPressed) x += 1f;
        moveInput = x;

        bool held = kb.spaceKey.isPressed || kb.wKey.isPressed || kb.upArrowKey.isPressed;
        jumpPressedThisFrame = held && !jumpHeld;
        jumpReleasedThisFrame = !held && jumpHeld;
        jumpHeld = held;
    }

    void GroundCheck()
    {
        bool wasGrounded = isGrounded;
        Vector2 origin = rb.position + Vector2.down * (col.radius * 0.35f);
        var hits = Physics2D.CircleCastAll(
            origin, col.radius * 0.92f, Vector2.down, groundCheckDistance, solidMask);

        // ignora propriul collider — altfel cast-ul se loveste de el si crede ca e mereu
        // pe sol (asta era cauza salturilor infinite)
        bool onSolid = false;
        foreach (var h in hits)
        {
            if (h.collider == null || h.collider == col) continue;
            onSolid = true;
            break;
        }

        isGrounded = onSolid && rb.linearVelocityY <= 0.01f;

        if (isGrounded)
        {
            lastGroundedTime = Time.time;
            if (!wasGrounded) OnLand(Mathf.Abs(rb.linearVelocityY));
        }
    }

    void ApplyHorizontal()
    {
        // imediat dupa wall bounce, lasa bounce-ul sa duca (altfel tasta tinuta te trage inapoi in perete)
        if (airControlLock > 0f && !isGrounded) return;

        float target = moveInput * moveSpeed;
        if (isCharging) target *= chargeMoveDamp;

        float accel;
        if (!isGrounded) accel = airAccel;
        else accel = Mathf.Abs(target) > 0.01f ? groundAccel : groundDecel;

        rb.linearVelocityX = Mathf.MoveTowards(rb.linearVelocityX, target, accel * Time.fixedDeltaTime);
    }

    void ApplyGravity()
    {
        rb.gravityScale = rb.linearVelocityY > 0.01f ? gravityUp : gravityDown;
    }

    bool CanGroundHop()
    {
        bool hasCoyote = Time.time - lastGroundedTime <= coyoteTime;
        bool hasBuffer = Time.time - lastJumpPressedTime <= jumpBuffer;
        return hasCoyote && hasBuffer;
    }

    void Hop()
    {
        rb.linearVelocityY = hopVelocity;
        lastGroundedTime = -99f;
        lastJumpPressedTime = -99f;
        isGrounded = false;
        jumpsUsed = 1;                   // primul salt consumat
        squash = -squashAmount * 0.8f;   // se alungeste la desprindere
    }

    void AirHop()
    {
        rb.linearVelocityY = hopVelocity;   // reseteaza caderea -> al doilea salt clar
        jumpsUsed++;                        // al doilea salt consumat (plafon la maxJumps)
        lastJumpPressedTime = -99f;
        squash = -squashAmount * 0.7f;
        CameraShake.Shake(0.08f);
    }

    void Launch()
    {
        float t = ChargeRatio;
        float speed = Mathf.Lerp(minLaunchSpeed, maxLaunchSpeed, t);
        Vector2 dir = new Vector2(moveInput * launchHorizontalBias, 1f).normalized;

        rb.linearVelocity = dir * speed;
        lastGroundedTime = -99f;
        lastJumpPressedTime = -99f;
        isGrounded = false;
        squash = -squashAmount * (0.9f + t * 0.6f);
        CameraShake.Shake(0.12f + t * 0.18f);
    }

    void OnLand(float impactSpeed)
    {
        jumpsUsed = 0;                 // reseteaza salturile la aterizare
        float strength = Mathf.InverseLerp(2f, 18f, impactSpeed);
        squash = squashAmount * (0.4f + strength);
        if (strength > 0.05f) CameraShake.Shake(strength * landShakeScale);
        if (!softLandingEnabled && strength > 0.5f) hitStopTimer = hitStopDuration;
    }

    void OnCollisionEnter2D(Collision2D c)
    {
        if (!wallBounceEnabled || isGrounded) return;

        // Cand se apeleaza asta, fizica a rezolvat deja ciocnirea: rb.linearVelocity spre perete e ~0.
        // De aceea bounce-ul se calculeaza din viteza de DINAINTE de impact (preStepVelocity).
        for (int i = 0; i < c.contactCount; i++)
        {
            Vector2 normal = c.GetContact(i).normal;

            if (Mathf.Abs(normal.x) < 0.7f) continue;            // perete = normala aproape orizontala
            if (preStepVelocity.x * normal.x >= 0f) continue;    // nu veneai spre perete

            float incoming = Mathf.Abs(preStepVelocity.x);
            if (incoming < minWallBounceSpeed) continue;

            float vy = preStepVelocity.y;
            if (vy < 0f) vy += wallBounceLift;
            rb.linearVelocity = new Vector2(Mathf.Sign(normal.x) * incoming * wallBounciness, vy);
            airControlLock = wallBounceControlLock;

            squash = squashAmount * 0.9f;
            hitStopTimer = hitStopDuration;
            CameraShake.Shake(0.15f);
            return;
        }
    }

    void UpdateVisual()
    {
        if (visual == null) return;

        squash = Mathf.MoveTowards(squash, 0f, squashRecovery * Time.deltaTime * (Mathf.Abs(squash) + 0.3f));

        // in aer, alungire usoara dupa viteza verticala
        float stretch = squash;
        if (!isGrounded)
            stretch += Mathf.Clamp(-rb.linearVelocityY * 0.012f, -0.18f, 0.18f);

        // charge = turtire progresiva
        if (isCharging)
            stretch = Mathf.Lerp(stretch, squashAmount * 1.15f, ChargeRatio);

        float sx = 1f + stretch;
        float sy = 1f - stretch;
        visual.localScale = new Vector3(sx, sy, sx);
    }

    void OnDrawGizmosSelected()
    {
        var c = GetComponent<CircleCollider2D>();
        if (c == null) return;
        Gizmos.color = Color.cyan;
        Vector3 o = transform.position + Vector3.down * (c.radius * 0.35f + groundCheckDistance);
        Gizmos.DrawWireSphere(o, c.radius * 0.92f);
    }
}
