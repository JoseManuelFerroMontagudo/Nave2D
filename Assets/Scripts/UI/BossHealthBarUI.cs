using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using TMPro;

/// <summary>
/// Barra de vida estilo Dark Souls para el boss.
/// Se muestra con una animación de entrada dramática.
/// Tiene barra de fondo (gris), barra de daño retrasado (roja), y barra actual (color principal).
/// </summary>
public class BossHealthBarUI : MonoBehaviour
{
    [Header("Elementos UI")]
    [Tooltip("Imagen de relleno de la barra de vida ACTUAL")]
    public Image barraVidaActual;

    [Tooltip("Imagen de relleno de la barra de vida RETRASADA (efecto daño)")]
    public Image barraVidaRetrasada;

    [Tooltip("Texto con el nombre del boss")]
    public TextMeshProUGUI textoNombreBoss;

    [Tooltip("Texto de alerta (¡PELIGRO!)")]
    public TextMeshProUGUI textoAlerta;

    [Tooltip("GameObject padre del contenedor completo (para show/hide)")]
    public GameObject contenedor;

    [Header("Configuración")]
    public string nombreBoss = "NAVE NODRIZA";
    public float velocidadBarraRetrasada = 0.5f;
    public float retardoDañoRetrasado = 0.6f;

    [Header("Colores")]
    public Color colorVidaAlta = new Color(0.8f, 0.2f, 1f, 1f);
    public Color colorVidaMedia = new Color(1f, 0.6f, 0f, 1f);
    public Color colorVidaBaja = new Color(0.9f, 0.1f, 0.1f, 1f);
    public Color colorBarraRetrasada = new Color(0.8f, 0.2f, 0.2f, 0.8f);

    private ControladorJefe jefeActual;
    private float vidaMaxima;
    private float vidaAnterior;
    private float objetivoRetrasado;
    private float timerRetrasado;
    private bool activa = false;
    private Coroutine coroutinaEntrada;

    void Start()
    {
        if (contenedor != null)
            contenedor.SetActive(false);

        if (textoAlerta != null)
            textoAlerta.gameObject.SetActive(false);
    }

    void Update()
    {
        if (!activa || jefeActual == null) return;

        float porcentaje = Mathf.Clamp01(jefeActual.vida / vidaMaxima);

        if (barraVidaActual != null)
        {
            barraVidaActual.fillAmount = porcentaje;
            barraVidaActual.color = ObtenerColorVida(porcentaje);
        }

        if (jefeActual.vida < vidaAnterior)
        {
            timerRetrasado = retardoDañoRetrasado;
            vidaAnterior = jefeActual.vida;
        }

        if (barraVidaRetrasada != null)
        {
            if (timerRetrasado > 0f)
            {
                timerRetrasado -= Time.deltaTime;
            }
            else
            {
                float objetivoRet = porcentaje;
                barraVidaRetrasada.fillAmount = Mathf.MoveTowards(
                    barraVidaRetrasada.fillAmount,
                    objetivoRet,
                    velocidadBarraRetrasada * Time.deltaTime
                );
            }
        }

        if (jefeActual.vida <= 0f)
        {
            activa = false;
        }
    }

    Color ObtenerColorVida(float porcentaje)
    {
        if (porcentaje > 0.6f) return colorVidaAlta;
        if (porcentaje > 0.3f) return Color.Lerp(colorVidaMedia, colorVidaAlta, (porcentaje - 0.3f) / 0.3f);
        return Color.Lerp(colorVidaBaja, colorVidaMedia, porcentaje / 0.3f);
    }

    public void InicializarConBoss(ControladorJefe jefe)
    {
        jefeActual = jefe;
        vidaMaxima = jefe.vida;
        vidaAnterior = jefe.vida;

        if (barraVidaActual != null)
        {
            barraVidaActual.fillAmount = 1f;
            barraVidaActual.color = colorVidaAlta;
        }

        if (barraVidaRetrasada != null)
        {
            barraVidaRetrasada.fillAmount = 1f;
            barraVidaRetrasada.color = colorBarraRetrasada;
        }

        if (textoNombreBoss != null)
            textoNombreBoss.text = nombreBoss;

        if (coroutinaEntrada != null) StopCoroutine(coroutinaEntrada);
        coroutinaEntrada = StartCoroutine(AnimarEntrada());
    }

    IEnumerator AnimarEntrada()
    {
        if (textoAlerta != null)
            textoAlerta.gameObject.SetActive(false);

        if (contenedor != null)
            contenedor.SetActive(true);

        if (barraVidaActual != null) barraVidaActual.fillAmount = 0f;
        if (barraVidaRetrasada != null) barraVidaRetrasada.fillAmount = 0f;

        float t = 0f;
        float duracion = 1.5f;
        while (t < duracion)
        {
            t += Time.deltaTime;
            float prog = t / duracion;
            float ease = 1f - Mathf.Pow(1f - prog, 3f);

            if (barraVidaActual != null) barraVidaActual.fillAmount = ease;
            if (barraVidaRetrasada != null) barraVidaRetrasada.fillAmount = ease;
            yield return null;
        }

        if (barraVidaActual != null) barraVidaActual.fillAmount = 1f;
        if (barraVidaRetrasada != null) barraVidaRetrasada.fillAmount = 1f;

        activa = true;
    }

    public void MostrarAlerta(string mensaje)
    {
        if (textoAlerta != null)
        {
            textoAlerta.gameObject.SetActive(true);
            textoAlerta.text = mensaje;
            StartCoroutine(AnimarAlerta());
        }
    }

    IEnumerator AnimarAlerta()
    {
        if (textoAlerta == null) yield break;

        for (int i = 0; i < 6; i++)
        {
            textoAlerta.alpha = 1f;
            yield return new WaitForSeconds(0.3f);
            textoAlerta.alpha = 0.3f;
            yield return new WaitForSeconds(0.2f);
        }

        textoAlerta.alpha = 0f;
        textoAlerta.gameObject.SetActive(false);
    }

    public void AnimarDerrota()
    {
        StartCoroutine(AnimarDerrotaCoroutine());
    }

    IEnumerator AnimarDerrotaCoroutine()
    {
        if (barraVidaActual != null) barraVidaActual.color = Color.white;
        yield return new WaitForSeconds(0.3f);

        float t = 0f;
        float duracion = 2f;
        CanvasGroup grupo = contenedor != null ? contenedor.GetComponent<CanvasGroup>() : null;

        if (grupo == null && contenedor != null)
            grupo = contenedor.AddComponent<CanvasGroup>();

        while (t < duracion)
        {
            t += Time.deltaTime;
            if (grupo != null) grupo.alpha = 1f - (t / duracion);
            yield return null;
        }

        if (contenedor != null) contenedor.SetActive(false);
        if (grupo != null) grupo.alpha = 1f;
    }

    public void Ocultar()
    {
        activa = false;
        if (contenedor != null) contenedor.SetActive(false);
        if (textoAlerta != null) textoAlerta.gameObject.SetActive(false);
    }
}
