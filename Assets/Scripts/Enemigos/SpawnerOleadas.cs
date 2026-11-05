using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;

public class SpawnerOleadas : MonoBehaviour
{
    [System.Serializable]
    public class GrupoEnemigos
    {
        public GameObject prefab;
        public int cantidad = 1;
    }

    [System.Serializable]
    public class EntradaOleada
    {
        public List<GrupoEnemigos> grupos = new List<GrupoEnemigos>();
        public float retardoAntes = 1f;
        public float retardoDespues = 1.5f;
        public float delayEntreEnemigos = 0.15f;
    }

    [Header("Límite del mapa (LineRenderer)")]
    public LineRenderer limiteMapa;

    [Header("Oleadas")]
    public List<EntradaOleada> oleadas = new List<EntradaOleada>();

    [Header("Jefe (spawnea en el centro al terminar)")]
    public GameObject prefabJefe;

    [Header("Al derrotar al jefe")]
    [Tooltip("Escena a cargar. Vacío = no cambia de escena, solo dispara el evento.")]
    public string escenaAlDerrotarJefe;
    [Tooltip("Segundos antes del cambio (para la animación de muerte).")]
    public float retardoCambioEscena = 1.5f;

    [Tooltip("TRUE = LoadSingle (reemplaza la escena actual, ideal para pantalla de victoria).\n" +
             "FALSE = LoadLevel (carga aditiva, descarga el nivel actual).")]
    public bool usarLoadSingle = true;

    [Tooltip("Se dispara al morir el jefe, antes del cambio de escena.")]
    public UnityEvent OnJefeDerrotado;

    [Header("Spawn de enemigos")]
    [Tooltip("Distancia mínima al jugador al aparecer")]
    public float distanciaMinimaJugador = 14f;
    [Tooltip("Separación mínima entre enemigos ya presentes")]
    public float separacionMinimaEnemigos = 10f;
    [Tooltip("Distancia mínima al borde exterior del mapa")]
    public float distanciaMinimaBorde = 14f;
    [Tooltip("Intentos máximos para encontrar un punto válido")]
    public int intentosMaximos = 40;

    // -------- Estado público para UI (Canvas) --------
    public int NumeroOleadaActual { get; private set; }
    public int TotalOleadas => oleadas.Count;
    public int EnemigosVivos => vivos.Count;
    public int EnemigosRestantesOleada => vivos.Count + pendientesSpawn;
    public bool EnFaseJefe { get; private set; }
    public bool JefeVivo => jefeActual != null;

    int pendientesSpawn;
    readonly List<GameObject> vivos = new List<GameObject>();  // wrappers, no enemigos
    readonly List<Vector3> puntos = new List<Vector3>();
    Transform jugador;
    GameObject jefeActual;
    Vector3 centro;
    Rect bounds;
    float minJugSqr, minEnemSqr;

    IEnumerator Start()
    {
        // Espera a que otros scripts rellenen el LineRenderer
        yield return null;

        if (limiteMapa != null)
            for (int i = 0; i < limiteMapa.positionCount; i++)
            {
                var p = limiteMapa.GetPosition(i);
                puntos.Add(limiteMapa.useWorldSpace ? p : limiteMapa.transform.TransformPoint(p));
            }

        centro = Centro();
        bounds = BoundsDePuntos();
        minJugSqr = distanciaMinimaJugador * distanciaMinimaJugador;
        minEnemSqr = separacionMinimaEnemigos * separacionMinimaEnemigos;
        StartCoroutine(BucleNivel());
    }

    IEnumerator BucleNivel()
    {
        while (jugador == null)
        {
            var p = GameObject.FindGameObjectWithTag("Player");
            if (p) jugador = p.transform;
            yield return null;
        }

        for (int i = 0; i < oleadas.Count; i++)
        {
            NumeroOleadaActual = i + 1;
            var o = oleadas[i];
            if (o.retardoAntes > 0) yield return new WaitForSeconds(o.retardoAntes);
            yield return SpawnearOleada(o);
            yield return new WaitUntil(() => vivos.Count == 0);
            if (o.retardoDespues > 0) yield return new WaitForSeconds(o.retardoDespues);
        }

        SpawnearJefe();
    }

