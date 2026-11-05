using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using UnityEngine.Audio;

public class MenuConfigUI : MonoBehaviour
{
    [Header("Audio")]
    [SerializeField] Slider sliderMaster;
    [SerializeField] Slider sliderMusica;
    [SerializeField] AudioMixer mixer;              // opcional si aún no lo tienes

    [Header("Minimapa")]
    [SerializeField] Toggle toggleModo;
    [SerializeField] Minimap minimap;

    [Header("Escenas")]
    [SerializeField] string mainMenuScene = "MainMenu";

    // Referencias al UIActions del Canvas principal
    UIActions ui;

    void Awake()
    {
        ui = FindObjectOfType<UIActions>();
        if (!minimap) minimap = FindObjectOfType<Minimap>();

        // Cargar valores actuales (o PlayerPrefs si ya existen)
        sliderMaster.value = PlayerPrefs.GetFloat("vol_master", 0.8f);
        sliderMusica.value = PlayerPrefs.GetFloat("vol_musica", 0.8f);
        if (minimap) toggleModo.isOn = minimap.CenterOnPlayer;

        // Cablear eventos
        sliderMaster.onValueChanged.AddListener(OnMasterChanged);
        sliderMusica.onValueChanged.AddListener(OnMusicaChanged);
        toggleModo.onValueChanged.AddListener(OnModoChanged);
        if (mixer)
        {
            mixer.SetFloat("MasterVol", Mathf.Log10(Mathf.Max(sliderMaster.value, 0.0001f)) * 20f);
            mixer.SetFloat("MusicVol", Mathf.Log10(Mathf.Max(sliderMusica.value, 0.0001f)) * 20f);
        }
    }
    void OnEnable()
    {
        if (!minimap) minimap = FindObjectOfType<Minimap>();
        if (minimap && toggleModo)
            toggleModo.SetIsOnWithoutNotify(minimap.CenterOnPlayer);
    }
    void Update()
    {
        if (minimap && toggleModo.isOn != minimap.CenterOnPlayer)
            toggleModo.SetIsOnWithoutNotify(minimap.CenterOnPlayer);
    }

    // ---- Audio ----
    void OnMasterChanged(float v)
    {
        if (mixer) mixer.SetFloat("MasterVol", Mathf.Log10(Mathf.Max(v, 0.0001f)) * 20f);
        PlayerPrefs.SetFloat("vol_master", v);
    }

    void OnMusicaChanged(float v)
    {
        if (mixer) mixer.SetFloat("MusicVol", Mathf.Log10(Mathf.Max(v, 0.0001f)) * 20f);
        PlayerPrefs.SetFloat("vol_musica", v);
    }

    // ---- Minimapa ----
    void OnModoChanged(bool on)
    {
        if (minimap) minimap.SetMode(on);
    }

    // ---- Botones ----
    public void Guardar()
    {
        // Placeholder: por ahora PlayerPrefs ya guarda audio.
        // Aquí irá tu sistema de guardado real.
        PlayerPrefs.Save();
        Debug.Log("[MenuConfig] Guardado (placeholder)");
    }

    public void Controles()
    {
        Debug.Log("[MenuConfig] Controles (placeholder)");
        // Futuro: abrir submenú o panel de controles
    }

    public void SalirAlMenuPrincipal()
    {
        if (ui) ui.OnMenu();
        SceneLoader.Instance?.LoadSingle(mainMenuScene);
    }

    public void Volver()
    {
        if (ui) ui.OnMenu();                 // reusa la misma lógica abrir/cerrar
        else gameObject.SetActive(false);
    }
    public void SalirDelJuego()
    {
        Time.timeScale = 1f;                     // por si acaso, salir limpio
        if (SceneLoader.Instance) SceneLoader.Instance.Salir();
        else Application.Quit();
    }
}