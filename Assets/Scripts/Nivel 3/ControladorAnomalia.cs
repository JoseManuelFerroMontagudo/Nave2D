using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class ControladorAnomalia : MonoBehaviour
{
    [Header("Supervivencia por Tiempo")]
    public float tiempoParaTransicion = 30f;
    public float tiempoAlarmaInestable = 10f;

    [Header("Sistemas de Láser Coordinados")]
    public LaserAnomalia laserAnomalia;
    public MaquinaLasers maquinaLasers;
    public float pausaEntreAtaquesLaser = 2.0f; // Tiempo de respiro entre cada ataque (en frenesí baja a la mitad)

    private float timerTurnoLaser = 0f;
    private bool turnoMaquinaLasers = false;

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
    private float tiempoRestante;
    private Vector3 posicionOriginal;
    private Vector3 escalaOriginal;
    private bool enFaseAgujeroBlanco = false;
    private bool cambiandoDeFase = false;
    private bool jugadorAbsorbido = false;
    private bool frenesiActivado = false;
    private int cicloDeCombate = 1;

    private void Start()
    {
        tiempoRestante = tiempoParaTransicion;
        posicionOriginal = transform.position;
        escalaOriginal = transform.localScale;

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

        tiempoRestante -= Time.deltaTime;

        if (tiempoRestante <= tiempoAlarmaInestable && tiempoRestante > 0f)
        {
            AplicarEfectosInestabilidad();
        }

        if (tiempoRestante <= 0f)
        {
            IniciarCambioAAgujeroBlanco();
            return;
        }

        // Detección de entrada al Anillo de Fuego / Radio de Muerte
        if (jugador != null && !jugadorAbsorbido)
        {
            float distancia = Vector2.Distance(transform.position, jugador.position);
            if (distancia <= radioMuerte)
            {
                AbsorberYMatarJugador();
            }
        }

        // Ejecutar la secuencia coordinada entre LaserAnomalia (escalado) y MaquinaLasers
        GestionarAtaquesLaserCoordinados();
    }

    private void GestionarAtaquesLaserCoordinados()
    {
        if (jugador == null || laserAnomalia == null || maquinaLasers == null) return;

        if (laserAnomalia.EstaDisparando() || maquinaLasers.EstaActiva())
        {
            return;
        }

        float pausaActual = frenesiActivado ? pausaEntreAtaquesLaser * 0.55f : pausaEntreAtaquesLaser;
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
                // Si estamos terminando el Ciclo 2, potenciamos la Máquina de Láseres (6 rayos más rápidos)
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
        if (!frenesiActivado)
        {
            frenesiActivado = true;
            SpawnerAnomalia spawner = GetComponentInChildren<SpawnerAnomalia>();
            if (spawner != null)
            {
                spawner.ActivarModoFrenesi();
            }
        }

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

        // Detener cualquier láser activo durante la transición al Agujero Blanco
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

        SpawnerAnomalia spawner = GetComponentInChildren<SpawnerAnomalia>();
        if (spawner != null)
        {
            spawner.DetenerGeneracion();
        }

        if (spriteAgujeroNegro != null) spriteAgujeroNegro.SetActive(false);
        if (spriteAgujeroBlanco != null) spriteAgujeroBlanco.SetActive(true);
        if (FXExplosion != null) FXExplosion.Play();

        enFaseAgujeroBlanco = true;
        transform.localScale = escalaOriginal * 1.3f;

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