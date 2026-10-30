using UnityEngine;
using System.Collections;

[CreateAssetMenu(menuName = "AtaqueJefe/Cono")]
public class AtaqueCono : PatronAtaque
{
    [Header("Cono")]
    [Tooltip("Balas por oleada, repartidas en el abanico")]
    public int cantidad = 3;
    [Tooltip("Apertura a cada lado del centro (grados)")]
    public float anguloApertura = 15f;
    [Tooltip("Número de ráfagas")]
    public int oleadas = 1;
    public float intervaloOleada = 0.3f;

    public override IEnumerator Ejecutar(Transform origen, Transform objetivo)
    {
        if (cantidad < 1) cantidad = 1;
        if (oleadas < 1) oleadas = 1;

        for (int o = 0; o < oleadas; o++)
        {
            if (objetivo == null) yield break;

            Vector2 centro = (objetivo.position - origen.position).normalized;
            float angCentro = Mathf.Atan2(centro.y, centro.x) * Mathf.Rad2Deg;

            for (int i = 0; i < cantidad; i++)
            {
                // t ∈ [-0.5, 0.5] (o 0 si sólo hay 1 bala)
                float t = cantidad > 1 ? (i / (float)(cantidad - 1)) - 0.5f : 0f;
                float delta = t * (anguloApertura * 2f);
                float a = (angCentro + delta) * Mathf.Deg2Rad;
                Vector2 dir = new Vector2(Mathf.Cos(a), Mathf.Sin(a));
                Disparar(origen, dir);
            }

            if (o < oleadas - 1)
                yield return new WaitForSeconds(intervaloOleada);
        }
    }
}