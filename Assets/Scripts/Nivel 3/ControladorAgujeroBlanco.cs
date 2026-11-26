using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ControladorAgujeroBlanco : MonoBehaviour
{
    [Header("Alcance del Mapa")]
    public float radioAlcanceMapa = 60f;

    [Header("Referencia de Salud Nativa")]
    public VidaPersonaje vidaPersonaje;

    [Header("Física Inversa: Repulsión Constante")]
    public float fuerzaRepulsion = 4.5f;

    [Header("Prefabs y Materiales Bullet-Hell")]
    public GameObject prefabBalaNormal;
    public GameObject prefabOrbeGigante;
    public Material materialLaserEstrella;
    public string sortingLayerLasers = "Default";
    public int orderInLayerLasers = 10;

    [Header("Enfoque hacia el Jugador")]
    [Tooltip("Amplitud del cono de disparo frente al jugador (ej. 160° cubre toda tu mitad de pantalla)")]
    public float amplitudConoAtaque = 160f;

    [Header("Tiempos entre Spell Cards")]
    public float duracionCadaPatron = 8f;
    public float pausaEntrePatrones = 2.0f;

    [Header("Efectos de Victoria")]
    public GameObject prefabExplosionFinal;

    private bool faseActiva = false;
    private Transform jugador;
    private Rigidbody2D rbJugador;
    private int indicePatron = 0;
    private List<LineRenderer> lasersEstrella = new List<LineRenderer>();

    private void Awake()
    {
        if (vidaPersonaje == null) vidaPersonaje = GetComponent<VidaPersonaje>();
    }

    private void OnEnable()
    {
        if (vidaPersonaje != null)
        {
            vidaPersonaje.OnMuere.AddListener(DerrotarAgujeroBlanco);
        }
        ActivarFaseAgujeroBlanco();
    }

    private void OnDisable()
    {
        if (vidaPersonaje != null)
        {
            vidaPersonaje.OnMuere.RemoveListener(DerrotarAgujeroBlanco);
        }
    }

    public void ActivarFaseAgujeroBlanco()
    {
        if (faseActiva) return;
        faseActiva = true;

        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
        {
            rbJugador = playerObj.GetComponentInChildren<Rigidbody2D>();
            jugador = (rbJugador != null) ? rbJugador.transform : playerObj.transform;
        }

        StartCoroutine(CicloMaestroBulletHell());
    }

    private void FixedUpdate()
    {
        if (!faseActiva || jugador == null) return;

        Vector2 dirRepulsion = (jugador.position - transform.position).normalized;
        if (dirRepulsion == Vector2.zero) dirRepulsion = Vector2.down;

        if (rbJugador != null)
        {
            rbJugador.AddForce(dirRepulsion * fuerzaRepulsion, ForceMode2D.Force);
        }
        else
        {
            jugador.position += (Vector3)(dirRepulsion * (fuerzaRepulsion * 0.25f) * Time.fixedDeltaTime);
        }
    }

    private float ObtenerAnguloHaciaJugador()
    {
        if (jugador == null) return -90f;
        Vector2 dir = (jugador.position - transform.position).normalized;
        if (dir == Vector2.zero) return -90f;
        return Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
    }

    private IEnumerator CicloMaestroBulletHell()
    {
        yield return new WaitForSeconds(1.0f);

        while (faseActiva && (vidaPersonaje == null || !vidaPersonaje.Muerto))
        {
            bool enFuria = (vidaPersonaje != null && vidaPersonaje.vidaActual <= vidaPersonaje.vidaMaxima * 0.35f);

            if (enFuria)
            {
                yield return StartCoroutine(PatronSupernovaDobleEspiral());
            }
            else
            {
                switch (indicePatron % 4)
                {
                    case 0:
                        yield return StartCoroutine(PatronEspiralFloral());
                        break;
                    case 1:
                        yield return StartCoroutine(PatronEstrellaLasersYAnillos());
                        break;
                    case 2:
                        yield return StartCoroutine(PatronOrbesGigantesYAbanicos());
                        break;
                    case 3:
                        yield return StartCoroutine(PatronEspiralArcoirisKogasa());
                        break;
                }
                indicePatron++;
            }

            LimpiarLasersEstrella();
            yield return new WaitForSeconds(pausaEntrePatrones);
        }
    }

    // =========================================================================
    // PATRÓN 1: ABANICO FLORAL BARRIDO (Curva amplia que cruza todo el mapa)
    // =========================================================================
    private IEnumerator PatronEspiralFloral()
    {
        float tiempo = 0f;
        float faseOnda = 0f;
        int brazos = 5;

        while (tiempo < duracionCadaPatron && faseActiva)
        {
            float anguloCentro = ObtenerAnguloHaciaJugador();
            float oscilacion = Mathf.Sin(faseOnda) * (amplitudConoAtaque * 0.22f);

            for (int i = 0; i < brazos; i++)
            {
                float t = (brazos > 1) ? (float)i / (brazos - 1) - 0.5f : 0f;
                float angulo = anguloCentro + oscilacion + (t * amplitudConoAtaque);
                Vector2 dir = Quaternion.Euler(0, 0, angulo) * Vector2.right;

                DispararBala(prefabBalaNormal, transform.position, dir, 12f, 4.5f, 1.0f, new Color(0.4f, 0.9f, 1f));
                DispararBala(prefabBalaNormal, transform.position, dir, 12f, -4.5f, 1.0f, new Color(1f, 0.5f, 0.9f));
            }

            faseOnda += 0.35f;
            tiempo += 0.16f;
            yield return new WaitForSeconds(0.16f);
        }
    }

    // =========================================================================
    // PATRÓN 2: ESTRELLA DE LÁSERES + ARCOS DE BALAS CON HUECO HACIA EL JUGADOR
    // =========================================================================
    private IEnumerator PatronEstrellaLasersYAnillos()
    {
        float tiempo = 0f;
        float rotacionEstrella = 0f;
        float timerArco = 0f;

        PrepararLasersEstrella(5);

        float adv = 0f;
        while (adv < 0.8f)
        {
            adv += Time.deltaTime;
            ActualizarGeometriaEstrella(rotacionEstrella, radioAlcanceMapa * 0.75f, 0.12f, new Color(1f, 0.9f, 0.2f, 0.5f));
            yield return null;
        }

        while (tiempo < duracionCadaPatron && faseActiva)
        {
            float dt = Time.deltaTime;
            tiempo += dt;
            timerArco += dt;
            rotacionEstrella += 22f * dt;

            ActualizarGeometriaEstrella(rotacionEstrella, radioAlcanceMapa * 0.75f, 0.45f, new Color(1f, 0.3f, 0.6f, 1f), true);

            if (timerArco >= 1.0f)
            {
                timerArco = 0f;
                float anguloCentro = ObtenerAnguloHaciaJugador();
                int cantidadBalas = 18;
                int huecoInicio = Random.Range(2, cantidadBalas - 4);

                for (int i = 0; i < cantidadBalas; i++)
                {
                    if (i == huecoInicio || i == huecoInicio + 1 || i == huecoInicio + 2)
                        continue;

                    float t = (float)i / (cantidadBalas - 1) - 0.5f;
                    float ang = anguloCentro + (t * amplitudConoAtaque);
                    Vector2 dir = Quaternion.Euler(0, 0, ang) * Vector2.right;
                    DispararBala(prefabBalaNormal, transform.position, dir, 9.5f, 0f, 1.1f, Color.white);
                }
            }

            yield return null;
        }

        LimpiarLasersEstrella();
    }

    // =========================================================================
    // PATRÓN 3: ORBES GIGANTES + ABANICOS (Concentrados en tu mitad de arena)
    // =========================================================================
    private IEnumerator PatronOrbesGigantesYAbanicos()
    {
        float tiempo = 0f;
        GameObject prefabGrande = (prefabOrbeGigante != null) ? prefabOrbeGigante : prefabBalaNormal;

        while (tiempo < duracionCadaPatron && faseActiva)
        {
            float anguloCentro = ObtenerAnguloHaciaJugador();

            int cantidadOrbes = 6;
            float jitter = Random.Range(-10f, 10f);
            for (int i = 0; i < cantidadOrbes; i++)
            {
                float t = (float)i / (cantidadOrbes - 1) - 0.5f;
                float ang = anguloCentro + jitter + (t * amplitudConoAtaque);
                Vector2 dir = Quaternion.Euler(0, 0, ang) * Vector2.right;
                DispararBala(prefabGrande, transform.position, dir, 7.0f, 0f, 1.0f, new Color(1f, 0.25f, 0.25f));
            }

            for (int j = -2; j <= 2; j++)
            {
                float angAbanico = anguloCentro + (j * 11f);
                Vector2 dirAbanico = Quaternion.Euler(0, 0, angAbanico) * Vector2.right;
                DispararBala(prefabBalaNormal, transform.position, dirAbanico, 12.5f, 0f, 0.9f, Color.yellow);
            }

            tiempo += 1.15f;
            yield return new WaitForSeconds(1.15f);
        }
    }

    // =========================================================================
    // PATRÓN 4: ESPIRAL ARCOÍRIS OPTIMIZADA (Solo instancia balas en tu lado)
    // =========================================================================
    private IEnumerator PatronEspiralArcoirisKogasa()
    {
        float tiempo = 0f;
        float anguloBase = ObtenerAnguloHaciaJugador();
        int brazos = 5;
        float intervaloDisparo = 0.08f;

        while (tiempo < duracionCadaPatron && faseActiva)
        {
            float anguloJugador = ObtenerAnguloHaciaJugador();
            float mitadCono = amplitudConoAtaque * 0.5f;

            for (int i = 0; i < brazos; i++)
            {
                float anguloBrazo = anguloBase + (360f / brazos) * i;

                if (Mathf.Abs(Mathf.DeltaAngle(anguloJugador, anguloBrazo)) > mitadCono)
                    continue;

                Vector2 dir = Quaternion.Euler(0, 0, anguloBrazo) * Vector2.right;

                float hue = Mathf.Repeat((anguloBrazo / 360f) + (tiempo * 0.25f), 1f);
                Color colorArcoiris = Color.HSVToRGB(hue, 0.85f, 1f);

                DispararBala(prefabBalaNormal, transform.position, dir, 13.5f, 0f, 1.05f, colorArcoiris);
            }

            anguloBase += 11f;
            tiempo += intervaloDisparo;
            yield return new WaitForSeconds(intervaloDisparo);
        }
    }

    // =========================================================================
    // PATRÓN 5 (FURIA < 35% VIDA): SUPERNOVA DOBLE ESPIRAL EN 360° COMPLETO
    // =========================================================================
    private IEnumerator PatronSupernovaDobleEspiral()
    {
        float tiempo = 0f;
        float ang1 = 0f;
        float ang2 = 0f;
        int brazos = 8;

        while (tiempo < duracionCadaPatron && faseActiva)
        {
            float pasoAngular = 360f / brazos;

            for (int i = 0; i < brazos; i++)
            {
                Vector2 d1 = Quaternion.Euler(0, 0, ang1 + (pasoAngular * i)) * Vector2.right;
                Vector2 d2 = Quaternion.Euler(0, 0, ang2 + (pasoAngular * i)) * Vector2.right;

                DispararBala(prefabBalaNormal, transform.position, d1, 12.5f, 0f, 1.0f, Color.cyan);
                DispararBala(prefabBalaNormal, transform.position, d2, 10.5f, 0f, 1.0f, new Color(1f, 0.4f, 0.8f));
            }

            ang1 += 13f;
            ang2 -= 17f;
            tiempo += 0.14f;
            yield return new WaitForSeconds(0.14f);
        }
    }

    private void DispararBala(GameObject prefab, Vector3 pos, Vector2 dir, float vel, float giro, float escala, Color color)
    {
        if (prefab == null) return;
        GameObject obj = Instantiate(prefab, pos, Quaternion.identity);
        ProyectilAgujeroBlanco proj = obj.GetComponent<ProyectilAgujeroBlanco>();
        if (proj == null) proj = obj.AddComponent<ProyectilAgujeroBlanco>();

        proj.tiempoVida = (radioAlcanceMapa / Mathf.Max(0.1f, vel)) * 1.1f;
        proj.Configurar(dir, vel, giro, escala, color);
    }

    private void PrepararLasersEstrella(int puntas)
    {
        while (lasersEstrella.Count < puntas)
        {
            GameObject go = new GameObject("LaserEstrella_" + lasersEstrella.Count);
            go.transform.SetParent(transform);
            LineRenderer lr = go.AddComponent<LineRenderer>();
            lr.positionCount = 2;
            lr.useWorldSpace = true;
            if (materialLaserEstrella != null) lr.material = materialLaserEstrella;
            lr.sortingLayerName = sortingLayerLasers;
            lr.sortingOrder = orderInLayerLasers;
            lasersEstrella.Add(lr);
        }
    }

    private void ActualizarGeometriaEstrella(float rotacionBase, float radioPuntas, float grosor, Color color, bool hacerDano = false)
    {
        int n = lasersEstrella.Count;
        Vector3[] vertices = new Vector3[n];

        for (int i = 0; i < n; i++)
        {
            float ang = (rotacionBase + (360f / n) * i) * Mathf.Deg2Rad;
            vertices[i] = transform.position + new Vector3(Mathf.Cos(ang), Mathf.Sin(ang), 0f) * radioPuntas;
        }

        for (int i = 0; i < n; i++)
        {
            LineRenderer lr = lasersEstrella[i];
            lr.enabled = true;
            lr.startWidth = grosor;
            lr.endWidth = grosor;
            lr.startColor = color;
            lr.endColor = color;

            Vector3 pA = vertices[i];
            Vector3 pB = vertices[(i + 2) % n];
            lr.SetPosition(0, pA);
            lr.SetPosition(1, pB);

            if (hacerDano)
            {
                Vector3 dir = (pB - pA).normalized;
                float dist = Vector3.Distance(pA, pB);
                RaycastHit2D[] hits = Physics2D.CircleCastAll(pA, grosor * 0.35f, dir, dist);
                foreach (var hit in hits)
                {
                    if (hit.collider != null && (hit.collider.CompareTag("Player") || hit.transform.root.CompareTag("Player")))
                    {
                        hit.transform.root.BroadcastMessage("RecibirDaño", 20, SendMessageOptions.DontRequireReceiver);
                    }
                }
            }
        }
    }

    private void LimpiarLasersEstrella()
    {
        foreach (var lr in lasersEstrella)
        {
            if (lr != null) lr.enabled = false;
        }
    }

    private void DerrotarAgujeroBlanco()
    {
        if (!faseActiva) return;
        faseActiva = false;
        StopAllCoroutines();
        LimpiarLasersEstrella();

        if (prefabExplosionFinal != null)
        {
            Instantiate(prefabExplosionFinal, transform.position, Quaternion.identity);
        }

        ProyectilAgujeroBlanco[] balasRestantes = FindObjectsByType<ProyectilAgujeroBlanco>(FindObjectsSortMode.None);
        foreach (var b in balasRestantes) Destroy(b.gameObject);

        gameObject.SetActive(false);
    }
}