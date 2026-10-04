using UnityEngine;

public class Projectile : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] float speed = 20f;
    [SerializeField] float lifetime = 3f;

    [Header("Damage (placeholder)")]
    [SerializeField] float damage = 10f;

    [Header("Impact")]
    [SerializeField] GameObject hitEffectPrefab;   // opcional

    Vector2 velocity;

    /// Llamado por quien lo instancia para configurar la velocidad inicial
    /// (incluye la velocidad heredada del disparador).
    public void Initialize(Vector2 initialVelocity)
    {
        velocity = initialVelocity;
    }

    void Start()
    {
        // Si nadie lo inicializó, dispara hacia su propio "up" a la velocidad por defecto
        if (velocity == Vector2.zero)
            velocity = transform.up * speed;

        Destroy(gameObject, lifetime);
    }

    void FixedUpdate()
    {
        transform.position += (Vector3)(velocity * Time.fixedDeltaTime);
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        // Ignora al que disparó: filtra por tag, layer, o componente
        if (other.CompareTag("Player")) return;
        if (other.CompareTag("Shield")) return;
        if (other.CompareTag("Bullet")) return;
        // TODO: aplicar daño aquí cuando exista sistema de vida
        // other.GetComponent<IDamageable>()?.TakeDamage(damage);

        if (hitEffectPrefab != null)
            Instantiate(hitEffectPrefab, transform.position, transform.rotation);

        Destroy(gameObject);
    }
}