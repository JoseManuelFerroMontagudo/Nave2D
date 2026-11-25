using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Canvas))] // o usa un Canvas hijo en World Space
public class BarraVidaEnemigo : MonoBehaviour
{
    [Header("Referencias")]
    [Tooltip("El VidaPersonaje del enemigo (padre o hermano)")]
    public VidaPersonaje vida;
    [Tooltip("La Image con Image Type = Filled")]
    public Image relleno;

    [Header("Seguimiento (mundo)")]
    [Tooltip("Offset respecto al enemigo (ej: 0, 1.2, 0)")]
    public Vector3 offset = new Vector3(0f, 1.2f, 0f);
    [Tooltip("Si está activo, la barra siempre mira a la cámara")]
    public bool mirarCamara = true;

    [Header("Fijar a pantalla")]
    [Tooltip("Si está activo, la barra no sigue al enemigo y queda fija en la pantalla (posición definida en el prefab)")]
    public bool fijarAPantalla = false;

    [Header("Visibilidad")]
    [Tooltip("Si está activo, la barra solo aparece al recibir daño; si no, siempre visible")]
    public bool soloVisibleAlDañar = true;
    [Tooltip("Segundos que permanece visible tras el último daño")]
    public float duracionVisible = 2f;
    [Tooltip("Suavizado al aparecer/desaparecer")]
    public float velocidadFade = 8f;

    CanvasGroup grupo;
    Transform objetivo;      // el enemigo al que sigue
    float tiempoUltimoDaño = -999f;
    float alphaObjetivo = 1f;

    void Awake()
    {
        grupo = GetComponent<CanvasGroup>();
        if (grupo == null) grupo = gameObject.AddComponent<CanvasGroup>();

        // Solo forzar modo si este Canvas es raíz (no tiene padre Canvas)
        var canvas = GetComponent<Canvas>();
        if (canvas != null && canvas.isRootCanvas)
        {
            canvas.renderMode = fijarAPantalla
                ? RenderMode.ScreenSpaceOverlay
                : RenderMode.WorldSpace;
        }

        // Objetivo a seguir: el transform del enemigo (padre)
        objetivo = transform.parent != null ? transform.parent : transform;

        // Si se fija a pantalla, no tiene sentido mirar a la cámara
        if (fijarAPantalla) mirarCamara = false;
    }

    void Start()
    {
        if (vida == null) vida = GetComponentInParent<VidaPersonaje>();

        if (vida != null)
        {
            vida.OnVidaCambia.AddListener(AlCambiarVida);
            vida.OnFaseCambia.AddListener(AlCambiarFase);
            vida.OnMuere.AddListener(AlMorir);
            ActualizarRelleno();
        }

        // Estado inicial de visibilidad
        if (soloVisibleAlDañar)
        {
            grupo.alpha = 0f;
            alphaObjetivo = 0f;
        }
        else
        {
            grupo.alpha = 1f;
            alphaObjetivo = 1f;
        }
    }

    void LateUpdate()
    {
        // 1) Seguimiento en mundo (solo si no está fija a pantalla)
        if (!fijarAPantalla)
        {
            if (objetivo != null)
                transform.position = objetivo.position + offset;

            if (mirarCamara && Camera.main != null)
                transform.rotation = Camera.main.transform.rotation;
        }

        // 2) Control de visibilidad
        if (soloVisibleAlDañar)
            alphaObjetivo = (Time.time - tiempoUltimoDaño <= duracionVisible) ? 1f : 0f;
        else
            alphaObjetivo = 1f;

        grupo.alpha = Mathf.Lerp(grupo.alpha, alphaObjetivo, velocidadFade * Time.deltaTime);
    }

    void AlCambiarVida(float valor)
    {
        tiempoUltimoDaño = Time.time;
        ActualizarRelleno();
    }

    void AlCambiarFase(int fase)
    {
        tiempoUltimoDaño = Time.time;
        ActualizarRelleno();
    }

    void AlMorir()
    {
        // Ocultar al morir (respetando el modo de visibilidad)
        if (soloVisibleAlDañar) alphaObjetivo = 0f;
    }

    void ActualizarRelleno()
    {
        if (vida == null || relleno == null) return;
        relleno.fillAmount = Mathf.Clamp01(vida.vidaActual / vida.vidaMaxima);
    }

    void OnDestroy()
    {
        if (vida != null)
        {
            vida.OnVidaCambia.RemoveListener(AlCambiarVida);
            vida.OnFaseCambia.RemoveListener(AlCambiarFase);
            vida.OnMuere.RemoveListener(AlMorir);
        }
    }
}