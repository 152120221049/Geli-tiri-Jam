using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

/// <summary>
/// Ses ve Görüntü ayarlarını yöneten ve PlayerPrefs ile kaydedip yükleyen sistem.
/// - Master, Müzik, Efekt (SFX) ses seviyeleri
/// - Tam Ekran (Fullscreen), Çözünürlük (Resolution), Grafik Kalitesi (Quality), VSync
/// </summary>
public class SettingsManager : MonoBehaviour
{
    public static SettingsManager Instance { get; private set; }

    [Header("Ses UI Referansları")]
    [SerializeField] private Slider masterVolumeSlider;
    [SerializeField] private Slider musicVolumeSlider;
    [SerializeField] private Slider sfxVolumeSlider;

    [Header("Görüntü UI Referansları")]
    [SerializeField] private Toggle fullscreenToggle;
    [SerializeField] private TMP_Dropdown resolutionDropdown;
    [SerializeField] private TMP_Dropdown qualityDropdown;
    [SerializeField] private Toggle vsyncToggle;

    // PlayerPrefs Anahtarları
    private const string PREF_MASTER_VOL = "Settings_MasterVol";
    private const string PREF_MUSIC_VOL = "Settings_MusicVol";
    private const string PREF_SFX_VOL = "Settings_SFXVol";
    private const string PREF_FULLSCREEN = "Settings_Fullscreen";
    private const string PREF_RES_INDEX = "Settings_ResIndex";
    private const string PREF_QUALITY = "Settings_Quality";
    private const string PREF_VSYNC = "Settings_VSync";

    private Resolution[] availableResolutions;
    private List<Resolution> filteredResolutions = new List<Resolution>();

    // Değerler
    public float MasterVolume { get; private set; } = 1.0f;
    public float MusicVolume { get; private set; } = 0.8f;
    public float SFXVolume { get; private set; } = 0.8f;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    private void Start()
    {
        InitializeResolutions();
        InitializeQualityDropdown();
        LoadSettings();
        BindUIEvents();
    }

    // ═══════════════════════════════════════════
    //  İLK AYARLAR & RESOLUTION SORGULAMA
    // ═══════════════════════════════════════════

    private void InitializeResolutions()
    {
        if (resolutionDropdown == null) return;

        availableResolutions = Screen.resolutions;
        filteredResolutions.Clear();
        resolutionDropdown.ClearOptions();

        List<string> options = new List<string>();
        int currentResIndex = 0;

        for (int i = 0; i < availableResolutions.Length; i++)
        {
            Resolution res = availableResolutions[i];

            // Benzersiz Genişlik x Yükseklik ekle
            bool alreadyAdded = false;
            foreach (var existing in filteredResolutions)
            {
                if (existing.width == res.width && existing.height == res.height)
                {
                    alreadyAdded = true;
                    break;
                }
            }

            if (!alreadyAdded)
            {
                filteredResolutions.Add(res);
                string option = $"{res.width} x {res.height}";
                options.Add(option);

                if (res.width == Screen.width && res.height == Screen.height)
                {
                    currentResIndex = filteredResolutions.Count - 1;
                }
            }
        }

        resolutionDropdown.AddOptions(options);
        resolutionDropdown.value = currentResIndex;
        resolutionDropdown.RefreshShownValue();
    }

    private void InitializeQualityDropdown()
    {
        if (qualityDropdown == null) return;

        qualityDropdown.ClearOptions();
        string[] names = QualitySettings.names;
        List<string> options = new List<string>();

        // Türkçe isimler
        for (int i = 0; i < names.Length; i++)
        {
            switch (names[i].ToLower())
            {
                case "very low": options.Add("Çok Düşük"); break;
                case "low": options.Add("Düşük"); break;
                case "medium": options.Add("Orta"); break;
                case "high": options.Add("Yüksek"); break;
                case "very high": options.Add("Çok Yüksek"); break;
                case "ultra": options.Add("Ultra"); break;
                default: options.Add(names[i]); break;
            }
        }

        qualityDropdown.AddOptions(options);
        qualityDropdown.value = QualitySettings.GetQualityLevel();
        qualityDropdown.RefreshShownValue();
    }

    // ═══════════════════════════════════════════
    //  AYARLARI YÜKLE & UYGULA
    // ═══════════════════════════════════════════

