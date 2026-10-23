using System.Collections;
using UnityEngine;

public class FlashDano : MonoBehaviour
{
    [Header("Sprites a teñir (vacío = hijos)")]
    public SpriteRenderer[] sprites;

    [Header("Apariencia")]
    public Color colorDaño = Color.red;
    public float duracion = 0.1f;   // tiempo en rojo por flash
    public float intervalo = 0.1f;  // mínimo entre flashes (throttle Stay)

    Color[] originales;
    float siguientePermitido;
    Coroutine rutina;

    void Awake()
    {
        if (sprites == null || sprites.Length == 0)
            sprites = GetComponentsInChildren<SpriteRenderer>();

        originales = new Color[sprites.Length];
        for (int i = 0; i < sprites.Length; i++)
            if (sprites[i] != null) originales[i] = sprites[i].color;
    }

    public void Flash()
    {
        if (Time.time < siguientePermitido) return;   // ← throttle
        siguientePermitido = Time.time + intervalo;

        if (rutina != null) StopCoroutine(rutina);
        rutina = StartCoroutine(Ejecutar());
    }

    IEnumerator Ejecutar()
    {
        Pintar(colorDaño);
        yield return new WaitForSeconds(duracion);
        PintarOriginal();
        rutina = null;
    }

    void Pintar(Color c)
    {
        for (int i = 0; i < sprites.Length; i++)
            if (sprites[i] != null) sprites[i].color = c;
    }

    void PintarOriginal()
    {
        for (int i = 0; i < sprites.Length; i++)
            if (sprites[i] != null) sprites[i].color = originales[i];
    }
}