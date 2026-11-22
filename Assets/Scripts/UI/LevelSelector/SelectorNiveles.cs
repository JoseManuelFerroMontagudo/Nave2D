using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

public class SelectorNiveles : MonoBehaviour
{
    [Header("Nave (RectTransform dentro del Canvas)")]
    public RectTransform nave;
    public float velocidadNave = 350f;
    public float duracionMinima = 0.6f;
    [Range(0f, 0.8f)] public float curvatura = 0.25f;
    public Vector2 offsetNaveEnPlaneta = new Vector2(0f, -90f);

    [Header("UI Confirmación")]
    public GameObject panelConfirmacion;
    public TMP_Text textoNombre;
    public TMP_Text textoDescripcion;
    public Image imagenSeleccion;
    public Button botonConfirmar;
    public Button botonCancelar;

    [Header("Botones adicionales")]
    public Button botonMenu;
    public Button botonReiniciar;

    [Header("Escena menú")]
    public string escenaMenu = "MainMenu";

    [Header("Confirmación de reinicio")]
    public Sprite imagenReinicio;
    public string tituloReinicio = "¿Reiniciar progreso?";
    [TextArea(3, 6)]
    public string descripcionReinicio =
        "Se borrará todo el progreso guardado. Los niveles volverán a estar bloqueados.";

    enum Modo { Ninguno, Planeta, Reiniciar }
    Modo modoActual = Modo.Ninguno;

    BotonPlaneta planetaSeleccionado;
    bool animando;
    bool playerReactivado;

    Vector3 navePosInicial;
    Quaternion naveRotInicial;

    void Start()
    {
        Time.timeScale = 1f;

        if (GameManager.Instance != null)
            GameManager.Instance.SetPlayerActive(false);

        if (panelConfirmacion != null) panelConfirmacion.SetActive(false);
        if (botonConfirmar != null) botonConfirmar.onClick.AddListener(OnConfirmar);
        if (botonCancelar != null) botonCancelar.onClick.AddListener(OnCancelar);
        if (botonMenu != null) botonMenu.onClick.AddListener(IrAlMenu);
        if (botonReiniciar != null) botonReiniciar.onClick.AddListener(AbrirConfirmacionReinicio);

        if (nave != null)
        {
            foreach (var img in nave.GetComponentsInChildren<Image>(true))
                img.raycastTarget = false;

            navePosInicial = nave.position;
            naveRotInicial = nave.localRotation;
        }

        ColocarNaveEnUltimoNivelCompletado();
    }

    void OnDestroy()
    {
        if (!playerReactivado && GameManager.Instance != null)
            GameManager.Instance.SetPlayerActive(true);
    }

    // ---------- Posición inicial ----------

    void ColocarNaveEnUltimoNivelCompletado()
    {
        if (nave == null) return;

        int maxReal = PlayerPrefs.GetInt(ProgressManager.KEY_NIVEL_MAX, 0);
        if (maxReal <= 1)
        {
            nave.position = navePosInicial;
            nave.localRotation = naveRotInicial;
            return;
        }

        int nivelParaColocar = maxReal - 1;
        BotonPlaneta destino = null;
        foreach (var b in FindObjectsOfType<BotonPlaneta>())
            if (b.numeroNivel == nivelParaColocar) { destino = b; break; }

        if (destino == null)
        {
            nave.position = navePosInicial;
            nave.localRotation = naveRotInicial;
            return;
        }

        var rt = destino.Rect;
        Vector3 offsetWorld = rt.TransformVector(
            new Vector3(offsetNaveEnPlaneta.x, offsetNaveEnPlaneta.y, 0f));
        nave.position = rt.position + offsetWorld;
        nave.localRotation = Quaternion.Euler(0f, 0f, Random.Range(0f, 360f));
    }

    // ---------- Selección de planeta ----------

    public void SeleccionarPlaneta(BotonPlaneta p)
    {
        if (animando) return;
        planetaSeleccionado = p;
        modoActual = Modo.Planeta;

        if (textoNombre != null) textoNombre.text = p.nombreMostrado;
        if (textoDescripcion != null) textoDescripcion.text = p.descripcion;

        if (imagenSeleccion != null)
        {
            var imgBoton = p.GetComponent<Image>();
            if (imgBoton != null) imagenSeleccion.sprite = imgBoton.sprite;
        }

        if (panelConfirmacion != null) panelConfirmacion.SetActive(true);
    }

    // ---------- Reinicio ----------

    void AbrirConfirmacionReinicio()
    {
        if (animando) return;
        planetaSeleccionado = null;
        modoActual = Modo.Reiniciar;

        if (textoNombre != null) textoNombre.text = tituloReinicio;
        if (textoDescripcion != null) textoDescripcion.text = descripcionReinicio;
        if (imagenSeleccion != null) imagenSeleccion.sprite = imagenReinicio;

        if (panelConfirmacion != null) panelConfirmacion.SetActive(true);
    }

    // ---------- Confirmación / cancelación ----------

    void OnConfirmar()
    {
        if (animando) return;

        switch (modoActual)
        {
            case Modo.Planeta:
                if (planetaSeleccionado == null) return;
                if (panelConfirmacion != null) panelConfirmacion.SetActive(false);
                StartCoroutine(AnimarYCargar());
                break;

            case Modo.Reiniciar:
                if (panelConfirmacion != null) panelConfirmacion.SetActive(false);
                ReiniciarProgreso();
                break;
        }
    }

    void OnCancelar()
    {
        if (animando) return;
        planetaSeleccionado = null;
        modoActual = Modo.Ninguno;
        if (panelConfirmacion != null) panelConfirmacion.SetActive(false);
    }

    void ReiniciarProgreso()
    {
        if (ProgressManager.Instance != null)
            ProgressManager.Instance.ResetearProgreso();
        else
        {
            PlayerPrefs.SetInt(ProgressManager.KEY_NIVEL_MAX, 0);
            PlayerPrefs.Save();
        }

        // Apagar debug también: reset = estado limpio
        PlayerPrefs.SetInt(ProgressManager.KEY_DEBUG_UNLOCK, 0);
        PlayerPrefs.Save();

        IrAlMenu();
    }

    // ---------- Volver al menú ----------

    void IrAlMenu()
    {
        if (animando) return;
        animando = true;
        playerReactivado = true; // evita reactivar el jugador durante el destroy

        if (SceneLoader.Instance != null)
            SceneLoader.Instance.LoadSingleQuick(escenaMenu);
        else
            SceneManager.LoadScene(escenaMenu);
    }

    // ---------- Animación de la nave ----------

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

    IEnumerator AnimarCurva(Vector3 start, Vector3 end)
    {
        Vector3 dir = end - start;
        float dist = dir.magnitude;
        if (dist < 0.01f) { nave.position = end; yield break; }

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
                nave.up = travel.normalized;

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