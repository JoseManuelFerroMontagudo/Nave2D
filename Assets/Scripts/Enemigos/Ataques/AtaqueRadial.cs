using UnityEngine;
using System.Collections;

[CreateAssetMenu(menuName = "AtaqueJefe/Radial")]
public class AtaqueRadial : PatronAtaque
{
    [Header("Emisión radial")]
    [Tooltip("Balas por oleada (brazos). Circular: 8–24. Espiral: 1–4.")]
    public int cantidad = 12;

    [Tooltip("Veces que se repite la emisión.")]
    public int oleadas = 1;

    [Tooltip("Segundos entre oleadas. Circular: 0.3–1. Espiral: 0.03–0.1.")]
    public float intervaloOleada = 0.4f;

    [Header("Rotación")]
    [Tooltip("Grados que gira el patrón entre oleada y oleada. 0 = circular sin giro.")]
    public float rotacionPorOleada = 0f;

    [Header("Ángulo inicial")]
    public float anguloInicial = 0f;

    public override IEnumerator Ejecutar(Transform origen, Transform objetivo)
    {
        if (cantidad < 1) cantidad = 1;
        if (oleadas < 1) oleadas = 1;

        float paso = 360f / cantidad;
        float offset = anguloInicial;

        for (int o = 0; o < oleadas; o++)
        {
            for (int i = 0; i < cantidad; i++)
            {
                float a = (offset + i * paso) * Mathf.Deg2Rad;
                Vector2 dir = new Vector2(Mathf.Cos(a), Mathf.Sin(a));
                Disparar(origen, dir);
            }

            offset += rotacionPorOleada;

            if (o < oleadas - 1)
                yield return new WaitForSeconds(intervaloOleada);
        }
    }
}