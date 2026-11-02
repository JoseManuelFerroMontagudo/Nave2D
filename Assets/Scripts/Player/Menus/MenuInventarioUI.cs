using UnityEngine;

public class MenuInventarioUI : MonoBehaviour
{
    [Header("Datos")]
    [SerializeField] ArsenalJugador arsenalJugador;

    [Header("Colección")]
    [SerializeField] Transform contenedorColeccion;
    [SerializeField] GameObject itemPrefab;

    [Header("Slots")]
    [SerializeField] SlotEquipadoUI slotPrimario;
    [SerializeField] SlotEquipadoUI slotSecundario;

    [Header("Tooltip")]
    [SerializeField] TooltipAtaqueUI tooltip;
    UIActions ui;

    bool arrastrando;
    public ArsenalJugador Arsenal => arsenalJugador;

    public AtaqueBase GetEquipada(int slot) =>
        slot == 0 ? arsenalJugador.equipadaPrimaria : arsenalJugador.equipadaSecundaria;
    void Awake()
    {
        ui = FindObjectOfType<UIActions>();
    }

    public void Desequipar(int slot)
    {
        arsenalJugador.Desequipar(slot);

        var ship = FindObjectOfType<ShipWeapons>();
        if (ship) ship.SincronizarDesdeArsenal();

        Refrescar();
        OcultarTooltip();
    }
    void OnEnable()
    {
        Refrescar();
        tooltip?.Ocultar();
    }

    public void Refrescar()
    {
        foreach (Transform hijo in contenedorColeccion) Destroy(hijo.gameObject);

        foreach (var atk in arsenalJugador.Desbloqueados)
        {
            // Ocultar los que ya están en un slot
            if (atk == arsenalJugador.equipadaPrimaria || atk == arsenalJugador.equipadaSecundaria)
                continue;

            var go = Instantiate(itemPrefab, contenedorColeccion);
            go.GetComponent<ItemAtaqueUI>().Configurar(atk, this);
        }

        slotPrimario.SetIcono(arsenalJugador.equipadaPrimaria);
        slotSecundario.SetIcono(arsenalJugador.equipadaSecundaria);
    }

    // ---- Equipar ----
    public void EquiparEnSlot(AtaqueBase atk, int slot)
    {
        arsenalJugador.Equipar(atk, slot);
        var ship = FindObjectOfType<ShipWeapons>();
        if (ship) ship.SincronizarDesdeArsenal();
        Refrescar();
        OcultarTooltip();
    }

    // ---- Drag / Tooltip ----
    public void OnDragStart() { arrastrando = true; OcultarTooltip(); }
    public void OnDragEnd() { arrastrando = false; }

    public void MostrarTooltip(AtaqueBase atk, Vector2 screenPos)
    {
        if (arrastrando || tooltip == null) return;
        tooltip.Mostrar(atk, screenPos);
    }
    public void OcultarTooltip() => tooltip?.Ocultar();
    public void Volver()
    {
        if (ui) ui.OnMenu();                 // reusa la misma lógica abrir/cerrar
        else gameObject.SetActive(false);
    }
}