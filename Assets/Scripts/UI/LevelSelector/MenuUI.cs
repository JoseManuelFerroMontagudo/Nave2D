using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class MenuUI : MonoBehaviour
{
    [Tooltip("Escena persistente (jugador + HUD + managers).")]
    public string EscenaPersistente = "EscenaJugador";

    [Tooltip("Escena de créditos.")]
    public string EscenaCreditos = "Creditos";

    [Header("Botones")]
    public Button botonContinuar; // se deshabilita si no hay progreso ni debug

    [Header("Debug")]
    [Tooltip("Activa un unlock temporal hasta debugNivelMaximo, " +
             "sin tocar el progreso real. Útil para testear el selector.")]
    public Toggle toggleDebugUnlock;
    public int debugNivelMaximo = 3; // de 4 niveles → desbloquea 1, 2 y 3; el 4 sigue bloqueado

    void Start()
    {
        ActualizarBotonContinuar();

        if (toggleDebugUnlock != null)
        {
            // Estado inicial leyendo PlayerPrefs; SetIsOnWithoutNotify evita disparar
            // el listener mientras inicializamos.
            toggleDebugUnlock.SetIsOnWithoutNotify(
                PlayerPrefs.GetInt(ProgressManager.KEY_DEBUG_UNLOCK, 0) > 0);

            toggleDebugUnlock.onValueChanged.AddListener(OnDebugToggleChanged);
        }
    }

    void ActualizarBotonContinuar()
    {
        if (botonContinuar == null) return;

        int maxReal = PlayerPrefs.GetInt(ProgressManager.KEY_NIVEL_MAX, 0);
        int maxDebug = PlayerPrefs.GetInt(ProgressManager.KEY_DEBUG_UNLOCK, 0);
        botonContinuar.interactable = Mathf.Max(maxReal, maxDebug) > 0;
    }

    // ---- Botones ----

    public void NuevaPartida()
    {
        PlayerPrefs.SetInt(ProgressManager.KEY_NIVEL_MAX, 1); // Nivel 1 desbloqueado
        PlayerPrefs.Save();

        ProgressManager.SetEscenaPendiente("Nivel_1");
        SceneManager.LoadScene(EscenaPersistente); // Single directo (sin SceneLoader)
    }

    public void Continuar()
    {
        if (!botonContinuar.interactable) return;

        ProgressManager.SetEscenaPendiente("SelectorNiveles");
        SceneManager.LoadScene(EscenaPersistente);
    }

    public void AbrirCreditos()
    {
        SceneManager.LoadScene(EscenaCreditos); // Single, reemplaza el menú
    }

    public void Salir()
    {
        Application.Quit();
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#endif
    }

    // ---- Debug ----

    void OnDebugToggleChanged(bool activo)
    {
        PlayerPrefs.SetInt(ProgressManager.KEY_DEBUG_UNLOCK,
                           activo ? debugNivelMaximo : 0);
        PlayerPrefs.Save();

        ActualizarBotonContinuar();
    }
}