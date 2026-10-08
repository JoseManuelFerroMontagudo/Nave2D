using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AsteroidSpawner : MonoBehaviour
{
    [Header("Pool de Asteroides Interactivos")]
    [SerializeField] private GameObject[] prefabsInteractivos;
    [SerializeField] private int tamañoPool = 25;

    [Header("Referencia y Distancia")]
    [SerializeField] private Transform jugador;
    [SerializeField] private float tiempoEntreSpawns = 2f;
    [SerializeField] private float radioMinimo = 12f;
    [SerializeField] private float radioMaximo = 18f;

    private List<GameObject> pool = new List<GameObject>();
    private float timer;

    private void Start()
    {
        InicializarPool();

        // Esperar hasta encontrar al Player
        StartCoroutine(EsperarJugadorYArrancar());
    }

    private IEnumerator EsperarJugadorYArrancar()
    {
        // Buscar continuamente al objeto con tag "Player"
        while (jugador == null)
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");

            if (player != null)
            {
                jugador = player.transform;
            }

            yield return null;
        }

        Debug.Log("[AsteroidSpawner] Player encontrado. Iniciando spawner.");
    }

    private void Update()
    {
        // Si todavía no encontramos al jugador, no hacemos nada
        if (jugador == null)
            return;

        timer += Time.deltaTime;

        if (timer >= tiempoEntreSpawns)
        {
            Spawnear();
            timer = 0f;
        }
    }

    private void InicializarPool()
    {
        for (int i = 0; i < tamañoPool; i++)
        {
            GameObject prefab = prefabsInteractivos[
                Random.Range(0, prefabsInteractivos.Length)
            ];

            GameObject obj = Instantiate(prefab, transform);

            obj.SetActive(false);
            pool.Add(obj);
        }
    }

    private void Spawnear()
    {
        GameObject asteroide = ObtenerDelPool();

        if (asteroide != null)
        {
            // El centro siempre es la posición ACTUAL del jugador
            Vector3 centro = jugador.position;

            Vector2 puntoRandom =
                Random.insideUnitCircle.normalized *
                Random.Range(radioMinimo, radioMaximo);

            asteroide.transform.position =
                centro + new Vector3(puntoRandom.x, puntoRandom.y, 0f);

            asteroide.SetActive(true);
        }
    }

    private GameObject ObtenerDelPool()
    {
        foreach (GameObject obj in pool)
        {
            if (!obj.activeInHierarchy)
                return obj;
        }

        return null;
    }
}