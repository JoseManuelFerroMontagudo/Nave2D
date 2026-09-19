using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Captura el input crudo del jugador. No toma decisiones de gameplay.
/// </summary>
public class ShipInput : MonoBehaviour
{
    // ─── Mouse ───────────────────────────────
    private float pendingMouseX;

    /// <summary>Delta de mouse acumulado. Se resetea al consumirlo.</summary>
    public float ConsumeMouseDeltaX()
    {
        float v = pendingMouseX;
        pendingMouseX = 0f;
        return v;
    }

    // ─── Estados continuos ──────────────────
    public bool ThrustHeld { get; private set; }
    public bool BrakeHeld { get; private set; }
    public bool StrafeLeftHeld { get; private set; }
    public bool StrafeRightHeld { get; private set; }

    // ─── Eventos de flanco ──────────────────
    public bool BoostPressed { get; private set; }
    public bool BrakeImpulsePressed { get; private set; }
    public bool UTurnPressed { get; private set; }
    public bool StrafeLeftReleased { get; private set; }
    public bool StrafeRightReleased { get; private set; }

    private Keyboard keyboard;

    void Start()
    {
        keyboard = Keyboard.current;
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    void Update()
    {
        if (keyboard == null) return;

        // Liberar cursor en editor
        if (keyboard.escapeKey.wasPressedThisFrame)
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        if (Mouse.current != null && Cursor.lockState == CursorLockMode.Locked)
            pendingMouseX += Mouse.current.delta.ReadValue().x;

        ThrustHeld = keyboard.wKey.isPressed;
        BrakeHeld = keyboard.sKey.isPressed;
        StrafeLeftHeld = keyboard.aKey.isPressed;
        StrafeRightHeld = keyboard.dKey.isPressed;

        BoostPressed = keyboard.qKey.wasPressedThisFrame;
        BrakeImpulsePressed = keyboard.eKey.wasPressedThisFrame;
        UTurnPressed = keyboard.spaceKey.wasPressedThisFrame;
        StrafeLeftReleased = keyboard.aKey.wasReleasedThisFrame;
        StrafeRightReleased = keyboard.dKey.wasReleasedThisFrame;
    }
}