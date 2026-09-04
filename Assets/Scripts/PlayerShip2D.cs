using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections;

[RequireComponent(typeof(Rigidbody2D))]
public class PlayerShip2D : MonoBehaviour
{
    // ─────────────────────────────────────────
    //  MOVIMIENTO LINEAL
    // ─────────────────────────────────────────
    [Header("Movimiento Lineal")]
    [Tooltip("Fuerza de aceleración con W")]
    public float thrustForce = 58f;

    [Tooltip("Tiempo de rampa de empuje — más bajo = más snappy")]
    public float thrustRampTime = 0.14f;

    [Tooltip("Velocidad máxima de crucero")]
    public float maxSpeed = 110f;

    [Tooltip("Resistencia del espacio — 0 = inercia perfecta infinita. 0.005 = deriva casi infinita con frenado apenas perceptible.")]
    [Range(0f, 0.2f)]
    public float spaceDrag = 0.005f;

    // ─────────────────────────────────────────
    //  FRENADO ACTIVO (S)
    // ─────────────────────────────────────────
    [Header("Frenado Activo (S)")]
    public float brakingForce = 140f;

    // ─────────────────────────────────────────
    //  ROTACIÓN – MOUSE
    // ─────────────────────────────────────────
    [Header("Rotación por Mouse")]
    [Tooltip("Velocidad angular máxima grados/s — sube para rotaciones más rápidas en combate")]
    public float maxTurnSpeed = 240f;

    [Tooltip("Aceleración angular — alto = respuesta inmediata a input")]
    public float turnAcceleration = 900f;

    [Tooltip("Amortiguación al soltar el mouse")]
    public float turnDamping = 12f;

    public float mouseSensitivity = 1.5f;

    // ─────────────────────────────────────────
    //  STRAFE LATERAL (A / D)
    //  Tap  → Dash instantáneo
    //  Hold → Strafe continuo
    // ─────────────────────────────────────────
    [Header("Strafe / Dash Lateral (A / D)")]
    [Tooltip("Fuerza de strafe sostenido al mantener A/D")]
    public float strafeForce = 55f;

    [Tooltip("Velocidad lateral máxima en strafe continuo")]
    public float maxStrafeSpeed = 60f;

    [Tooltip("Tiempo máximo de tap antes de considerarse 'hold'")]
    public float tapThreshold = 0.12f;

    [Tooltip("Velocidad del dash (tap rápido)")]
    public float dashSpeed = 70f;

    [Tooltip("Duración del dash")]
    public float dashDuration = 0.28f;

    [Tooltip("Fricción lateral al final del dash")]
    public float dashLateralFriction = 9f;

    public float dashEnergyConsume = 180f;

    // ─────────────────────────────────────────
    //  IMPULSOS Q / E  (boost/freno forward)
    // ─────────────────────────────────────────
    [Header("Impulsos Forward (Q / E)")]
    public float shipBoost = 65f;
    public float boostEnergyConsume = 220f;
    public float brakeEnergyConsume = 220f;

    // ─────────────────────────────────────────
    //  U-TURN (Espacio)
    // ─────────────────────────────────────────
    [Header("U-Turn (Espacio)")]
    public float uTurnTime = 0.9f;
    public float uTurnEnergyConsume = 260f;

    // ─────────────────────────────────────────
    //  ENERGÍA
    // ─────────────────────────────────────────
    [Header("Energía")]
    public float shipTotalEnergy = 1000f;
    public float energyReplenish = 55f;

    // ─────────────────────────────────────────
    //  REFERENCIAS
    // ─────────────────────────────────────────
    [Header("Referencias")]
    public IsometricCamera2D cameraRef;

    // ─── PRIVADAS ────────────────────────────
    private Rigidbody2D rb;
    private Keyboard keyboard;
    private float currentEnergy;

    // Rotación angular propia
    private float angularVelocity = 0f;
    private float rawMouseX = 0f;

    // Rampa de empuje
    private float thrustT = 0f;

    // Estados globales
    private bool isTurning = false;
    private bool isDashing  = false;

    // Lógica de tap/hold para A/D
    private float aHoldTime = 0f;
    private float dHoldTime = 0f;

    // ════════════════════════════════════════
    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        rb.gravityScale   = 0f;
        rb.linearDamping  = 0f;
        rb.angularDamping = 0f;
        rb.freezeRotation = true;

