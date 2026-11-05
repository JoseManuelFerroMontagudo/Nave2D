using UnityEngine;

public enum ModoEscalado { Absorcion, Tiempo, Ambos }

[RequireComponent(typeof(Projectile), typeof(CircleCollider2D))]
public class ProyectilEscalable : MonoBehaviour
{
    [Header("Modo de escalado")]
    public ModoEscalado modoEscalado = ModoEscalado.Absorcion;

    [Header("Tamaños (× lado menor del sprite)")]
    public float multiplicadorDiametroColider = 0.6f;
    public float multiplicadorDiametroAtraccion = 3f;

    [Header("Límites")]
    public float escalaMinima = 0.1f;
    public float escalaMaxima = 3f;

    [Header("Escalado por tiempo")]
    [Tooltip("Positivo crece, negativo encoge. Solo aplica en modo Tiempo/Ambos.")]
    public float escalaPorSegundo = 0f;

    [Header("Absorción")]
    public float escalaPorAbsorcion = 0.08f;
    public int dañoPorAbsorcion = 3;
    public string[] tagsProyectilEnemigo = { "Bullet", "Bullet1", "Bullet2", "Bullet3" };
    public float fuerzaAtraccion = 20f;

    [Header("Destrucción automática")]
    public bool destruirAlMinimo = true;
    public bool destruirAlMaximo = false;

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
        if (sr) sr.transform.localScale = escalaBase * escala;
    }

    void FixedUpdate()
    {
        if (modoEscalado == ModoEscalado.Tiempo || modoEscalado == ModoEscalado.Ambos)
            EscalarPorTiempo(Time.fixedDeltaTime);

        // Atracción (misma que antes)
        if (escala <= 0.001f) return;
        float radioAtraccion = 0.5f * multiplicadorDiametroAtraccion * MedidaActual;
        if (radioAtraccion <= 0.001f || fuerzaAtraccion <= 0f) return;

        var hits = Physics2D.OverlapCircleAll(transform.position, radioAtraccion);
        float step = fuerzaAtraccion * Time.fixedDeltaTime;
        foreach (var h in hits)
        {
            if (!h || h.transform == transform) continue;
            if (System.Array.IndexOf(tagsProyectilEnemigo, h.tag) < 0) continue;

            Vector2 hacia = (Vector2)transform.position - (Vector2)h.transform.position;
            float dist = hacia.magnitude;
            if (dist < 0.05f) continue;
            float t = 1f - Mathf.Clamp01(dist / radioAtraccion);
            h.transform.position += (Vector3)(hacia / dist * step * t);
        }
    }

    void EscalarPorTiempo(float dt)
    {
        if (Mathf.Approximately(escalaPorSegundo, 0f)) return;

        float nueva = Mathf.Clamp(escala + escalaPorSegundo * dt, escalaMinima, escalaMaxima);
        if (!Mathf.Approximately(nueva, escala))
        {
            escala = nueva;
            AplicarEscalas();
        }
        VerificarDestruccion();
    }

    void OnTriggerEnter2D(Collider2D otro)
    {
        if (modoEscalado == ModoEscalado.Tiempo) return;
        if (System.Array.IndexOf(tagsProyectilEnemigo, otro.tag) < 0) return;

        proyectil.daño = Mathf.Max(0, proyectil.daño + dañoPorAbsorcion);
        escala = Mathf.Clamp(escala + escalaPorAbsorcion, escalaMinima, escalaMaxima);
        AplicarEscalas();
        Destroy(otro.gameObject);
        VerificarDestruccion();
    }

    void VerificarDestruccion()
    {
        if (destruirAlMinimo && escala <= escalaMinima + 0.0001f) Destroy(gameObject);
        else if (destruirAlMaximo && escala >= escalaMaxima - 0.0001f) Destroy(gameObject);
    }
}