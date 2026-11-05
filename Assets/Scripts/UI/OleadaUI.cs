using UnityEngine;
using TMPro;

public class OleadaUI : MonoBehaviour
{
    public SpawnerOleadas spawner;
    public TMP_Text texto;
    public int NivelActual;

    void Update()
    {
        if (spawner == null || texto == null) return;

        if (spawner.EnFaseJefe)
        {
            texto.text = $"Nivel: {NivelActual}\n" + "JEFE!";
            return;
        }

        if (spawner.NumeroOleadaActual == 0)
        {
            texto.text = $"Nivel: {NivelActual}\n" + "Preparando…";
            return;
        }

        texto.text =
            $"Nivel: {NivelActual}\n" +
            $"Oleada {spawner.NumeroOleadaActual}/{spawner.TotalOleadas}\n" +
            $"Enemigos: {spawner.EnemigosRestantesOleada}";
    }
}