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

    [Header("Física de Impacto (Opcional)")]
    [Tooltip("Pon en 0 para Nivel 1 si no quieres que empuje al jugador")]
    [SerializeField] private float fuerzaEmpuje = 0f;

    private Rigidbody2D rb;
    private float velocidadRotacion;
    private float anguloZ;

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
            direccionBase = (jugadorObj.transform.position - transform.position).normalized;
        }
        else
        {
            direccionBase = Random.insideUnitCircle.normalized;
        }

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
        if (collision.gameObject.CompareTag("Player"))
        {
            AplicarEmpuje(collision.gameObject);
            Explotar();
        }
        else if (collision.gameObject.CompareTag("Asteroid"))
        {
            Explotar();
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            AplicarEmpuje(other.gameObject);
            Explotar();
        }
        else if (other.CompareTag("Asteroid") || other.CompareTag("Shield"))
        {
            Explotar();
        }
    }

    private void AplicarEmpuje(GameObject objetoJugador)
    {
        // Si la fuerza es 0 (como en Nivel 1), no hace nada
        if (fuerzaEmpuje <= 0f) return;

        Rigidbody2D rbJugador = objetoJugador.GetComponent<Rigidbody2D>();
        if (rbJugador != null)
        {
            Vector2 direccionEmpuje = (objetoJugador.transform.position - transform.position).normalized;
            rbJugador.AddForce(direccionEmpuje * fuerzaEmpuje, ForceMode2D.Impulse);
        }
    }

    public void Explotar()
    {
        if (prefabExplosion != null)
        {
            GameObject efecto = Instantiate(prefabExplosion, transform.position, Quaternion.identity);
            Destroy(efecto, 1.5f);
        }

        gameObject.SetActive(false);
    }
}