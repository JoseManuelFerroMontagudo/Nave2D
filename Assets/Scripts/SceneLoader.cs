using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneLoader : MonoBehaviour
{
    public static SceneLoader Instance { get; private set; }
    private string currentLevel;

    void Awake() => Instance = this;

    public void SetCurrentLevel(string scene) => currentLevel = scene;

    // Niveles: con overlay (oculta el unload+load)
    public void LoadLevel(string sceneName)
    {
        string prev = currentLevel;
        currentLevel = sceneName;
        if (TransitionManager.Instance != null)
            TransitionManager.Instance.LoadWithOverlayOnly(sceneName, single: false, unload: prev);
        else
            StartCoroutine(FallbackLoad(sceneName, single: false, unload: prev));
    }

    // Volver a menú, morir, créditos: SIN overlay (Single es atómico, no hay hueco)
    public void LoadSingleQuick(string sceneName)
    {
        currentLevel = null;
        if (TransitionManager.Instance != null)
            TransitionManager.Instance.LoadDirect(sceneName, single: true);
        else
            SceneManager.LoadScene(sceneName);
    }

    IEnumerator FallbackLoad(string scene, bool single, string unload)
    {
        if (!string.IsNullOrEmpty(unload) && SceneManager.GetSceneByName(unload).isLoaded)
        {
            var u = SceneManager.UnloadSceneAsync(unload);
            while (u != null && !u.isDone) yield return null;
        }
        yield return SceneManager.LoadSceneAsync(scene,
            single ? LoadSceneMode.Single : LoadSceneMode.Additive);
    }

    public void Salir()
    {
        Application.Quit();
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#endif
    }
}