using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneLoader : MonoBehaviour
{
    public static SceneLoader Instance { get; private set; }

    private string currentLevel;

    void Awake() => Instance = this;

    public void LoadLevel(string sceneName)
    {
        if (!string.IsNullOrEmpty(currentLevel) &&
            SceneManager.GetSceneByName(currentLevel).isLoaded)
        {
            SceneManager.UnloadSceneAsync(currentLevel);
        }

        SceneManager.LoadScene(sceneName, LoadSceneMode.Additive);
        currentLevel = sceneName;
    }

    public void LoadSingle(string sceneName)
    {
        SceneManager.LoadScene(sceneName);
    }
}