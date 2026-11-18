using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class BootstrapJuego : MonoBehaviour
{
    public string EscenaFallback = "Nivel1";

    IEnumerator Start()
    {
        yield return null; // esperar 1 frame: managers listos

        string pendiente = ProgressManager.ConsumirEscenaPendiente(EscenaFallback);

        if (SceneLoader.Instance != null)
            SceneLoader.Instance.LoadLevel(pendiente);
        else
            SceneManager.LoadScene(pendiente, LoadSceneMode.Additive);
    }
}