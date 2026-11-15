using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Miscarea mochi-ului: hop precis, charge launch, wall bounce, dash, double bounce si wall stick.
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
    public bool dashEnabled = false;          // dash orizontal (SHIFT). IMPLICIT OPRIT — se castiga la nivelul 1.
    public bool wallStickEnabled = false;     // tii SPACE langa perete: te lipesti; dai drumul: te lansezi. Nivelul 3.

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

    [Header("Charge Launch (W)")]
    public float minChargeHold = 0.08f;          // sub atat, eliberarea lui W nu lanseaza (atingere accidentala)
    public float maxChargeTime = 0.55f;
    public float minLaunchSpeed = 13f;
    public float maxLaunchSpeed = 18f;
    [Range(0f, 1f)] public float chargeMoveDamp = 0.25f;
    [Range(0f, 2f)] public float launchHorizontalBias = 0.72f;

    [Header("Dash (abilitatea de la nivelul 1)")]
    public float dashSpeed = 16f;
    public float dashTime = 0.18f;
    public float dashCooldown = 0.3f;

    [Header("Wall Bounce")]
    [Range(0f, 1f)] public float wallBounciness = 0.82f;
    public float minWallBounceSpeed = 6f;
    public float wallBounceLift = 2.5f;
    public float wallBounceControlLock = 0.2f;   // cat timp dupa bounce nu poti trage inapoi spre perete

    [Header("Wall Stick (nivelul 3)")]
    public float wallChargeTime = 0.45f;                  // cat tii lipit pana la lansarea maxima
    public Vector2 wallLaunchMin = new Vector2(6f, 11f);  // (departe de perete, in sus)
    public Vector2 wallLaunchMax = new Vector2(9f, 15.5f);
    public float wallCheckDistance = 0.12f;

    [Header("Detectie")]
    public LayerMask solidMask = ~0;
    public float groundCheckDistance = 0.10f;

    [Header("Feel (jeleu)")]
    public Transform visual;
    public float springStiffness = 260f;                 // cat de repede revine forma
    public float springDamping = 14f;                    // mic = tremura mai mult (jeleu), mare = se opreste sec
    [Range(0f, 0.7f)] public float chargeSquash = 0.45f; // cat de turtit e la charge complet
    public float landShakeScale = 0.35f;
    public float hitStopDuration = 0.045f;
    public float maxLean = 12f;                          // cat se apleaca in directia in care alearga (grade)

    // --- evenimente pentru efecte (DashFx etc.) ---
    public event System.Action<float> Dashed;            // directia
    public event System.Action<float> Landed;            // viteza de impact
    public event System.Action<float> WallStuck;         // partea peretelui (-1 stanga, +1 dreapta)
    public event System.Action<float> WallLaunched;      // forta 0..1

    // --- stare interna ---
    Rigidbody2D rb;
    CircleCollider2D col;

    float moveInput;
    bool jumpHeld, jumpPressedThisFrame;          // SPACE / sageata sus = hop
    bool chargeHeld, chargeReleasedThisFrame;     // W = charge

    float lastGroundedTime = -99f;
    float lastJumpPressedTime = -99f;
    float lastTakeoffTime = -99f;
    float holdTime;
    bool isCharging;
    bool isGrounded;
    int jumpsUsed;         // cate salturi s-au folosit de la ultima aterizare

    float squash;          // >0 turtit, <0 alungit
    float squashVel;       // viteza arcului care conduce forma (jeleul)
    Vector3 visualBasePos;
    float groundOffset;    // cat coboara vizualul ca talpa sa ramana pe sol cand e turtit
    float hitStopTimer;
    float airControlLock;  // >0 imediat dupa wall bounce
    bool dashPressedThisFrame;
    float facing = 1f;     // ultima directie in care te-ai miscat (pentru dash fara tasta de directie)
    float dashTimer, dashCooldownTimer, dashDir;
    bool airDashUsed;      // un singur dash in aer, pana aterizezi
    bool phasing;          // dash-ul ignora acum peretii glitch
    float dashExtra, lastDashX;
    bool dashMoved;
    Vector2 preStepVelocity;   // viteza cu care intra in pasul de fizica (pentru wall bounce)

    bool wallStuck;
    float wallSide, wallHold;
    float lean, leanVel;
    bool externalRotation; // FezMochiView roteste vizualul (yaw); noi dam doar aplecarea

    public bool IsGrounded => isGrounded;
    public bool IsCharging => isCharging;
    public bool IsDashing => dashTimer > 0f;
    public bool IsWallStuck => wallStuck;
    public float WallSide => wallSide;
    public float Facing => facing;
    public bool CanAirDash => dashEnabled && !airDashUsed;
    public float ChargeRatio => Mathf.Clamp01(holdTime / Mathf.Max(0.01f, maxChargeTime));
    public float WallChargeRatio => Mathf.Clamp01(wallHold / Mathf.Max(0.01f, wallChargeTime));
    /// <summary>Coborarea vizualului spre sol (pentru cine muta vizualul din alta parte, ex. FezMochiView).</summary>
    public float VisualGroundOffset => groundOffset;
    /// <summary>Aplecarea vizualului (grade, pe axa ecranului). FezMochiView o compune cu rotirea camerei.</summary>
    public float VisualLean => lean;
    int maxJumps => doubleBounceEnabled ? 2 : 1;   // 1 de sol + 1 in aer = maxim dublu

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        col = GetComponent<CircleCollider2D>();
        rb.freezeRotation = true;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        rb.interpolation = RigidbodyInterpolation2D.Interpolate;
        if (visual == null && transform.childCount > 0) visual = transform.GetChild(0);
        if (visual != null) visualBasePos = visual.localPosition;
    }

    void Start() => externalRotation = GetComponent<FezMochiView>() != null;

    /// <summary>Cristalul de reincarcare: inca un dash in aer.</summary>
    public void RefreshAirDash()
    {
        airDashUsed = false;
        dashCooldownTimer = 0f;
        Kick(-6f);
    }

    void Update()
    {
        ReadInput();

        if (jumpPressedThisFrame && !isCharging) lastJumpPressedTime = Time.time;

        // Dash: oricand pe sol (cu cooldown), o singura data in aer
        if (dashCooldownTimer > 0f) dashCooldownTimer -= Time.deltaTime;
        if (dashEnabled && dashPressedThisFrame && dashTimer <= 0f && dashCooldownTimer <= 0f && (isGrounded || !airDashUsed))
            StartDash();

        // Charge (W): tii apasat pe sol -> te comprimi; dai drumul -> lansare.
        // Pe tasta lui, separat de hop: nu mai trebuie sa sari intai. Tinut in aer, incepe cand aterizezi.
        if (chargeLaunchEnabled && chargeHeld && isGrounded && dashTimer <= 0f)
        {
            holdTime += Time.deltaTime;
            isCharging = true;
        }

        // ai iesit de pe sol cu W tinut (si a trecut coyote time-ul): charge-ul se pierde
        if (isCharging && !isGrounded && Time.time - lastGroundedTime > coyoteTime)
            CancelCharge();

        if (chargeReleasedThisFrame && isCharging)
        {
            if (holdTime >= minChargeHold) { Launch(); jumpsUsed = Mathf.Max(jumpsUsed, 1); }
            CancelCharge();
        }

        // Hop (SPACE): pe apasare, cu coyote time si jump buffer
        if (!wallStuck && !isCharging && hopEnabled && CanGroundHop())
            Hop();

        // Double bounce: al doilea impuls in aer, pe apasare. Plafon dur = maxim dublu salt.
        // Langa un perete, cu wall stick, SPACE inseamna "lipeste-te", nu al doilea salt.
        bool wallNear = wallStickEnabled && !isGrounded && WallContact() != 0f;
        if (hopEnabled && doubleBounceEnabled && jumpPressedThisFrame && !wallStuck && !wallNear
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

        // in timpul dash-ului: linie dreapta pe orizontala, fara gravitatie, prin peretii glitch
        if (dashTimer > 0f)
        {
            dashTimer -= Time.fixedDeltaTime;
            if (phasing) ApplyPhasing();          // setul glitch activ se poate schimba (rotire): ignora-l mereu
            // nu te opri in mijlocul unui perete glitch: dash-ul mai tine putin pana iesi din el (maxim 0.25 s)
            if (dashTimer <= 0f && phasing && dashExtra < 0.25f && DashPhase.Overlapping(rb.position, col.radius * 0.9f))
            {
                dashTimer = Time.fixedDeltaTime;
                dashExtra += Time.fixedDeltaTime;
            }
            // te-ai lovit de ceva solid (si pe sol): dash-ul se termina
            if (dashMoved && Mathf.Abs(rb.position.x - lastDashX) < 0.02f) dashTimer = 0f;
            dashMoved = true;
            lastDashX = rb.position.x;
            rb.gravityScale = 0f;
            rb.linearVelocity = new Vector2(dashDir * dashSpeed, 0f);
            GroundCheck();
            preStepVelocity = rb.linearVelocity;
            if (dashTimer <= 0f)
            {
                SetPhasing(false);
                rb.gravityScale = gravityDown;
                if (DashPhase.Overlapping(rb.position, col.radius * 0.8f)) EjectFromGlitch();
            }
            return;
        }

        // plasa de siguranta: daca dash-ul s-a oprit (lovitura, rotire) cu mochi in interiorul unui perete glitch,
        // il scoatem afara pe directia dash-ului (sau inapoi), niciodata nu ramane blocat in el
        if (DashPhase.Colliders.Count > 0 && DashPhase.Overlapping(rb.position, col.radius * 0.8f)) EjectFromGlitch();

        GroundCheck();

        if (wallStuck) { UpdateWallStick(); preStepVelocity = rb.linearVelocity; return; }
        if (wallStickEnabled && !isGrounded && jumpHeld && Time.time - lastTakeoffTime > 0.1f)
        {
            float side = WallContact();
            // te lipesti doar daca tragi spre perete sau deja cazi (nu la orice SPACE tinut langa o margine)
            if (side != 0f && (moveInput * side > 0.1f || rb.linearVelocityY <= 0f)) { Stick(side); preStepVelocity = rb.linearVelocity; return; }
        }

        ApplyHorizontal();
        ApplyGravity();

        // retinuta ACUM, inainte de pasul de fizica: dupa ciocnire viteza spre perete e deja ~0
        preStepVelocity = rb.linearVelocity;
    }

    void ReadInput()
    {
        var kb = Keyboard.current;
        if (kb == null) { moveInput = 0f; dashPressedThisFrame = false; chargeReleasedThisFrame = false; return; }

        float x = 0f;
        if (kb.aKey.isPressed || kb.leftArrowKey.isPressed) x -= 1f;
        if (kb.dKey.isPressed || kb.rightArrowKey.isPressed) x += 1f;
        moveInput = x;
        if (x != 0f) facing = Mathf.Sign(x);

        dashPressedThisFrame = kb.leftShiftKey.wasPressedThisFrame || kb.rightShiftKey.wasPressedThisFrame;

        bool jumpKey = kb.spaceKey.isPressed || kb.upArrowKey.isPressed;
        jumpPressedThisFrame = jumpKey && !jumpHeld;
        jumpHeld = jumpKey;

        bool chargeKey = kb.wKey.isPressed;
        chargeReleasedThisFrame = !chargeKey && chargeHeld;
        chargeHeld = chargeKey;
    }

    void GroundCheck()
    {
        bool wasGrounded = isGrounded;
        Vector2 origin = rb.position + Vector2.down * (col.radius * 0.35f);
        var hits = Physics2D.CircleCastAll(
            origin, col.radius * 0.92f, Vector2.down, groundCheckDistance, solidMask);

        // ignora propriul collider — altfel cast-ul se loveste de el si crede ca e mereu
        // pe sol (asta era cauza salturilor infinite). In dash ignora si peretii glitch prin care treci.
        bool onSolid = false;
        float groundVy = 0f;
        foreach (var h in hits)
        {
            if (h.collider == null || h.collider == col || h.collider.isTrigger) continue;
            if (phasing && DashPhase.Contains(h.collider)) continue;
            onSolid = true;
            if (h.rigidbody != null && h.rigidbody.bodyType == RigidbodyType2D.Kinematic) groundVy = h.rigidbody.linearVelocityY;
            break;
        }

        // viteza fata de sol: pe un lift care urca, mochi urca si el, dar sta pe ceva
        isGrounded = onSolid && rb.linearVelocityY - groundVy <= 0.01f;

        if (isGrounded)
        {
            lastGroundedTime = Time.time;
            if (dashTimer <= 0f) airDashUsed = false;   // nu in timpul dash-ului: altfel un dash de pe margine ar lasa inca unul
            if (wallStuck) wallStuck = false;
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
        lastTakeoffTime = Time.time;
        isGrounded = false;
        jumpsUsed = 1;                   // primul salt consumat
        Kick(-9f);                       // se alungeste la desprindere
    }

    /// <summary>Impuls dat arcului formei: pozitiv = se turteste, negativ = se alungeste.</summary>
    void Kick(float impulse) => squashVel += impulse;

    void CancelCharge()
    {
        isCharging = false;
        holdTime = 0f;
    }

    void AirHop()
    {
        rb.linearVelocityY = hopVelocity;   // reseteaza caderea -> al doilea salt clar
        jumpsUsed++;                        // al doilea salt consumat (plafon la maxJumps)
        lastJumpPressedTime = -99f;
        lastTakeoffTime = Time.time;
        Kick(-8f);
        CameraShake.Shake(0.08f);
    }

    void StartDash()
    {
        if (wallStuck) wallStuck = false;
        dashDir = Mathf.Abs(moveInput) > 0.01f ? Mathf.Sign(moveInput) : facing;
        dashTimer = dashTime;
        dashCooldownTimer = dashCooldown;
        airDashUsed = true;             // un dash pe zbor: se reincarca la aterizare sau dintr-un cristal
        isCharging = false;
        holdTime = 0f;
        SetPhasing(true);
        dashExtra = 0f;
        dashMoved = false;
        rb.linearVelocity = new Vector2(dashDir * dashSpeed, 0f);
        Kick(7f);                       // se lungeste pe orizontala (turtit pe verticala)
        CameraShake.Shake(0.08f);
        Dashed?.Invoke(dashDir);
    }

    void EjectFromGlitch()
    {
        float dir = dashDir != 0f ? dashDir : facing;
        for (float d = 0.1f; d <= 3f; d += 0.1f)
        {
            foreach (float s in new[] { dir, -dir })
            {
                Vector2 p = rb.position + new Vector2(s * d, 0f);
                if (DashPhase.Overlapping(p, col.radius * 0.85f)) continue;
                bool blocked = false;
                foreach (var hit in Physics2D.OverlapCircleAll(p, col.radius * 0.85f, solidMask))
                    if (hit != col && !hit.isTrigger && !DashPhase.Contains(hit)) { blocked = true; break; }
                if (blocked) continue;
                rb.position = p;
                return;
            }
        }
        rb.position += Vector2.up * 0.5f;   // nu e loc pe orizontala: in sus
    }

    void SetPhasing(bool on)
    {
        if (phasing == on) return;
        phasing = on;
        foreach (var c in DashPhase.Colliders)
            if (c != null) Physics2D.IgnoreCollision(col, c, on);
    }

    void ApplyPhasing()
    {
        foreach (var c in DashPhase.Colliders)
            if (c != null && c.isActiveAndEnabled) Physics2D.IgnoreCollision(col, c, true);
    }

    void Launch()
    {
        float t = ChargeRatio;
        float speed = Mathf.Lerp(minLaunchSpeed, maxLaunchSpeed, t);
        Vector2 dir = new Vector2(moveInput * launchHorizontalBias, 1f).normalized;

        rb.linearVelocity = dir * speed;
        lastGroundedTime = -99f;
        lastJumpPressedTime = -99f;
        lastTakeoffTime = Time.time;
        isGrounded = false;
        squashVel = -(14f + 10f * t);   // energia stransa se elibereaza: din turtit, tasneste alungit
        CameraShake.Shake(0.12f + t * 0.18f);
    }

    // ---------------------------------------------------------------- wall stick
    /// <summary>-1 = perete in stanga, +1 = in dreapta, 0 = niciunul. Peretii glitch nu tin (sunt "deprecated").</summary>
    float WallContact()
    {
        for (int s = -1; s <= 1; s += 2)
        {
            var hits = Physics2D.CircleCastAll(rb.position, col.radius * 0.85f, new Vector2(s, 0f),
                                               wallCheckDistance + col.radius * 0.15f, solidMask);
            foreach (var h in hits)
            {
                if (h.collider == null || h.collider == col || h.collider.isTrigger) continue;
                if (DashPhase.Contains(h.collider)) continue;
                if (Mathf.Abs(h.normal.x) < 0.7f) continue;
                return s;
            }
        }
        return 0f;
    }

    void Stick(float side)
    {
        wallStuck = true;
        wallSide = side;
        wallHold = 0f;
        CancelCharge();
        rb.linearVelocity = Vector2.zero;
        rb.gravityScale = 0f;
        Kick(10f);                         // pleosc pe perete
        CameraShake.Shake(0.06f);
        WallStuck?.Invoke(side);
    }

    void UpdateWallStick()
    {
        rb.linearVelocity = Vector2.zero;
        rb.gravityScale = 0f;
        wallHold += Time.fixedDeltaTime;

        if (WallContact() != wallSide)        // peretele a disparut (ex. ai rotit lumea): cazi
        {
            wallStuck = false;
            return;
        }
        if (!jumpHeld) WallLaunch();
    }

    void WallLaunch()
    {
        float t = WallChargeRatio;
        Vector2 v = Vector2.Lerp(wallLaunchMin, wallLaunchMax, t);
        wallStuck = false;
        rb.linearVelocity = new Vector2(-wallSide * v.x, v.y);
        airControlLock = 0.22f;               // nu te trage inapoi in perete imediat
        lastTakeoffTime = Time.time;
        jumpsUsed = 1;                         // cu double jump: mai ai al doilea salt
        airDashUsed = false;                   // si dash-ul se reincarca: lanturi perete -> dash -> perete
        facing = -wallSide;
        squashVel = -(14f + 10f * t);
        CameraShake.Shake(0.1f + 0.15f * t);
        WallLaunched?.Invoke(t);
    }

    void OnLand(float impactSpeed)
    {
        jumpsUsed = 0;                 // reseteaza salturile la aterizare
        float strength = Mathf.InverseLerp(2f, 18f, impactSpeed);
        Kick(4f + 12f * strength);      // se turteste la aterizare, apoi tremura inapoi
        if (strength > 0.05f) CameraShake.Shake(strength * landShakeScale);
        if (!softLandingEnabled && strength > 0.5f) hitStopTimer = hitStopDuration;
        Landed?.Invoke(impactSpeed);
    }

    void OnCollisionEnter2D(Collision2D c)
    {
        if (!wallBounceEnabled || isGrounded || wallStuck) return;
        if (wallStickEnabled && jumpHeld) return;   // tii SPACE: te lipesti, nu ricosezi

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
            dashTimer = 0f;   // un dash intr-un perete devine bounce, nu continua in perete
            SetPhasing(false);

            Kick(-8f);                  // lipit de perete: subtire si inalt, apoi revine
            hitStopTimer = hitStopDuration;
            CameraShake.Shake(0.15f);
            return;
        }
    }

    void UpdateVisual()
    {
        if (visual == null) return;
        float dt = Time.deltaTime;
        if (dt <= 0f) return;   // timp oprit (dialog, rotire)

        // tinta formei: in aer putin alungit dupa viteza; la charge tot mai turtit, si tremura cand e plin
        float target = 0f;
        if (!isGrounded)
            target = Mathf.Clamp(-Mathf.Abs(rb.linearVelocityY) * 0.015f, -0.25f, 0f);
        else if (Mathf.Abs(rb.linearVelocityX) < 0.3f && !isCharging)
            target = 0.035f * Mathf.Sin(Time.time * 2.4f);          // respira cand sta pe loc
        if (isCharging)
        {
            float r = ChargeRatio;
            target = Mathf.Lerp(0.12f, chargeSquash, r) + Mathf.Sin(Time.time * 38f) * 0.03f * r;
        }
        if (wallStuck)
        {
            // lipit: turtit spre perete, tremura tot mai tare pe masura ce strange energie
            float r = WallChargeRatio;
            target = -Mathf.Lerp(0.15f, 0.35f, r) + Mathf.Sin(Time.time * 40f) * 0.03f * r;
        }

        // arc amortizat: forma trece putin de tinta si revine, ca un jeleu
        squashVel += (springStiffness * (target - squash) - springDamping * squashVel) * dt;
        squash = Mathf.Clamp(squash + squashVel * dt, -0.6f, 0.7f);

        // volumul ramane acelasi: cand se turteste, se lateste in ambele directii (si invers)
        float sy = 1f - squash;
        float sxz = 1f / Mathf.Sqrt(Mathf.Max(0.2f, sy));
        visual.localScale = new Vector3(sxz, sy, sxz);

        // pe sol, talpa ramane pe podea: compresia coboara spre sol, nu se face in aer
        float wantOffset = (isGrounded || isCharging) ? -col.radius * (1f - sy) : 0f;
        groundOffset = Mathf.Lerp(groundOffset, wantOffset, 1f - Mathf.Exp(-30f * dt));
        Vector3 wallShift = wallStuck ? new Vector3(wallSide * 0.12f, 0f, 0f) : Vector3.zero;
        visual.localPosition = visualBasePos + Vector3.up * groundOffset + wallShift;

        // se apleaca in directia in care alearga (arc, ca sa se legene putin cand se opreste)
        float leanTarget = wallStuck ? wallSide * 8f : Mathf.Clamp(-rb.linearVelocityX * 1.6f, -maxLean, maxLean);
        leanVel += (120f * (leanTarget - lean) - 10f * leanVel) * dt;
        lean += leanVel * dt;
        if (!externalRotation) visual.localRotation = Quaternion.Euler(0f, 0f, lean);
    }

    void OnDisable() => SetPhasing(false);

    void OnDrawGizmosSelected()
    {
        var c = GetComponent<CircleCollider2D>();
        if (c == null) return;
        Gizmos.color = Color.cyan;
        Vector3 o = transform.position + Vector3.down * (c.radius * 0.35f + groundCheckDistance);
        Gizmos.DrawWireSphere(o, c.radius * 0.92f);
    }
}
