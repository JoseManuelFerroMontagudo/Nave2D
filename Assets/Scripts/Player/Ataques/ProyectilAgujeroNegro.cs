using UnityEngine;

[RequireComponent(typeof(Projectile), typeof(CircleCollider2D))]
public class ProyectilAgujeroNegro : MonoBehaviour
{
    [Header("Tamaños (× lado menor del sprite)")]
    public float multiplicadorDiametroColider = 0.6f;
    public float multiplicadorDiametroAtraccion = 3f;

    [Header("Absorción (acepta negativos)")]
    public float escalaPorAbsorcion = 0.08f;
    public float escalaMaxima = 3f;
    public float escalaMinima = 0.1f;
    public float dañoPorAbsorcion = 3f;
    public string[] tagsProyectilEnemigo = { "Bullet", "Bullet1", "Bullet2", "Bullet3" };
    public float fuerzaAtraccion = 20f;
    public bool destruirAlMinimo = true;

    Projectile proyectil;
    CircleCollider2D col;
    SpriteRenderer sr;
    Vector3 escalaBase;
    float medidaBase, escala = 1f;

    float MedidaActual => medidaBase * escala;

    void Awake()
    {
        proyectil = GetComponent<Projectile>();
        col = GetComponent<CircleCollider2D>();
        sr = GetComponentInChildren<SpriteRenderer>();
        escalaBase = sr ? sr.transform.localScale : Vector3.one;
    }

    void Start()
    {
        if (sr && sr.sprite)
            medidaBase = Mathf.Min(sr.bounds.size.x, sr.bounds.size.y);
        AplicarEscalas();
    }

    void AplicarEscalas()
    {
        float radio = 0.5f * multiplicadorDiametroColider * MedidaActual;
        float sx = Mathf.Abs(transform.lossyScale.x);
        col.radius = sx > 0.0001f ? radio / sx : radio;
        col.enabled = escala > 0.001f;
    }

    void FixedUpdate()
    {
        if (escala <= 0.001f) return;
        float radio = 0.5f * multiplicadorDiametroAtraccion * MedidaActual;
        if (radio <= 0.001f) return;

        var hits = Physics2D.OverlapCircleAll(transform.position, radio);
        float step = fuerzaAtraccion * Time.fixedDeltaTime;
        foreach (var h in hits)
        {
            if (!h || h.transform == transform) continue;
            if (System.Array.IndexOf(tagsProyectilEnemigo, h.tag) < 0) continue;

            Vector2 hacia = (Vector2)transform.position - (Vector2)h.transform.position;
            float dist = hacia.magnitude;
            if (dist < 0.05f) continue;
            float t = 1f - Mathf.Clamp01(dist / radio);
            h.transform.position += (Vector3)(hacia / dist * step * t);
        }
    }

    void OnTriggerEnter2D(Collider2D otro)
    {
        if (System.Array.IndexOf(tagsProyectilEnemigo, otro.tag) < 0) return;

        proyectil.daño = Mathf.Max(0f, proyectil.daño + dañoPorAbsorcion);
        escala = Mathf.Clamp(escala + escalaPorAbsorcion, escalaMinima, escalaMaxima);
        if (sr) sr.transform.localScale = escalaBase * escala;
        AplicarEscalas();
        Destroy(otro.gameObject);

        if (destruirAlMinimo && escala <= escalaMinima + 0.0001f) Destroy(gameObject);
    }
}