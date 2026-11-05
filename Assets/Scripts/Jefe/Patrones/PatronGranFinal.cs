using UnityEngine;
using System.Collections;

[CreateAssetMenu(menuName = "Jefe/Patrones/Gran Final", fileName = "PatronGranFinal")]
public class PatronGranFinal : PatronBase
{
    [Header("Fase 1: Espiral")]
    public int pasosEspiral = 30;
    public float rotacionEspiral = 17f;
    public float intervaloEspiral = 0.02f;

    [Header("Fase 2: Escopeta")]
    public int rafagasEscopeta = 3;
    public int balasEscopeta = 14;
    public float aperturaEscopeta = 55f;
    public float intervaloEscopeta = 0.1f;

    [Header("Fase 3: Lluvia caótica")]
    public int balasCaos = 25;
    public float intervaloCaos = 0.02f;

    [Header("Velocidad base")]
    public float velocidadBase = 9f;
    public float tiempoVuelo = 2.5f;

    [Header("Colores")]
    public Color colorEspiral = Color.cyan;
    public Color colorEscopeta = Color.yellow;
    public Color colorCaos = new Color(1f, 0.5f, 0f);
    public Color colorLaser = Color.red;
    public Color colorPetalo = Color.magenta;
    public Color colorOnda = Color.green;

    public override IEnumerator Ejecutar(ContextoPatron ctx)
    {
        // Fase 1: Espiral triple rápida
        float angulo = 0f;
        for (int i = 0; i < pasosEspiral; i++)
        {
            for (int brazo = 0; brazo < 3; brazo++)
                ctx.Disparar(ctx.DireccionDesdeGrados(angulo + brazo * 120f), Recto(colorEspiral, 1.2f));
            angulo += rotacionEspiral;
            yield return new WaitForSeconds(intervaloEspiral);
        }

        // Fase 2: Triple escopeta
        for (int r = 0; r < rafagasEscopeta; r++)
        {
            float apuntado = ctx.AnguloAlJugador();
            for (int b = 0; b < balasEscopeta; b++)
            {
                float dispersion = ((float)b / (balasEscopeta - 1) - 0.5f) * aperturaEscopeta;
                ctx.Disparar(ctx.DireccionDesdeGrados(apuntado + dispersion), Recto(colorEscopeta, 1.3f));
            }
            yield return new WaitForSeconds(intervaloEscopeta);
        }

        // Fase 3: Lluvia caótica
        for (int i = 0; i < balasCaos; i++)
        {
            var opc = Recto(colorCaos, Random.Range(0.8f, 1.3f));
            if (i % 3 == 0) opc.modo = ModoMovimiento.Ondulado;
            ctx.Disparar(ctx.DireccionDesdeGrados(Random.Range(0f, 360f)), opc);
            yield return new WaitForSeconds(intervaloCaos);
        }

        // Fase 4: Mega burst
        ctx.DispararCirculo(16, 0f, Recto(colorLaser, 1.1f));
        var ondaPetalo = Recto(colorPetalo, 0.9f); ondaPetalo.modo = ModoMovimiento.Ondulado;
        ctx.DispararCirculo(16, 11f, ondaPetalo);
        yield return new WaitForSeconds(0.1f);
        ctx.DispararCirculo(14, 7f, Recto(colorOnda, 1.2f));
    }

    OpcionesProyectil Recto(Color color, float mult) => new OpcionesProyectil
    {
        modo = ModoMovimiento.Recto,
        velocidad = velocidadBase * mult,
        tiempoVuelo = tiempoVuelo,
        color = color
    };
}