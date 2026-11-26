using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Audio;

public class MenuConfigUI : MonoBehaviour
{
    [Header("Audio")]
    [SerializeField] Slider sliderEfects;      // Ahora: Efectos (SFX)
    [SerializeField] Slider sliderMusica;      // Música
    [SerializeField] Toggle toggleModo;        // Ahora: Silenciar todo
    [SerializeField] AudioMixer mixer;

    [Header("Escenas")]
    [SerializeField] string mainMenuScene = "MainMenu";

    UIActions ui;
    float _lastSfxVolume = 0.8f; // Para restaurar al desactivar silencio

    void Awake()
    {
        ui = FindObjectOfType<UIActions>();

        // Cargar valores
        sliderEfects.value = PlayerPrefs.GetFloat("vol_sfx", 0.8f);
        _lastSfxVolume = sliderEfects.value;
        sliderMusica.value = PlayerPrefs.GetFloat("vol_musica", 0.8f);
        toggleModo.isOn = PlayerPrefs.GetInt("mute_all", 0) == 1;

        // Cablear eventos
        sliderEfects.onValueChanged.AddListener(OnSfxChanged);
        sliderMusica.onValueChanged.AddListener(OnMusicaChanged);
        toggleModo.onValueChanged.AddListener(OnMuteAllChanged);

        // Aplicar estado inicial
        ApplySfxVolume(sliderEfects.value);
        ApplyMusicVolume(sliderMusica.value);
        ApplyMuteState(toggleModo.isOn, savePrefs: false);
    }

    // ---- Audio ----
    void OnSfxChanged(float v)
    {
        _lastSfxVolume = v;
        ApplySfxVolume(v);
        PlayerPrefs.SetFloat("vol_sfx", v);
        // Si el usuario mueve el slider de efectos estando silenciado, desactivar silencio automáticamente
        if (toggleModo.isOn)
        {
            toggleModo.SetIsOnWithoutNotify(false);
            ApplyMuteState(false, savePrefs: true);
        }
    }

    void OnMusicaChanged(float v)
    {
        ApplyMusicVolume(v);
        PlayerPrefs.SetFloat("vol_musica", v);
    }

    void OnMuteAllChanged(bool muted)
    {
        ApplyMuteState(muted, savePrefs: true);
    }

    void ApplySfxVolume(float v)
    {
        if (mixer) mixer.SetFloat("SFXVol", Mathf.Log10(Mathf.Max(v, 0.0001f)) * 20f);
    }

    void ApplyMusicVolume(float v)
    {
        if (mixer) mixer.SetFloat("MusicVol", Mathf.Log10(Mathf.Max(v, 0.0001f)) * 20f);
    }

    void ApplyMuteState(bool muted, bool savePrefs)
    {
        if (mixer)
        {
            // Silenciar todo: Master a -80dB (silencio), o restaurar
            mixer.SetFloat("MasterVol", muted ? -80f : 0f);
        }
        if (savePrefs) PlayerPrefs.SetInt("mute_all", muted ? 1 : 0);
    }

    // ---- Botones ----
    public void Guardar()
    {
        PlayerPrefs.Save();
        Debug.Log("[MenuConfig] Guardado");
    }

    public void Controles()
    {
        Debug.Log("[MenuConfig] Abrir panel de controles");
    }

    public void SalirAlMenuPrincipal()
    {
        if (ui) ui.OnMenu();
        SceneLoader.Instance?.LoadSingleQuick(mainMenuScene);
    }

    public void Volver()
    {
        if (ui) ui.OnMenu();
        else gameObject.SetActive(false);
    }

    public void SalirDelJuego()
    {
        Time.timeScale = 1f;
        if (SceneLoader.Instance) SceneLoader.Instance.Salir();
        else Application.Quit();
    }
}