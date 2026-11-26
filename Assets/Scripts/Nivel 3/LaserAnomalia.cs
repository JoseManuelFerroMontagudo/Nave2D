using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LaserAnomalia : MonoBehaviour
{
    [Header("Configuración General")]
    public float distanciaMax = 30f;
    public AudioClip sonidoDisparo;           // Laser1
    [Range(0f, 1f)] public float volumenDisparo = 0.55f;
    public float tiempoAdvertencia = 1.25f;
    public float duracionDisparo = 0.5f;
    public int danoPorImpacto = 25;

    [Header("Configuración Mini-Agujeros (Ciclo 3)")]
    public GameObject prefabMiniAgujero;
    public GameObject prefabPortalHijos;      // Arrastra aquí el efecto de portal de tu pack
    public float retrasoAparicionHijos = 0.4f; // Tiempo que tarda en abrirse el portal antes de salir el satélite
    public float distanciaOrbitaHijos = 5.5f;
    public float anguloFlancosHijos = 48f;

    [Header("Efectos Visuales (VFX)")]
    public GameObject prefabImpactoLaser; // Arrastra aquí un prefab de chispa, rayo o mini-explosión

    [Header("Materiales / Diseño")]
    public Material materialAdvertencia;
    public Material materialDisparo;
    public float grosorAdvertencia = 0.14f;
    public float grosorDisparo = 1.0f;
    public Color colorAdvertencia = new Color(1f, 0.9f, 0.1f, 0.45f);
    public Color colorDisparo = new Color(1f, 0.1f, 0.1f, 1f);
    public string sortingLayerName = "Elementos_Juego";

    [Header("Progresión y Ciclos")]
    public int nivelAtaqueActual = 1;
    public int cicloActual = 1; // 1 = Recto, 2 = Zig-Zag, 3 = Triangulación con 2 Mini-Agujeros
    public float anguloAbanico = 18f;

    [Header("Configuración Zig-Zag (Ciclo 2)")]
    public int segmentosZigZag = 10;
    public float amplitudZigZag = 1.1f;

    private List<LineRenderer> poolRayos = new List<LineRenderer>();
    private List<GameObject> hijosActivos = new List<GameObject>();
    private Transform jugador;
    private bool disparando = false;

    private LineRenderer ObtenerOcrearRayo(int indice)
    {
        while (poolRayos.Count <= indice)
        {
            GameObject obj = new GameObject("RayoAnomalia_" + poolRayos.Count);
            obj.transform.SetParent(transform);
            obj.transform.localPosition = Vector3.zero;

            LineRenderer lr = obj.AddComponent<LineRenderer>();
            lr.useWorldSpace = true;
            lr.sortingLayerName = sortingLayerName;
            lr.textureMode = LineTextureMode.Tile;
            lr.enabled = false;
            poolRayos.Add(lr);
        }
        return poolRayos[indice];
    }

    public void DispararAlJugador(Transform objetivo)
    {
        if (!disparando && objetivo != null)
        {
            jugador = objetivo;
            StartCoroutine(RutinaDisparoEscalado());
        }
    }

    public void AvanzarDeCiclo(int nuevoCiclo, Transform objetivo)
    {
        nivelAtaqueActual = 1;
        cicloActual = nuevoCiclo;

        // Si entramos al Ciclo 3, invocamos los 2 Mini-Agujeros Negros a los costados
        if (cicloActual >= 3 && hijosActivos.Count == 0 && prefabMiniAgujero != null && objetivo != null)
        {
            InvocarMiniAgujeros(objetivo);
        }
    }

    private void InvocarMiniAgujeros(Transform objetivo)
    {
        StartCoroutine(RutinaInvocarMiniAgujerosConPortal(objetivo));
    }

    private IEnumerator RutinaInvocarMiniAgujerosConPortal(Transform objetivo)
    {
        // Calcular el ángulo actual desde el Agujero Negro hacia el jugador para saber dónde abrir los portales
        Vector2 dirAlJugador = (objetivo.position - transform.position).normalized;
        if (dirAlJugador == Vector2.zero) dirAlJugador = Vector2.up;
        float anguloBase = Mathf.Atan2(dirAlJugador.y, dirAlJugador.x) * Mathf.Rad2Deg;

        float[] angulos = { -anguloFlancosHijos, anguloFlancosHijos };

        // 1. Abrir los 2 Portales en los flancos del jefe
        if (prefabPortalHijos != null)
        {
            foreach (float ang in angulos)
            {
                float anguloRad = (anguloBase + ang) * Mathf.Deg2Rad;
                Vector3 posPortal = transform.position + new Vector3(Mathf.Cos(anguloRad), Mathf.Sin(anguloRad), 0f) * distanciaOrbitaHijos;

                GameObject portal = Instantiate(prefabPortalHijos, posPortal, Quaternion.identity);
                Destroy(portal, 1.8f); // Se limpia automáticamente tras cerrarse
            }
        }

        // Esperar un instante a que se abran los portales
        yield return new WaitForSeconds(retrasoAparicionHijos);

        // Si mientras tanto el jefe ya detonó al Agujero Blanco, cancelamos la invocación
        if (cicloActual < 3 || objetivo == null) yield break;

        // 2. Hacer emerger los 2 Mini-Agujeros desde los portales
        foreach (float ang in angulos)
        {
            float anguloRad = (anguloBase + ang) * Mathf.Deg2Rad;
            Vector3 posSpawn = transform.position + new Vector3(Mathf.Cos(anguloRad), Mathf.Sin(anguloRad), 0f) * distanciaOrbitaHijos;

            GameObject hijo = Instantiate(prefabMiniAgujero, posSpawn, Quaternion.identity);
            MiniAgujeroSatelite sat = hijo.AddComponent<MiniAgujeroSatelite>();
            sat.Inicializar(transform, objetivo, ang, distanciaOrbitaHijos);
            hijosActivos.Add(hijo);

            // Animación rápida de aparición creciendo desde 0 hasta su escala del prefab
            StartCoroutine(AnimarEscalaAparicion(hijo.transform));
        }
    }

    private IEnumerator AnimarEscalaAparicion(Transform objetivoEscala)
    {
        if (objetivoEscala == null) yield break;
        Vector3 escalaFinal = objetivoEscala.localScale;
        objetivoEscala.localScale = Vector3.zero;

        float t = 0f;
        while (t < 1f && objetivoEscala != null)
        {
            t += Time.deltaTime * 4f; // Aparece en 0.25 segundos
            objetivoEscala.localScale = Vector3.Lerp(Vector3.zero, escalaFinal, t);
            yield return null;
        }
    }

    public void LimpiarMiniAgujeros()
    {
        foreach (var hijo in hijosActivos)
        {
            if (hijo != null) Destroy(hijo);
        }
        hijosActivos.Clear();
    }

    private IEnumerator RutinaDisparoEscalado()
    {
        disparando = true;
        Vector3 origenCentro = transform.position;
        Vector3 posJugador = (jugador != null) ? jugador.position : (origenCentro + Vector3.up * 8f);
        Vector3 dirBase = (posJugador - origenCentro).normalized;
        if (dirBase == Vector3.zero) dirBase = Vector3.up;

        List<List<Vector3>> trayectoriasPuntos = new List<List<Vector3>>();

        if (cicloActual < 3)
        {
            // CICLO 1 (Recto) y CICLO 2 (Zig-Zag): Disparan desde el Agujero Negro central
            bool usarZigZag = (cicloActual == 2);
            List<Vector3> direcciones = CalcularDireccionesPorNivel(dirBase, nivelAtaqueActual);
            foreach (Vector3 dir in direcciones)
            {
                trayectoriasPuntos.Add(GenerarPuntosRayo(origenCentro, dir, usarZigZag));
            }
        }
        else
        {
            // CICLO 3: Triangulación real desde el Jefe + los 2 Mini-Agujeros Negros
            trayectoriasPuntos = GenerarPatronesTriangulados(origenCentro, posJugador, dirBase, nivelAtaqueActual);
        }

        // FASE 1: ADVERTENCIA
        for (int i = 0; i < trayectoriasPuntos.Count; i++)
        {
            LineRenderer lr = ObtenerOcrearRayo(i);
            lr.enabled = true;
            if (materialAdvertencia != null) lr.material = materialAdvertencia;
            lr.startWidth = grosorAdvertencia;
            lr.endWidth = grosorAdvertencia;
            lr.startColor = colorAdvertencia;
            lr.endColor = colorAdvertencia;

            lr.positionCount = trayectoriasPuntos[i].Count;
            lr.SetPositions(trayectoriasPuntos[i].ToArray());
        }

        yield return new WaitForSeconds(tiempoAdvertencia);

        // FASE 2: DISPARO REAL
        if (sonidoDisparo != null)
        {
            var sfx = new GameObject("SFX_Laser").AddComponent<AudioSource>();
            sfx.clip = sonidoDisparo; sfx.volume = volumenDisparo; sfx.spatialBlend = 0f;
            sfx.Play(); Destroy(sfx.gameObject, sonidoDisparo.length + 0.1f);
        }
        for (int i = 0; i < trayectoriasPuntos.Count; i++)
        {
            LineRenderer lr = poolRayos[i];
            if (materialDisparo != null) lr.material = materialDisparo;
            lr.startWidth = grosorDisparo;
            lr.endWidth = grosorDisparo;
            lr.startColor = colorDisparo;
            lr.endColor = colorDisparo;
        }

        float tiempoDisparo = 0f;
        bool yaHizoDano = false;

        while (tiempoDisparo < duracionDisparo)
        {
            tiempoDisparo += Time.deltaTime;

            if (!yaHizoDano)
            {
                foreach (List<Vector3> puntos in trayectoriasPuntos)
                {
                    for (int p = 0; p < puntos.Count - 1; p++)
                    {
                        Vector3 inicioSeg = puntos[p];
                        Vector3 finSeg = puntos[p + 1];
                        Vector3 dirSeg = (finSeg - inicioSeg).normalized;
                        float distSeg = Vector3.Distance(inicioSeg, finSeg);

                        RaycastHit2D[] impactos = Physics2D.CircleCastAll(inicioSeg, grosorDisparo * 0.36f, dirSeg, distSeg);
                        foreach (RaycastHit2D hit in impactos)
                        {
                            if (hit.collider != null && (hit.collider.CompareTag("Player") || hit.transform.root.CompareTag("Player")))
                            {
                                hit.transform.root.BroadcastMessage("RecibirDaño", danoPorImpacto, SendMessageOptions.DontRequireReceiver);
                                // Instanciar efecto de impacto en el punto exacto donde tocó la nave
                                if (prefabImpactoLaser != null)
                                {
                                    GameObject fx = Instantiate(prefabImpactoLaser, hit.point, Quaternion.identity);
                                    Destroy(fx, 1.5f); // Lo destruye automáticamente tras 1.5 segundos para no llenar la escena
                                }

                                yaHizoDano = true;
                                break;
                            }
                        }
                        if (yaHizoDano) break;
                    }
                    if (yaHizoDano) break;
                }
            }

            yield return null;
        }

        foreach (var lr in poolRayos)
        {
            lr.enabled = false;
        }

        nivelAtaqueActual++;
        disparando = false;
    }

    private List<List<Vector3>> GenerarPatronesTriangulados(Vector3 origenCentro, Vector3 posJugador, Vector3 dirBase, int nivel)
    {
        List<List<Vector3>> rayos = new List<List<Vector3>>();

        // Obtener referencias seguras a los 2 mini-agujeros
        Vector3 origenIzq = (hijosActivos.Count > 0 && hijosActivos[0] != null) ? hijosActivos[0].transform.position : origenCentro;
        Vector3 origenDer = (hijosActivos.Count > 1 && hijosActivos[1] != null) ? hijosActivos[1].transform.position : origenCentro;

        Vector3 dirIzqAlJugador = (posJugador - origenIzq).normalized;
        Vector3 dirDerAlJugador = (posJugador - origenDer).normalized;

        if (nivel == 1)
        {
            // NIVEL 1: "Tridente Convergente" (Los 3 agujeros cruzan sus rayos justo en la posición del jugador)
            rayos.Add(GenerarPuntosRayo(origenCentro, dirBase, false));
            rayos.Add(GenerarPuntosRayo(origenIzq, dirIzqAlJugador, false));
            rayos.Add(GenerarPuntosRayo(origenDer, dirDerAlJugador, false));
        }
        else if (nivel == 2)
        {
            // NIVEL 2: "Tenaza Lateral" (El centro cierra los flancos en V amplia, los hijos disparan paralelos al centro)
            rayos.Add(GenerarPuntosRayo(origenCentro, Quaternion.Euler(0, 0, -22f) * dirBase, false));
            rayos.Add(GenerarPuntosRayo(origenCentro, Quaternion.Euler(0, 0, 22f) * dirBase, false));

            // Los satélites disparan ligeramente desplazados hacia el interior para formar un pasillo estrecho
            rayos.Add(GenerarPuntosRayo(origenIzq, Quaternion.Euler(0, 0, 6f) * dirIzqAlJugador, false));
            rayos.Add(GenerarPuntosRayo(origenDer, Quaternion.Euler(0, 0, -6f) * dirDerAlJugador, false));
        }
        else if (nivel == 3)
        {
            // NIVEL 3: "Telaraña de Rombos" (3 rayos centrales + tijeras cruzadas desde los satélites)
            rayos.Add(GenerarPuntosRayo(origenCentro, Quaternion.Euler(0, 0, -18f) * dirBase, false));
            rayos.Add(GenerarPuntosRayo(origenCentro, dirBase, false));
            rayos.Add(GenerarPuntosRayo(origenCentro, Quaternion.Euler(0, 0, 18f) * dirBase, false));

            // Tijera desde el satélite izquierdo
            rayos.Add(GenerarPuntosRayo(origenIzq, Quaternion.Euler(0, 0, -14f) * dirIzqAlJugador, false));
            rayos.Add(GenerarPuntosRayo(origenIzq, Quaternion.Euler(0, 0, 14f) * dirIzqAlJugador, false));

            // Tijera desde el satélite derecho
            rayos.Add(GenerarPuntosRayo(origenDer, Quaternion.Euler(0, 0, -14f) * dirDerAlJugador, false));
            rayos.Add(GenerarPuntosRayo(origenDer, Quaternion.Euler(0, 0, 14f) * dirDerAlJugador, false));
        }
        else
        {
            // NIVEL 4: "Juicio Triangular" (3 rayos por cada uno de los 3 agujeros = 9 rayos en malla total)
            float[] abanicoTriple = { -16f, 0f, 16f };
            foreach (float ang in abanicoTriple)
            {
                rayos.Add(GenerarPuntosRayo(origenCentro, Quaternion.Euler(0, 0, ang) * dirBase, false));
                rayos.Add(GenerarPuntosRayo(origenIzq, Quaternion.Euler(0, 0, ang) * dirIzqAlJugador, false));
                rayos.Add(GenerarPuntosRayo(origenDer, Quaternion.Euler(0, 0, ang) * dirDerAlJugador, false));
            }
        }

        return rayos;
    }

    private List<Vector3> GenerarPuntosRayo(Vector3 origen, Vector3 direccion, bool usarZigZag)
    {
        List<Vector3> puntos = new List<Vector3> { origen };

        if (!usarZigZag)
        {
            puntos.Add(origen + direccion * distanciaMax);
        }
        else
        {
            Vector3 perpendicular = new Vector3(-direccion.y, direccion.x, 0f);
            float pasoDistancia = distanciaMax / segmentosZigZag;

            for (int i = 1; i < segmentosZigZag; i++)
            {
                float signo = (i % 2 == 0) ? 1f : -1f;
                Vector3 puntoBase = origen + (direccion * (pasoDistancia * i));
                puntos.Add(puntoBase + (perpendicular * (amplitudZigZag * signo)));
            }
            puntos.Add(origen + direccion * distanciaMax);
        }

        return puntos;
    }

    private List<Vector3> CalcularDireccionesPorNivel(Vector3 dirBase, int nivel)
    {
        List<Vector3> dirs = new List<Vector3>();

        if (nivel == 1)
        {
            // Nivel 1: 2 láseres en V estrecha
            dirs.Add(Quaternion.Euler(0, 0, -anguloAbanico * 0.5f) * dirBase);
            dirs.Add(Quaternion.Euler(0, 0, anguloAbanico * 0.5f) * dirBase);
        }
        else if (nivel == 2)
        {
            // Nivel 2: 3 láseres (uno central directo al jugador + dos flancos)
            dirs.Add(Quaternion.Euler(0, 0, -anguloAbanico) * dirBase);
            dirs.Add(dirBase);
            dirs.Add(Quaternion.Euler(0, 0, anguloAbanico) * dirBase);
        }
        else if (nivel == 3)
        {
            // Nivel 3: 4 láseres en abanico abierto
            dirs.Add(Quaternion.Euler(0, 0, -anguloAbanico * 1.5f) * dirBase);
            dirs.Add(Quaternion.Euler(0, 0, -anguloAbanico * 0.5f) * dirBase);
            dirs.Add(Quaternion.Euler(0, 0, anguloAbanico * 0.5f) * dirBase);
            dirs.Add(Quaternion.Euler(0, 0, anguloAbanico * 1.5f) * dirBase);
        }
        else
        {
            // Nivel 4: Jaula Frontal de 5 láseres
            float separacionJaula = 16f;
            for (int i = -2; i <= 2; i++)
            {
                dirs.Add(Quaternion.Euler(0, 0, i * separacionJaula) * dirBase);
            }
        }

        return dirs;
    }

    public bool EstaDisparando()
    {
        return disparando;
    }
}