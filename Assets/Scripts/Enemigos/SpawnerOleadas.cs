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

    [Header("Entrada desde el borde")]
    public float velocidadEntrada = 5f;
    public float radioPosicionamiento = 3f;   // separación entre enemigos de la misma oleada

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

        foreach (var oleada in oleadas)
        {
            if (oleada.retardoAntes > 0) yield return new WaitForSeconds(oleada.retardoAntes);

            yield return StartCoroutine(SpawnearOleada(oleada));

            yield return new WaitUntil(() => vivos.Count == 0);

            if (oleada.retardoDespues > 0) yield return new WaitForSeconds(oleada.retardoDespues);
        }

        SpawnearJefe();
    }

    void SpawnearJefe()
    {
        if (prefabJefe == null) return;

        Vector3 centro = Centro();
        var j = Instantiate(prefabJefe, centro, Quaternion.identity);

        // El jefe arranca ya "en posición" en el centro
        var ej = j.GetComponent<Enemigo>();
        if (ej != null) { ej.puntoAncla = centro; ej.enPosicion = true; }
    }

    IEnumerator SpawnearOleada(EntradaOleada oleada)
    {
        int total = 0;
        foreach (var g in oleada.grupos) total += g.cantidad;

        int i = 0;
        foreach (var grupo in oleada.grupos)
        {
            for (int k = 0; k < grupo.cantidad; k++)
            {
                Vector3 borde = PuntoEnBorde();
                Vector3 destino = PuntoInterior(i, total);

                var e = Instantiate(grupo.prefab, borde, Quaternion.identity);

                // Registro de vida para saber cuándo termina la oleada
                var vida = e.GetComponent<VidaPersonaje>();
                if (vida != null)
                {
                    var capt = e;
                    vida.OnMuere.AddListener(() => vivos.Remove(capt));
                }
                vivos.Add(e);

                // El spawner lo lleva a su punto dentro del límite
                var enem = e.GetComponent<Enemigo>();
                if (enem != null) enem.enPosicion = false;
                StartCoroutine(LlevarAPosicion(e, destino));

                i++;
                if (oleada.delayEntreEnemigos > 0)
                    yield return new WaitForSeconds(oleada.delayEntreEnemigos);
            }
        }
    }

    // Mueve al enemigo desde el borde hasta su ancla; al llegar lo activa.
    IEnumerator LlevarAPosicion(GameObject enemigo, Vector3 destino)
    {
        while (enemigo != null &&
               Vector3.Distance(enemigo.transform.position, destino) > 0.05f)
        {
            enemigo.transform.position = Vector3.MoveTowards(
                enemigo.transform.position, destino,
                velocidadEntrada * Time.deltaTime);
            yield return null;
        }
        if (enemigo == null) yield break;

        var e = enemigo.GetComponent<Enemigo>();
        if (e != null)
        {
            e.puntoAncla = destino;
            e.enPosicion = true;   // a partir de aquí manda el Animator (o el timer)
        }
    }

    // -------- Geometría del límite --------
    Vector3 PuntoEnBorde()
    {
        if (puntos.Count < 2) return Vector3.zero;
        int s = Random.Range(0, puntos.Count);
        return Vector3.Lerp(puntos[s], puntos[(s + 1) % puntos.Count], Random.value);
    }

    Vector3 PuntoInterior(int index, int total)
    {
        Vector3 c = Centro();
        float a = Mathf.PI * 2f * index / Mathf.Max(1, total);
        return c + new Vector3(Mathf.Cos(a), Mathf.Sin(a), 0f) * radioPosicionamiento;
    }

    Vector3 Centro()
    {
        if (puntos.Count == 0) return Vector3.zero;
        Vector3 s = Vector3.zero;
        foreach (var p in puntos) s += p;
        return s / puntos.Count;
    }
}