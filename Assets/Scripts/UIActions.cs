using UnityEngine;
using UnityEngine.InputSystem;

public class UIActions : MonoBehaviour
{
    [Header("Paneles (ya en escena, desactivados)")]
    [SerializeField] GameObject menuPanel;
    [SerializeField] GameObject inventoryPanel;
    [SerializeField] Minimap minimap;

    [Header("Input del juego")]
    [SerializeField] PlayerInput gameplayInput;

    void Awake()
    {
        // Aseguramos estado inicial: ambos cerrados
        if (menuPanel) menuPanel.SetActive(false);
        if (inventoryPanel) inventoryPanel.SetActive(false);
    }

    void Start()
    {
        RefreshPause();
    }

    void RefreshPause()
    {
        bool anyOpen = (menuPanel && menuPanel.activeSelf) || (inventoryPanel && inventoryPanel.activeSelf);
        if (gameplayInput)
        {
            if (anyOpen) gameplayInput.DeactivateInput();
            else gameplayInput.ActivateInput();
        }
        Time.timeScale = anyOpen ? 0f : 1f;
    }

    // ---- Invocado por botón de UI ----
    public void OnMenu()
    {
        if (inventoryPanel && inventoryPanel.activeSelf)
        {
            inventoryPanel.SetActive(false);
            RefreshPause();
            return;
        }
        if (menuPanel && menuPanel.activeSelf)
        {
            menuPanel.SetActive(false);
            RefreshPause();
            return;
        }
        if (menuPanel) menuPanel.SetActive(true);
        RefreshPause();
    }

    public void OnInventory()
    {
        if (menuPanel && menuPanel.activeSelf) return;

        if (inventoryPanel) inventoryPanel.SetActive(!inventoryPanel.activeSelf);
        RefreshPause();
    }

    public void OnMinimap()
    {
        if (minimap) minimap.ToggleMode();
    }

    // ---- Cierre explícito (para botones "Volver") ----
    public void CloseMenu()
    {
        if (menuPanel && menuPanel.activeSelf)
        {
            menuPanel.SetActive(false);
            RefreshPause();
        }
    }

    public void CloseInventory()
    {
        if (inventoryPanel && inventoryPanel.activeSelf)
        {
            inventoryPanel.SetActive(false);
            RefreshPause();
        }
    }
}