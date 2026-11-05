using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class ItemAtaqueUI : MonoBehaviour,
    IBeginDragHandler, IDragHandler, IEndDragHandler,
    IPointerEnterHandler, IPointerExitHandler
{
    [SerializeField] Image icono;

    public AtaqueBase Ataque { get; private set; }
    MenuInventarioUI menu;
    Canvas canvas;
    GameObject fantasma;
    CanvasGroup grupo;

    public void Configurar(AtaqueBase atk, MenuInventarioUI m)
    {
        Ataque = atk;
        menu = m;
        if (menu == null) menu = GetComponentInParent<MenuInventarioUI>();
        if (icono) { icono.sprite = atk.icono; icono.enabled = true; }
    }

    // ---- Drag ----
    public void OnBeginDrag(PointerEventData e)
    {
        if (Ataque == null || menu == null) return;
        canvas = GetComponentInParent<Canvas>();
        menu.OnDragStart();

        grupo = GetComponent<CanvasGroup>();
        if (grupo == null) grupo = gameObject.AddComponent<CanvasGroup>();
        grupo.alpha = 0.4f;

        // Fantasma que sigue al cursor
        fantasma = new GameObject("Fantasma", typeof(RectTransform), typeof(Image));
        fantasma.transform.SetParent(canvas.transform, false);
        fantasma.transform.SetAsLastSibling();
        var img = fantasma.GetComponent<Image>();
        img.sprite = Ataque.icono;
        img.raycastTarget = false;                    // no bloquea drops
        var rt = fantasma.GetComponent<RectTransform>();
        rt.sizeDelta = ((RectTransform)transform).sizeDelta;
        rt.position = e.position;
    }

    public void OnDrag(PointerEventData e)
    {
        if (fantasma) fantasma.transform.position = e.position;
    }

    public void OnEndDrag(PointerEventData e)
    {
        if (fantasma) Destroy(fantasma);
        if (grupo) grupo.alpha = 1f;
        menu.OnDragEnd();
    }

    // ---- Hover ----
    public void OnPointerEnter(PointerEventData e) => menu?.MostrarTooltip(Ataque, e.position);
    public void OnPointerExit(PointerEventData e) => menu?.OcultarTooltip();

    void OnDisable() { if (fantasma) { Destroy(fantasma); fantasma = null; } }
}