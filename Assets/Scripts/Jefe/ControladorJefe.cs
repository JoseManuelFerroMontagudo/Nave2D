using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// Controlador del Boss para un shooter espacial 2D con vista isométrica.
/// Movimiento omnidireccional (sin gravedad), persigue al jugador agresivamente
/// manteniéndose dentro de la cámara, usa 8 sprites direccionales y 3 tipos de proyectiles.
/// </summary>
public class ControladorJefe : MonoBehaviour
{
    [Header("Estadísticas")]
    public float vida = 100f;
    public float velocidadMovimiento = 6.5f;
    public float distanciaOrbitaIdeal = 3.5f;
    public float distanciaOrbitaMinima = 2.0f;
    public float velocidadOrbita = 45f;

    [Header("Disparo — Proyectil Principal (Energía)")]
    public GameObject prefabProyectil;
    public Transform puntoDisparo;
    public float dañoProyectil = 2f;

    [Header("Disparo — Proyectil Plasma")]
    public GameObject prefabProyectilPlasma;
    public float dañoPlasma = 1.5f;

    [Header("Disparo — Proyectil Perseguidor")]
    public GameObject prefabProyectilPerseguidor;
    public float dañoPerseguidor = 3f;

    [Header("Sprites Direccionales (8 direcciones)")]
    public Sprite[] spritesDirecciones;

    [Header("Secuencia de Patrones")]
    public List<EntradaPatron> patronesFase1 = new List<EntradaPatron>();
    public List<EntradaPatron> patronesFase2 = new List<EntradaPatron>();
    public List<EntradaPatron> patronesFase3 = new List<EntradaPatron>();
    public bool repetirCiclo = true;
    public float esperaInicial = 0.5f;

    [Header("Lluvia de fondo")]
    public bool lluviaActivada = true;
    public int lluviaBalasPorAnillo = 10;
    public float lluviaIntervalo = 0.35f;
    public OpcionesProyectil lluviaOpciones = new OpcionesProyectil
    {
        modo = ModoMovimiento.Recto,
        velocidad = 5.5f,
        tiempoVuelo = 3f,
        color = new Color(0.7f, 0.2f, 1f, 0.7f)
    };

    [Header("Fases de Combate")]
    public float umbralFase2 = 0.66f;
    public float umbralFase3 = 0.33f;

    [Header("Comportamiento de Movimiento")]
    public float velocidadPersecucion = 8.5f;
    public float suavizadoMovimiento = 6f;
    public float amplitudVariacionOrbita = 0.8f;
    public float frecuenciaVariacionOrbita = 0.4f;

    private float vidaMaxima;
    public float VidaMaxima => vidaMaxima > 0 ? vidaMaxima : 100f;

    private bool estaMuerto = false;
    private bool estaAtacando = false;
    private bool estaGolpeado = false;
    [HideInInspector] public bool esInvulnerable = false;
    private bool lluviaActiva = false;
    private int faseActual = 1;
    private float anguloOrbita = 0f;
    private float tiempoVivo = 0f;

    private Transform jugador;
    private Rigidbody2D rb;
    private SpriteRenderer spriteRenderer;

    public event System.Action OnMuerte;
    public event System.Action<int> OnCambioFase;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        vidaMaxima = vida;

        if (rb != null)
        {
            rb.gravityScale = 0f;
            rb.linearDamping = 1f;
            rb.angularDamping = 0f;
            rb.freezeRotation = true;
        }

