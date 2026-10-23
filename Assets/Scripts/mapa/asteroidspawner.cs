using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AsteroidSpawner : MonoBehaviour
{
    [Header("Prefab de Asteroide")]
    [SerializeField] private GameObject prefabAsteroide;
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
        StartCoroutine(EsperarJugadorYArrancar());
    }

    private IEnumerator EsperarJugadorYArrancar()
    {
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
        if (jugador == null)
            return;

        timer += Time.deltaTime;

        if (timer >= tiempoEntreSpawns)
        {
            Spawnear();
            timer = 0f;
        }
    }

    // MANTENER: Crea la reserva inicial de objetos
    private void InicializarPool()
    {
        for (int i = 0; i < tamañoPool; i++)
        {
            GameObject obj = Instantiate(prefabAsteroide, transform);
            obj.SetActive(false);
            pool.Add(obj);
        }
    }

    private void Spawnear()
    {
        GameObject asteroide = ObtenerDelPool();

        if (asteroide != null)
        {
            Vector3 centro = jugador.position;

            Vector2 puntoRandom =
                Random.insideUnitCircle.normalized *
                Random.Range(radioMinimo, radioMaximo);

            asteroide.transform.position =
                centro + new Vector3(puntoRandom.x, puntoRandom.y, 0f);

            asteroide.SetActive(true);
        }
    }

    // REEMPLAZAR: Busca disponibles y, si no hay, expande el pool dinámicamente
    private GameObject ObtenerDelPool()
    {
        // 1. ELIMINA referencias muertas/destruidas si las hay
        pool.RemoveAll(obj => obj == null);

        // 2. Busca uno desactivado en el pool
        foreach (GameObject obj in pool)
        {
            if (obj != null && !obj.activeInHierarchy)
                return obj;
        }

        // 3. Si se agotaron o no hay disponibles, crea uno nuevo
        GameObject nuevoObj = Instantiate(prefabAsteroide, transform);
        nuevoObj.SetActive(false);
        pool.Add(nuevoObj);

        return nuevoObj;
    }
}