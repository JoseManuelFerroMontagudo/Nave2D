using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

public class MenuVictoriaController : MonoBehaviour
{
    // -------- Estado estático (pausa global) --------
    static readonly List<MenuVictoriaController> abiertos = new List<MenuVictoriaController>();
    static MenuVictoriaController instancia;

    // -------- Referencias (Inspector) --------
    [Header("UI (arrastrar desde el menú)")]
    public TextMeshProUGUI textoTitulo;
    public Image imagen;
    public TextMeshProUGUI textoPuntos;   // muestra Puntos + Récord
    public Button btnConfirmar;
    public Button btnVolver;

    [Header("Botón externo para reabrir (opcional)")]
    [Tooltip("Botón del Canvas que se activa al pulsar 'Volver' para reabrir el menú.")]
    public GameObject botonReabrirMenu;

    // -------- Estado runtime --------
    SpawnerOleadas spawner;
    Stats estadisticas;
    int numeroNivel;
    bool confirmado;

    // ============================================================
    //  API estática
    // ============================================================
    public static MenuVictoriaController Abrir(SpawnerOleadas s, GameObject botonReabrir)
    {
        if (instancia == null)
        {
            Debug.LogError("[MenuVictoria] No hay instancia en la escena. " +
                           "Asegúrate de que el menú esté presente y activo al menos una vez.");
            return null;
        }

        instancia.spawner = s;
        instancia.numeroNivel = s != null ? s.numeroNivel : 0;
        instancia.estadisticas = s != null ? s.EstadisticasJugador : null;
        instancia.confirmado = false;

        if (instancia.botonReabrirMenu == null)
            instancia.botonReabrirMenu = botonReabrir;

        instancia.gameObject.SetActive(true);
        instancia.RefrescarContenido();
        instancia.Mostrar();

        return instancia;
    }

    /// Reabre el menú (llamado desde el botón externo).
    public static void ReabrirExistente()
    {
        if (instancia == null || instancia.confirmado) return;

        instancia.gameObject.SetActive(true);
        if (!abiertos.Contains(instancia)) abiertos.Add(instancia);

        instancia.RefrescarContenido();
        if (instancia.botonReabrirMenu != null)
            instancia.botonReabrirMenu.SetActive(false);

        ActualizarPausa();
    }

    static void ActualizarPausa()
    {
        Time.timeScale = abiertos.Count > 0 ? 0f : 1f;
    }

    // ============================================================
    //  Ciclo de vida
    // ============================================================
    void Awake()
    {
        instancia = this;
        if (botonReabrirMenu != null) botonReabrirMenu.SetActive(false);
        gameObject.SetActive(false);   // oculto por defecto hasta que el spawner lo abra
    }

    void OnEnable()
    {
        if (btnConfirmar != null)
        {
            btnConfirmar.onClick.RemoveListener(Confirmar);
            btnConfirmar.onClick.AddListener(Confirmar);
        }
        if (btnVolver != null)
        {
            btnVolver.onClick.RemoveListener(Volver);
            btnVolver.onClick.AddListener(Volver);
        }
    }

    void OnDisable()
    {
        if (btnConfirmar != null) btnConfirmar.onClick.RemoveListener(Confirmar);
        if (btnVolver != null) btnVolver.onClick.RemoveListener(Volver);

        abiertos.Remove(this);
        ActualizarPausa();
    }

    // ============================================================
    //  Contenido
    // ============================================================
    void RefrescarContenido()
    {
        // Título
        if (textoTitulo != null)
            textoTitulo.text = numeroNivel > 0
                ? $"Nivel {numeroNivel} Victoria"
                : "Victoria";

        // Descripción (puntos + récord en el mismo TMP)
        if (textoPuntos != null)
        {
            int pts = estadisticas != null ? estadisticas.puntos : 0;
            int record = ObtenerRecordActual();
            textoPuntos.text = $"Puntos: {pts}\nRécord: {record}";
        }

        // Imagen: se configura por Inspector, no se toca.
    }

    int ObtenerRecordActual()
    {
        if (numeroNivel <= 0) return 0;
        return PlayerPrefs.GetInt("PuntosNivel_" + numeroNivel, 0);
    }

    void Mostrar()
    {
        if (!abiertos.Contains(this)) abiertos.Add(this);
        if (botonReabrirMenu != null) botonReabrirMenu.SetActive(false);
        ActualizarPausa();
    }

    // ============================================================
    //  Botones
    // ============================================================
    void Confirmar()
    {
        if (confirmado) return;
        confirmado = true;

        int pts = estadisticas != null ? estadisticas.puntos : 0;

        if (numeroNivel > 0)
        {
            string key = "PuntosNivel_" + numeroNivel;
            int guardado = PlayerPrefs.GetInt(key, 0);
            if (pts > guardado)
            {
                PlayerPrefs.SetInt(key, pts);
                PlayerPrefs.Save();
                Debug.Log($"[MenuVictoria] Récord nivel {numeroNivel}: {pts} (antes {guardado}).");
            }
            else
            {
                Debug.Log($"[MenuVictoria] Nivel {numeroNivel}: {pts} pts (récord {guardado}, no superado).");
            }
        }

        if (estadisticas != null) estadisticas.ReiniciarPuntos();

        // NO ocultar: la escena se lleva el Canvas (evita StartCoroutine en GO inactivo).
        abiertos.Remove(this);
        if (botonReabrirMenu != null) botonReabrirMenu.SetActive(false);

        SaltarSiguienteNivel();
    }

    void Volver()
    {
        CerrarYOcultar();
        if (botonReabrirMenu != null) botonReabrirMenu.SetActive(true);
    }

    void CerrarYOcultar()
    {
        abiertos.Remove(this);
        ActualizarPausa();
        gameObject.SetActive(false);
    }

    // ============================================================
    //  Cambio de escena
    // ============================================================
    void SaltarSiguienteNivel()
    {
        if (spawner == null)
        {
            Debug.LogWarning("[MenuVictoria] Sin spawner: no se cambia de escena.");
            return;
        }

        string escena = spawner.escenaAlDerrotarJefe;
        if (string.IsNullOrEmpty(escena))
        {
            Debug.Log("[MenuVictoria] Sin escena configurada: nivel cerrado sin cambio.");
            return;
        }

        StartCoroutine(CambiarEscenaRutina(escena, spawner.usarLoadSingle, spawner.retardoCambioEscena));
    }

    IEnumerator CambiarEscenaRutina(string escena, bool single, float retardo)
    {
        // Restaurar pausa ya (evita timeScale=0 si el retardo es largo o falla la carga)
        ActualizarPausa();

        if (retardo > 0f)
            yield return new WaitForSecondsRealtime(retardo);

        Time.timeScale = 1f;

        if (SceneLoader.Instance != null)
        {
            if (single) SceneLoader.Instance.LoadSingleQuick(escena);
            else SceneLoader.Instance.LoadLevel(escena);
        }
        else
        {
            SceneManager.LoadScene(escena,
                single ? LoadSceneMode.Single : LoadSceneMode.Additive);
        }
    }
}