using UnityEngine;

public class Asteroide : MonoBehaviour
{
    public GameObject prefabExplosion;

    public float velocidadMin = 2f;
    public float velocidadMax = 5f;
    [SerializeField] private float rotacionMin = -40f;
    [SerializeField] private float rotacionMax = 40f;

    private Rigidbody2D rb;
    private SpriteRenderer spriteRenderer;
    private float velocidadRotacion;
    private float anguloZ;

    // Color rojo brillante de peligro para diferenciar los interactivos
    private Color colorPeligro = new Color(1f, 0.25f, 0.25f, 1f);

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        spriteRenderer = GetComponent<SpriteRenderer>();
    }

    private void OnEnable()
    {
        velocidadRotacion = Random.Range(rotacionMin, rotacionMax);

        // ✅ AHORA SÍ USA LAS VARIABLES DEL INSPECTOR:
        float velocidadAleatoria = Random.Range(velocidadMin, velocidadMax);
        Vector2 direccionAleatoria = Random.insideUnitCircle.normalized;

        if (rb != null)
        {
            rb.linearVelocity = direccionAleatoria * velocidadAleatoria;
        }

        if (spriteRenderer != null)
        {
            spriteRenderer.color = colorPeligro;
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

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Player") || collision.gameObject.CompareTag("Asteroide"))
        {
            Explotar();
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player") || other.CompareTag("Asteroide"))
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