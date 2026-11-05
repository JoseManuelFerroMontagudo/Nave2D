using UnityEngine;
using UnityEngine.UI;
using TMPro;

public static class BossUIHelper
{
    public static void CrearBarraVidaRuntime(ControladorJefe jefe, out GameObject canvasObj, out Image healthFill, out TextMeshProUGUI nameText)
    {
        canvasObj = new GameObject("BossUI_RuntimeCanvas");
        Canvas canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 100;
        canvasObj.AddComponent<CanvasScaler>();
        canvasObj.AddComponent<GraphicRaycaster>();

        GameObject panel = new GameObject("BossHealthPanel");
        panel.transform.SetParent(canvasObj.transform, false);
        RectTransform panelRect = panel.AddComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(0.2f, 0.9f);
        panelRect.anchorMax = new Vector2(0.8f, 0.96f);
        panelRect.offsetMin = Vector2.zero;
        panelRect.offsetMax = Vector2.zero;

        Image bg = panel.AddComponent<Image>();
        bg.color = new Color(0.1f, 0.1f, 0.15f, 0.85f);

        GameObject fillObj = new GameObject("HealthFill");
        fillObj.transform.SetParent(panel.transform, false);
        RectTransform fillRect = fillObj.AddComponent<RectTransform>();
        fillRect.anchorMin = Vector2.zero;
        fillRect.anchorMax = Vector2.one;
        fillRect.offsetMin = new Vector2(4, 4);
        fillRect.offsetMax = new Vector2(-4, -4);

        healthFill = fillObj.AddComponent<Image>();
        healthFill.type = Image.Type.Simple;
        healthFill.color = new Color(0.8f, 0.2f, 1f, 1f);

        GameObject textObj = new GameObject("BossNameText");
        textObj.transform.SetParent(panel.transform, false);
        RectTransform textRect = textObj.AddComponent<RectTransform>();
        textRect.anchorMin = new Vector2(0, 1f);
        textRect.anchorMax = new Vector2(1f, 1.6f);
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;

        nameText = textObj.AddComponent<TextMeshProUGUI>();
        nameText.text = "JEFE: NAVE NODRIZA";
        nameText.fontSize = 20;
        nameText.alignment = TextAlignmentOptions.Center;
        nameText.color = Color.yellow;
        nameText.fontStyle = FontStyles.Bold;

        Debug.Log("[BossEncounter] UI de barra de vida creada automáticamente en pantalla.");
    }
}