    IEnumerator SpawnearOleada(EntradaOleada o)
    {
        pendientesSpawn = 0;
        foreach (var g in o.grupos) if (g.prefab != null) pendientesSpawn += g.cantidad;

        var espera = o.delayEntreEnemigos > 0 ? new WaitForSeconds(o.delayEntreEnemigos) : null;
        foreach (var g in o.grupos)
        {
            if (g.prefab == null) continue;
            for (int k = 0; k < g.cantidad; k++)
            {
                SpawnUno(g.prefab);
                if (espera != null) yield return espera;
            }
        }
    }

    // Wrapper a destino + enemigo hijo con localPos = 0:
    // así el Animator (con curvas de posición) solo mueve la localPosition
    // del hijo, y el wrapper conserva la posición de spawn.
    void SpawnUno(GameObject prefab)
    {
        Vector3 destino = PuntoValidoParaSpawn();

        var wrapper = new GameObject($"EnemyWrapper_{prefab.name}").transform;
        wrapper.position = destino;
        wrapper.gameObject.AddComponent<EnemyTracker>().Init(this);
        vivos.Add(wrapper.gameObject);
        pendientesSpawn--;

        var e = Instantiate(prefab, wrapper);
        e.transform.localPosition = Vector3.zero;
        e.transform.localRotation = Quaternion.identity;

        var vida = e.GetComponent<VidaPersonaje>();
        if (vida != null)
            vida.OnMuere.AddListener(() => { if (wrapper != null) Destroy(wrapper.gameObject); });
        else
        {
            Destroy(wrapper.gameObject);
            return;
        }

        var enem = e.GetComponent<Enemigo>();
        if (enem != null) { enem.puntoAncla = destino; enem.enPosicion = true; }
    }

    void SpawnearJefe()
    {
        if (prefabJefe == null) { Debug.LogError("[SpawnerOleadas] Sin prefabJefe."); return; }
        EnFaseJefe = true;

        var wrapper = new GameObject("BossWrapper").transform;
        wrapper.position = centro;
        wrapper.gameObject.AddComponent<EnemyTracker>().Init(this);
        vivos.Add(wrapper.gameObject);

        jefeActual = Instantiate(prefabJefe, wrapper);
        jefeActual.transform.localPosition = Vector3.zero;
        jefeActual.transform.localRotation = Quaternion.identity;

        var vida = FindBossVida(jefeActual);
        if (vida != null) vida.OnMuere.AddListener(AlMorirJefe);

        var enem = jefeActual.GetComponent<Enemigo>();
        if (enem != null) { enem.puntoAncla = centro; enem.enPosicion = true; }
    }

    void AlMorirJefe()
    {
        OnJefeDerrotado?.Invoke();

        if (jefeActual != null)
        {
            var wrapper = jefeActual.transform.parent;
            Destroy(wrapper != null ? wrapper.gameObject : jefeActual);
        }
        jefeActual = null;

        if (!string.IsNullOrEmpty(escenaAlDerrotarJefe)) StartCoroutine(CambiarEscena());
    }

    IEnumerator CambiarEscena()
    {
        if (retardoCambioEscena > 0f) yield return new WaitForSeconds(retardoCambioEscena);

        if (SceneLoader.Instance != null)
        {
            if (usarLoadSingle)
                SceneLoader.Instance.LoadSingle(escenaAlDerrotarJefe);
            else
                SceneLoader.Instance.LoadLevel(escenaAlDerrotarJefe);
        }
        else
        {
            // Sin SceneLoader en escena: fallback directo
            SceneManager.LoadScene(
                escenaAlDerrotarJefe,
                usarLoadSingle ? LoadSceneMode.Single : LoadSceneMode.Additive);
        }
    }

    static VidaPersonaje FindBossVida(GameObject root)
    {
        var jefe = root.GetComponentInChildren<Jefe>(true);
        if (jefe != null) return jefe.GetComponent<VidaPersonaje>();

        var vidas = root.GetComponentsInChildren<VidaPersonaje>(true);
        if (vidas.Length == 0) return null;
        VidaPersonaje mejor = vidas[0];
        foreach (var v in vidas) if (v.vidaMaxima > mejor.vidaMaxima) mejor = v;
        return mejor;
    }

    public void RemoveEnemy(GameObject go)
    {
        vivos.RemoveAll(v => v == null || v == go);
    }

    // -------- Geometría / validación --------

