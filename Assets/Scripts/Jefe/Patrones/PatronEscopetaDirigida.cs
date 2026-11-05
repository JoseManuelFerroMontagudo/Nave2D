using UnityEngine;
using System.Collections;

[CreateAssetMenu(menuName = "Jefe/Patrones/Escopeta Dirigida", fileName = "PatronEscopetaDirigida")]
public class PatronEscopetaDirigida : PatronBase
{
    [Header("Configuración")]
    public int rafagas = 8;
    public int balasPorRafaga = 12;
    public float aperturaRafaga = 50f;
    public float intervaloEntreRafagas = 0.18f;
    public int cadaNCirculoSorpresa = 2;
    public int balasCirculo = 8;

    [Header("Proyectil")]
    public OpcionesProyectil opciones = new OpcionesProyectil
    {
        modo = ModoMovimiento.Recto,
        velocidad = 9f,
        tiempoVuelo = 2.5f,
        color = Color.yellow
    };

    public override IEnumerator Ejecutar(ContextoPatron ctx)
    {
        for (int r = 0; r < rafagas; r++)
        {
            float apuntado = ctx.AnguloAlJugador();
            for (int b = 0; b < balasPorRafaga; b++)
            {
                float fraccion = balasPorRafaga > 1 ? (float)b / (balasPorRafaga - 1) : 0.5f;
                float dispersion = (fraccion - 0.5f) * aperturaRafaga;
                var opc = opciones.Clonar();
                opc.velocidad = opciones.velocidad * Random.Range(1f, 1.4f);
                if (b % 4 == 0) opc.modo = ModoMovimiento.Ondulado;
                ctx.Disparar(ctx.DireccionDesdeGrados(apuntado + dispersion), opc);
            }

            if (cadaNCirculoSorpresa > 0 && r % cadaNCirculoSorpresa == 1)
                ctx.DispararCirculo(balasCirculo, Random.Range(0f, 45f), opciones);

            yield return new WaitForSeconds(intervaloEntreRafagas);
        }
    }
}