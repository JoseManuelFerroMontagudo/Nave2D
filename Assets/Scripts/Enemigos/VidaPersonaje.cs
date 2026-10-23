using UnityEngine;
using UnityEngine.Events;

public class VidaPersonaje : MonoBehaviour
{
    [Header("Vida / Fases (jefe)")]
    public float vidaMaxima = 100f;
    public int fasesTotales = 1;
    [HideInInspector] public float vidaActual;
    [HideInInspector] public int faseActual = 1;

    [Header("Feedback de daño")]
    public float duracionFlash = 0.1f;   // cuánto tarda en volver a color normal
    public float cadenciaFlash = 0.15f;  // separación mínima entre flashes
    public Color colorFlash = Color.red;

    public UnityEvent<float> OnVidaCambia;
    public UnityEvent<int> OnFaseCambia;
    public UnityEvent OnMuere;

    SpriteRenderer sprite;
    Color colorOriginal;
    float flashTimer;
    float ultimoFlash = -999f;

    bool muerto;

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

        if (faseActual < fasesTotales)
        {
            faseActual++;
            vidaActual = vidaMaxima;
            OnFaseCambia?.Invoke(faseActual);
        }
        else
        {
            muerto = true;
            OnMuere?.Invoke();
            Destroy(gameObject);
        }
    }

    void OnDestroy()
    {
        if (!muerto) { muerto = true; OnMuere?.Invoke(); }
    }
}