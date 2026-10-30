using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class StatsUI : MonoBehaviour
{
    [Header("Sliders")]
    public Slider sliderVida;
    public Slider sliderEnergia;
    public Slider sliderCalor;
    public TMP_Text PuntajeText;

    [Header("Color del calor (gradiente frío → caliente)")]
    public Color colorCalorFrio = Color.green;
    public Color colorCalorMedio = Color.yellow;   // pico intermedio (se pasa al ~60%)
    public Color colorCalorCaliente = Color.red;

    [Header("Munición (iconos repetidos)")]
    public RectTransform contenedorIconos;
    public Image iconoPlantilla;

    [Header("Referencias")]
    public Stats estadisticas;

    private Image[] iconos;
    private int ultimaMunicion = -1;
    private Image rellenoCalor;

    void Start()
    {
        CrearIconos(estadisticas.municionMaxima);

        // Cachear el Image del fill del slider de calor para recolorear sin GetComponent cada frame
        if (sliderCalor != null && sliderCalor.fillRect != null)
            rellenoCalor = sliderCalor.fillRect.GetComponent<Image>();
    }

    void Update()
    {
        if (estadisticas == null) return;

        // --- Sliders ---
        if (sliderVida != null)
            sliderVida.value = estadisticas.vida;

        if (sliderEnergia != null)
            sliderEnergia.value = estadisticas.energia;

        if (sliderCalor != null)
        {
            sliderCalor.value = estadisticas.calorActual;

            if (rellenoCalor != null)
            {
                float t = Mathf.Clamp01(estadisticas.CalorNormalizado);
                rellenoCalor.color = EvaluarColorCalor(t);
            }
        }
        if (estadisticas.Recalentado) // recalentado
            rellenoCalor.color = Color.Lerp(Color.red, Color.white, Mathf.PingPong(Time.time * 4f, 1f));

        // --- Puntaje ---
        if (PuntajeText != null)
            PuntajeText.text = $"Puntaje: {estadisticas.puntos}";

        // --- Iconos de munición ---
        ActualizarIconos((int)estadisticas.municion);
    }

    /// <summary>
    /// Gradiente de 3 puntos: frío → medio → caliente.
    /// </summary>
    Color EvaluarColorCalor(float t)
    {
        if (t < 0.6f)
            return Color.Lerp(colorCalorFrio, colorCalorMedio, t / 0.6f);
        else
            return Color.Lerp(colorCalorMedio, colorCalorCaliente, (t - 0.6f) / 0.4f);
    }

    void CrearIconos(int cantidad)
    {
        if (contenedorIconos == null || iconoPlantilla == null) return;

        foreach (Transform hijo in contenedorIconos)
        {
            if (hijo != iconoPlantilla.transform)
                Destroy(hijo.gameObject);
        }

        iconos = new Image[cantidad];

        for (int i = 0; i < cantidad; i++)
        {
            Image nuevo = Instantiate(iconoPlantilla, contenedorIconos);
            nuevo.gameObject.SetActive(true);
            iconos[i] = nuevo;
        }

        iconoPlantilla.gameObject.SetActive(false);
        ultimaMunicion = -1;
    }

    void ActualizarIconos(int cantidadActual)
    {
        if (iconos == null) return;
        if (cantidadActual == ultimaMunicion) return;
        ultimaMunicion = cantidadActual;

        cantidadActual = Mathf.Clamp(cantidadActual, 0, iconos.Length);

        for (int i = 0; i < iconos.Length; i++)
        {
            iconos[i].color = i < cantidadActual ? Color.white : new Color(1f, 1f, 1f, 0.25f);
        }
    }
}