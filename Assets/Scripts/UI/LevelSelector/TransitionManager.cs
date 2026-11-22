using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class TransitionManager : MonoBehaviour
{
    public static TransitionManager Instance { get; private set; }

    [SerializeField] GameObject overlay;   // el Canvas
    [SerializeField] CanvasGroup group;     // CanvasGroup del Canvas
    [SerializeField] Animator animator;  // animator de la nave
    [SerializeField] float minDisplayTime = 1.2f;
    [SerializeField] float fadeInTime = 0.15f;
    [SerializeField] float fadeOutTime = 0.2f;

    bool busy;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        if (overlay) overlay.SetActive(false);
        if (group) { group.alpha = 0f; group.blocksRaycasts = false; }
    }

    // ============ API ============

    public void LoadSingleWithFollowUp(string persistentScene, string levelScene)
        => StartCoroutine(LoadWithOverlay(persistentScene, single: true,
                                          unload: null, followUp: levelScene));

    public void LoadWithOverlayOnly(string scene, bool single, string unload = null)
        => StartCoroutine(LoadWithOverlay(scene, single, unload, null));

    public void LoadDirect(string scene, bool single, string unload = null)
        => StartCoroutine(LoadDirectRoutine(scene, single, unload));

    // ============ Con overlay (niveles, menú→juego) ============

    IEnumerator LoadWithOverlay(string scene, bool single, string unload, string followUp)
    {
        while (busy) yield return null;
        busy = true;
        float t0 = Time.unscaledTime;

        if (overlay) overlay.SetActive(true);
        if (group) { group.blocksRaycasts = true; group.alpha = 0f; }
        yield return FadeAlpha(0f, 1f, fadeInTime);

        if (animator != null)
            animator.Play(animator.GetCurrentAnimatorStateInfo(0).fullPathHash, 0, 0f);

        if (!string.IsNullOrEmpty(unload) && SceneManager.GetSceneByName(unload).isLoaded)
        {
            var u = SceneManager.UnloadSceneAsync(unload);
            while (u != null && !u.isDone) yield return null;
        }

        var op = SceneManager.LoadSceneAsync(scene,
            single ? LoadSceneMode.Single : LoadSceneMode.Additive);
        if (op == null) { Debug.LogError($"[Transition] '{scene}' no en Build Settings."); busy = false; yield break; }

        op.allowSceneActivation = false;
        while (op.progress < 0.9f) yield return null;

        while (Time.unscaledTime - t0 < minDisplayTime) yield return null;

        op.allowSceneActivation = true;
        while (!op.isDone) yield return null;

        if (!string.IsNullOrEmpty(followUp))
        {
            var op2 = SceneManager.LoadSceneAsync(followUp, LoadSceneMode.Additive);
            if (op2 != null) while (!op2.isDone) yield return null;
            if (SceneLoader.Instance != null) SceneLoader.Instance.SetCurrentLevel(followUp);
        }

        yield return FadeAlpha(group != null ? group.alpha : 1f, 0f, fadeOutTime);
        if (overlay) overlay.SetActive(false);
        busy = false;
    }

    // ============ Sin overlay (menú, derrota, swaps duros) ============

    IEnumerator LoadDirectRoutine(string scene, bool single, string unload)
    {
        while (busy) yield return null;
        busy = true;

        // Seguridad: por si el overlay quedó visible de una transición anterior
        if (overlay) overlay.SetActive(false);
        if (group) { group.alpha = 0f; group.blocksRaycasts = false; }

        if (!string.IsNullOrEmpty(unload) && SceneManager.GetSceneByName(unload).isLoaded)
        {
            var u = SceneManager.UnloadSceneAsync(unload);
            while (u != null && !u.isDone) yield return null;
        }

        var op = SceneManager.LoadSceneAsync(scene,
            single ? LoadSceneMode.Single : LoadSceneMode.Additive);
        if (op == null) Debug.LogError($"[Transition] '{scene}' no en Build Settings.");
        else while (!op.isDone) yield return null;

        busy = false;
    }

    IEnumerator FadeAlpha(float from, float to, float dur)
    {
        if (group == null) yield break;
        if (dur <= 0f) { group.alpha = to; yield break; }
        float t = 0f;
        while (t < dur)
        {
            t += Time.unscaledDeltaTime;
            group.alpha = Mathf.Lerp(from, to, t / dur);
            yield return null;
        }
        group.alpha = to;
    }
}