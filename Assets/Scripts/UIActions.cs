using UnityEngine;
using UnityEngine.InputSystem;

public class UIActions : MonoBehaviour
{
    [Header("Menú superpuesto")]
    [SerializeField] GameObject menuPrefab;
    [SerializeField] GameObject inventoryPrefab;
    [SerializeField] Minimap minimap;

    [Header("Input del juego")]
    [SerializeField] PlayerInput gameplayInput;   // el PlayerInput del Player

    GameObject menuInstance;
    GameObject invInstance;

    void Start()
    {
        menuInstance = Instantiate(menuPrefab);
        menuInstance.SetActive(false);
        invInstance = Instantiate(inventoryPrefab);
        invInstance.SetActive(false);
    }

    void RefreshPause()
    {
        bool anyOpen = menuInstance.activeSelf || invInstance.activeSelf;
        if (anyOpen) gameplayInput.DeactivateInput();
        else gameplayInput.ActivateInput();
        Time.timeScale = anyOpen ? 0f : 1f;
    }

    public void OnMenu()
    {
        if (invInstance.activeSelf) { invInstance.SetActive(false); RefreshPause(); return; }
        if (menuInstance.activeSelf) { menuInstance.SetActive(false); RefreshPause(); return; }
        menuInstance.SetActive(true); RefreshPause();
    }

    public void OnInventory()
    {
        if (menuInstance.activeSelf) return;   // menú modal: bloquea el inventario

        invInstance.SetActive(!invInstance.activeSelf);
        RefreshPause();
    }

    public void OnMinimap() => minimap.ToggleMode();
}