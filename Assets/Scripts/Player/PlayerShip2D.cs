using UnityEngine;

/// <summary>
/// Coordinador principal de la nave. Orquesta los subsistemas.
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(ShipInput))]
[RequireComponent(typeof(ShipEnergy))]
[RequireComponent(typeof(ShipHealth))]
[RequireComponent(typeof(ShipMovement))]
[RequireComponent(typeof(ShipRotation))]
[RequireComponent(typeof(ShipAbilities))]
public class PlayerShip2D : MonoBehaviour
{
    [Header("Referencias externas")]
    public IsometricCamera2D cameraRef;

    private Rigidbody2D rb;
    private ShipMovement movement;
    private ShipRotation rotation;
    private ShipAbilities abilities;
    private ShipEnergy energy;
    private ShipHealth health;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        rb.gravityScale = 0f;
        rb.linearDamping = 0f;
        rb.angularDamping = 0f;
        rb.freezeRotation = true;

        movement = GetComponent<ShipMovement>();
        rotation = GetComponent<ShipRotation>();
        abilities = GetComponent<ShipAbilities>();
        energy = GetComponent<ShipEnergy>();
        health = GetComponent<ShipHealth>();

        abilities.cameraRef = cameraRef;
    }

    void Update()
    {
        // Habilidades en Update (necesitan wasPressedThisFrame)
        abilities.Tick(Time.deltaTime);
    }

    void FixedUpdate()
    {
        rotation.Tick(Time.fixedDeltaTime);
        movement.Tick(Time.fixedDeltaTime);
    }

    // ════════════════════════════════════════
    //  API pública (fachada)
    // ════════════════════════════════════════
    public float GetEnergyPercent() => energy.Percent;
    public float GetHealthPercent() => health.Percent;
    public float GetSpeedPercent() => rb.linearVelocity.magnitude / movement.maxSpeed;

    public void ApplyDamage(float dmg) => health.ApplyDamage(dmg);

    /// <summary>Suscribite para saber cuándo muere la nave.</summary>
    public System.Action OnDeath
    {
        get => health.OnDeath;
        set => health.OnDeath = value;
    }
}