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

    [Header("Referencias")]
    public Stats estadisticas;

    private Image rellenoCalor;

    void Start()
    {
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
}