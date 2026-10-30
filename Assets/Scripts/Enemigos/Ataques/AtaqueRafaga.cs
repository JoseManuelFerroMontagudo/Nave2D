using UnityEngine;
using System.Collections;

[CreateAssetMenu(menuName = "AtaqueJefe/Ráfaga")]
public class AtaqueRafaga : PatronAtaque
{
    public int cantidad = 3;
    public float intervalo = 0.18f;

    public override IEnumerator Ejecutar(Transform origen, Transform objetivo)
    {
        for (int i = 0; i < cantidad; i++)
        {
            if (objetivo == null) yield break;
            Disparar(origen, (objetivo.position - origen.position).normalized);
            yield return new WaitForSeconds(intervalo);
        }
    }
}