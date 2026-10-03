using UnityEngine;
using System.Collections;

[CreateAssetMenu(menuName = "EnemyNv1/Patrones/Ráfaga Directa", fileName = "PatronRafagaEnemyNv1")]
public class PatronRafagaEnemyNv1 : PatronBase
{
    [Header("Configuración de Ráfaga")]
    public int cantidadBalas = 3;
    public float intervaloEntreBalas = 0.18f;

    [Header("Proyectil")]
    public OpcionesProyectil opciones = new OpcionesProyectil
    {
        modo = ModoMovimiento.Recto,
        velocidad = 6f,
        vidaUtil = 4f,
        color = Color.cyan
    };

    public override IEnumerator Ejecutar(ContextoPatron ctx)
    {
        if (ctx.jugador == null) yield break;

        for (int i = 0; i < cantidadBalas; i++)
        {
            Vector2 direccion = (ctx.jugador.position - ctx.puntoDisparo.position).normalized;
            ctx.Disparar(direccion, opciones);
            yield return new WaitForSeconds(intervaloEntreBalas);
        }
    }
}
