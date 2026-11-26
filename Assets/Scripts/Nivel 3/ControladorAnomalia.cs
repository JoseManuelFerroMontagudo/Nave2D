using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class ControladorAnomalia : MonoBehaviour
{
    [Header("Línea de Tiempo de la Fase Agujero Negro")]
    [Tooltip("Duración total del Agujero Negro antes de explotar al Agujero Blanco (ej. 55s o 60s)")]
    public float tiempoParaTransicion = 55f;
    public float segundoInicioFrenesiAsteroides = 5f;
    public float segundoInicioSecuenciaLasers = 15f;
    public float tiempoAlarmaInestable = 10f; // Últimos 10s de temblor antes de explotar

    [Header("Sistemas de Láser Coordinados")]
    public LaserAnomalia laserAnomalia;
    public MaquinaLasers maquinaLasers;
    public float pausaEntreAtaquesLaser = 2.0f;

    private float timerTurnoLaser = 0f;
    private bool turnoMaquinaLasers = false;
    private int cicloDeCombate = 1;

    [Header("Efectos de Inestabilidad (Shake & Pulse)")]
    public float intensidadTemblor = 0.15f;
    public float velocidadPulsacion = 12f;
    public float escalaPulsacion = 0.2f;

    [Header("Radio de Absorción y Muerte")]
    [Tooltip("Ajusta este radio en el Editor (círculo rojo) para que coincida con el borde del anillo de fuego")]
    public float radioMuerte = 7.5f;
    public float velocidadSuccionMuerte = 15f;

    [Header("Referencias de Sprites")]
    public GameObject spriteAgujeroNegro;
    public GameObject spriteAgujeroBlanco;
    private SpriteRenderer rendererAgujeroNegro;

    [Header("Efecto de Luz Cegadora (UI)")]
    public Image imagenFlashUI;
    public ParticleSystem FXExplosion;

    [Header("Tiempos de Transición")]
    public float velocidadImplosion = 5f;
    public float duracionCeguera = 0.15f;
    public float velocidadDesvanecimientoFlash = 2f;

    [Header("Escena de Derrota")]
    public string nombreEscenaGameOver = "GameOver";

    private Transform jugador;
    private SpawnerAnomalia spawnerAsteroides;
    private float tiempoRestante;
    private float tiempoTranscurrido = 0f;

    private Vector3 posicionOriginal;
    private Vector3 escalaOriginal;
    private bool enFaseAgujeroBlanco = false;
    private bool cambiandoDeFase = false;
    private bool jugadorAbsorbido = false;

    private bool frenesiAsteroidesActivado = false;
    private bool secuenciaLasersIniciada = false;

    private void Start()
    {
        tiempoRestante = tiempoParaTransicion;
        posicionOriginal = transform.position;
        escalaOriginal = transform.localScale;
        spawnerAsteroides = GetComponentInChildren<SpawnerAnomalia>();

        if (spriteAgujeroNegro != null)
        {
            spriteAgujeroNegro.SetActive(true);
            rendererAgujeroNegro = spriteAgujeroNegro.GetComponent<SpriteRenderer>();
        }

        if (spriteAgujeroBlanco != null) spriteAgujeroBlanco.SetActive(false);
        if (FXExplosion != null) FXExplosion.Stop();

        if (imagenFlashUI != null)
        {
            Color c = imagenFlashUI.color;
            c.a = 0f;
            imagenFlashUI.color = c;
        }

        StartCoroutine(BuscarJugador());
    }

    private IEnumerator BuscarJugador()
    {
        while (jugador == null)
        {
            GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
            if (playerObj != null)
            {
                Rigidbody2D rb = playerObj.GetComponentInChildren<Rigidbody2D>();
                jugador = (rb != null) ? rb.transform : playerObj.transform;
            }
            yield return null;
        }
    }

    private void Update()
    {
        if (enFaseAgujeroBlanco || cambiandoDeFase) return;

        tiempoTranscurrido += Time.deltaTime;
        tiempoRestante -= Time.deltaTime;

        // 1. SEGUNDO 5: Activa el frenesí de absorción de asteroides
        if (tiempoTranscurrido >= segundoInicioFrenesiAsteroides && !frenesiAsteroidesActivado)
        {
            frenesiAsteroidesActivado = true;
            if (spawnerAsteroides != null) spawnerAsteroides.ActivarModoFrenesi();
        }

        // 2. SEGUNDO 15: Detiene los asteroides e inicia la secuencia de láseres
        if (tiempoTranscurrido >= segundoInicioSecuenciaLasers && !secuenciaLasersIniciada)
        {
            secuenciaLasersIniciada = true;
            if (spawnerAsteroides != null) spawnerAsteroides.DetenerGeneracion();
        }

        // 3. Ejecutar láseres únicamente a partir del segundo 15
        if (secuenciaLasersIniciada)
        {
            GestionarAtaquesLaserCoordinados();
        }

        // 4. Efectos visuales de inestabilidad en los últimos 10 segundos antes del Agujero Blanco
        if (tiempoRestante <= tiempoAlarmaInestable && tiempoRestante > 0f)
        {
            AplicarEfectosInestabilidad();
        }

        if (tiempoRestante <= 0f)
        {
            IniciarCambioAAgujeroBlanco();
            return;
        }

        // 5. Detección de entrada al Anillo de Fuego / Radio de Muerte
        if (jugador != null && !jugadorAbsorbido)
        {
            float distancia = Vector2.Distance(transform.position, jugador.position);
            if (distancia <= radioMuerte)
            {
                AbsorberYMatarJugador();
            }
        }
    }

    private void GestionarAtaquesLaserCoordinados()
    {
        if (jugador == null || laserAnomalia == null || maquinaLasers == null) return;

        if (laserAnomalia.EstaDisparando() || maquinaLasers.EstaActiva())
        {
            return;
        }

        bool enInestabilidadFinal = (tiempoRestante <= tiempoAlarmaInestable);
        float pausaActual = enInestabilidadFinal ? pausaEntreAtaquesLaser * 0.65f : pausaEntreAtaquesLaser;
        timerTurnoLaser += Time.deltaTime;

        if (timerTurnoLaser >= pausaActual)
        {
            timerTurnoLaser = 0f;

            if (!turnoMaquinaLasers)
            {
                laserAnomalia.DispararAlJugador(jugador);

                // Cada ciclo tiene 4 niveles de escalado antes de ceder el turno a la Máquina Giratoria
                if (laserAnomalia.nivelAtaqueActual >= 4)
                {
                    turnoMaquinaLasers = true;
                }
            }
            else
            {
                // Mantiene tu configuración personalizada de 20 rayos al terminar el Ciclo 2
                if (cicloDeCombate == 2)
                {
                    maquinaLasers.ConfigurarDificultad(20, 22f);
                }

                maquinaLasers.ActivarMaquina();

                // Avanzamos al siguiente ciclo (1 -> 2 Zig-Zag -> 3 Triangulación con Mini-Agujeros)
                cicloDeCombate++;
                laserAnomalia.AvanzarDeCiclo(cicloDeCombate, jugador);
                turnoMaquinaLasers = false;
            }
        }
    }

    private void AplicarEfectosInestabilidad()
    {
        float factorUrgencia = 1f - (tiempoRestante / tiempoAlarmaInestable);

        float offsetX = (Mathf.PerlinNoise(Time.time * 25f, 0f) - 0.5f) * 2f * intensidadTemblor * factorUrgencia;
        float offsetY = (Mathf.PerlinNoise(0f, Time.time * 25f) - 0.5f) * 2f * intensidadTemblor * factorUrgencia;
        transform.position = posicionOriginal + new Vector3(offsetX, offsetY, 0f);

        float pulso = Mathf.Sin(Time.time * velocidadPulsacion * (1f + factorUrgencia)) * escalaPulsacion * factorUrgencia;
        transform.localScale = escalaOriginal + (Vector3.one * pulso);

        if (rendererAgujeroNegro != null)
        {
            float alphaColor = (Mathf.Sin(Time.time * velocidadPulsacion) + 1f) / 2f;
            rendererAgujeroNegro.color = Color.Lerp(Color.white, new Color(1f, 0.4f, 0.4f), alphaColor * factorUrgencia);
        }
    }

    private void AbsorberYMatarJugador()
    {
        if (jugadorAbsorbido) return;
        jugadorAbsorbido = true;

        MonoBehaviour controlNave = jugador.GetComponent<MonoBehaviour>();
        if (controlNave != null) controlNave.enabled = false;

        Rigidbody2D rbJugador = jugador.GetComponent<Rigidbody2D>();
        if (rbJugador != null) rbJugador.linearVelocity = Vector2.zero;

        StartCoroutine(RutinaAnimacionAbsorcion());
    }

    private IEnumerator RutinaAnimacionAbsorcion()
    {
        float t = 0f;
        Vector3 escalaInicialJugador = jugador.localScale;
        Vector3 posInicialJugador = jugador.position;

        while (t < 1f)
        {
            t += Time.deltaTime * 2.5f;
            jugador.position = Vector3.Lerp(posInicialJugador, transform.position, t);
            jugador.localScale = Vector3.Lerp(escalaInicialJugador, Vector3.zero, t);
            yield return null;
        }

        jugador.gameObject.SetActive(false);
        yield return new WaitForSeconds(0.3f);

        SceneManager.LoadScene(nombreEscenaGameOver);
    }

    public void IniciarCambioAAgujeroBlanco()
    {
        if (!cambiandoDeFase)
        {
            transform.position = posicionOriginal;
            StartCoroutine(RutinaTransicionCegadora());
        }
    }

    private IEnumerator RutinaTransicionCegadora()
    {
        cambiandoDeFase = true;

        if (maquinaLasers != null) maquinaLasers.DetenerMaquina();
        if (laserAnomalia != null) laserAnomalia.LimpiarMiniAgujeros();

        float t = 0f;
        while (t < 1f)
        {
            t += Time.deltaTime * velocidadImplosion;
            transform.localScale = Vector3.Lerp(escalaOriginal, Vector3.one * 0.01f, t);

            if (imagenFlashUI != null)
            {
                Color c = imagenFlashUI.color;
                c.a = Mathf.Lerp(0f, 0.4f, t);
                imagenFlashUI.color = c;
            }
            yield return null;
        }

        if (imagenFlashUI != null)
        {
            Color c = imagenFlashUI.color;
            c.a = 1f;
            imagenFlashUI.color = c;
        }

        if (spawnerAsteroides != null)
        {
            spawnerAsteroides.DetenerGeneracion();
        }

        if (spriteAgujeroNegro != null) spriteAgujeroNegro.SetActive(false);
        if (spriteAgujeroBlanco != null) spriteAgujeroBlanco.SetActive(true);
        if (FXExplosion != null) FXExplosion.Play();

        enFaseAgujeroBlanco = true;
        transform.localScale = escalaOriginal * 1.3f;
        ControladorAgujeroBlanco faseBlanca = GetComponent<ControladorAgujeroBlanco>();
        if (faseBlanca != null) faseBlanca.ActivarFaseAgujeroBlanco();

        yield return new WaitForSeconds(duracionCeguera);

        float tiempoFlash = 0f;
        while (tiempoFlash < 1f)
        {
            tiempoFlash += Time.deltaTime * velocidadDesvanecimientoFlash;

            if (imagenFlashUI != null)
            {
                Color c = imagenFlashUI.color;
                c.a = Mathf.Lerp(1f, 0f, tiempoFlash);
                imagenFlashUI.color = c;
            }

            transform.localScale = Vector3.Lerp(escalaOriginal * 1.3f, escalaOriginal, tiempoFlash);
            yield return null;
        }

        cambiandoDeFase = false;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, radioMuerte);
    }
}