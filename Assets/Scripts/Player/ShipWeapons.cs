using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public enum FireMode { Simultaneous, Sequential }

public class ShipWeapons : MonoBehaviour
{
    [Header("References")]
    [SerializeField] Transform firePointRoot;   // child en el frente de la nave

    [Header("Barrels")]
    [SerializeField] int barrelCount = 2;
    [SerializeField] float barrelSpacing = 0.25f;   // separación entre cañones
    [SerializeField] FireMode fireMode = FireMode.Simultaneous;

    [Header("Input")]
    [SerializeField] InputActionReference primaryAttackAction;
    [SerializeField] InputActionReference secondaryAttackAction;

    [Header("Primary")]
    [SerializeField] GameObject primaryProjectilePrefab;
    [SerializeField] float primaryFireRate = 6f;
    [SerializeField] float primaryProjectileSpeed = 20f;
    [SerializeField] bool  primaryInheritVelocity = true;

    [Header("Secondary (missile)")]
    [SerializeField] GameObject secondaryProjectilePrefab;
    [SerializeField] float secondaryFireRate = 1.5f;
    [SerializeField] float secondaryProjectileSpeed = 12f;
    [SerializeField] bool  secondaryInheritVelocity = false;

    Rigidbody2D shipRb;
    readonly List<Transform> barrels = new();
    int nextBarrelIndex;
    float nextPrimaryTime;
    float nextSecondaryTime;

    void Awake()
    {
        shipRb = GetComponent<Rigidbody2D>();
        if (firePointRoot == null) firePointRoot = transform;
        BuildBarrels();
    }

    void BuildBarrels()
    {
        barrels.Clear();
        int count = Mathf.Max(1, barrelCount);
        float center = (count - 1) * 0.5f;

        for (int i = 0; i < count; i++)
        {
            var go = new GameObject($"Barrel_{i}");
            go.transform.SetParent(firePointRoot, false);
            // Offset perpendicular al frente (a lo largo del eje X local del root)
            go.transform.localPosition = Vector3.right * (i - center) * barrelSpacing;
            go.transform.localRotation = Quaternion.identity;
            barrels.Add(go.transform);
        }
    }

    // ==================== INPUT ====================

    void Update()
    {
        if (primaryAttackAction != null &&
            primaryAttackAction.action.IsPressed() &&
            Time.time >= nextPrimaryTime)
        {
            nextPrimaryTime = Time.time + 1f / primaryFireRate;
            FirePrimary();
        }

        if (secondaryAttackAction != null &&
            secondaryAttackAction.action.WasPressedThisFrame() &&
            Time.time >= nextSecondaryTime)
        {
            nextSecondaryTime = Time.time + 1f / secondaryFireRate;
            Fire(secondaryProjectilePrefab, secondaryProjectileSpeed,
                 secondaryInheritVelocity, firePointRoot);
        }
    }

    // ==================== DISPARO ====================

    void FirePrimary()
    {
        if (primaryProjectilePrefab == null) return;

        switch (fireMode)
        {
            case FireMode.Simultaneous:
                foreach (var b in barrels)
                    Fire(primaryProjectilePrefab, primaryProjectileSpeed,
                         primaryInheritVelocity, b);
                break;

            case FireMode.Sequential:
                var barrel = barrels[nextBarrelIndex];
                nextBarrelIndex = (nextBarrelIndex + 1) % barrels.Count;
                Fire(primaryProjectilePrefab, primaryProjectileSpeed,
                     primaryInheritVelocity, barrel);
                break;
        }
    }

    void Fire(GameObject prefab, float projectileSpeed, bool inheritVelocity, Transform from)
    {
        if (prefab == null) return;

        GameObject go = Instantiate(prefab, from.position, from.rotation);

        Vector2 shotVel = from.up * projectileSpeed;
        if (inheritVelocity && shipRb != null)
            shotVel += shipRb.linearVelocity;

        if (go.TryGetComponent<Projectile>(out var p))
            p.Initialize(shotVel);
        else if (go.TryGetComponent<HomingMissile>(out var m))
            m.Initialize(shotVel);
    }
}