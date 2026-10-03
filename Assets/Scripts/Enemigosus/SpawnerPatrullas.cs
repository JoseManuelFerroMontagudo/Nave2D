using System.Collections;
using UnityEngine;

public class SpawnerPatrullas : MonoBehaviour
{
    [Header("Prefab y cantidad")]
    public GameObject prefabEnemigo;
    public int enemigosPorPatrulla = 4;

    [Header("Tiempos")]
    public float tiempoEntrePatrullas = 10f;
    public float retardoInicial = 5f;

    [Header("Distancia de spawn")]
    public float distanciaFueraDeCamara = 14f;

    [Header("Formación en V")]
    public float separacionLateral = 1.5f;
    public float profundidadV = 1.2f;   // cuánto retroceden los de atrás

    [Header("Dirección del spawn")]
    [Tooltip("Ángulo aleatorio de dispersión (grados) alrededor de la dirección de aparición")]
    public float dispersionAngular = 20f;

    [Header("Referencias")]
    public Transform jugador;

    private float cronometro;

    void Start()
    {
        if (jugador == null)
        {
            var p = GameObject.FindGameObjectWithTag("Player");
            if (p != null) jugador = p.transform;
        }

        cronometro = -retardoInicial;
    }

    void Update()
    {
        if (jugador == null || prefabEnemigo == null) return;

        cronometro += Time.deltaTime;
        if (cronometro >= tiempoEntrePatrullas)
        {
            cronometro = 0f;
            StartCoroutine(SpawnearPatrulla());
        }
    }

    IEnumerator SpawnearPatrulla()
    {
        // 1) Dirección base: como el jugador mira hacia ARRIBA,
        //    la patrulla aparece desde ARRIBA del jugador.
        //    Y como el jugador no rota, siempre será arriba (con dispersión).
        float anguloBase = 90f; // 90° = arriba
        float anguloPatrulla = anguloBase + Random.Range(-dispersionAngular, dispersionAngular);

        Vector2 dir = new Vector2(
            Mathf.Cos(anguloPatrulla * Mathf.Deg2Rad),
            Mathf.Sin(anguloPatrulla * Mathf.Deg2Rad)
        );

        // 2) Punto de origen (fuera de cámara, arriba del jugador)
        Vector2 origen = (Vector2)jugador.position + dir * distanciaFueraDeCamara;

        // 3) Vector perpendicular para repartir en V
        Vector2 perp = new Vector2(-dir.y, dir.x);

        for (int i = 0; i < enemigosPorPatrulla; i++)
        {
            Vector2 pos = CalcularPosicionV(i, enemigosPorPatrulla, origen, dir, perp);

            Instantiate(prefabEnemigo, pos, Quaternion.identity);
            yield return new WaitForSeconds(0.15f);
        }
    }

    /// <summary>
    /// Coloca los enemigos en V apuntando hacia el jugador.
    /// El primero va al frente (punta de la V), los demás se abren hacia atrás.
    /// </summary>
    Vector2 CalcularPosicionV(int indice, int total, Vector2 origen, Vector2 dir, Vector2 perp)
    {
        // El enemigo 0 es la punta de la V (más cerca del jugador).
        // Los demás van detrás, alternando izquierda/derecha.
        int fila = (indice + 1) / 2;          // 0, 1, 1, 2, 2, 3, 3...
        int lado = (indice % 2 == 1) ? -1 : 1; // -1 izquierda, +1 derecha (el 0 da 1 pero fila=0)

        // Si indice == 0 → punta, sin desplazamiento lateral ni profundidad
        if (indice == 0)
            return origen;

        // Los demás: retroceden en -dir y se abren en perp
        float lateral = lado * fila * separacionLateral;
        float atras = fila * profundidadV;

        return origen - dir * atras + perp * lateral;
    }
}