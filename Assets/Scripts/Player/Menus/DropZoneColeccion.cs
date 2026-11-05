using UnityEngine;
using UnityEngine.EventSystems;

public class DropZoneColeccion : MonoBehaviour, IDropHandler
{
    [SerializeField] MenuInventarioUI menu;
    void Awake() { if (menu == null) menu = GetComponentInParent<MenuInventarioUI>(); }

    public void OnDrop(PointerEventData e)
    {
        if (menu == null) return;
        var slot = e.pointerDrag != null ? e.pointerDrag.GetComponent<SlotEquipadoUI>() : null;
        if (slot != null) menu.Desequipar(slot.IndiceSlot);
    }
}