using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

public class MenuUI : MonoBehaviour
{
    public string EscenaPersistente = "PlayerOnly";
    public string EscenaCreditos = "Credits";

    [Header("Botones")]
    public Button botonNuevaPartida;
    public TMP_Text textoBotonNuevaPartida;
    public Button botonNiveles;

    [Header("Niveles (índice 0 = nivel 1)")]
    public string[] escenasNivel = { "Nivel1", "Nivel2", "Nivel3", "Nivel4" };

    [Header("Debug")]
    public Toggle toggleDebugUnlock;
    public int debugNivelMaximo = 3;

    void Start()
    {
        ActualizarBotones();

        if (toggleDebugUnlock != null)
        {
            toggleDebugUnlock.SetIsOnWithoutNotify(
                PlayerPrefs.GetInt(ProgressManager.KEY_DEBUG_UNLOCK, 0) > 0);
            toggleDebugUnlock.onValueChanged.AddListener(OnDebugToggleChanged);
        }
    }

    int MaxEfectivo()
    {
        int maxReal = PlayerPrefs.GetInt(ProgressManager.KEY_NIVEL_MAX, 0);
        int maxDebug = PlayerPrefs.GetInt(ProgressManager.KEY_DEBUG_UNLOCK, 0);
        return Mathf.Max(maxReal, maxDebug);
    }

    void ActualizarBotones()
    {
        int max = MaxEfectivo();

        // "Nueva Partida" vs "Continuar"
        bool continuar = max > 1;
        if (textoBotonNuevaPartida != null)
            textoBotonNuevaPartida.text = continuar ? "Continuar" : "Nueva Partida";

        // "Niveles": solo si hay algo más que el nivel 1
        if (botonNiveles != null)
            botonNiveles.interactable = max > 1;
    }

    // ---------- Botones ----------

    public void NuevaPartida()
    {
        int maxReal = PlayerPrefs.GetInt(ProgressManager.KEY_NIVEL_MAX, 0);
        int max = MaxEfectivo();

        string escena;
        if (maxReal <= 1 && max <= 1)
        {
            PlayerPrefs.SetInt(ProgressManager.KEY_NIVEL_MAX, 1);
            PlayerPrefs.Save();
            escena = escenasNivel.Length > 0 ? escenasNivel[0] : "Nivel1";
        }
        else
        {
            int idx = Mathf.Clamp(max - 1, 0, escenasNivel.Length - 1);
            escena = escenasNivel[idx];
        }

        CargarJuegoConNivel(escena);
    }

    public void AbrirNiveles()
    {
        if (botonNiveles != null && !botonNiveles.interactable) return;
        CargarJuegoConNivel("SelectorNiveles");
    }

    void CargarJuegoConNivel(string nivel)
    {
        if (TransitionManager.Instance == null)
        {
            Debug.LogError("[MenuUI] Falta el prefab TransitionOverlay en MenuPrincipal.");
            return;
        }
        TransitionManager.Instance.LoadSingleWithFollowUp(EscenaPersistente, nivel);
    }

    public void AbrirCreditos() => SceneManager.LoadScene(EscenaCreditos);

    public void Salir()
    {
        Application.Quit();
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#endif
    }

    void OnDebugToggleChanged(bool activo)
    {
        PlayerPrefs.SetInt(ProgressManager.KEY_DEBUG_UNLOCK, activo ? debugNivelMaximo : 0);
        PlayerPrefs.Save();
        ActualizarBotones();
    }
}