    Vector3 PuntoValidoParaSpawn()
    {
        if (puntos.Count < 3)
        {
            Debug.LogError("[SpawnerOleadas] Sin polígono. Spawneando en centro.");
            return centro;
        }

        if (TryPunto(out var p, true)) return p;
        if (TryPunto(out p, false)) { Debug.LogWarning($"[SpawnerOleadas] Fallback 'sin borde' → {p}"); return p; }

        Vector3 mejor = centro;
        float mejorDist = -1f;
        for (int i = 0; i < intentosMaximos * 10; i++)
        {
            var c = RandomEnBounds();
            if (!PuntoDentroPoligono(c)) continue;
            float d = DistanciaMinimaAOtros(c);
            if (d > mejorDist) { mejorDist = d; mejor = c; }
        }
        return mejorDist >= 0f ? mejor : centro;
    }

    bool TryPunto(out Vector3 result, bool checkBorde)
    {
        var j2d = jugador != null ? (Vector2)jugador.position : (Vector2)centro;
        for (int i = 0; i < intentosMaximos * 2; i++)
        {
            var c = RandomEnBounds();
            if (!PuntoDentroPoligono(c)) continue;
            if (checkBorde && DistanciaAlBorde(c) < distanciaMinimaBorde) continue;
            if (((Vector2)c - j2d).sqrMagnitude < minJugSqr) continue;
            if (MuyCercaDeOtro(c)) continue;
            result = c;
            return true;
        }
        result = default;
        return false;
    }

    float DistanciaMinimaAOtros(Vector3 c)
    {
        if (vivos.Count == 0) return float.MaxValue;
        float min = float.MaxValue;
        foreach (var v in vivos)
        {
            if (v == null) continue;
            float d = (v.transform.position - c).sqrMagnitude;
            if (d < min) min = d;
        }
        return min;
    }

    Vector3 RandomEnBounds() =>
        new Vector3(Random.Range(bounds.xMin, bounds.xMax), Random.Range(bounds.yMin, bounds.yMax), centro.z);

    bool MuyCercaDeOtro(Vector3 c)
    {
        foreach (var v in vivos)
            if (v != null && (v.transform.position - c).sqrMagnitude < minEnemSqr) return true;
        return false;
    }

    Rect BoundsDePuntos()
    {
        if (puntos.Count == 0) return new Rect();
        Vector3 min = puntos[0], max = puntos[0];
        foreach (var p in puntos) { min = Vector3.Min(min, p); max = Vector3.Max(max, p); }
        return Rect.MinMaxRect(min.x, min.y, max.x, max.y);
    }

    float DistanciaAlBorde(Vector3 p)
    {
        float min = float.MaxValue;
        int n = puntos.Count;
        for (int i = 0; i < n; i++)
        {
            float d = DistanciaPuntoSegmento(p, puntos[i], puntos[(i + 1) % n]);
            if (d < min) min = d;
        }
        return min;
    }

    static float DistanciaPuntoSegmento(Vector3 p, Vector3 a, Vector3 b)
    {
        Vector3 ab = b - a;
        float abSqr = ab.sqrMagnitude;
        if (abSqr < 0.0001f) return Vector3.Distance(p, a);
        float t = Mathf.Clamp01(Vector3.Dot(p - a, ab) / abSqr);
        return Vector3.Distance(p, a + ab * t);
    }

    bool PuntoDentroPoligono(Vector3 p)
    {
        bool dentro = false;
        int n = puntos.Count;
        for (int i = 0, j = n - 1; i < n; j = i++)
        {
            Vector3 vi = puntos[i], vj = puntos[j];
            if (((vi.y > p.y) != (vj.y > p.y)) &&
                (p.x < (vj.x - vi.x) * (p.y - vi.y) / (vj.y - vi.y) + vi.x))
                dentro = !dentro;
        }
        return dentro;
    }

    Vector3 Centro()
    {
        if (puntos.Count == 0) return Vector3.zero;
        Vector3 s = Vector3.zero;
        foreach (var p in puntos) s += p;
        return s / puntos.Count;
    }
}

/// <summary>Vive en el wrapper. Se limpia de 'vivos' al destruirse.</summary>
public class EnemyTracker : MonoBehaviour
{
    SpawnerOleadas spawner;
    public void Init(SpawnerOleadas s) => spawner = s;
    void OnDestroy() { if (spawner != null) spawner.RemoveEnemy(gameObject); }
}