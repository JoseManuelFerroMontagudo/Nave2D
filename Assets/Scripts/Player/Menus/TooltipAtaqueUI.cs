using UnityEngine;
using UnityEngine.UI;
using TMPro;

[RequireComponent(typeof(CanvasGroup))]
public class TooltipAtaqueUI : MonoBehaviour
{
    [SerializeField] CanvasGroup grupo;          // se auto-rellena en Awake
    [SerializeField] RectTransform fondo;
    [SerializeField] TMP_Text titulo;
    [SerializeField] TMP_Text descripcion;

    [SerializeField] Vector2 offset = new Vector2(20f, -20f);
    [SerializeField] float velocidadFade = 10f;
    [SerializeField] bool logDebug = false;

    float alphaObjetivo;

    void Awake()
    {
        // Unity ya añadió el CanvasGroup gracias a [RequireComponent]
        if (grupo == null) grupo = GetComponent<CanvasGroup>();
        if (fondo == null) fondo = GetComponentInChildren<Image>()?.rectTransform;

        grupo.blocksRaycasts = false;
        grupo.interactable = false;
        grupo.alpha = 0f;
        alphaObjetivo = 0f;
    }

    T FindChild<T>(string nombre) where T : Component
    {
        foreach (var t in GetComponentsInChildren<T>(true))
            if (t.gameObject.name == nombre) return t;
        return null;
    }

    public void Mostrar(AtaqueBase atk, Vector2 screenPos)
    {
        if (atk == null) return;

        if (titulo) titulo.text = string.IsNullOrEmpty(atk.nombreVisible) ? atk.name : atk.nombreVisible;
        if (descripcion) descripcion.text = atk.descripcion;

        if (fondo) LayoutRebuilder.ForceRebuildLayoutImmediate(fondo);

        var rt = (RectTransform)transform;
        var canvas = GetComponentInParent<Canvas>();
        if (canvas == null) { if (logDebug) Debug.LogWarning("[Tooltip] sin Canvas padre"); return; }

        Vector2 pos = screenPos + offset;
        Vector2 size = rt.rect.size;
        pos.x = Mathf.Clamp(pos.x, 0, Screen.width - size.x);
        pos.y = Mathf.Clamp(pos.y, size.y, Screen.height);

        if (canvas.renderMode == RenderMode.ScreenSpaceOverlay)
        {
            rt.position = pos;
        }
        else
        {
            var cam = canvas.worldCamera;
            if (RectTransformUtility.ScreenPointToWorldPointInRectangle(
                    (RectTransform)canvas.transform, pos, cam, out var world))
                rt.position = world;
        }

        alphaObjetivo = 1f;
        if (logDebug) Debug.Log($"[Tooltip] Mostrar '{titulo?.text}' en {rt.position}");
    }

    public void Ocultar()
    {
        alphaObjetivo = 0f;
        if (logDebug) Debug.Log("[Tooltip] Ocultar");
    }

    void Update()
    {
        if (Mathf.Approximately(grupo.alpha, alphaObjetivo)) return;
        grupo.alpha = Mathf.MoveTowards(grupo.alpha, alphaObjetivo, velocidadFade * Time.unscaledDeltaTime);
    }
}