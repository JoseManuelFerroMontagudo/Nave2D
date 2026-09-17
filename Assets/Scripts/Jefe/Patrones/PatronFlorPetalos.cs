using UnityEngine;
using System.Collections;

[CreateAssetMenu(menuName = "Jefe/Patrones/Flor de Pétalos", fileName = "PatronFlorPetalos")]
public class PatronFlorPetalos : PatronBase
{
    [Header("Configuración")]
    public int petalos = 8;
    public int balasPorPetalo = 7;
    public float aperturaPetalo = 20f;
    public float intervaloEntrePetalos = 0.08f;
    public float pausaEntreOleadas = 0.15f;

    [Header("Proyectil")]
    public OpcionesProyectil opciones = new OpcionesProyectil
    {
        modo = ModoMovimiento.Ondulado,
        velocidad = 9f,
        tiempoVuelo = 2.5f,
        color = Color.magenta,
        amplitudOnda = 1.5f,
        frecuenciaOnda = 4f
    };

    public override IEnumerator Ejecutar(ContextoPatron ctx)
    {
        yield return DispararOleada(ctx, 0f);
        yield return new WaitForSeconds(pausaEntreOleadas);
        yield return DispararOleada(ctx, 0.5f);
    }

    IEnumerator DispararOleada(ContextoPatron ctx, float desfase)
    {
        float paso = 360f / petalos;
        for (int p = 0; p < petalos; p++)
        {
            float centro = paso * p + paso * desfase;
            for (int b = 0; b < balasPorPetalo; b++)
            {
                float fraccion = balasPorPetalo > 1 ? (float)b / (balasPorPetalo - 1) : 0.5f;
                float dispersion = (fraccion - 0.5f) * aperturaPetalo;
                var opc = opciones.Clonar();
                opc.velocidad = opciones.velocidad * (0.9f + b * 0.08f);
                opc.amplitudOnda = 1.5f + b * 0.5f;
                opc.frecuenciaOnda = 4f + b * 1.2f;
                ctx.Disparar(ctx.DireccionDesdeGrados(centro + dispersion), opc);
            }
            yield return new WaitForSeconds(intervaloEntrePetalos);
        }
    }
}