using UnityEngine;

public class Asteroide : MonoBehaviour
{
    [Header("Configuración de Explosión")]
    [SerializeField] private GameObject prefabExplosion;

    [Header("Movimiento")]
    [SerializeField] private float velocidadMin = 2f;
    [SerializeField] private float velocidadMax = 5f;
    [SerializeField] private float rotacionMin = -40f;
    [SerializeField] private float rotacionMax = 40f;

    private Rigidbody2D rb;
    private float velocidadRotacion;
    private float anguloZ;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    private void OnEnable()
    {
        velocidadRotacion = Random.Range(rotacionMin, rotacionMax);

        Vector2 direccionAleatoria = Random.insideUnitCircle.normalized;
        float velocidadAleatoria = Random.Range(velocidadMin, velocidadMax);

        if (rb != null)
        {
            rb.linearVelocity = direccionAleatoria * velocidadAleatoria;
        }
    }

    private void LateUpdate()
    {
        // Billboard perfecto mirando siempre a la cámara
        if (Camera.main != null)
        {
            transform.rotation = Camera.main.transform.rotation;
        }

        // Giro en Z sobre su propio eje
        anguloZ += velocidadRotacion * Time.deltaTime;
        transform.Rotate(0f, 0f, anguloZ, Space.Self);
    }

    // Usamos OnCollisionEnter2D para colisiones físicas reales con masa/empuje
    private void OnCollisionEnter2D(Collision2D collision)
    {
        // 1. Choque con el Jugador
        if (collision.gameObject.CompareTag("Player"))
        {
            // Aquí puedes llamar al script del jugador para restarle vida:
            // collision.gameObject.GetComponent<JugadorSalud>()?.RecibirDaño(10);

            Explotar();
        }
        // 2. Choque entre Asteroides
        else if (collision.gameObject.CompareTag("Asteroide"))
        {
            Explotar();
        }
    }

    // Por si usas Triggers en lugar de Colliders normales:
    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player") || other.CompareTag("Asteroide") || other.CompareTag("Shield"))
        {
            Explotar();
        }
    }

    private void Explotar()
    {
        // Instancia el efecto visual de partículas en la posición del choque
        if (prefabExplosion != null)
        {
            Instantiate(prefabExplosion, transform.position, Quaternion.identity);
        }

        // Se desactiva para volver al Object Pool sin destruir el objeto
        gameObject.SetActive(false);
    }
}