using UnityEngine;

/// <summary>
/// Movimiento lineal: empuje, frenado activo, strafe y drag espacial.
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(ShipInput))]
public class ShipMovement : MonoBehaviour
{
    [Header("Empuje (W)")]
    public float thrustForce = 58f;
    public float thrustRampTime = 0.14f;
    public float maxSpeed = 110f;

    [Tooltip("0 = inercia perfecta. 0.005 = deriva casi infinita.")]
    [Range(0f, 0.2f)]
    public float spaceDrag = 0.005f;

    [Header("Frenado Activo (S)")]
    public float brakingForce = 140f;

    [Header("Strafe (A / D sostenido)")]
    public float strafeForce = 55f;
    public float maxStrafeSpeed = 60f;

    /// <summary>Deshabilita el control (durante dash o u-turn).</summary>
    public bool Blocked { get; set; }

    private Rigidbody2D rb;
    private ShipInput input;
    private float thrustT;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        input = GetComponent<ShipInput>();
    }

    public void Tick(float dt)
    {
        if (Blocked)
        {
            ApplySpaceDrag(dt);
            ClampSpeed(dt);
            return;
        }

        ApplyThrust(dt);
        ApplyBrake();
        ApplyStrafe();
        ApplySpaceDrag(dt);
        ClampSpeed(dt);
    }

    // ─── Empuje con rampa ──────────────────────
    void ApplyThrust(float dt)
    {
        if (input.ThrustHeld)
        {
            thrustT = Mathf.MoveTowards(thrustT, 1f, dt / thrustRampTime);
            float eased = thrustT * thrustT * (3f - 2f * thrustT);
            rb.AddForce(transform.up * thrustForce * eased, ForceMode2D.Force);
        }
        else
        {
            thrustT = Mathf.MoveTowards(thrustT, 0f, dt / (thrustRampTime * 0.4f));
        }
    }

    // ─── Frenado activo ────────────────────────
    void ApplyBrake()
    {
        if (!input.BrakeHeld) return;

        Vector2 vel = rb.linearVelocity;
        if (vel.magnitude > 0.15f)
            rb.AddForce(-vel.normalized * brakingForce, ForceMode2D.Force);
        else
            rb.linearVelocity = Vector2.zero;
    }

    // ─── Strafe lateral ────────────────────────
    void ApplyStrafe()
    {
        float strafeDir = 0f;
        if (input.StrafeLeftHeld) strafeDir = -1f;
        if (input.StrafeRightHeld) strafeDir = 1f;
        if (Mathf.Abs(strafeDir) < 0.01f) return;

        rb.AddForce(transform.right * strafeDir * strafeForce, ForceMode2D.Force);

        Vector2 rightDir = transform.right;
        Vector2 fwdDir = transform.up;
        float latSpeed = Vector2.Dot(rb.linearVelocity, rightDir);
        float fwdSpeed = Vector2.Dot(rb.linearVelocity, fwdDir);

        if (Mathf.Abs(latSpeed) > maxStrafeSpeed)
        {
            latSpeed = Mathf.Sign(latSpeed) * maxStrafeSpeed;
            rb.linearVelocity = fwdDir * fwdSpeed + rightDir * latSpeed;
        }
    }

    // ─── Drag y clamp ──────────────────────────
    void ApplySpaceDrag(float dt)
    {
        if (spaceDrag > 0f)
            rb.linearVelocity *= Mathf.Pow(1f - spaceDrag, dt * 60f);
    }

    void ClampSpeed(float dt)
    {
        float speed = rb.linearVelocity.magnitude;
        if (speed > maxSpeed)
            rb.linearVelocity = rb.linearVelocity.normalized
                * Mathf.Lerp(speed, maxSpeed, dt * 10f);
    }
}