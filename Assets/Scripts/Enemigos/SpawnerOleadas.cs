using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SpawnerOleadas : MonoBehaviour
{
    [Header("Límite del mapa (LineRenderer)")]
    public LineRenderer limiteMapa;

    [Header("Oleadas")]
    public List<EntradaOleada> oleadas = new List<EntradaOleada>();

    [Header("Jefe (spawnea en el centro al terminar)")]
    public GameObject prefabJefe;

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
    /// <summary>0 = aún no empieza, 1..N = oleada en curso.</summary>
    public int NumeroOleadaActual { get; private set; } = 0;
    public int TotalOleadas => oleadas.Count;
    public int EnemigosVivos => vivos.Count;
    /// <summary>Enemigos vivos + los que faltan por spawnear en la oleada actual.</summary>
    public int EnemigosRestantesOleada => vivos.Count + pendientesSpawn;
    public bool EnFaseJefe { get; private set; }

    int pendientesSpawn = 0;

    readonly List<GameObject> vivos = new List<GameObject>();
    readonly List<Vector3> puntos = new List<Vector3>();
    Transform jugador;

    void Start()
    {
        if (limiteMapa != null)
            for (int i = 0; i < limiteMapa.positionCount; i++)
                puntos.Add(limiteMapa.GetPosition(i));

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

        for (int idx = 0; idx < oleadas.Count; idx++)
        {
            NumeroOleadaActual = idx + 1;
            var oleada = oleadas[idx];

            if (oleada.retardoAntes > 0) yield return new WaitForSeconds(oleada.retardoAntes);

            yield return StartCoroutine(SpawnearOleada(oleada));

            yield return new WaitUntil(() => vivos.Count == 0);

            if (oleada.retardoDespues > 0) yield return new WaitForSeconds(oleada.retardoDespues);
        }

        EnFaseJefe = true;
        SpawnearJefe();
    }

    void SpawnearJefe()
    {
        if (prefabJefe == null) return;

        Vector3 centro = Centro();
        var wrapperGO = new GameObject("BossWrapper");
        wrapperGO.transform.position = centro;

        var j = Instantiate(prefabJefe, wrapperGO.transform);
        j.transform.localPosition = Vector3.zero;
        j.transform.localRotation = Quaternion.identity;

        var ej = j.GetComponent<Enemigo>();
        if (ej != null) { ej.puntoAncla = centro; ej.enPosicion = true; }
    }

    IEnumerator SpawnearOleada(EntradaOleada oleada)
    {
        int total = 0;
        foreach (var g in oleada.grupos) total += g.cantidad;
        pendientesSpawn = total;

        foreach (var grupo in oleada.grupos)
        {
            for (int k = 0; k < grupo.cantidad; k++)
            {
                Vector3 destino = PuntoValidoParaSpawn();

                // Wrapper en el punto de spawn
                var wrapperGO = new GameObject($"EnemyWrapper_{grupo.prefab.name}");
                var wrapper = wrapperGO.transform;
                wrapper.position = destino;

                // Enemigo como HIJO del wrapper
                var e = Instantiate(grupo.prefab, wrapper);
                e.transform.localPosition = Vector3.zero;
                e.transform.localRotation = Quaternion.identity;
                pendientesSpawn--;

                // Vida: al morir se destruye también el wrapper
                var vida = e.GetComponent<VidaPersonaje>();
                if (vida != null)
                {
                    vida.OnMuere.AddListener(() =>
                    {
                        vivos.Remove(wrapperGO);
                        if (wrapperGO != null) Destroy(wrapperGO);
                    });
                }
                vivos.Add(wrapperGO);

                // El enemigo queda "en posición" desde el frame 0
                var enem = e.GetComponent<Enemigo>();
                if (enem != null)
                {
                    enem.puntoAncla = destino;
                    enem.enPosicion = true;
                }

                if (oleada.delayEntreEnemigos > 0)
                    yield return new WaitForSeconds(oleada.delayEntreEnemigos);
            }
        }
    }

    // -------- Geometría / validación --------

    Vector3 PuntoValidoParaSpawn()
    {
        Vector3 centro = Centro();
        if (puntos.Count < 3) return centro;

        // Bounding box del polígono para no generar candidatos a lo loco
        float minX = float.MaxValue, minY = float.MaxValue;
        float maxX = float.MinValue, maxY = float.MinValue;
        foreach (var p in puntos)
        {
            if (p.x < minX) minX = p.x;
            if (p.y < minY) minY = p.y;
            if (p.x > maxX) maxX = p.x;
            if (p.y > maxY) maxY = p.y;
        }

        Vector3 jugadorPos = jugador != null ? jugador.position : centro;
        float minJugSqr = distanciaMinimaJugador * distanciaMinimaJugador;
        float minEnemSqr = separacionMinimaEnemigos * separacionMinimaEnemigos;

        for (int intento = 0; intento < intentosMaximos; intento++)
        {
            float x = Random.Range(minX, maxX);
            float y = Random.Range(minY, maxY);
            Vector3 candidato = new Vector3(x, y, centro.z);

            if (!PuntoDentroPoligono(candidato)) continue;
            if (DistanciaAlBorde(candidato) < distanciaMinimaBorde) continue;
            if ((candidato - jugadorPos).sqrMagnitude < minJugSqr) continue;

            bool muyCerca = false;
            foreach (var v in vivos)
            {
                if (v == null) continue;
                if ((v.transform.position - candidato).sqrMagnitude < minEnemSqr)
                {
                    muyCerca = true;
                    break;
                }
            }
            if (muyCerca) continue;

            return candidato;
        }

        // Fallback: centro (puede no cumplir todos los criterios si el área es pequeña)
        return centro;
    }

    float DistanciaAlBorde(Vector3 p)
    {
        float min = float.MaxValue;
        int n = puntos.Count;
        for (int i = 0; i < n; i++)
        {
            Vector3 a = puntos[i];
            Vector3 b = puntos[(i + 1) % n];
            float d = DistanciaPuntoSegmento(p, a, b);
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
        Vector3 proj = a + ab * t;
        return Vector3.Distance(p, proj);
    }

    bool PuntoDentroPoligono(Vector3 p)
    {
        bool dentro = false;
        int n = puntos.Count;
        for (int i = 0, j = n - 1; i < n; j = i++)
        {
            Vector3 vi = puntos[i];
            Vector3 vj = puntos[j];
            if (((vi.y > p.y) != (vj.y > p.y)) &&
                (p.x < (vj.x - vi.x) * (p.y - vi.y) / (vj.y - vi.y) + vi.x))
            {
                dentro = !dentro;
            }
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