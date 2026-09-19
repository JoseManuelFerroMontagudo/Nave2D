using UnityEngine;

/// <summary>
/// Energía de la nave — consumo y recarga.
/// </summary>
public class ShipEnergy : MonoBehaviour
{
    public float totalEnergy = 1000f;
    public float replenishRate = 55f;

    public float Current { get; private set; }
    public float Percent => Current / totalEnergy;

    void Awake() => Current = totalEnergy;

    void FixedUpdate()
    {
        if (Current < totalEnergy)
            Current = Mathf.Min(totalEnergy, Current + replenishRate * Time.fixedDeltaTime);
    }

    public bool HasEnough(float amount) => Current >= amount;

    public bool TryConsume(float amount)
    {
        if (Current < amount) return false;
        Current -= amount;
        return true;
    }
}