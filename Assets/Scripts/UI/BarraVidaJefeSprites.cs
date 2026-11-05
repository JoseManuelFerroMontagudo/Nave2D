using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

/// <summary>
/// Barra de vida del jefe basada en sprites (estilo Dark Souls).
/// Cambia entre los frames del PNG según el porcentaje de vida del ControladorJefe.
/// Frame 0 = vida llena ... último frame = vacía.
/// </summary>
public class BarraVidaJefeSprites : MonoBehaviour
{
    [Header("Referencias")]
    public ControladorJefe jefe;                 // Si se deja vacío, se busca automáticamente
    public Image imagenBarra;                    // Image UI que mostrará los sprites
    public TextMeshProUGUI textoNombre;          // Opcional
    public CanvasGroup grupo;                    // Para fade in/out (en el contenedor)

    [Header("Posición en pantalla")]
    public bool forzarPosicion = true;
    public Vector2 anclaPantalla = new Vector2(0.5f, 0f);   // (0.5,0) = abajo centro, (0.5,1) = arriba centro
    public Vector2 desplazamiento = new Vector2(0f, 90f);   // separación del borde

    [Header("Sprites (de llena a vacía)")]
    public Sprite[] frames;                      // Arrastra los 7 sprites en orden

    [Header("Estilo")]
    public string nombreJefe = "NAVE NODRIZA";
    public float duracionEntrada = 1.2f;
    public float intensidadSacudida = 6f;
    public float duracionSacudida = 0.15f;
    public Color colorGolpe = new Color(1f, 0.85f, 0.85f);

    RectTransform rect;
    Vector2 posOriginal;
    float vidaAnterior;
    bool activa;
    bool muerto;
    Coroutine sacudida;

    void Awake()
    {
        if (forzarPosicion && transform is RectTransform hud)
        {
            hud.anchorMin = hud.anchorMax = anclaPantalla;
            hud.pivot = new Vector2(0.5f, anclaPantalla.y);
            hud.anchoredPosition = new Vector2(desplazamiento.x, anclaPantalla.y >= 0.5f ? -desplazamiento.y : desplazamiento.y);
        }

        rect = imagenBarra != null ? imagenBarra.rectTransform : null;
        if (rect != null) posOriginal = rect.anchoredPosition;
        if (grupo != null) grupo.alpha = 0f;
    }

    void Update()
    {
        if (jefe == null)
        {
            if (muerto) return;
            jefe = FindFirstObjectByType<ControladorJefe>();
            if (jefe != null)
            {
                Debug.Log($"[BarraVidaJefe] Jefe encontrado: {jefe.name} ({jefe.vida}/{jefe.VidaMaxima}). Mostrando barra.");
                StartCoroutine(Entrada());
            }
            return;
        }
        if (!activa) return;

        float pct = Mathf.Clamp01(jefe.vida / jefe.VidaMaxima);
        PonerFrame(pct);

        if (jefe.vida < vidaAnterior)
        {
            vidaAnterior = jefe.vida;
            if (sacudida != null) StopCoroutine(sacudida);
            sacudida = StartCoroutine(Sacudir());
        }

        if (jefe.vida <= 0f && !muerto)
        {
            muerto = true;
            activa = false;
            StartCoroutine(Salida());
        }
    }

    void PonerFrame(float pct)
    {
        if (frames == null || frames.Length == 0 || imagenBarra == null) return;
        // Vida > 0 nunca muestra el frame vacío
        int ultimo = frames.Length - 1;
        int idx = pct <= 0f ? ultimo : Mathf.Clamp(Mathf.FloorToInt((1f - pct) * ultimo), 0, ultimo - 1);
        imagenBarra.sprite = frames[idx];
    }

    IEnumerator Entrada()
    {
        if (textoNombre != null) textoNombre.text = nombreJefe;
        vidaAnterior = jefe.vida;

        // Rellena la barra de vacía a llena + fade in
        float t = 0f;
        while (t < duracionEntrada)
        {
            t += Time.deltaTime;
            float p = t / duracionEntrada;
            if (grupo != null) grupo.alpha = Mathf.Clamp01(p * 2f);
            PonerFrame(1f - Mathf.Pow(1f - p, 3f));
            yield return null;
        }
        if (grupo != null) grupo.alpha = 1f;
        activa = true;
    }

    IEnumerator Sacudir()
    {
        if (imagenBarra != null) imagenBarra.color = colorGolpe;
        float t = 0f;
        while (t < duracionSacudida)
        {
            t += Time.deltaTime;
            if (rect != null) rect.anchoredPosition = posOriginal + Random.insideUnitCircle * intensidadSacudida;
            yield return null;
        }
        if (rect != null) rect.anchoredPosition = posOriginal;
        if (imagenBarra != null) imagenBarra.color = Color.white;
    }

    IEnumerator Salida()
    {
        PonerFrame(0f);
        yield return new WaitForSeconds(1f);
        float t = 0f;
        while (t < 1.5f)
        {
            t += Time.deltaTime;
            if (grupo != null) grupo.alpha = 1f - t / 1.5f;
            yield return null;
        }
        gameObject.SetActive(false);
    }
}
