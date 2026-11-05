using UnityEngine;

public class SpawnerAnomalia : MonoBehaviour
{
    [Header("Prefab del Asteroide")]
    public GameObject prefabAsteroide;

    [Header("Configuración de Distancia y Frecuencia")]
    public float radioSpawn = 18f;        // Distancia desde el agujero donde nacen
    public float intervaloSpawn = 0.5f;   // Tiempo entre cada asteroide
    public float velocidadSuccion = 9f;   // Velocidad hacia el centro

    [Header("Configuración del Abanico (Dirección al Jugador)")]
    [Tooltip("Ángulo del cono de emisión enfocado hacia la nave (ej. 90° = abanico frontal)")]
    public float anguloAbanico = 90f;

    private float timer;
    private bool generando = true;
    private Transform jugador;

    private void Start()
    {
        BuscarJugador();
    }

    private void BuscarJugador()
    {
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
        {
            Rigidbody2D rb = playerObj.GetComponentInChildren<Rigidbody2D>();
            jugador = (rb != null) ? rb.transform : playerObj.transform;
        }
    }

    private void Update()
    {
        if (!generando || prefabAsteroide == null) return;

        // Reintentar encontrar al jugador si no lo detectó en el Start
        if (jugador == null)
        {
            BuscarJugador();
            return;
        }

        timer += Time.deltaTime;
        if (timer >= intervaloSpawn)
        {
            timer = 0f;
            SpawnearAsteroideEnAbanico();
        }
    }

    private void SpawnearAsteroideEnAbanico()
    {
        // 1. Obtener la dirección desde la Anomalía hacia el Jugador
        Vector2 direccionHaciaJugador = (jugador.position - transform.position).normalized;

        // 2. Si la nave está exactamente sobre la anomalía, usa una dirección por defecto
        if (direccionHaciaJugador == Vector2.zero) direccionHaciaJugador = Vector2.right;

        // 3. Obtener el ángulo base hacia el jugador
        float anguloBase = Mathf.Atan2(direccionHaciaJugador.y, direccionHaciaJugador.x) * Mathf.Rad2Deg;

        // 4. Aplicar una variación aleatoria dentro del abanico (ej. entre -45° y +45°)
        float anguloAleatorio = Random.Range(-anguloAbanico / 2f, anguloAbanico / 2f);
        float anguloFinal = anguloBase + anguloAleatorio;

        // 5. Convertir el ángulo final en una posición Vector3
        Vector3 direccionSpawn = Quaternion.Euler(0, 0, anguloFinal) * Vector3.right;
        Vector3 puntoSpawn = transform.position + (direccionSpawn * radioSpawn);

        // 6. Instanciar el asteroide en el borde del abanico
        GameObject nuevoAsteroide = Instantiate(prefabAsteroide, puntoSpawn, Quaternion.identity);

        // 7. Aplicar el script de succión que lo arrastra de vuelta al centro
        AtraccionAnomalia atraccion = nuevoAsteroide.GetComponent<AtraccionAnomalia>();
        if (atraccion == null)
        {
            atraccion = nuevoAsteroide.AddComponent<AtraccionAnomalia>();
        }

        atraccion.IniciarSuccion(transform, velocidadSuccion);
    }

    public void DetenerGeneracion()
    {
        generando = false;
    }

    // Dibujar el cono en la ventana Scene para calibrarlo visualmente
    private void OnDrawGizmosSelected()
    {
        if (jugador == null) return;

        Gizmos.color = Color.yellow;
        Vector3 dirJugador = (jugador.position - transform.position).normalized;

        Vector3 izquierda = Quaternion.Euler(0, 0, -anguloAbanico / 2f) * dirJugador;
        Vector3 derecha = Quaternion.Euler(0, 0, anguloAbanico / 2f) * dirJugador;

        Gizmos.DrawLine(transform.position, transform.position + izquierda * radioSpawn);
        Gizmos.DrawLine(transform.position, transform.position + derecha * radioSpawn);
    }
}