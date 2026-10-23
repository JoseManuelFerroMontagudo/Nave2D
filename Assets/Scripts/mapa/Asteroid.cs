using UnityEngine;

public class Asteroide : MonoBehaviour
{
    [Header("Variedad Visual")]
    [SerializeField] private Sprite[] variantesDeSprites;
    private SpriteRenderer spriteRenderer;

    [Header("Configuración de Explosión")]
    [SerializeField] private GameObject prefabExplosion;

    [Header("Movimiento")]
    [SerializeField] private float velocidadMin = 3f;
    [SerializeField] private float velocidadMax = 7f;
    [SerializeField] private float rotacionMin = -40f;
    [SerializeField] private float rotacionMax = 40f;

    [Header("Dirigido al Jugador")]
    [Tooltip("Ángulo de desviación en grados para que no vayan exacto al centro del jugador")]
    [SerializeField] private float desviacionAngulo = 35f;

    private Rigidbody2D rb;
    private float velocidadRotacion;
    private float anguloZ;
    [SerializeField] private float distanciaMaximoJugador = 22f;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        spriteRenderer = GetComponent<SpriteRenderer>();
    }

    private void OnEnable()
    {
        AsignarSpriteAleatorio();

        velocidadRotacion = Random.Range(rotacionMin, rotacionMax);

        // Apuntar hacia el jugador con cierto margen de error
        CalcularDireccionHaciaJugador();
    }

    private void AsignarSpriteAleatorio()
    {
        if (spriteRenderer != null && variantesDeSprites != null && variantesDeSprites.Length > 0)
        {
            int indice = Random.Range(0, variantesDeSprites.Length);
            spriteRenderer.sprite = variantesDeSprites[indice];
        }
    }

    private void CalcularDireccionHaciaJugador()
    {
        GameObject jugadorObj = GameObject.FindGameObjectWithTag("Player");

        Vector2 direccionBase;

        if (jugadorObj != null)
        {
            // Vector desde la posición del asteroide hacia la posición del jugador
            direccionBase = (jugadorObj.transform.position - transform.position).normalized;
        }
        else
        {
            // Si no encuentra al jugador, cae en una dirección aleatoria por defecto
            direccionBase = Random.insideUnitCircle.normalized;
        }

        // Añadir una pequeña desviación aleatoria (en grados) para que no parezca un misil teledirigido
        float anguloDesviacion = Random.Range(-desviacionAngulo, desviacionAngulo);
        Vector2 direccionFinal = Quaternion.Euler(0, 0, anguloDesviacion) * direccionBase;

        float velocidadAleatoria = Random.Range(velocidadMin, velocidadMax);

        if (rb != null)
        {
            rb.linearVelocity = direccionFinal * velocidadAleatoria;
        }
    }

    private void LateUpdate()
    {
        if (Camera.main != null)
        {
            transform.rotation = Camera.main.transform.rotation;
        }

        anguloZ += velocidadRotacion * Time.deltaTime;
        transform.Rotate(0f, 0f, anguloZ, Space.Self);
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Player") || collision.gameObject.CompareTag("Asteroide"))
        {
            Explotar();
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player") || other.CompareTag("Asteroide") || other.CompareTag("Shield"))
        {
            Explotar();
        }
    }

    private void Explotar()
    {
        if (prefabExplosion != null)
        {
            Instantiate(prefabExplosion, transform.position, Quaternion.identity);
        }

        gameObject.SetActive(false);
    }
}