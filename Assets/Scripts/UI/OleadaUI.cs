using UnityEngine;
using TMPro;

public class OleadaUI : MonoBehaviour
{
    public SpawnerOleadas spawner;
    public TMP_Text texto;

    void Update()
    {
        if (spawner == null || texto == null) return;

        if (spawner.EnFaseJefe)
        {
            texto.text = "¡JEFE!";
            return;
        }

        if (spawner.NumeroOleadaActual == 0)
        {
            texto.text = "Preparando…";
            return;
        }

        texto.text =
            $"Oleada {spawner.NumeroOleadaActual}/{spawner.TotalOleadas}\n" +
            $"Enemigos: {spawner.EnemigosRestantesOleada}";
    }
}