        var jugadorObj = GameObject.FindGameObjectWithTag("Player");
        if (jugadorObj != null) jugador = jugadorObj.transform;
    }

    void Start()
    {
        anguloOrbita = Random.Range(0f, 360f);
    }

    void FixedUpdate()
    {
        if (estaMuerto) return;

        if (jugador == null)
        {
            var p = GameObject.FindGameObjectWithTag("Player");
            if (p != null) jugador = p.transform;
            return;
        }

        tiempoVivo += Time.fixedDeltaTime;
        MoverHaciaObjetivo();
        ActualizarSpriteDir();
        VerificarFaseCombate();

        if (!estaAtacando && !estaGolpeado)
            StartCoroutine(RutinaAtaque());
    }

    void MoverHaciaObjetivo()
    {
        if (estaGolpeado) return;
        float distancia = Vector2.Distance(transform.position, jugador.position);

        if (distancia > 5.5f)
        {
            Vector2 dirHaciaJugador = ((Vector2)jugador.position - (Vector2)transform.position).normalized;
            rb.linearVelocity = dirHaciaJugador * 12f;
            return;
        }

        float variacion = Mathf.Sin(tiempoVivo * frecuenciaVariacionOrbita * Mathf.PI * 2f) * amplitudVariacionOrbita;
        float distanciaObjetivo = distanciaOrbitaIdeal + variacion;

        anguloOrbita += velocidadOrbita * Time.fixedDeltaTime;
        if (anguloOrbita >= 360f) anguloOrbita -= 360f;

        float rad = anguloOrbita * Mathf.Deg2Rad;
        Vector2 posDeseada = (Vector2)jugador.position + new Vector2(
            Mathf.Cos(rad) * distanciaObjetivo,
            Mathf.Sin(rad) * distanciaObjetivo
        );

        Vector2 dirHaciaOrbita = (posDeseada - (Vector2)transform.position).normalized;
        float velActual = Mathf.Lerp(velocidadMovimiento, velocidadPersecucion, Mathf.Clamp01((distancia - distanciaOrbitaMinima) / distanciaObjetivo));
        rb.linearVelocity = Vector2.Lerp(rb.linearVelocity, dirHaciaOrbita * velActual, suavizadoMovimiento * Time.fixedDeltaTime);
    }

    void ActualizarSpriteDir()
    {
        if (spritesDirecciones == null || spritesDirecciones.Length < 8 || spriteRenderer == null) return;
        Vector2 dirAlJugador = ((Vector2)jugador.position - (Vector2)transform.position).normalized;
        float angulo = Mathf.Atan2(dirAlJugador.y, dirAlJugador.x) * Mathf.Rad2Deg;
        spriteRenderer.sprite = spritesDirecciones[4];
        spriteRenderer.transform.rotation = Quaternion.Euler(0, 0, angulo + 90f);
    }

    void VerificarFaseCombate()
    {
        float porcentaje = vida / vidaMaxima;
        int nuevaFase = 1;

        if (porcentaje <= umbralFase3) nuevaFase = 3;
        else if (porcentaje <= umbralFase2) nuevaFase = 2;

        if (nuevaFase != faseActual)
        {
            faseActual = nuevaFase;
            OnCambioFase?.Invoke(faseActual);
            if (estaAtacando)
            {
                StopAllCoroutines();
                estaAtacando = false;
                lluviaActiva = false;
            }
        }
    }

    List<EntradaPatron> ObtenerPatronesFaseActual()
    {
        switch (faseActual)
        {
            case 2: return (patronesFase2 != null && patronesFase2.Count > 0) ? patronesFase2 : patronesFase1;
            case 3: return (patronesFase3 != null && patronesFase3.Count > 0) ? patronesFase3 : patronesFase2;
            default: return patronesFase1;
        }
    }

    IEnumerator RutinaAtaque()
    {
        estaAtacando = true;
        yield return new WaitForSeconds(esperaInicial);
        var patrones = ObtenerPatronesFaseActual();

        if (patrones == null || patrones.Count == 0)
        {
            int patronIndex = 0;
            while (!estaMuerto)
            {
                if (jugador != null)
                {
                    Transform origen = puntoDisparo != null ? puntoDisparo : transform;
                    Vector2 dir = ((Vector2)jugador.position - (Vector2)origen.position).normalized;
                    var opc = new OpcionesProyectil { velocidad = 8.5f, vidaUtil = 3.5f };

                    if (patronIndex % 3 == 0)
                    {
                        for (int i = -2; i <= 2; i++)
                        {
                            Vector2 dirSub = Quaternion.Euler(0, 0, i * 12f) * dir;
                            InstanciarProyectil(origen, dirSub, opc);
                        }
                    }
                    else if (patronIndex % 3 == 1)
                    {
                        for (int i = 0; i < 10; i++)
                        {
                            float ang = i * 36f;
                            Vector2 dirRing = new Vector2(Mathf.Cos(ang * Mathf.Deg2Rad), Mathf.Sin(ang * Mathf.Deg2Rad));
                            InstanciarProyectilDeTipo(TipoProyectilBoss.Plasma, origen, dirRing, opc);
                        }
                    }
                    else
                    {
                        for (int i = 0; i < 3; i++)
                        {
                            var opcPerseguidor = opc.Clonar();
                            opcPerseguidor.modo = ModoMovimiento.Perseguidor;
                            opcPerseguidor.fuerzaPersecucion = 3.5f;
                            InstanciarProyectilDeTipo(TipoProyectilBoss.Perseguidor, origen, dir, opcPerseguidor);
                            yield return new WaitForSeconds(0.12f);
                        }
                    }
                    patronIndex++;
                }
                yield return new WaitForSeconds(0.75f);
            }
            estaAtacando = false;
            yield break;
        }

        int idx = 0;
        while (true)
        {
            if (estaMuerto) yield break;
            var entrada = patrones[idx];
            if (entrada != null && entrada.patron != null)
            {
                var ctx = new ContextoPatron { jefe = this, puntoDisparo = puntoDisparo != null ? puntoDisparo : transform, jugador = jugador };
                yield return StartCoroutine(entrada.patron.Ejecutar(ctx));
            }
            idx++;
            if (idx >= patrones.Count) { if (repetirCiclo) idx = 0; else break; }
        }
        estaAtacando = false;
    }

    public void InstanciarProyectil(Transform origen, Vector2 direccion, OpcionesProyectil opciones)
    {
        InstanciarProyectilDeTipo(TipoProyectilBoss.Energia, origen, direccion, opciones);
    }

    public void InstanciarProyectilDeTipo(TipoProyectilBoss tipo, Transform origen, Vector2 direccion, OpcionesProyectil opciones)
    {
        GameObject prefab = ObtenerPrefabPorTipo(tipo);
        float daño = ObtenerDañoPorTipo(tipo);
        if (prefab == null) return;

        Vector3 pos = origen != null ? origen.position : transform.position;
        GameObject go = Instantiate(prefab, pos, Quaternion.identity);
        var p = go.GetComponent<ProyectilJefe>();
        if (p != null) p.Configurar(direccion.normalized, opciones, daño);

        float rot = Mathf.Atan2(direccion.y, direccion.x) * Mathf.Rad2Deg;
        go.transform.rotation = Quaternion.Euler(0, 0, rot);
    }

    GameObject ObtenerPrefabPorTipo(TipoProyectilBoss tipo)
    {
        switch (tipo)
        {
            case TipoProyectilBoss.Plasma: return prefabProyectilPlasma != null ? prefabProyectilPlasma : prefabProyectil;
            case TipoProyectilBoss.Perseguidor: return prefabProyectilPerseguidor != null ? prefabProyectilPerseguidor : prefabProyectil;
            default: return prefabProyectil;
        }
    }

    float ObtenerDañoPorTipo(TipoProyectilBoss tipo)
    {
        switch (tipo)
        {
            case TipoProyectilBoss.Plasma: return dañoPlasma;
            case TipoProyectilBoss.Perseguidor: return dañoPerseguidor;
            default: return dañoProyectil;
        }
    }

    public void TakeDamage(float damage) => AplicarDaño(damage);

    public void AplicarDaño(float daño)
    {
        if (esInvulnerable || estaMuerto) return;
        vida -= Mathf.Abs(daño);

        if (vida <= 0f) { vida = 0f; Morir(); }
        else StartCoroutine(ParpadeoDaño());
    }

    IEnumerator ParpadeoDaño()
    {
        estaGolpeado = true;
        if (spriteRenderer != null)
        {
            Color original = spriteRenderer.color;
            spriteRenderer.color = Color.red;
            yield return new WaitForSeconds(0.06f);
            spriteRenderer.color = original;
        }
        estaGolpeado = false;
    }

    void Morir()
    {
        estaMuerto = true;
        estaAtacando = false;
        StopAllCoroutines();
        if (rb != null) rb.linearVelocity = Vector2.zero;
        OnMuerte?.Invoke();
        StartCoroutine(AnimacionMuerte());
    }

    IEnumerator AnimacionMuerte()
    {
        if (spriteRenderer != null)
        {
            for (int i = 0; i < 10; i++)
            {
                spriteRenderer.color = (i % 2 == 0) ? Color.white : Color.red;
                transform.localScale = Vector3.one * (1f + Mathf.Sin(i * 0.5f) * 0.15f);
                yield return new WaitForSeconds(0.1f);
            }
        }
        Destroy(gameObject);
    }
}