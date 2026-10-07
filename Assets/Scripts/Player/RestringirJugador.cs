using UnityEngine;

public class RestringirJugador : MonoBehaviour
{
    private void LateUpdate()
    {
        // Verifica que la instancia del manager de límites exista
        if (LimitesMapa.Instancia == null) return;

        Vector3 centro = LimitesMapa.Instancia.CentroMapa;
        float radioMaximo = LimitesMapa.Instancia.RadioMapa;

        // Calcula el vector de distancia desde el centro del mapa hasta la nave
        Vector3 desfase = transform.position - centro;
        float distanciaActual = desfase.magnitude;

        // Si la nave intenta salir del círculo, la reubicamos en el límite exacto
        if (distanciaActual > radioMaximo)
        {
            Vector3 posicionLimite = centro + desfase.normalized * radioMaximo;
            transform.position = posicionLimite;

            // Frenar el empuje físico del Rigidbody2D en dirección hacia afuera
            Rigidbody2D rb = GetComponent<Rigidbody2D>();
            if (rb != null)
            {
                rb.velocity = Vector2.ClampMagnitude(rb.velocity, 2f);
            }
        }
    }
}