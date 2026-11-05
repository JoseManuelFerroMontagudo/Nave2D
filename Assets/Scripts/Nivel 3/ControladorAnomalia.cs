using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class ControladorAnomalia : MonoBehaviour
{
    [Header("Supervivencia por Tiempo")]
    public float tiempoParaTransicion = 40f;   // Tiempo total antes del colapso
    public float tiempoAlarmaInestable = 10f; // Últimos X segundos donde empieza a temblar y pulsar

    [Header("Efectos de Inestabilidad (Shake & Pulse)")]
    public float intensidadTemblor = 0.15f;  // Qué tanto se sacude
    public float velocidadPulsacion = 12f;   // Rápidez del latido
    public float escalaPulsacion = 0.2f;      // Cuánto crece/decrece en cada latido

    [Header("Radio de Muerte Absoluta")]
    public float radioMuerte = 2.5f;

    [Header("Referencias de Sprites y Renderers")]
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
    public float retardoGameOver = 1f;

    private Transform jugador;
    private float tiempoRestante;
    private Vector3 posicionOriginal;
    private Vector3 escalaOriginal;
    private bool enFaseAgujeroBlanco = false;
    private bool cambiandoDeFase = false;
    private bool jugadorAbsorbido = false;

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

        // 1. Conteo de tiempo global
        tiempoRestante -= Time.deltaTime;

        // 2. Efecto de Inestabilidad (Últimos segundos)
        if (tiempoRestante <= tiempoAlarmaInestable && tiempoRestante > 0f)
        {
            AplicarEfectosInestabilidad();
        }

        // 3. Detonación automática al agotarse el tiempo
        if (tiempoRestante <= 0f)
        {
            IniciarCambioAAgujeroBlanco();
        }

        // 4. Detección de absorción del jugador
        if (jugador != null && !jugadorAbsorbido)
        {
            float distancia = Vector2.Distance(transform.position, jugador.position);
            if (distancia <= radioMuerte)
            {
                AbsorberYMatarJugador();
            }
        }
    }

    private void AplicarEfectosInestabilidad()
    {
        // Factor de urgencia de 0 a 1 (conforme se acerca a 0 sube la intensidad)
        float factorUrgencia = 1f - (tiempoRestante / tiempoAlarmaInestable);

        // A. TEMBLOR (SHAKE)
        float offsetX = (Mathf.PerlinNoise(Time.time * 25f, 0f) - 0.5f) * 2f * intensidadTemblor * factorUrgencia;
        float offsetY = (Mathf.PerlinNoise(0f, Time.time * 25f) - 0.5f) * 2f * intensidadTemblor * factorUrgencia;
        transform.position = posicionOriginal + new Vector3(offsetX, offsetY, 0f);

        // B. PULSACIÓN DE ESCALA (LATIDO)
        float pulso = Mathf.Sin(Time.time * velocidadPulsacion * (1f + factorUrgencia)) * escalaPulsacion * factorUrgencia;
        transform.localScale = escalaOriginal + (Vector3.one * pulso);

        // C. FLASH ROJO EN EL SPRITE (Opcional: destello suave de advertencia)
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

        if (jugador != null)
        {
            jugador.gameObject.SetActive(false);
        }

        StartCoroutine(RutinaCargarGameOver());
    }

    private IEnumerator RutinaCargarGameOver()
    {
        yield return new WaitForSeconds(retardoGameOver);
        SceneManager.LoadScene(nombreEscenaGameOver);
    }

    public void IniciarCambioAAgujeroBlanco()
    {
        if (!cambiandoDeFase)
        {
            // Restaurar posición base antes de implosionar
            transform.position = posicionOriginal;
            StartCoroutine(RutinaTransicionCegadora());
        }
    }

    private IEnumerator RutinaTransicionCegadora()
    {
        cambiandoDeFase = true;
        float t = 0f;

        // 1. IMPLOSIÓN
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

        // 2. DETONACIÓN LUMINOSA
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

        // 3. DESVANECIMIENTO DE LUZ
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