        keyboard      = Keyboard.current;
        currentEnergy = shipTotalEnergy;
        currentHealth = maxHealth;

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible   = false;
    }

    // ════════════════════════════════════════
    //  UPDATE – captura input
    // ════════════════════════════════════════
    void Update()
    {
        if (keyboard == null) return;

        // Liberar cursor en editor
        if (keyboard.escapeKey.wasPressedThisFrame)
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible   = true;
        }

        // Mouse siempre acumula (incluso en dash/uturn podemos seguir apuntando)
        if (Mouse.current != null && Cursor.lockState == CursorLockMode.Locked)
            rawMouseX += Mouse.current.delta.ReadValue().x;

        if (isTurning) return;

        // ── U-Turn ───────────────────────────────
        if (keyboard.spaceKey.wasPressedThisFrame && currentEnergy >= uTurnEnergyConsume)
        {
            StartCoroutine(ExecuteUTurn());
            return;
        }

        // ── Impulsos Q / E ───────────────────────
        if (keyboard.qKey.wasPressedThisFrame && currentEnergy >= boostEnergyConsume)
        {
            rb.AddForce(transform.up * shipBoost, ForceMode2D.Impulse);
            currentEnergy = Mathf.Max(0, currentEnergy - boostEnergyConsume);
        }
        if (keyboard.eKey.wasPressedThisFrame && currentEnergy >= brakeEnergyConsume)
        {
            rb.AddForce(-transform.up * shipBoost * 0.75f, ForceMode2D.Impulse);
            currentEnergy = Mathf.Max(0, currentEnergy - brakeEnergyConsume);
        }

        // ── Lógica tap / hold para A ─────────────
        if (keyboard.aKey.isPressed)
            aHoldTime += Time.deltaTime;
        if (keyboard.aKey.wasReleasedThisFrame)
        {
            if (aHoldTime <= tapThreshold && !isDashing && currentEnergy >= dashEnergyConsume)
                StartCoroutine(ExecuteDash(-1f));   // tap → dash
            aHoldTime = 0f;
        }

        // ── Lógica tap / hold para D ─────────────
        if (keyboard.dKey.isPressed)
            dHoldTime += Time.deltaTime;
        if (keyboard.dKey.wasReleasedThisFrame)
        {
            if (dHoldTime <= tapThreshold && !isDashing && currentEnergy >= dashEnergyConsume)
                StartCoroutine(ExecuteDash(1f));    // tap → dash
            dHoldTime = 0f;
        }
    }

    // ════════════════════════════════════════
    //  FIXED UPDATE – física
    // ════════════════════════════════════════
    void FixedUpdate()
    {
        // ─── ROTACIÓN ANGULAR CON INERCIA ──────────
        // La rotación NUNCA se bloquea — el jugador siempre apunta
        if (!isTurning)
        {
            float desired = 0f;
            if (Cursor.lockState == CursorLockMode.Locked)
            {
                desired = Mathf.Clamp(
                    -rawMouseX * mouseSensitivity * 90f,
                    -maxTurnSpeed, maxTurnSpeed
                );
            }
            rawMouseX = 0f;

            angularVelocity = Mathf.Abs(desired) > 0.5f
                ? Mathf.MoveTowards(angularVelocity, desired, turnAcceleration * Time.fixedDeltaTime)
                : Mathf.Lerp(angularVelocity, 0f, turnDamping * Time.fixedDeltaTime);

            rb.MoveRotation(rb.rotation + angularVelocity * Time.fixedDeltaTime);
        }

        if (isTurning)
        {
            ApplySpaceDrag(); ClampSpeed(); RechargeEnergy();
            return;
        }

        // ─── EMPUJE (W) CON RAMPA ─────────────────
        if (keyboard != null && keyboard.wKey.isPressed)
        {
            thrustT = Mathf.MoveTowards(thrustT, 1f, Time.fixedDeltaTime / thrustRampTime);
            float eased = thrustT * thrustT * (3f - 2f * thrustT);
            rb.AddForce(transform.up * thrustForce * eased, ForceMode2D.Force);
        }
        else
        {
            thrustT = Mathf.MoveTowards(thrustT, 0f, Time.fixedDeltaTime / (thrustRampTime * 0.4f));
        }

        // ─── FRENADO ACTIVO (S) ───────────────────
        if (keyboard != null && keyboard.sKey.isPressed)
        {
            Vector2 vel = rb.linearVelocity;
            if (vel.magnitude > 0.15f)
                rb.AddForce(-vel.normalized * brakingForce, ForceMode2D.Force);
            else
                rb.linearVelocity = Vector2.zero;
        }

        // ─── STRAFE (A / D) ───────────────────────
        // La fuerza lateral empieza desde el PRIMER frame que se presiona la tecla.
        // El sistema tap/hold solo determina si al SOLTAR se lanza un dash extra.
        if (!isDashing && keyboard != null)
        {
            float strafeDir = 0f;
            if (keyboard.aKey.isPressed) strafeDir = -1f;
            if (keyboard.dKey.isPressed) strafeDir =  1f;

            if (Mathf.Abs(strafeDir) > 0f)
            {
                // Fuerza lateral inmediata — sin esperar threshold
                rb.AddForce(transform.right * strafeDir * strafeForce, ForceMode2D.Force);

                // Limitar velocidad lateral máxima durante strafe
                Vector2 rightDir = transform.right;
                Vector2 fwdDir   = transform.up;
                float latSpeed   = Vector2.Dot(rb.linearVelocity, rightDir);
                float fwdSpeed   = Vector2.Dot(rb.linearVelocity, fwdDir);

                if (Mathf.Abs(latSpeed) > maxStrafeSpeed)
                {
                    latSpeed = Mathf.Sign(latSpeed) * maxStrafeSpeed;
                    rb.linearVelocity = fwdDir * fwdSpeed + rightDir * latSpeed;
                }
            }
            // SIN fricción lateral extra — el spaceDrag normal preserva la inercia
            // La nave se desliza sola suavemente igual que en el eje forward
        }

        ApplySpaceDrag();
        ClampSpeed();
        RechargeEnergy();
    }

    // ════════════════════════════════════════
    //  HELPERS
    // ════════════════════════════════════════
    void ApplySpaceDrag()
    {
        // Con spaceDrag = 0.005: en 10 segundos la nave mantiene ~74% de su velocidad
        // Con spaceDrag = 0:     inercia perfecta, la nave NUNCA frena sola
        if (spaceDrag > 0f)
            rb.linearVelocity *= Mathf.Pow(1f - spaceDrag, Time.fixedDeltaTime * 60f);
        // No forzamos velocidad a cero — la nave se mueve hasta que algo la detenga
    }

    void ClampSpeed()
    {
        float speed = rb.linearVelocity.magnitude;
        if (speed > maxSpeed)
            rb.linearVelocity = rb.linearVelocity.normalized
                * Mathf.Lerp(speed, maxSpeed, Time.fixedDeltaTime * 10f);
    }

    void RechargeEnergy()
    {
        if (currentEnergy < shipTotalEnergy)
            currentEnergy = Mathf.Min(shipTotalEnergy, currentEnergy + energyReplenish * Time.fixedDeltaTime);
    }

    // ════════════════════════════════════════
    //  DASH (tap A / D)
    // ════════════════════════════════════════
    IEnumerator ExecuteDash(float direction)
    {
        isDashing = true;
        currentEnergy = Mathf.Max(0, currentEnergy - dashEnergyConsume);

        Vector2 fwd        = transform.up;
        Vector2 rght       = transform.right;
        float   fwdSpeed   = Vector2.Dot(rb.linearVelocity, fwd);

        // Velocidad lateral del dash — sobreescribe cualquier lateral previo
        rb.linearVelocity  = fwd * fwdSpeed + rght * (direction * dashSpeed);

        float elapsed = 0f;
        while (elapsed < dashDuration)
        {
            elapsed += Time.fixedDeltaTime;

            Vector2 forwardDir = transform.up;
            Vector2 rightDir   = transform.right;
            float fs  = Vector2.Dot(rb.linearVelocity, forwardDir);
            float ls  = Vector2.Dot(rb.linearVelocity, rightDir);

            // Ease-out cuadrático de la componente lateral
            float t   = elapsed / dashDuration;
            float tgt = Mathf.Lerp(direction * dashSpeed, 0f, t * t);
            ls = Mathf.Lerp(ls, tgt, dashLateralFriction * Time.fixedDeltaTime);

            rb.linearVelocity = forwardDir * fs + rightDir * ls;
            yield return new WaitForFixedUpdate();
        }

        isDashing = false;
    }

    // ════════════════════════════════════════
    //  U-TURN (Espacio)
    // ════════════════════════════════════════
    IEnumerator ExecuteUTurn()
    {
        isTurning = true;
        currentEnergy = Mathf.Max(0, currentEnergy - uTurnEnergyConsume);
        if (cameraRef != null) cameraRef.StartUTurn();

        float elapsed   = 0f;
        float startRot  = rb.rotation;
        float targetRot = startRot + 180f;
        angularVelocity = 0f;

        while (elapsed < uTurnTime)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / uTurnTime;
            t = t * t * (3f - 2f * t);
            rb.MoveRotation(Mathf.Lerp(startRot, targetRot, t));
            yield return null;
        }

        rb.MoveRotation(targetRot);
        isTurning = false;
    }

    // ════════════════════════════════════════
    //  SALUD — recibe daño de proyectiles
    // ════════════════════════════════════════
    [Header("Salud")]
    public float maxHealth = 100f;
    private float currentHealth;

    // Evento opcional: suscríbete para saber cuándo muere la nave
    public System.Action OnDeath;

    public float GetHealthPercent() => currentHealth / maxHealth;

    public void ApplyDamage(float damage)
    {
        if (currentHealth <= 0f) return;
        currentHealth -= damage;
        if (currentHealth <= 0f)
        {
            currentHealth = 0f;
            OnDeath?.Invoke();
            Debug.Log("Nave destruida!");
        }
    }

    // ════════════════════════════════════════
    //  API PÚBLICA
    // ════════════════════════════════════════
    public float GetEnergyPercent() => currentEnergy / shipTotalEnergy;
    public float GetSpeedPercent()  => rb != null ? rb.linearVelocity.magnitude / maxSpeed : 0f;
}
