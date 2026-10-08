using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using TMPro;

/// <summary>
/// Controla la aparición del boss tras un timer fijo.
/// Soporta dos modos:
///   A) Boss ya en la escena (desactivado al inicio) -> se activa tras el timer.
///   B) Boss como prefab -> se instancia tras el timer.
/// Incluye auto-recuperación: si el prefab no tiene ControladorJefe, se lo añade automáticamente.
/// </summary>
public class BossEncounterManager : MonoBehaviour
{
    [Header("Timer")]
    [Tooltip("Segundos hasta que aparece el boss")]
    public float tiempoParaBoss = 1000f;

    [Header("Referencias")]
    [Tooltip("Spawner de patrullas (legacy) — se detiene al llegar el boss")]

    //public SpawnerPatrullas spawnerPatrullas;
    //[Tooltip("Spawner de oleadas — se detiene al llegar el boss (asigna el GameObject aquí)")]
    public SpawnerOleadas spawnerOleadas;

    [Tooltip("Transform del jugador (se busca automáticamente si es null)")]
    public Transform jugador;

    [Header("Boss — Modo A: Ya en la escena")]
    [Tooltip("Si el boss ya está en la escena, arrástralo aquí. Se desactivará al inicio y se activará tras el timer.")]
    public GameObject bossEnEscena;

    [Header("Boss — Modo B: Instanciar prefab")]
    [Tooltip("Si prefieres instanciar un prefab, asígnalo aquí (ej. tu Enemy.prefab)")]
    public GameObject prefabBoss;

    [Header("Spawn del Boss")]
    [Tooltip("Distancia a la que aparece el boss desde el jugador")]
    public float distanciaSpawn = 4.5f;

    [Header("Alerta Visual")]
    [Tooltip("Duración de la alerta '¡PELIGRO!' antes de activar el boss")]
    public float duracionAlerta = 1.5f;

    [Header("UI")]
    [Tooltip("Referencia al script de barra de vida del boss (se auto-crea si es null)")]
    public BossHealthBarUI barraVidaBoss;

    // --- Estado ---
    private float cronometro;
    private bool bossActivo = false;
    private bool secuenciaIniciada = false;
    private GameObject bossInstancia;

    // Eventos
    public static event System.Action OnBossAparece;
    public static event System.Action OnBossDerrotado;

    // UI Runtime Fallback
    private Image runtimeHealthFill;
    private TextMeshProUGUI runtimeBossNameText;
    private GameObject runtimeCanvasObj;
    private ControladorJefe jefeConectado;

    void Start()
    {
        if (jugador == null)
        {
            var p = GameObject.FindGameObjectWithTag("Player");
            if (p != null) jugador = p.transform;
        }

        cronometro = 0f;

        // Auto-detectar boss en escena si no hay asignado
        if (bossEnEscena == null && prefabBoss == null)
        {
            var bossObj = GameObject.FindGameObjectWithTag("Boss");
            if (bossObj == null)
            {
                var jefeComp = Object.FindAnyObjectByType<ControladorJefe>();
                if (jefeComp != null) bossObj = jefeComp.gameObject;
            }
            if (bossObj != null)
            {
                bossEnEscena = bossObj;
                Debug.Log($"[BossEncounter] Boss en escena auto-detectado: '{bossObj.name}'. Desactivando...");
            }
        }

        // Desactivar el boss en la escena al inicio si estamos en Modo A
        if (bossEnEscena != null)
        {
            bossEnEscena.SetActive(false);
            Debug.Log("[BossEncounter] Boss desactivado al inicio. Aparecerá tras el timer.");
        }

        // Ocultar barra de vida manual si existe
        if (barraVidaBoss != null)
            barraVidaBoss.Ocultar();

        Debug.Log($"[BossEncounter] Timer iniciado: {tiempoParaBoss}s para la llegada del Boss.");
    }

    void Update()
    {
        // Actualizar barra de vida runtime si fue autogenerada
        if (bossActivo && jefeConectado != null && runtimeHealthFill != null)
        {
            float pct = Mathf.Clamp01(jefeConectado.vida / jefeConectado.VidaMaxima);
            
            // Usar anchorMax para escalar la barra ya que no tenemos Sprite para usar fillAmount
            RectTransform rect = runtimeHealthFill.rectTransform;
            rect.anchorMax = new Vector2(pct, 1f);
            
            runtimeHealthFill.color = Color.Lerp(Color.red, Color.magenta, pct);
        }

        if (bossActivo || jugador == null) return;

        cronometro += Time.deltaTime;

        if (cronometro >= tiempoParaBoss && !secuenciaIniciada)
        {
            secuenciaIniciada = true;
            Debug.Log("[BossEncounter] ¡Timer cumplido! Iniciando secuencia del boss...");
            StartCoroutine(SecuenciaAparicionBoss());
        }
    }

