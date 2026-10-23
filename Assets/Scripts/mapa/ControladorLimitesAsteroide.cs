using UnityEngine;

public class ControladorLimitesAsteroid : MonoBehaviour
{
    [SerializeField] private float distanciaMaximoJugador = 22f;
    private Transform jugadorTransform;

    private void OnEnable()
    {
        // Buscar al jugador cada vez que el asteroide se activa desde el pool
        if (jugadorTransform == null)
        {
            GameObject jugador = GameObject.FindWithTag("Player");
            if (jugador != null)
            {
                jugadorTransform = jugador.transform;
            }
        }
    }

    private void Update()
    {
        // 1. Desactivar si se sale del BORDE DEL MAPA
        if (LimitesMapa.Instancia != null)
        {
            float distanciaAlCentro = Vector3.Distance(transform.position, LimitesMapa.Instancia.CentroMapa);
            if (distanciaAlCentro > LimitesMapa.Instancia.RadioMapa)
            {
                gameObject.SetActive(false); // Vuelve al pool
                return;
            }
        }

        // 2. Desactivar si queda demasiado lejos del jugador
        if (jugadorTransform != null)
        {
            float distanciaAlJugador = Vector3.Distance(transform.position, jugadorTransform.position);
            if (distanciaAlJugador > distanciaMaximoJugador)
            {
                gameObject.SetActive(false); // Vuelve al pool
            }
        }
    }
}