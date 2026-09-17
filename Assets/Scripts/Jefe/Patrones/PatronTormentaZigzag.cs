using UnityEngine;
using System.Collections;

[CreateAssetMenu(menuName = "Jefe/Patrones/Tormenta Zigzag", fileName = "PatronTormentaZigzag")]
public class PatronTormentaZigzag : PatronBase
{
    [Header("Oleadas")]
    public int oleadas = 10;
    public int balasPorOleada = 10;
    public float intervaloEntreOleadas = 0.07f;

    [Header("Ángulo del zigzag (dispersión)")]
    [Tooltip("Separación angular entre balas consecutivas de una misma oleada (grados).")]
    public float separacionEntreBalas = 7f;

    [Tooltip("Ángulo de inversión del zigzag entre oleadas (grados). Alterna signo cada oleada.")]
    public float anguloZigzagPorOleada = 35f;

    [Tooltip("Dispersión de la alternancia dentro de la oleada (grados). 0 = todas las balas usan el mismo desvío.")]
    public float anguloZigzagInterno = 0f;

    [Header("Amplitud del zigzag (trayectoria)")]
    [Tooltip("Amplitud lateral real de la onda de cada proyectil.")]
    public float amplitudZigzag = 2f;

    [Tooltip("Frecuencia de la onda de cada proyectil.")]
    public float frecuenciaZigzag = 8f;

    [Header("Progresión")]
    [Tooltip("Incremento de velocidad por oleada (0.1 = +10% por oleada).")]
    public float aceleracionPorOleada = 0.1f;

    [Header("Proyectil")]
    public OpcionesProyectil opciones = new OpcionesProyectil
    {
        modo = ModoMovimiento.Ondulado,
        velocidad = 9f,
        tiempoVuelo = 2.5f,
        color = new Color(1f, 0.5f, 0f)
    };

    public override IEnumerator Ejecutar(ContextoPatron ctx)
    {
        for (int o = 0; o < oleadas; o++)
        {
            float apuntado = ctx.AnguloAlJugador();
            float dirZig = (o % 2 == 0) ? anguloZigzagPorOleada : -anguloZigzagPorOleada;

            for (int b = 0; b < balasPorOleada; b++)
            {
                // Inversión interna opcional (independiente de la inversión por oleada)
                float zigInterno = (b % 2 == 0) ? anguloZigzagInterno : -anguloZigzagInterno;
                float a = apuntado + dirZig + zigInterno + b * separacionEntreBalas;

                var opc = opciones.Clonar();
                opc.modo = ModoMovimiento.Ondulado;
                opc.velocidad = opciones.velocidad * (1f + o * aceleracionPorOleada);
                opc.amplitudOnda = amplitudZigzag;
                opc.frecuenciaOnda = frecuenciaZigzag;

                ctx.Disparar(ctx.DireccionDesdeGrados(a), opc);
            }

            yield return new WaitForSeconds(intervaloEntreOleadas);
        }
    }
}