    IEnumerator SecuenciaAparicionBoss()
    {
        // 1) Detener TODOS los spawners (patrullas y oleadas)
        //SpawnerPatrullas.SpawningPermitido = false;
        SpawnerOleadas.SpawningPermitido = false;

        //var spawnersPatr = Object.FindObjectsByType<SpawnerPatrullas>(FindObjectsSortMode.None);
        //foreach (var sp in spawnersPatr) { sp.StopAllCoroutines(); sp.enabled = false; }

        var spawnersOl = Object.FindObjectsByType<SpawnerOleadas>(FindObjectsSortMode.None);
        foreach (var sp in spawnersOl) { sp.StopAllCoroutines(); sp.enabled = false; }

        Debug.Log($"[BossEncounter] Todos los spawners detenidos (patrullas + oleadas).");

        // 2) Limpiar enemigos normales de la escena
        yield return StartCoroutine(LimpiarEnemigosNormales());

        // 3) Pausa
        yield return new WaitForSeconds(0.5f);

        // 4) Alerta visual
        if (barraVidaBoss != null)
            barraVidaBoss.MostrarAlerta("¡PELIGRO!");

        yield return new WaitForSeconds(duracionAlerta);

        // 5) Activar Boss
        ActivarBoss();
    }

    IEnumerator LimpiarEnemigosNormales()
    {
        var enemigos = GameObject.FindGameObjectsWithTag("Enemy");
        int count = 0;
        foreach (var enemigo in enemigos)
        {
            if (enemigo.CompareTag("Boss")) continue;
            Destroy(enemigo);
            count++;
        }
        Debug.Log($"[BossEncounter] {count} enemigos normales eliminados de la pantalla.");
        yield return null;
    }

    void ActivarBoss()
    {
        // MODO A: Boss ya en la escena
        if (bossEnEscena != null)
        {
            if (jugador != null)
            {
                Vector3 posSpawn = jugador.position + Vector3.up * distanciaSpawn;
                bossEnEscena.transform.position = posSpawn;
            }

            bossEnEscena.SetActive(true);
            bossInstancia = bossEnEscena;
            Debug.Log("[BossEncounter] Boss activado (Modo A: escena).");
        }
        // MODO B: Instanciar prefab
        else if (prefabBoss != null && jugador != null)
        {
            Vector3 posSpawn = jugador.position + Vector3.up * distanciaSpawn;
            bossInstancia = Instantiate(prefabBoss, posSpawn, Quaternion.identity);
            Debug.Log("[BossEncounter] Boss instanciado (Modo B: prefab).");
        }
        else
        {
            Debug.LogError("[BossEncounter] ¡ERROR! No hay boss asignado. Asigna 'prefabBoss' o 'bossEnEscena' en el Inspector de BossEncounterManager.");
            return;
        }

        bossActivo = true;
        bossInstancia.tag = "Boss";

        // Auto-recuperación: asegurar que el GameObject instanciado tenga ControladorJefe
        jefeConectado = bossInstancia.GetComponent<ControladorJefe>();
        if (jefeConectado == null)
        {
            Debug.LogWarning("[BossEncounter] El prefab del boss no tenía el componente ControladorJefe. Añadiéndolo automáticamente...");
            jefeConectado = bossInstancia.AddComponent<ControladorJefe>();
        }

        // Conectar la UI de barra de vida
        if (barraVidaBoss != null)
        {
            barraVidaBoss.InicializarConBoss(jefeConectado);
        }
        else
        {
            CrearBarraVidaRuntime(jefeConectado);
        }

        jefeConectado.OnMuerte += BossDerrotado;
        Debug.Log($"[BossEncounter] Boss preparado exitosamente. Vida: {jefeConectado.vida}");

        OnBossAparece?.Invoke();
    }

    void CrearBarraVidaRuntime(ControladorJefe jefe)
    {
        if (runtimeCanvasObj != null) return;

        runtimeCanvasObj = new GameObject("BossUI_RuntimeCanvas");
        Canvas canvas = runtimeCanvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 100;
        runtimeCanvasObj.AddComponent<CanvasScaler>();
        runtimeCanvasObj.AddComponent<GraphicRaycaster>();

        GameObject panel = new GameObject("BossHealthPanel");
        panel.transform.SetParent(runtimeCanvasObj.transform, false);
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

        runtimeHealthFill = fillObj.AddComponent<Image>();
        // Ya no usamos Filled porque requiere un Sprite, usamos Simple y controlamos el ancho con anchorMax.x
        runtimeHealthFill.type = Image.Type.Simple;
        runtimeHealthFill.color = new Color(0.8f, 0.2f, 1f, 1f);

        GameObject textObj = new GameObject("BossNameText");
        textObj.transform.SetParent(panel.transform, false);
        RectTransform textRect = textObj.AddComponent<RectTransform>();
        textRect.anchorMin = new Vector2(0, 1f);
        textRect.anchorMax = new Vector2(1f, 1.6f);
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;

        runtimeBossNameText = textObj.AddComponent<TextMeshProUGUI>();
        runtimeBossNameText.text = "JEFE: NAVE NODRIZA";
        runtimeBossNameText.fontSize = 20;
        runtimeBossNameText.alignment = TextAlignmentOptions.Center;
        runtimeBossNameText.color = Color.yellow;
        runtimeBossNameText.fontStyle = FontStyles.Bold;

        Debug.Log("[BossEncounter] UI de barra de vida creada automáticamente en pantalla.");
    }

    void BossDerrotado()
    {
        OnBossDerrotado?.Invoke();

        if (barraVidaBoss != null)
            barraVidaBoss.AnimarDerrota();

        if (runtimeCanvasObj != null)
            Destroy(runtimeCanvasObj, 2f);

        Debug.Log("[BossEncounter] ¡Boss derrotado! 🎉");
    }

    public void ForzarAparicionBoss()
    {
        if (!bossActivo)
        {
            secuenciaIniciada = true;
            StartCoroutine(SecuenciaAparicionBoss());
        }
    }
}
