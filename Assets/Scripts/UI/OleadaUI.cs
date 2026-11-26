using UnityEngine;
using TMPro;

public class OleadaUI : MonoBehaviour
{
    public SpawnerOleadas spawner;
    public TMP_Text texto;

    void Update()
    {
        if (spawner == null || texto == null) return;

        int puntos = spawner.EstadisticasJugador != null
            ? spawner.EstadisticasJugador.puntos
            : 0;

        if (spawner.EnFaseJefe)
        {
            texto.text = $"¡JEFE!\nPuntos: {puntos}";
            return;
        }

        if (spawner.NumeroOleadaActual == 0)
        {
            texto.text = $"Preparando…\nPuntos: {puntos}";
            return;
        }

        texto.text =
            $"Oleada {spawner.NumeroOleadaActual}/{spawner.TotalOleadas}\n" +
            $"Enemigos: {spawner.EnemigosRestantesOleada}\n" +
            $"Puntos: {puntos}";
    }
}