using UnityEngine;
using System.Collections;

[CreateAssetMenu(menuName = "Ataques/Cono")]
public class AtaqueCono : PatronAtaque
{
    public float anguloApertura = 15f;

    public override IEnumerator Ejecutar(Transform origen, Transform objetivo)
    {
        if (objetivo == null) yield break;
        Vector2 centro = (objetivo.position - origen.position).normalized;
        Disparar(origen, centro);
        Disparar(origen, Quaternion.Euler(0, 0, anguloApertura) * centro);
        Disparar(origen, Quaternion.Euler(0, 0, -anguloApertura) * centro);
        yield return null;
    }
}