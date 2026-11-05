using System.Collections;
using UnityEngine;

[CreateAssetMenu(menuName = "Ataques/Proyectil")]
public class AtaqueProyectil : AtaqueBase
{
    public GameObject prefab;

    [Header("Disparo por cañón")]
    [Tooltip("ON: dispara todos los cañones en el mismo frame. Ignora intervaloBala. " +
             "Ideal para cadencias altas o escopetas.")]
    public bool simultaneo = false;

    [Header("Ráfagas / Oleadas")]
    public int oleadas = 1;
    public float intervaloOleada = 0.3f;
    [Tooltip("Solo si simultaneo = OFF. 0 = todos a la vez.")]
    public float intervaloBala = 0f;

    public override IEnumerator Ejecutar(AtaqueContext ctx)
    {
        if (!prefab) yield break;
        if (oleadas < 1) oleadas = 1;
        int total = Mathf.Max(1, cantidadCañones);

        for (int o = 0; o < oleadas; o++)
        {
            for (int i = 0; i < total; i++)
            {
                ctx.Disparar(prefab, i, total, separacion, modoSeparacion);

                if (!simultaneo && intervaloBala > 0f && i < total - 1)
                    yield return new WaitForSeconds(intervaloBala);
            }

            if (o < oleadas - 1)
                yield return new WaitForSeconds(intervaloOleada);
        }
    }
}