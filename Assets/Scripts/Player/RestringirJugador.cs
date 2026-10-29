using UnityEngine;

public class RestringirJugador : MonoBehaviour
{
    Rigidbody2D rb;
    ShipController nave;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        nave = GetComponent<ShipController>();
    }

    void LateUpdate()
    {
        if (LimitesMapa.Instancia == null) return;

        Vector3 centro = LimitesMapa.Instancia.CentroMapa;
        float radioMaximo = LimitesMapa.Instancia.RadioMapa;

        Vector3 desfase = transform.position - centro;
        if (desfase.magnitude <= radioMaximo) return;

        Vector2 normal = desfase.normalized;

        transform.position = centro + (Vector3)(normal * radioMaximo);

        if (nave != null) nave.Rebotar(normal);
        else if (rb != null)
        {
            Vector2 v = rb.linearVelocity;
            if (Vector2.Dot(v, normal) > 0f)
                rb.linearVelocity = v - 2f * Vector2.Dot(v, normal) * normal;
        }
    }
}