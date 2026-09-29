using UnityEngine;
using UnityEngine.InputSystem;

public class ShipWeapons : MonoBehaviour
{
    [Header("References")]
    [SerializeField] Transform firePoint;

    [Header("Input")]
    [SerializeField] InputActionReference primaryAttackAction;
    [SerializeField] InputActionReference secondaryAttackAction;

    [Header("Primary")]
    [SerializeField] GameObject primaryProjectilePrefab;
    [SerializeField] float primaryFireRate = 6f;
    [SerializeField] float primaryProjectileSpeed = 20f;
    [SerializeField] bool primaryInheritVelocity = true;

    [Header("Secondary (missile)")]
    [SerializeField] GameObject secondaryProjectilePrefab;
    [SerializeField] float secondaryFireRate = 1.5f;
    [SerializeField] float secondaryProjectileSpeed = 12f;
    [SerializeField] bool secondaryInheritVelocity = false;

    Rigidbody2D shipRb;

    float nextPrimaryTime;
    float nextSecondaryTime;

    void Awake()
    {
        shipRb = GetComponent<Rigidbody2D>();
        if (firePoint == null) firePoint = transform;
    }

    void Update()
    {
        // PRIMARIO: auto-fire mientras esté pulsado
        if (primaryAttackAction != null &&
            primaryAttackAction.action.IsPressed() &&
            Time.time >= nextPrimaryTime)
        {
            nextPrimaryTime = Time.time + 1f / primaryFireRate;
            Fire(primaryProjectilePrefab, primaryProjectileSpeed, primaryInheritVelocity);
        }

        // SECUNDARIO: un disparo por pulsación
        if (secondaryAttackAction != null &&
            secondaryAttackAction.action.WasPressedThisFrame() &&
            Time.time >= nextSecondaryTime)
        {
            nextSecondaryTime = Time.time + 1f / secondaryFireRate;
            Fire(secondaryProjectilePrefab, secondaryProjectileSpeed, secondaryInheritVelocity);
        }
    }

    void Fire(GameObject prefab, float projectileSpeed, bool inheritShipVelocity)
    {
        if (prefab == null) return;

        GameObject go = Instantiate(prefab, firePoint.position, firePoint.rotation);

        Vector2 shotVel = firePoint.up * projectileSpeed;
        if (inheritShipVelocity && shipRb != null)
            shotVel += shipRb.linearVelocity;

        if (go.TryGetComponent<Projectile>(out var p))
            p.Initialize(shotVel);
        else if (go.TryGetComponent<HomingMissile>(out var m))
            m.Initialize(shotVel);
        else
            Debug.LogWarning($"[ShipWeapons] El prefab {prefab.name} no tiene Projectile ni HomingMissile.");
    }
}