    public void LoadSettings()
    {
        // Ses Ayarları
        MasterVolume = PlayerPrefs.GetFloat(PREF_MASTER_VOL, 1.0f);
        MusicVolume = PlayerPrefs.GetFloat(PREF_MUSIC_VOL, 0.8f);
        SFXVolume = PlayerPrefs.GetFloat(PREF_SFX_VOL, 0.8f);

        if (masterVolumeSlider != null) masterVolumeSlider.value = MasterVolume;
        if (musicVolumeSlider != null) musicVolumeSlider.value = MusicVolume;
        if (sfxVolumeSlider != null) sfxVolumeSlider.value = SFXVolume;

        ApplyAudioSettings();

        // Tam Ekran
        bool isFullscreen = PlayerPrefs.GetInt(PREF_FULLSCREEN, Screen.fullScreen ? 1 : 0) == 1;
        Screen.fullScreen = isFullscreen;
        if (fullscreenToggle != null) fullscreenToggle.isOn = isFullscreen;

        // Çözünürlük
        int savedResIndex = PlayerPrefs.GetInt(PREF_RES_INDEX, -1);
        if (savedResIndex >= 0 && savedResIndex < filteredResolutions.Count)
        {
            SetResolution(savedResIndex);
            if (resolutionDropdown != null) resolutionDropdown.value = savedResIndex;
        }

        // Grafik Kalitesi
        int savedQuality = PlayerPrefs.GetInt(PREF_QUALITY, QualitySettings.GetQualityLevel());
        QualitySettings.SetQualityLevel(savedQuality);
        if (qualityDropdown != null) qualityDropdown.value = savedQuality;

        // VSync
        bool isVSync = PlayerPrefs.GetInt(PREF_VSYNC, QualitySettings.vSyncCount > 0 ? 1 : 0) == 1;
        QualitySettings.vSyncCount = isVSync ? 1 : 0;
        if (vsyncToggle != null) vsyncToggle.isOn = isVSync;
    }

    private void BindUIEvents()
    {
        if (masterVolumeSlider != null)
            masterVolumeSlider.onValueChanged.AddListener(SetMasterVolume);

        if (musicVolumeSlider != null)
            musicVolumeSlider.onValueChanged.AddListener(SetMusicVolume);

        if (sfxVolumeSlider != null)
            sfxVolumeSlider.onValueChanged.AddListener(SetSFXVolume);

        if (fullscreenToggle != null)
            fullscreenToggle.onValueChanged.AddListener(SetFullscreen);

        if (resolutionDropdown != null)
            resolutionDropdown.onValueChanged.AddListener(SetResolution);

        if (qualityDropdown != null)
            qualityDropdown.onValueChanged.AddListener(SetQualityLevel);

        if (vsyncToggle != null)
            vsyncToggle.onValueChanged.AddListener(SetVSync);
    }

    // ═══════════════════════════════════════════
    //  PUBLIC SETTER METODLARI
    // ═══════════════════════════════════════════

    public void SetMasterVolume(float value)
    {
        MasterVolume = value;
        AudioListener.volume = value;
        PlayerPrefs.SetFloat(PREF_MASTER_VOL, value);
        PlayerPrefs.Save();
    }

    public void SetMusicVolume(float value)
    {
        MusicVolume = value;
        PlayerPrefs.SetFloat(PREF_MUSIC_VOL, value);
        PlayerPrefs.Save();
    }

    public void SetSFXVolume(float value)
    {
        SFXVolume = value;
        PlayerPrefs.SetFloat(PREF_SFX_VOL, value);
        PlayerPrefs.Save();
    }

    private void ApplyAudioSettings()
    {
        AudioListener.volume = MasterVolume;
    }

    public void SetFullscreen(bool isFullscreen)
    {
        Screen.fullScreen = isFullscreen;
        PlayerPrefs.SetInt(PREF_FULLSCREEN, isFullscreen ? 1 : 0);
        PlayerPrefs.Save();
    }

    public void SetResolution(int index)
    {
        if (index < 0 || index >= filteredResolutions.Count) return;

        Resolution res = filteredResolutions[index];
        Screen.SetResolution(res.width, res.height, Screen.fullScreen);
        PlayerPrefs.SetInt(PREF_RES_INDEX, index);
        PlayerPrefs.Save();
    }

    public void SetQualityLevel(int index)
    {
        QualitySettings.SetQualityLevel(index);
        PlayerPrefs.SetInt(PREF_QUALITY, index);
        PlayerPrefs.Save();
    }

    public void SetVSync(bool isVSync)
    {
        QualitySettings.vSyncCount = isVSync ? 1 : 0;
        PlayerPrefs.SetInt(PREF_VSYNC, isVSync ? 1 : 0);
        PlayerPrefs.Save();
    }

    /// <summary>Tüm ayarları fabrika varsayılanlarına sıfırlar.</summary>
    public void ResetToDefaults()
    {
        PlayerPrefs.DeleteKey(PREF_MASTER_VOL);
        PlayerPrefs.DeleteKey(PREF_MUSIC_VOL);
        PlayerPrefs.DeleteKey(PREF_SFX_VOL);
        PlayerPrefs.DeleteKey(PREF_FULLSCREEN);
        PlayerPrefs.DeleteKey(PREF_RES_INDEX);
        PlayerPrefs.DeleteKey(PREF_QUALITY);
        PlayerPrefs.DeleteKey(PREF_VSYNC);

        LoadSettings();
    }
}
