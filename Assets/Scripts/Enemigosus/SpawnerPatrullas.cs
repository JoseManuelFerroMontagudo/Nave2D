using System.Collections;
using UnityEngine;

public class SpawnerPatrullas : MonoBehaviour
{
    public GameObject prefabEnemigo;
    public int enemigosPorPatrulla = 4;
    public float tiempoEntrePatrullas = 10f;
    public float retardoInicial = 5f;
    public float distanciaFueraDeCamara = 14f;
    public float separacionLateral = 1.5f;
    public float profundidadV = 1.2f;
    public float dispersionAngular = 20f;
    public Transform jugador;

    public static bool SpawningPermitido = true;
    private float cronometro;

    void Start()
    {
        SpawningPermitido = true;
        if (jugador == null)
        {
            var p = GameObject.FindGameObjectWithTag("Player");
            if (p != null) jugador = p.transform;
        }
        cronometro = -retardoInicial;
    }

    void Update()
    {
        if (!SpawningPermitido || !enabled || jugador == null || prefabEnemigo == null) return;
        cronometro += Time.deltaTime;
        if (cronometro >= tiempoEntrePatrullas)
        {
            cronometro = 0f;
            StartCoroutine(SpawnearPatrulla());
        }
    }

    IEnumerator SpawnearPatrulla()
    {
        float anguloBase = 90f;
        float anguloPatrulla = anguloBase + Random.Range(-dispersionAngular, dispersionAngular);

        Vector2 dir = new Vector2(Mathf.Cos(anguloPatrulla * Mathf.Deg2Rad), Mathf.Sin(anguloPatrulla * Mathf.Deg2Rad));
        Vector2 origen = (Vector2)jugador.position + dir * distanciaFueraDeCamara;
        Vector2 perp = new Vector2(-dir.y, dir.x);

        for (int i = 0; i < enemigosPorPatrulla; i++)
        {
            if (!SpawningPermitido) yield break;
            Vector2 pos = CalcularPosicionV(i, enemigosPorPatrulla, origen, dir, perp);
            Instantiate(prefabEnemigo, pos, Quaternion.identity);
            yield return new WaitForSeconds(0.15f);
        }
    }

    Vector2 CalcularPosicionV(int indice, int total, Vector2 origen, Vector2 dir, Vector2 perp)
    {
        int fila = (indice + 1) / 2;
        int lado = (indice % 2 == 1) ? -1 : 1;
        if (indice == 0) return origen;

        float lateral = lado * fila * separacionLateral;
        float atras = fila * profundidadV;
        return origen - dir * atras + perp * lateral;
    }
}