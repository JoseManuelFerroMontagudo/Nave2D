using UnityEngine;

/// <summary>
/// Vida de la nave.
/// </summary>
public class ShipHealth : MonoBehaviour
{
    public float maxHealth = 100f;
    private float current;

    public float Current => current;
    public float Percent => current / maxHealth;

    public System.Action OnDeath;

    void Awake() => current = maxHealth;

    public void ApplyDamage(float damage)
    {
        if (current <= 0f) return;

        current -= damage;
        if (current <= 0f)
        {
            current = 0f;
            OnDeath?.Invoke();
            Debug.Log("Nave destruida!");
        }
    }
}