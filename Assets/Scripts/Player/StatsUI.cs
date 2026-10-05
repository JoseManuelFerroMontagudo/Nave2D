using UnityEngine;
using TMPro;

public class StatsUI : MonoBehaviour
{
    [Header("Textos")]
    public TMP_Text textoVida;
    public TMP_Text textoEnergia;
    public TMP_Text textoMunicion;

    [Header("Referencias")]
    public Stats estadisticas;

    void Update()
    {
        if (estadisticas == null) return;

        textoVida.text = $"Vida: {estadisticas.vida}";
        textoEnergia.text = $"Energía: {estadisticas.energia}";
        textoMunicion.text = $"Munición: {estadisticas.municion}";
    }
}