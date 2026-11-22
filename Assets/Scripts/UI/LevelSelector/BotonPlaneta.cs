using UnityEngine;
using UnityEngine.UI;

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

    Button boton;
    CanvasGroup canvasGroup;

    void Awake()
    {
        boton = GetComponent<Button>();
        canvasGroup = GetComponent<CanvasGroup>();
        if (canvasGroup == null) canvasGroup = gameObject.AddComponent<CanvasGroup>();
    }

    void Start()
    {
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

    void AplicarEstado(bool desbloqueado)
    {
        boton.interactable = desbloqueado;
        canvasGroup.alpha = desbloqueado ? 1f : alphaBloqueado;

        // Redundancia útil: aunque boton.interactable ya bloquea el clic,
        // esto asegura que NINGÚN hijo sea clicable (ej. si un día añades
        // un botón de "candado" o tooltip dentro del planeta).
        canvasGroup.interactable = desbloqueado;
        canvasGroup.blocksRaycasts = desbloqueado;
    }

    public RectTransform Rect => (RectTransform)transform;
}