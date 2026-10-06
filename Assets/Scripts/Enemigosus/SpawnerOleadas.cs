using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SpawnerOleadas : MonoBehaviour
{
    [Header("Oleadas (configurables en el Inspector)")]
    public List<EntradaOleada> oleadas = new List<EntradaOleada>();

    [Header("Jefe gay")]
    public GameObject prefabJefe;

    [Header("Referencias")]
    public Transform jugador;

    [Header("Debug")]
    public bool mostrarLogs = true;

    // Estado interno
    private readonly List<GameObject> enemigosVivos = new List<GameObject>();

    void Start()
    {
        if (jugador == null)
        {
            var p = GameObject.FindGameObjectWithTag("Player");
            if (p != null) jugador = p.transform;
        }

        if (jugador == null)
        {
            Debug.LogError("[SpawnerOleadas] No se encontró al jugador.");
            enabled = false;
            return;
        }

        StartCoroutine(BucleNivel());
    }

    IEnumerator BucleNivel()
    {
        for (int i = 0; i < oleadas.Count; i++)
        {
            EntradaOleada oleada = oleadas[i];

            if (oleada == null || oleada.prefabEnemigo == null)
            {
                Debug.LogWarning($"[SpawnerOleadas] Oleada {i} vacía, saltando.");
                continue;
            }

            if (mostrarLogs)
                Debug.Log($"[SpawnerOleadas] Iniciando oleada {i + 1}/{oleadas.Count}");

            if (oleada.retardoAntes > 0f)
                yield return new WaitForSeconds(oleada.retardoAntes);

            yield return StartCoroutine(SpawnearOleada(oleada));

            yield return new WaitUntil(() => enemigosVivos.Count == 0);

            if (mostrarLogs)
                Debug.Log($"[SpawnerOleadas] Oleada {i + 1} completada.");

            if (oleada.retardoDespues > 0f)
                yield return new WaitForSeconds(oleada.retardoDespues);
        }

        if (mostrarLogs)
            Debug.Log("[SpawnerOleadas] Todas las oleadas completadas.");

        if (prefabJefe != null)
        {
            Vector2 pos = CalcularPosicionSpawn(90f, 14f);
            Instantiate(prefabJefe, pos, Quaternion.identity);
            if (mostrarLogs) Debug.Log("[SpawnerOleadas] Jefe spawneado.");
        }
    }

    IEnumerator SpawnearOleada(EntradaOleada oleada)
    {
        Vector2 dir = CalcularDireccionSpawn(oleada);
        Vector2 origen = (Vector2)jugador.position + dir * oleada.distanciaFueraDeCamara;
        Vector2 perp = new Vector2(-dir.y, dir.x);

        for (int i = 0; i < oleada.cantidad; i++)
        {
            Vector2 pos = CalcularPosicionFormacion(i, oleada, origen, dir, perp);

            GameObject enemigo = Instantiate(oleada.prefabEnemigo, pos, Quaternion.identity);
            enemigosVivos.Add(enemigo);

            var detector = enemigo.AddComponent<DetectorMuerteEnemigo>();
            var capturado = enemigo;
            detector.OnMuerte += () => enemigosVivos.Remove(capturado);

            if (oleada.delayEntreEnemigos > 0f)
                yield return new WaitForSeconds(oleada.delayEntreEnemigos);
        }
    }

    Vector2 CalcularDireccionSpawn(EntradaOleada oleada)
    {
        float anguloBase = 90f; // siempre desde arriba
        float angulo = anguloBase + Random.Range(-oleada.dispersionAngular, oleada.dispersionAngular);
        return new Vector2(Mathf.Cos(angulo * Mathf.Deg2Rad), Mathf.Sin(angulo * Mathf.Deg2Rad));
    }

    Vector2 CalcularPosicionSpawn(float anguloGrados, float distancia)
    {
        Vector2 dir = new Vector2(
            Mathf.Cos(anguloGrados * Mathf.Deg2Rad),
            Mathf.Sin(anguloGrados * Mathf.Deg2Rad)
        );
        return (Vector2)jugador.position + dir * distancia;
    }

    Vector2 CalcularPosicionFormacion(int indice, EntradaOleada oleada, Vector2 origen, Vector2 dir, Vector2 perp)
    {
        switch (oleada.formacion)
        {
            case EntradaOleada.Formacion.V:
                if (indice == 0) return origen;
                int fila = (indice + 1) / 2;
                int lado = (indice % 2 == 1) ? -1 : 1;
                return origen - dir * (fila * oleada.profundidadV) + perp * (lado * fila * oleada.separacionLateral);

            case EntradaOleada.Formacion.Linea:
                float offsetL = (indice - (oleada.cantidad - 1) / 2f) * oleada.separacionLateral;
                return origen + perp * offsetL;

            case EntradaOleada.Formacion.Circulo:
                float ang = (360f / oleada.cantidad) * indice;
                Vector2 offsetC = new Vector2(Mathf.Cos(ang * Mathf.Deg2Rad), Mathf.Sin(ang * Mathf.Deg2Rad));
                return origen + offsetC * oleada.separacionLateral;

            case EntradaOleada.Formacion.Aleatoria:
                Vector2 offsetR = Random.insideUnitCircle * oleada.separacionLateral * 2f;
                return origen + offsetR;

            default:
                return origen;
        }
    }

    void OnDrawGizmosSelected()
    {
        if (jugador == null) return;
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(jugador.position, 14f);
    }
}