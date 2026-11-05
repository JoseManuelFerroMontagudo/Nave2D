using System.Collections;
using UnityEngine;

[CreateAssetMenu(menuName = "Ataques/Radial")]
public class AtaqueRadial : AtaqueBase
{
    public GameObject prefab;

    [Header("Emisión radial")]
    public int cantidad = 12;
    public int oleadas = 1;
    public float intervaloOleada = 0.4f;
    public float rotacionPorOleada = 0f;
    public float anguloInicial = 0f;

    public override IEnumerator Ejecutar(AtaqueContext ctx)
    {
        if (!prefab) yield break;
        if (cantidad < 1) cantidad = 1;
        if (oleadas < 1) oleadas = 1;

        float paso = 360f / cantidad;
        float offset = anguloInicial;

        for (int o = 0; o < oleadas; o++)
        {
            for (int i = 0; i < cantidad; i++)
            {
                float a = (offset + i * paso) * Mathf.Deg2Rad;
                var dir = new Vector2(Mathf.Cos(a), Mathf.Sin(a));
                ctx.Disparar(prefab, ctx.owner.position, dir);
            }
            offset += rotacionPorOleada;
            if (o < oleadas - 1)
                yield return new WaitForSeconds(intervaloOleada);
        }
    }
}