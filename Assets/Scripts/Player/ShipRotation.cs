using UnityEngine;

/// <summary>
/// Rotación por mouse con aceleración angular e inercia.
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(ShipInput))]
public class ShipRotation : MonoBehaviour
{
    [Header("Rotación por Mouse")]
    public float maxTurnSpeed = 240f;
    public float turnAcceleration = 900f;
    public float turnDamping = 12f;
    public float mouseSensitivity = 1.5f;

    /// <summary>Bloquea la rotación (usado por U-Turn).</summary>
    public bool IsTurning { get; set; }

    private Rigidbody2D rb;
    private ShipInput input;
    private float angularVelocity;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        input = GetComponent<ShipInput>();
    }

    public void Tick(float dt)
    {
        if (IsTurning) return;

        float desired = 0f;
        if (Cursor.lockState == CursorLockMode.Locked)
        {
            float mouseX = input.ConsumeMouseDeltaX();
            desired = Mathf.Clamp(
                -mouseX * mouseSensitivity * 90f,
                -maxTurnSpeed, maxTurnSpeed
            );
        }

        angularVelocity = Mathf.Abs(desired) > 0.5f
            ? Mathf.MoveTowards(angularVelocity, desired, turnAcceleration * dt)
            : Mathf.Lerp(angularVelocity, 0f, turnDamping * dt);

        rb.MoveRotation(rb.rotation + angularVelocity * dt);
    }
}