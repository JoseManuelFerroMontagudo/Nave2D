using UnityEngine;
using UnityEngine.Events;

public class VidaPersonaje : MonoBehaviour
{
    [Header("Vida / Fases (jefe)")]
    public float vidaMaxima = 100f;
    public int fasesTotales = 1;
    [HideInInspector] public float vidaActual;
    [HideInInspector] public int faseActual = 1;

    [Header("Comportamiento al llegar a 0")]
    [Tooltip("true = destruye el GameObject (enemigo normal). " +
             "false = solo dispara OnMuere; el GO queda vivo (hardpoint, cañón, etc.).")]
    public bool destruyeAlMorir = true;

    [Header("Feedback de daño")]
    public float duracionFlash = 0.1f;
    public float cadenciaFlash = 0.15f;
    public Color colorFlash = Color.red;

    public UnityEvent<float> OnVidaCambia;
    public UnityEvent<int> OnFaseCambia;
    [Tooltip("Se dispara al llegar a 0 (con o sin destrucción)")]
    public UnityEvent OnMuere;

    SpriteRenderer sprite;
    Color colorOriginal;
    float flashTimer;
    float ultimoFlash = -999f;

    bool muerto;
    public bool Muerto => muerto;

    void Awake()
    {
        vidaActual = vidaMaxima;
        faseActual = 1;
        sprite = GetComponentInChildren<SpriteRenderer>();
        if (sprite != null) colorOriginal = sprite.color;
    }

    void Update()
    {
        if (sprite == null || flashTimer <= 0f) return;
        flashTimer -= Time.deltaTime;
        float t = Mathf.Clamp01(flashTimer / duracionFlash);
        sprite.color = Color.Lerp(colorOriginal, colorFlash, t);
    }

    void OnTriggerEnter2D(Collider2D otro)
    {
        if (muerto || !otro.CompareTag("Attack1")) return;
        var d = otro.GetComponentInParent<IDamageDealer>();
        if (d != null) { RecibirDaño(d.Daño); DispararFlash(); }
    }

    void OnTriggerStay2D(Collider2D otro)
    {
        if (muerto || !otro.CompareTag("Attack2")) return;
        var d = otro.GetComponentInParent<IDamageDealer>();
        if (d != null) { RecibirDaño(d.Daño * Time.deltaTime); DispararFlash(); }
    }

    void DispararFlash()
    {
        if (Time.time - ultimoFlash < cadenciaFlash) return;
        ultimoFlash = Time.time;
        flashTimer = duracionFlash;
    }

    public void RecibirDaño(float daño)
    {
        if (muerto) return;

        vidaActual -= daño;
        OnVidaCambia?.Invoke(vidaActual);
        if (vidaActual > 0f) return;

        // Se agotó la vida de la fase actual
        if (faseActual < fasesTotales)
        {
            faseActual++;
            vidaActual = vidaMaxima;
            OnFaseCambia?.Invoke(faseActual);
            return;
        }

        // Última fase agotada
        muerto = true;
        OnMuere?.Invoke();

        if (destruyeAlMorir)
            Destroy(gameObject);
        // Si no destruye, el GO queda vivo pero "muerto" (ignora más daño)
    }

    void OnDestroy()
    {
        if (!muerto) { muerto = true; OnMuere?.Invoke(); }
    }
}