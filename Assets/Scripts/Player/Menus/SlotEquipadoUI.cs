using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class SlotEquipadoUI : MonoBehaviour, IDropHandler,
    IPointerEnterHandler, IPointerExitHandler,
    IBeginDragHandler, IDragHandler, IEndDragHandler
{
    [SerializeField] int indiceSlot;
    [SerializeField] Image fondo;   // raíz, siempre activo (raycast target)
    [SerializeField] Image icono;   // hijo, se oculta si no hay item
    [SerializeField] MenuInventarioUI menu;

    public int IndiceSlot => indiceSlot;

    GameObject fantasma;
    CanvasGroup grupo;
    Canvas canvas;

    void Awake()
    {
        if (menu == null) menu = GetComponentInParent<MenuInventarioUI>();
        if (fondo == null) fondo = GetComponent<Image>();
        if (icono == null) icono = GetComponentInChildren<Image>();
    }


    public void SetIcono(AtaqueBase atk)
    {
        if (icono) { icono.sprite = atk ? atk.icono : null; icono.enabled = atk != null; }
    }

    // ---- Drop ----
    public void OnDrop(PointerEventData e)
    {
        if (menu == null) return;

        // Desde la colección
        var item = e.pointerDrag != null ? e.pointerDrag.GetComponent<ItemAtaqueUI>() : null;
        if (item != null && item.Ataque != null)
        {
            menu.EquiparEnSlot(item.Ataque, indiceSlot);
            return;
        }

        // Desde otro slot → swap
        var slotOrigen = e.pointerDrag != null ? e.pointerDrag.GetComponent<SlotEquipadoUI>() : null;
        if (slotOrigen != null && slotOrigen.IndiceSlot != indiceSlot)
        {
            var atk = menu.GetEquipada(slotOrigen.IndiceSlot);
            if (atk != null) menu.EquiparEnSlot(atk, indiceSlot);
        }
    }

    // ---- Hover (tooltip sin arrastrar) ----
    public void OnPointerEnter(PointerEventData e)
    {
        if (menu == null) return;
        var atk = menu.GetEquipada(indiceSlot);
        if (atk != null) menu.MostrarTooltip(atk, e.position);
    }
    public void OnPointerExit(PointerEventData e) { if (menu != null) menu.OcultarTooltip(); }

    // ---- Drag desde el slot ----
    public void OnBeginDrag(PointerEventData e)
    {
        if (menu == null) return;
        var atk = menu.GetEquipada(indiceSlot);
        if (atk == null) return; // slot vacío no se arrastra

        menu.OnDragStart();
        canvas = GetComponentInParent<Canvas>();

        grupo = GetComponent<CanvasGroup>();
        if (grupo == null) grupo = gameObject.AddComponent<CanvasGroup>();
        grupo.alpha = 0.4f;

        fantasma = new GameObject("Fantasma", typeof(RectTransform), typeof(Image));
        fantasma.transform.SetParent(canvas.transform, false);
        fantasma.transform.SetAsLastSibling();
        var img = fantasma.GetComponent<Image>();
        img.sprite = atk.icono;
        img.raycastTarget = false;
        var rt = fantasma.GetComponent<RectTransform>();
        rt.sizeDelta = ((RectTransform)transform).sizeDelta;
        rt.position = e.position;
    }

    public void OnDrag(PointerEventData e) { if (fantasma) fantasma.transform.position = e.position; }

    public void OnEndDrag(PointerEventData e)
    {
        if (fantasma) Destroy(fantasma);
        if (grupo) grupo.alpha = 1f;
        menu.OnDragEnd();
    }

    void OnDisable() { if (fantasma) { Destroy(fantasma); fantasma = null; } }
}