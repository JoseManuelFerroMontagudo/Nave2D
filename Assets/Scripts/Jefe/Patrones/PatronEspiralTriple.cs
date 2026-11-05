using UnityEngine;
using System.Collections;

[CreateAssetMenu(menuName = "Jefe/Patrones/Espiral Triple", fileName = "PatronEspiralTriple")]
public class PatronEspiralTriple : PatronBase
{
    [Header("Configuración")]
    public int pasos = 50;
    public int brazos = 3;
    public float rotacionPorPaso = 13f;
    public float intervalo = 0.025f;

    [Header("Proyectil")]
    public OpcionesProyectil opciones = new OpcionesProyectil
    {
        modo = ModoMovimiento.Recto,
        velocidad = 9.9f,
        tiempoVuelo = 2.5f,
        color = Color.cyan
    };

    public override IEnumerator Ejecutar(ContextoPatron ctx)
    {
        float angulo = 0f;
        for (int i = 0; i < pasos; i++)
        {
            for (int brazo = 0; brazo < brazos; brazo++)
            {
                float a = angulo + brazo * (360f / brazos);
                var opc = opciones.Clonar();
                if (i % 5 == 0) opc.modo = ModoMovimiento.Ondulado;
                ctx.Disparar(ctx.DireccionDesdeGrados(a), opc);
            }
            angulo += rotacionPorPaso;
            yield return new WaitForSeconds(intervalo);
        }
    }
}