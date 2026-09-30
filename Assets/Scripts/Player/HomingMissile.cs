using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class HomingMissile : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] float speed = 12f;
    [SerializeField] float lifetime = 5f;
    [Tooltip("Offset en grados si el sprite no apunta hacia 'arriba'")]
    [SerializeField] float spriteRotationOffset = -90f;

    [Header("Homing")]
    [SerializeField] string targetTag = "Enemy";
    [SerializeField] float detectionRadius = 6f;
    [Tooltip("Grados por segundo iniciales. Más bajo = más esquivable.")]
    [SerializeField] float turnRate = 90f;

    [Tooltip("Grados/seg que aumenta el giro mientras el misil sigue enganchado.")]
    [SerializeField] float turnRateGrowth = 120f;

    [Tooltip("Tope máximo de giro (grados/seg). Evita misiles imposibles de esquivar.")]
    [SerializeField] float maxTurnRate = 360f;

    [Tooltip("Cada cuánto re-evalúa el objetivo (permite re-enganche).")]
    [SerializeField] float reacquireInterval = 0.25f;
    [SerializeField] LayerMask targetLayers = ~0;

    [Header("Impact")]
    [SerializeField] GameObject hitEffectPrefab;

    Rigidbody2D rb;
    Vector2 direction;
    Transform target;
    float lifeRemaining;
    float nextReacquireTime;
    float currentTurnRate;

    // ---- API pública para el arma ----
    public void Initialize(Vector2 initialVelocity, Transform explicitTarget = null)
    {
        direction = initialVelocity.sqrMagnitude > 0.0001f
            ? initialVelocity.normalized
            : (Vector2)transform.up;
        target = explicitTarget;
    }

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        rb.gravityScale = 0f;
        rb.linearDamping = 0f;
        rb.angularDamping = 0f;
        lifeRemaining = lifetime;
        currentTurnRate = turnRate;
    }

    void FixedUpdate()
    {
        // --- Vida ---
        lifeRemaining -= Time.fixedDeltaTime;
        if (lifeRemaining <= 0f) { Destroy(gameObject); return; }

        // --- Re-adquisición periódica del objetivo ---
        if (Time.time >= nextReacquireTime)
        {
            nextReacquireTime = Time.time + reacquireInterval;
            AcquireTarget();
        }

        // --- Giro suave hacia el objetivo ---
        if (target != null)
        {
            // Mientras sigue enganchado, el giro crece progresivamente.
            // Esto rompe las órbitas estables: tarde o temprano cierra el ángulo
            // y termina impactando.
            currentTurnRate = Mathf.Min(
                currentTurnRate + turnRateGrowth * Time.fixedDeltaTime,
                maxTurnRate);

            Vector2 toTarget = ((Vector2)target.position - rb.position).normalized;
            float signedAngle = Vector2.SignedAngle(direction, toTarget);
            float maxStep = currentTurnRate * Time.fixedDeltaTime;
            float step = Mathf.Clamp(signedAngle, -maxStep, maxStep);
            direction = Rotate(direction, step);
        }
        else
        {
            // Se desvinculó: reinicia la agresividad del giro.
            currentTurnRate = turnRate;
        }

        // --- Movimiento a velocidad CONSTANTE en la dirección actual ---
        rb.linearVelocity = direction * speed;

        // --- Orientación visual del sprite ---
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        rb.MoveRotation(angle + spriteRotationOffset);
    }

    void AcquireTarget()
    {
        // Histéresis: si el objetivo actual sigue vivo y cerca, no buscamos otro
        if (target != null)
        {
            float d = Vector2.Distance(rb.position, target.position);
            if (d <= detectionRadius * 1.5f) return;
        }

        target = null;

        Collider2D[] hits = Physics2D.OverlapCircleAll(rb.position, detectionRadius, targetLayers);
        float bestSqr = float.MaxValue;
        Transform best = null;
        foreach (var h in hits)
        {
            if (!h.CompareTag(targetTag)) continue;
            float sqr = ((Vector2)h.transform.position - rb.position).sqrMagnitude;
            if (sqr < bestSqr) { bestSqr = sqr; best = h.transform; }
        }
        target = best;
    }

    static Vector2 Rotate(Vector2 v, float degrees)
    {
        float r = degrees * Mathf.Deg2Rad;
        float c = Mathf.Cos(r), s = Mathf.Sin(r);
        return new Vector2(v.x * c - v.y * s, v.x * s + v.y * c);
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player")) return;   // red de seguridad
        // TODO: aplicar daño
        if (other.CompareTag("Shield")) return;
        if (other.CompareTag("Muros")) return;
        if (hitEffectPrefab != null)
            Instantiate(hitEffectPrefab, transform.position, transform.rotation);
        Destroy(gameObject);
    }
}