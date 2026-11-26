using UnityEngine;
using UnityEngine.Rendering.Universal; // Para Light2D

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(Collider2D))]
public class ItemRecogible : MonoBehaviour
{
    [Header("Billboard")]
    [Tooltip("Hijo con el SpriteRenderer. Si se deja vacío se busca el primer hijo con SpriteRenderer.")]
    public Transform spriteHijo;

    [Header("Vida")]
    [Tooltip("Se destruye solo después de este tiempo (0 = no).")]
    public float tiempoVida = 15f;

    [Header("Brillo pulsante (Light 2D)")]
    [Tooltip("Light2D hijo que pulsará. Si se deja vacío se busca uno en los hijos.")]
    public Light2D spriteLight;

    [Tooltip("Intensidad mínima del pulso.")]
    public float intensidadMinima = 0.3f;

    [Tooltip("Intensidad máxima del pulso.")]
    public float intensidadMaxima = 1.5f;

    [Tooltip("Velocidad del pulso (ciclos por segundo).")]
    [Range(0.1f, 10f)] public float velocidadPulso = 1.5f;

    [Header("Atracción al jugador")]
    [Tooltip("Activa o desactiva el acercamiento al jugador.")]
    public bool atraerAlJugador = true;

    [Tooltip("Distancia a la que empieza a acercarse al jugador.")]
    public float distanciaAtraccion = 5f;

    [Tooltip("Velocidad con la que se acerca al jugador.")]
    public float velocidadAtraccion = 4f;

    [Tooltip("Tag del objeto al que se acerca.")]
    public string tagObjetivo = "Player";

    [Header("Arma almacenada")]
    [Tooltip("Arma que otorga este item al recogerse.")]
    public AtaqueBase armaAlmacenada;

    [Tooltip("Puntos que otorga si el arma ya estaba desbloqueada.")]
    public int puntosSiDuplicada = 50;

    [Header("Victoria (opcional)")]
    [Tooltip("Prefab del menú de victoria. Si se asigna, se instancia al recoger este item.")]
    public GameObject prefabMenuVictoria;

    [Tooltip("Si es true, al recoger este item se muestra el menú de victoria.")]
    public bool esItemDeVictoria = false;

    // Flag para evitar doble recogida
    [HideInInspector] public bool recogido = false;

    Rigidbody2D rb;
    Camera cam;
    Transform objetivo;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        rb.freezeRotation = true;
        cam = Camera.main;

        if (spriteHijo == null)
        {
            var sr = GetComponentInChildren<SpriteRenderer>();
            if (sr != null) spriteHijo = sr.transform;
        }

        if (spriteLight == null)
            spriteLight = GetComponentInChildren<Light2D>();

        if (tiempoVida > 0f) Destroy(gameObject, tiempoVida);

        BuscarObjetivo();
    }

    void BuscarObjetivo()
    {
        if (string.IsNullOrEmpty(tagObjetivo)) return;
        var go = GameObject.FindGameObjectWithTag(tagObjetivo);
        if (go != null) objetivo = go.transform;
    }

    void LateUpdate()
    {
        if (spriteHijo != null && cam != null)
            spriteHijo.rotation = cam.transform.rotation;

        ActualizarPulso();
    }

    void FixedUpdate()
    {
        if (!atraerAlJugador) return;

        // Si no hay objetivo (o fue destruido), se vuelve a buscar.
        if (objetivo == null)
        {
            BuscarObjetivo();
            return;
        }

        Vector2 pos = rb.position;
        Vector2 objetivoPos = objetivo.position;
        float dist = Vector2.Distance(pos, objetivoPos);

        // Se acerca si está dentro del rango y aún no está encima.
        if (dist <= distanciaAtraccion && dist > 0.05f)
        {
            Vector2 dir = (objetivoPos - pos).normalized;
            rb.linearVelocity = dir * velocidadAtraccion;
        }
    }

    void ActualizarPulso()
    {
        if (spriteLight == null) return;

        float t = Mathf.PingPong(Time.time * velocidadPulso, 1f);
        t = Mathf.SmoothStep(0f, 1f, t); // suaviza el vaivén
        spriteLight.intensity = Mathf.Lerp(intensidadMinima, intensidadMaxima, t);
    }

    public void Rebotar(Vector2 normalHaciaFuera)
    {
        Vector2 v = rb.linearVelocity;
        if (Vector2.Dot(v, normalHaciaFuera) <= 0f) return;

        v -= 2f * Vector2.Dot(v, normalHaciaFuera) * normalHaciaFuera;
        rb.linearVelocity = v;
    }

    /// <summary>
    /// Intenta marcar el item como recogido. Devuelve false si ya lo estaba.
    /// </summary>
    public bool IntentarRecoger()
    {
        if (recogido) return false;
        recogido = true;
        return true;
    }
}