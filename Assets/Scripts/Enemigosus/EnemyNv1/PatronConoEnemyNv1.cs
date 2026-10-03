using UnityEngine;
using System.Collections;

[CreateAssetMenu(menuName = "EnemyNv1/Patrones/Cono Abanico", fileName = "PatronConoEnemyNv1")]
public class PatronConoEnemyNv1 : PatronBase
{
    [Header("Configuración del Cono")]
    public float anguloApertura = 15f;

    [Header("Proyectil")]
    public OpcionesProyectil opciones = new OpcionesProyectil
    {
        modo = ModoMovimiento.Recto,
        velocidad = 6f,
        vidaUtil = 4f,
        color = Color.yellow
    };

    public override IEnumerator Ejecutar(ContextoPatron ctx)
    {
        if (ctx.jugador == null) yield break;

        Vector2 dirCentro = (ctx.jugador.position - ctx.puntoDisparo.position).normalized;

        Vector2 dirIzquierda = Quaternion.Euler(0, 0, anguloApertura) * dirCentro;
        Vector2 dirDerecha = Quaternion.Euler(0, 0, -anguloApertura) * dirCentro;

        ctx.Disparar(dirCentro, opciones);
        ctx.Disparar(dirIzquierda, opciones);
        ctx.Disparar(dirDerecha, opciones);

        yield return null;
    }
}
