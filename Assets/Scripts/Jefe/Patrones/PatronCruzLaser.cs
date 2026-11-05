using UnityEngine;
using System.Collections;

[CreateAssetMenu(menuName = "Jefe/Patrones/Cruz Láser", fileName = "PatronCruzLaser")]
public class PatronCruzLaser : PatronBase
{
    [Header("Configuración")]
    public int pasos = 10;
    public int brazos = 4;
    public int grosorPorBrazo = 3;
    public float separacionGrosor = 4f;
    public float rotacionPorPaso = 36f;
    public float intervalo = 0.04f;

    [Header("Proyectil")]
    public OpcionesProyectil opciones = new OpcionesProyectil
    {
        modo = ModoMovimiento.Recto,
        velocidad = 11.7f,
        tiempoVuelo = 2.5f,
        color = Color.red
    };

    public override IEnumerator Ejecutar(ContextoPatron ctx)
    {
        float angulo = 0f;
        for (int paso = 0; paso < pasos; paso++)
        {
            for (int brazo = 0; brazo < brazos; brazo++)
            {
                float baseBrazo = angulo + brazo * (360f / brazos);
                for (int t = -(grosorPorBrazo / 2); t <= (grosorPorBrazo / 2); t++)
                    ctx.Disparar(ctx.DireccionDesdeGrados(baseBrazo + t * separacionGrosor), opciones);
            }
            angulo += rotacionPorPaso;
            yield return new WaitForSeconds(intervalo);
        }
    }
}