using UnityEngine;
using System.Collections;

[CreateAssetMenu(menuName = "Jefe/Patrones/Muro de Ondas", fileName = "PatronMuroOndas")]
public class PatronMuroOndas : PatronBase
{
    [Header("Configuración")]
    public int oleadas = 6;
    public int balasPorOleada = 10;
    public float aperturaOleada = 120f;
    public float intervaloEntreOleadas = 0.15f;

    [Header("Proyectil")]
    public OpcionesProyectil opciones = new OpcionesProyectil
    {
        modo = ModoMovimiento.Ondulado,
        velocidad = 9f,
        tiempoVuelo = 2.5f,
        color = Color.green,
        amplitudOnda = 2f,
        frecuenciaOnda = 5f
    };

    public override IEnumerator Ejecutar(ContextoPatron ctx)
    {
        for (int o = 0; o < oleadas; o++)
        {
            float apuntado = ctx.AnguloAlJugador();
            for (int b = 0; b < balasPorOleada; b++)
            {
                float fraccion = balasPorOleada > 1 ? (float)b / (balasPorOleada - 1) : 0.5f;
                float dispersion = (fraccion - 0.5f) * aperturaOleada;
                var opc = opciones.Clonar();
                opc.amplitudOnda = 2f + o * 0.7f;
                opc.frecuenciaOnda = 5f + b * 0.6f;
                ctx.Disparar(ctx.DireccionDesdeGrados(apuntado + dispersion), opc);
            }
            yield return new WaitForSeconds(intervaloEntreOleadas);
        }
    }
}