using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody2D))]
public class ShipController : MonoBehaviour
{
    [Header("Thrust")]
    [SerializeField] float thrustAcceleration = 12f;
    [SerializeField] float maxSpeed = 10f;
    [SerializeField] float linearDamping = 0.15f;

    [Header("Rotation")]
    [SerializeField] float mouseSensitivity = 0.15f;
    [Header("Rotation Smoothing")]
    [SerializeField] float rotationSmoothTime = 0.08f;   // 0 = sin suavizado, mayor = más suave
    [SerializeField] float maxRotationSpeed = 720f;      // grados/seg (evita saltos enormes)

    float targetRotation;
    float rotationVelocity;   // usado por SmoothDampAngle

    [Header("U-Turn")]
    [SerializeField] float uTurnSpeed = 540f;

    [Header("Dash (doble tap)")]
    [SerializeField] float dashSpeed = 25f;
    [SerializeField] float dashDuration = 0.15f;
    [SerializeField] float doubleTapWindow = 0.30f;

    [Header("Brake")]
    [SerializeField] float brakeDeceleration = 15f;
    [SerializeField] float brakeStopThreshold = 0.05f;

    Rigidbody2D rb;

    // --- Estado de input ---
    Vector2 moveInput;
    Vector2 lastMoveInput;
    Vector2 pendingMouseDelta;
    bool braking;

    // --- U-Turn ---
    bool uTurning;
    float uTurnTarget;

    // --- Dash ---
    Vector2 dashVelocity;
    bool dashActive;
    float dashTimeRemaining;
    readonly float[] lastTapTimes = new float[4];
    static readonly Vector2[] dirs = { Vector2.up, Vector2.down, Vector2.left, Vector2.right };

    // --- Velocidad base PERSISTENTE (fuera del dash) ---
    Vector2 baseVelocity;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        rb.gravityScale = 0f;
        rb.linearDamping = 0f;
        rb.angularDamping = 0f;
        targetRotation = rb.rotation;
        for (int i = 0; i < lastTapTimes.Length; i++) lastTapTimes[i] = -999f;
    }

    // ================== INPUT ==================

    public void OnMove(InputValue value)
    {
        Vector2 newInput = value.Get<Vector2>();
        DetectDoubleTap(newInput);
        moveInput = newInput;
        lastMoveInput = newInput;
    }

    public void OnLook(InputValue value)
    {
        Vector2 mouseDelta = value.Get<Vector2>();
        targetRotation -= mouseDelta.x * mouseSensitivity;
    }

    public void OnUTurn(InputValue value)
    {
        if (!value.isPressed || uTurning) return;
        uTurning = true;
        uTurnTarget = rb.rotation + 180f;
        targetRotation = uTurnTarget;   // ← el ratón ya no manda
        rotationVelocity = 0f;          // ← evita que SmoothDamp empuje al volver
        braking = false;
    }

    // CallbackContext: performed/canceled son 100% fiables para botones
    public void OnBrake(InputValue value)
    {
        if (value.isPressed) braking = true;
    }

    // ================== DOBLE TAP ==================

    void DetectDoubleTap(Vector2 newInput)
    {
        for (int i = 0; i < dirs.Length; i++)
        {
            bool nowPressed = Vector2.Dot(newInput, dirs[i]) > 0.5f;
            bool wasPressed = Vector2.Dot(lastMoveInput, dirs[i]) > 0.5f;

            if (nowPressed && !wasPressed)
            {
                if (Time.time - lastTapTimes[i] <= doubleTapWindow)
                {
                    TriggerDash(dirs[i]);
                    lastTapTimes[i] = -999f;
                }
                else
                {
                    lastTapTimes[i] = Time.time;
                }
            }
        }
    }

    void TriggerDash(Vector2 localDir)
    {
        Vector2 worldDir = transform.TransformDirection(localDir.normalized);
        dashVelocity = worldDir * dashSpeed;
        dashActive = true;
        dashTimeRemaining = dashDuration;
    }

    // ================== FÍSICA ==================

    void FixedUpdate()
    {
        // --- Rotación con ratón ---
        if (!uTurning)
        {
            // SmoothDampAngle se encarga del wrap-around de 360º
            float newRot = Mathf.SmoothDampAngle(
                rb.rotation,
                targetRotation,
                ref rotationVelocity,
                rotationSmoothTime,
                maxRotationSpeed,
                Time.fixedDeltaTime
            );
            rb.MoveRotation(newRot);
        }

        // --- U-Turn ---
        if (uTurning)
        {
            float newRot = Mathf.MoveTowardsAngle(rb.rotation, uTurnTarget,
                                                  uTurnSpeed * Time.fixedDeltaTime);
            rb.MoveRotation(newRot);
            if (Mathf.Abs(Mathf.DeltaAngle(newRot, uTurnTarget)) < 0.1f)
            {
                rb.MoveRotation(uTurnTarget);
                uTurning = false;
                targetRotation = uTurnTarget;   // ← resync
                rotationVelocity = 0f;          // ← resync
            }
        }

        // --- Thrust aditivo: modifica SOLO baseVelocity ---
        bool hasThrust = moveInput.sqrMagnitude > 0.001f && !uTurning;
        if (hasThrust)
        {
            Vector2 worldDir = transform.TransformDirection(moveInput.normalized);
            baseVelocity += worldDir * thrustAcceleration * Time.fixedDeltaTime;

            // El jugador retoma el control: cancelamos el freno
            braking = false;
        }


        // --- Fricción reducida sobre baseVelocity ---
        baseVelocity *= Mathf.Max(0f, 1f - linearDamping * Time.fixedDeltaTime);

        // --- Freno ---
        if (braking)
        {
            float speed = baseVelocity.magnitude;
            if (speed <= brakeStopThreshold)
            {
                baseVelocity = Vector2.zero;
                braking = false;              // termina solo
            }
            else
            {
                float newSpeed = Mathf.Max(0f, speed - brakeDeceleration * Time.fixedDeltaTime);
                baseVelocity = baseVelocity.normalized * newSpeed;
            }
        }

        // --- Clamp ---
        if (baseVelocity.magnitude > maxSpeed)
            baseVelocity = baseVelocity.normalized * maxSpeed;

        // --- Timer del dash ---
        if (dashActive)
        {
            dashTimeRemaining -= Time.fixedDeltaTime;
            if (dashTimeRemaining <= 0f)
            {
                dashActive = false;
                dashVelocity = Vector2.zero;
            }
        }

        // --- Output: base persistente + dash temporal ---
        rb.linearVelocity = baseVelocity + (dashActive ? dashVelocity : Vector2.zero);
    }
}