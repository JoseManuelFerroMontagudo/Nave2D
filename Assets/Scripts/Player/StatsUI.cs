using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class StatsUI : MonoBehaviour
{
    [Header("Sliders")]
    public Slider sliderVida;
    public Slider sliderEnergia;

    [Header("Munición (iconos repetidos)")]
    public RectTransform contenedorIconos;   // PanelMunicion (Horizontal Layout Group)
    public Image iconoPlantilla;             // IconoMisil (desactivado en la jerarquía)

    [Header("Referencias")]
    public Stats estadisticas;

    private Image[] iconos;
    private int ultimaMunicion = -1;

    void Start()
    {
        CrearIconos(estadisticas.municionMaxima);
    }

    void Update()
    {
        if (estadisticas == null) return;
        // --- Sliders---
        if (sliderVida != null)
            sliderVida.value = estadisticas.vida;

        if (sliderEnergia != null)
            sliderEnergia.value = estadisticas.energia;

        // --- Iconos de munición ---
        ActualizarIconos((int)estadisticas.municion);
    }

    void CrearIconos(int cantidad)
    {
        if (contenedorIconos == null || iconoPlantilla == null) return;

        // Limpiar hijos previos (excepto la plantilla)
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
        ultimaMunicion = -1; // forzar refresco
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