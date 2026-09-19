using System.Collections;
using UnityEngine;

/// <summary>
/// Habilidades activas: dash lateral, U-turn e impulsos forward.
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(ShipInput))]
[RequireComponent(typeof(ShipEnergy))]
[RequireComponent(typeof(ShipMovement))]
[RequireComponent(typeof(ShipRotation))]
public class ShipAbilities : MonoBehaviour
{
    [Header("Dash (tap A / D)")]
    public float tapThreshold = 0.12f;
    public float dashSpeed = 70f;
    public float dashDuration = 0.28f;
    public float dashLateralFriction = 9f;
    public float dashEnergyConsume = 180f;

    [Header("Impulsos (Q / E)")]
    public float shipBoost = 65f;
    public float boostEnergyConsume = 220f;
    public float brakeEnergyConsume = 220f;

    [Header("U-Turn (Espacio)")]
    public float uTurnTime = 0.9f;
    public float uTurnEnergyConsume = 260f;

    [Header("Referencias")]
    public IsometricCamera2D cameraRef;

    public bool IsDashing { get; private set; }
    public bool IsTurning { get; private set; }

    private Rigidbody2D rb;
    private ShipInput input;
    private ShipEnergy energy;
    private ShipMovement movement;
    private ShipRotation rotation;

    private float aHoldTime, dHoldTime;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        input = GetComponent<ShipInput>();
        energy = GetComponent<ShipEnergy>();
        movement = GetComponent<ShipMovement>();
        rotation = GetComponent<ShipRotation>();
    }

    public void Tick(float dt)
    {
        if (IsTurning) return;

        // ── U-Turn ──────────────────────────────
        if (input.UTurnPressed && energy.HasEnough(uTurnEnergyConsume))
        {
            StartCoroutine(ExecuteUTurn());
            return;
        }

        // ── Impulsos Q / E ─────────────────────
        if (input.BoostPressed && energy.HasEnough(boostEnergyConsume))
        {
            rb.AddForce(transform.up * shipBoost, ForceMode2D.Impulse);
            energy.TryConsume(boostEnergyConsume);
        }
        if (input.BrakeImpulsePressed && energy.HasEnough(brakeEnergyConsume))
        {
            rb.AddForce(-transform.up * shipBoost * 0.75f, ForceMode2D.Impulse);
            energy.TryConsume(brakeEnergyConsume);
        }

        // ── Tap / hold A ───────────────────────
        if (input.StrafeLeftHeld) aHoldTime += dt;
        if (input.StrafeLeftReleased)
        {
            if (aHoldTime <= tapThreshold && !IsDashing && energy.HasEnough(dashEnergyConsume))
                StartCoroutine(ExecuteDash(-1f));
            aHoldTime = 0f;
        }

        // ── Tap / hold D ───────────────────────
        if (input.StrafeRightHeld) dHoldTime += dt;
        if (input.StrafeRightReleased)
        {
            if (dHoldTime <= tapThreshold && !IsDashing && energy.HasEnough(dashEnergyConsume))
                StartCoroutine(ExecuteDash(1f));
            dHoldTime = 0f;
        }
    }

    // ════════════════════════════════════════
    IEnumerator ExecuteDash(float direction)
    {
        IsDashing = true;
        movement.Blocked = true;
        energy.TryConsume(dashEnergyConsume);

        Vector2 fwd = transform.up;
        Vector2 rght = transform.right;
        float fwdSpeed = Vector2.Dot(rb.linearVelocity, fwd);

        rb.linearVelocity = fwd * fwdSpeed + rght * (direction * dashSpeed);

        float elapsed = 0f;
        while (elapsed < dashDuration)
        {
            elapsed += Time.fixedDeltaTime;

            Vector2 forwardDir = transform.up;
            Vector2 rightDir = transform.right;
            float fs = Vector2.Dot(rb.linearVelocity, forwardDir);
            float ls = Vector2.Dot(rb.linearVelocity, rightDir);

            float t = elapsed / dashDuration;
            float tgt = Mathf.Lerp(direction * dashSpeed, 0f, t * t);
            ls = Mathf.Lerp(ls, tgt, dashLateralFriction * Time.fixedDeltaTime);

            rb.linearVelocity = forwardDir * fs + rightDir * ls;
            yield return new WaitForFixedUpdate();
        }

        movement.Blocked = false;
        IsDashing = false;
    }

    // ════════════════════════════════════════
    IEnumerator ExecuteUTurn()
    {
        IsTurning = true;
        rotation.IsTurning = true;
        movement.Blocked = true;
        energy.TryConsume(uTurnEnergyConsume);

        if (cameraRef != null) cameraRef.StartUTurn();

        float elapsed = 0f;
        float startRot = rb.rotation;
        float targetRot = startRot + 180f;

        while (elapsed < uTurnTime)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / uTurnTime;
            t = t * t * (3f - 2f * t);
            rb.MoveRotation(Mathf.Lerp(startRot, targetRot, t));
            yield return null;
        }

        rb.MoveRotation(targetRot);

        rotation.IsTurning = false;
        movement.Blocked = false;
        IsTurning = false;
    }
}