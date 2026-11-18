using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

public class SelectorNiveles : MonoBehaviour
{
    [Header("Nave (RectTransform dentro del Canvas)")]
    public RectTransform nave;
    [Tooltip("Velocidad media en px/s a lo largo de la curva.")]
    public float velocidadNave = 350f;
    [Tooltip("Duración mínima aunque el destino esté muy cerca.")]
    public float duracionMinima = 0.6f;
    [Tooltip("Cuánto se desvía la curva. 0 = recta, 0.3 = curva notable.")]
    [Range(0f, 0.8f)]
    public float curvatura = 0.25f;

    [Header("UI Confirmación")]
    public GameObject panelConfirmacion;
    public TMP_Text textoNombre;
    public Button botonConfirmar;
    public Button botonCancelar;

    BotonPlaneta planetaSeleccionado;
    bool animando;
    bool playerReactivado;

    void Start()
    {
        Time.timeScale = 1f;

        if (GameManager.Instance != null)
            GameManager.Instance.SetPlayerActive(false);

        if (panelConfirmacion != null) panelConfirmacion.SetActive(false);
        if (botonConfirmar != null) botonConfirmar.onClick.AddListener(Confirmar);
        if (botonCancelar != null) botonCancelar.onClick.AddListener(Cancelar);

        ColocarNaveEnUltimoNivelCompletado();
    }

    void OnDestroy()
    {
        if (!playerReactivado && GameManager.Instance != null)
            GameManager.Instance.SetPlayerActive(true);
    }

    // ---- Posición inicial de la nave ----

    void ColocarNaveEnUltimoNivelCompletado()
    {
        if (nave == null) return;

        int nivelParaColocar = 1; // fallback
        int maxReal = PlayerPrefs.GetInt(ProgressManager.KEY_NIVEL_MAX, 0);
        if (maxReal > 1) nivelParaColocar = maxReal - 1; // último realmente completado

        // (debug unlock no cuenta: queremos la posición "honesta")
        BotonPlaneta destino = null;
        foreach (var b in FindObjectsOfType<BotonPlaneta>())
            if (b.numeroNivel == nivelParaColocar) { destino = b; break; }

        if (destino != null)
            nave.position = destino.Rect.position;
        // Si no, la nave se queda donde esté configurada en la escena.
    }

    // ---- Selección / confirmación ----

    public void SeleccionarPlaneta(BotonPlaneta p)
    {
        if (animando) return;
        planetaSeleccionado = p;
        if (textoNombre != null) textoNombre.text = p.nombreMostrado;
        if (panelConfirmacion != null) panelConfirmacion.SetActive(true);
    }

    void Cancelar()
    {
        if (animando) return;
        planetaSeleccionado = null;
        if (panelConfirmacion != null) panelConfirmacion.SetActive(false);
    }

    void Confirmar()
    {
        if (animando || planetaSeleccionado == null) return;
        if (panelConfirmacion != null) panelConfirmacion.SetActive(false);
        StartCoroutine(AnimarYCargar());
    }

    IEnumerator AnimarYCargar()
    {
        animando = true;

        Vector3 start = nave.position;
        Vector3 end = planetaSeleccionado.Rect.position;

        yield return AnimarCurva(start, end);

        if (GameManager.Instance != null)
            GameManager.Instance.SetPlayerActive(true);
        playerReactivado = true;

        string escena = planetaSeleccionado.escenaDestino;
        if (SceneLoader.Instance != null)
            SceneLoader.Instance.LoadLevel(escena);
        else
            SceneManager.LoadScene(escena, LoadSceneMode.Additive);
    }

    // ---- Curva de Bézier cuadrática + rotación de la nave ----

    IEnumerator AnimarCurva(Vector3 start, Vector3 end)
    {
        Vector3 dir = end - start;
        float dist = dir.magnitude;
        if (dist < 0.01f) { nave.position = end; yield break; }

        // Perpendicular a la recta, siempre con Y >= 0 → la curva siempre se "arquea" hacia arriba
        Vector3 perp = new Vector3(-dir.y, dir.x, 0f).normalized;
        if (perp.y < 0f) perp = -perp;

        Vector3 ctrl = (start + end) * 0.5f + perp * (dist * curvatura);

        float duracion = Mathf.Max(duracionMinima, dist / Mathf.Max(1f, velocidadNave));
        float t = 0f;
        Vector3 prev = start;
        nave.position = start;

        while (t < 1f)
        {
            t += Time.unscaledDeltaTime / duracion;
            t = Mathf.Clamp01(t);

            Vector3 pos = Bezier(start, ctrl, end, t);

            Vector3 travel = pos - prev;
            if (travel.sqrMagnitude > 0.0001f)
                nave.up = travel.normalized; // el Sprite8Direction2D lee transform.up

            nave.position = pos;
            prev = pos;
            yield return null;
        }

        nave.position = end;
    }

    static Vector3 Bezier(Vector3 a, Vector3 b, Vector3 c, float t)
    {
        float u = 1f - t;
        return u * u * a + 2f * u * t * b + t * t * c;
    }
}