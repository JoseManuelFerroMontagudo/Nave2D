using UnityEngine;

public class ProgressManager : MonoBehaviour
{
    public static ProgressManager Instance { get; private set; }

    public const string KEY_NIVEL_MAX = "NivelMaxDesbloqueado";
    public const string KEY_DEBUG_UNLOCK = "DebugUnlockNivelMax"; // 0=off, N=desbloquea hasta N

    public int NivelMaxDesbloqueado { get; private set; }

    /// <summary>Máximo efectivo contando debug unlock.</summary>
    public int NivelMaxEfectivo =>
        Mathf.Max(NivelMaxDesbloqueado, PlayerPrefs.GetInt(KEY_DEBUG_UNLOCK, 0));

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        NivelMaxDesbloqueado = PlayerPrefs.GetInt(KEY_NIVEL_MAX, 0);
    }

    public bool EstaDesbloqueado(int numeroNivel)
    {
        int debug = PlayerPrefs.GetInt(KEY_DEBUG_UNLOCK, 0);
        if (debug > 0) return numeroNivel <= debug;
        return numeroNivel <= NivelMaxDesbloqueado;
    }

    public void CompletarNivel(int numeroNivel)
    {
        int nuevo = Mathf.Max(NivelMaxDesbloqueado, numeroNivel + 1);
        if (nuevo == NivelMaxDesbloqueado) return;
        NivelMaxDesbloqueado = nuevo;
        PlayerPrefs.SetInt(KEY_NIVEL_MAX, nuevo);
        PlayerPrefs.Save();
    }

    public void ResetearProgreso()
    {
        NivelMaxDesbloqueado = 0;
        PlayerPrefs.SetInt(KEY_NIVEL_MAX, 0);
        PlayerPrefs.Save();
    }
}