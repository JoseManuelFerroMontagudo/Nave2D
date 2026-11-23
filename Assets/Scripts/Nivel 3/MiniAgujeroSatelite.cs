using UnityEngine;

public class MiniAgujeroSatelite : MonoBehaviour
{
    private Transform centroAnomalia;
    private Transform jugador;
    private float anguloOffset;
    private float radioOrbita;

    public void Inicializar(Transform centro, Transform objetivo, float offsetGrados, float distanciaOrbita)
    {
        centroAnomalia = centro;
        jugador = objetivo;
        anguloOffset = offsetGrados;
        radioOrbita = distanciaOrbita;
    }

    private void Update()
    {
        if (centroAnomalia == null || jugador == null) return;

        // Calcula el ángulo actual desde el Agujero Negro hacia el jugador
        Vector2 dirAlJugador = (jugador.position - centroAnomalia.position).normalized;
        float anguloBase = Mathf.Atan2(dirAlJugador.y, dirAlJugador.x) * Mathf.Rad2Deg;

        // Coloca este mini-agujero a un costado (+45° o -45°) manteniendo la formación rígida
        float anguloFinal = (anguloBase + anguloOffset) * Mathf.Deg2Rad;
        Vector3 offsetPos = new Vector3(Mathf.Cos(anguloFinal), Mathf.Sin(anguloFinal), 0f) * radioOrbita;

        transform.position = centroAnomalia.position + offsetPos;
        transform.Rotate(0f, 0f, 180f * Time.deltaTime); // Giro visual propio
    }
}