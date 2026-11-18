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

    // -------- Estado --------
    readonly List<GameObject> pool = new List<GameObject>(); // wrappers
    float timer;

    void Awake()
    {
        ValidarJerarquia();
    }

    void Start()
    {
        if (prefabAsteroide == null)
        {
            Debug.LogError("[AsteroidSpawner] Falta prefabAsteroide. Deshabilitado.", this);
            enabled = false;
            return;
        }

        InicializarPool();
        StartCoroutine(EsperarJugadorYArrancar());
    }

    IEnumerator EsperarJugadorYArrancar()
    {
        while (jugador == null)
        {
            var p = GameObject.FindGameObjectWithTag("Player");
            if (p) jugador = p.transform;
            yield return null;
        }
        Debug.Log("[AsteroidSpawner] Player encontrado. Iniciando spawner.");
    }

    void Update()
    {
        if (jugador == null || prefabAsteroide == null) return;

        timer += Time.deltaTime;
        if (timer < tiempoEntreSpawns) return;

        Spawnear();
        timer = 0f;
    }

    void InicializarPool()
    {
        for (int i = 0; i < tamañoPool; i++)
            pool.Add(CrearWrapperInactivo());
    }

    // Crea el wrapper (hijo del spawner) con el asteroide inactivo dentro.
    // El asteroide es HIJO del wrapper → si tiene Animator con curvas de posición,
    // solo mueve su localPosition, no lo teleporta al origen.
    GameObject CrearWrapperInactivo()
    {
        var wrapper = new GameObject($"AsteroidWrapper_{pool.Count}");
        wrapper.transform.SetParent(transform, false);
        wrapper.SetActive(false);

        var asteroide = Instantiate(prefabAsteroide, wrapper.transform);
        asteroide.transform.localPosition = Vector3.zero;
        asteroide.transform.localRotation = Quaternion.identity;
        asteroide.SetActive(false);

        return wrapper;
    }

    void Spawnear()
    {
        var wrapper = ObtenerDelPool();
        if (wrapper == null) return;

        // Distribución uniforme en el anillo (no concentrada en el borde)
        float ang = Random.Range(0f, Mathf.PI * 2f);
        float rMin = radioMinimo * radioMinimo;
        float rMax = radioMaximo * radioMaximo;
        float r = Mathf.Sqrt(Random.Range(rMin, rMax));

        Vector3 offset = new Vector3(Mathf.Cos(ang) * r, Mathf.Sin(ang) * r, 0f);
        wrapper.transform.position = jugador.position + offset;
        wrapper.transform.rotation = Quaternion.identity;

        // Reactiva el asteroide hijo
        var asteroide = wrapper.transform.childCount > 0 ? wrapper.transform.GetChild(0).gameObject : null;
        if (asteroide != null) asteroide.SetActive(true);
        wrapper.SetActive(true);
    }

    GameObject ObtenerDelPool()
    {
        // Limpia referencias muertas (fake-null de Unity)
        pool.RemoveAll(w => w == null);

        foreach (var w in pool)
            if (w != null && !w.activeInHierarchy)
                return w;

        // Expande dinámicamente si se agotó
        var nuevo = CrearWrapperInactivo();
        pool.Add(nuevo);
        return nuevo;
    }

    // -------- Seguridad / diagnóstico --------

    void ValidarJerarquia()
    {
        var p = transform.parent;
        while (p != null)
        {
            if (p.GetComponentInParent<VidaPersonaje>(true) != null)
            {
                Debug.LogError(
                    $"[AsteroidSpawner] Cuelga de '{p.name}' que tiene VidaPersonaje → " +
                    "se destruirá con el enemigo. Muévelo a la raíz de la escena.", this);
                break;
            }
            p = p.parent;
        }
    }

    void OnDestroy()
    {
        Debug.LogWarning(
            $"[AsteroidSpawner] DESTRUIDO en frame {Time.frameCount}. pool={pool.Count}, padre={(transform.parent ? transform.parent.name : "raíz")}",
            this);
        Debug.LogWarning(System.Environment.StackTrace, this);
    }
}