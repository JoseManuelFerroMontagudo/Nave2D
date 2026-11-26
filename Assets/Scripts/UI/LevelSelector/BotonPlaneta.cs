using UnityEngine;
using UnityEngine.UI;
using TMPro;

[RequireComponent(typeof(Button))]
public class BotonPlaneta : MonoBehaviour
{
    [Header("Datos")]
    public string nombreMostrado = "Planeta";
    public string descripcion = "Un lugar inhóspito...";
    public string escenaDestino = "Nivel_1";
    public int numeroNivel = 1;

    [Header("Apariencia bloqueada")]
    [Range(0f, 1f)]
    [Tooltip("Alpha del botón cuando está bloqueado. 0.3 ≈ oscurecido sobre fondo espacial.")]
    public float alphaBloqueado = 0.3f;

    [Header("Récord (opcional)")]
    [Tooltip("TMP hijo del botón donde mostrar 'Récord: #'. Si null no se muestra.")]
    public TMP_Text textoRecord;

    Button boton;
    CanvasGroup canvasGroup;

    public int Record => PlayerPrefs.GetInt("PuntosNivel_" + numeroNivel, 0);

    void Awake()
    {
        boton = GetComponent<Button>();
        canvasGroup = GetComponent<CanvasGroup>();
        if (canvasGroup == null) canvasGroup = gameObject.AddComponent<CanvasGroup>();
    }

    void Start()
    {
        RefrescarRecord();

        if (ProgressManager.Instance == null)
        {
            Debug.LogWarning("[BotonPlaneta] ProgressManager.Instance no disponible. " +
                             "El botón queda bloqueado.");
            AplicarEstado(false);
            return;
        }

        bool desbloqueado = ProgressManager.Instance.EstaDesbloqueado(numeroNivel);
        AplicarEstado(desbloqueado);

        if (desbloqueado)
            boton.onClick.AddListener(() =>
                FindObjectOfType<SelectorNiveles>()?.SeleccionarPlaneta(this));
    }

    /// Actualiza el TMP de récord (si está asignado).
    public void RefrescarRecord()
    {
        if (textoRecord == null) return;
        int r = Record;
        textoRecord.text = r > 0 ? $"Récord: {r}" : "Récord: —";
    }

    void AplicarEstado(bool desbloqueado)
    {
        boton.interactable = desbloqueado;
        canvasGroup.alpha = desbloqueado ? 1f : alphaBloqueado;
        canvasGroup.interactable = desbloqueado;
        canvasGroup.blocksRaycasts = desbloqueado;
    }

    public RectTransform Rect => (RectTransform)transform;
}