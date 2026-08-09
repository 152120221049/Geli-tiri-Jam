using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

/// <summary>
/// Ana Menü panel geçişlerini, sahne yüklemesini ve oyundan çıkışı yönetir.
/// - Oyuna Başla (Play)
/// - Ayarlar (Settings) -> Paneli açar, Ana Menüyü kapatır
/// - Emeği Geçenler (Credits) -> Paneli açar, Ana Menüyü kapatır
/// - Geri / Çıkış Butonu -> Alt paneli kapatır, Ana Menüyü geri açar
/// - Oyunu Kapat (Quit)
/// </summary>
public class MainMenuController : MonoBehaviour
{
    public static MainMenuController Instance { get; private set; }

    [Header("Panel Referansları")]
    [SerializeField] private GameObject mainMenuPanel;
    [SerializeField] private GameObject settingsPanel;
    [SerializeField] private GameObject creditsPanel;

    [Header("Sahne Ayarları")]
    [Tooltip("Oyuna Başla'ya basıldığında yüklenecek sahne adı")]
    [SerializeField] private string gameSceneName = "SampleScene";

    [Header("Ses Efekti (Opsiyonel)")]
    [SerializeField] private AudioSource buttonClickAudio;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        EnsureInputModule();
    }

    private void EnsureInputModule()
    {
        UnityEngine.EventSystems.EventSystem es = FindObjectOfType<UnityEngine.EventSystems.EventSystem>();
        if (es == null)
        {
            GameObject esObj = new GameObject("EventSystem");
            es = esObj.AddComponent<UnityEngine.EventSystems.EventSystem>();
        }

#if ENABLE_INPUT_SYSTEM
        var standalone = es.GetComponent<UnityEngine.EventSystems.StandaloneInputModule>();
        if (standalone != null)
        {
            Destroy(standalone);
        }

        var inputModule = es.GetComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
        if (inputModule == null)
        {
            es.gameObject.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
        }
#else
        var inputModule = es.GetComponent<UnityEngine.EventSystems.StandaloneInputModule>();
        if (inputModule == null)
        {
            es.gameObject.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
        }
#endif
    }

    private void Start()
    {
        ShowMainMenu();
    }

    private void Update()
    {
        // ESC ile alt panellerden Ana Menüye dönüş
        bool escPressed = false;
#if ENABLE_INPUT_SYSTEM
        if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
            escPressed = true;
#else
        if (Input.GetKeyDown(KeyCode.Escape))
            escPressed = true;
#endif

        if (escPressed)
        {
            if (settingsPanel != null && settingsPanel.activeSelf)
            {
                ShowMainMenu();
            }
            else if (creditsPanel != null && creditsPanel.activeSelf)
            {
                ShowMainMenu();
            }
        }
    }

    // ═══════════════════════════════════════════
    //  PANEL GEÇİŞ METODLARI
    // ═══════════════════════════════════════════

    /// <summary>Ana Menü panelini açar, alt panelleri kapatır.</summary>
    public void ShowMainMenu()
    {
        PlayButtonClickSound();

        if (mainMenuPanel != null) mainMenuPanel.SetActive(true);
        if (settingsPanel != null) settingsPanel.SetActive(false);
        if (creditsPanel != null) creditsPanel.SetActive(false);
    }

    /// <summary>Ayarlar panelini açar, Ana Menüyü kapatır.</summary>
    public void ShowSettings()
    {
        PlayButtonClickSound();

        if (mainMenuPanel != null) mainMenuPanel.SetActive(false);
        if (settingsPanel != null) settingsPanel.SetActive(true);
        if (creditsPanel != null) creditsPanel.SetActive(false);

        // Ayarları UI'a yükle
        if (SettingsManager.Instance != null)
        {
            SettingsManager.Instance.LoadSettings();
        }
    }

    /// <summary>Emeği Geçenler (Credits) panelini açar, Ana Menüyü kapatır.</summary>
    public void ShowCredits()
    {
        PlayButtonClickSound();

        if (mainMenuPanel != null) mainMenuPanel.SetActive(false);
        if (settingsPanel != null) settingsPanel.SetActive(false);
        if (creditsPanel != null) creditsPanel.SetActive(true);
    }

    // ═══════════════════════════════════════════
    //  AKSİYONLAR
    // ═══════════════════════════════════════════

    /// <summary>Oyun sahnesini yükler.</summary>
    public void StartGame()
    {
        PlayButtonClickSound();
        Debug.Log($"🎮 [ANA MENÜ] Oyuna başlanıyor... Sahne: '{gameSceneName}'");

        if (!string.IsNullOrEmpty(gameSceneName) && Application.CanStreamedLevelBeLoaded(gameSceneName))
        {
            SceneManager.LoadScene(gameSceneName);
        }
        else
        {
            // İsimle bulunamazsa sonraki sahneye geç
            int nextBuildIndex = SceneManager.GetActiveScene().buildIndex + 1;
            if (nextBuildIndex < SceneManager.sceneCountInBuildSettings)
            {
                SceneManager.LoadScene(nextBuildIndex);
            }
            else
            {
                Debug.LogWarning($"⚠️ [ANA MENÜ] '{gameSceneName}' sahnesi Build Settings'e eklenmemiş! Lütfen File -> Build Settings altından sahneyi ekleyin.");
            }
        }
    }

    /// <summary>Oyunu tamamen kapatır.</summary>
    public void QuitGame()
    {
        PlayButtonClickSound();
        Debug.Log("🚪 [ANA MENÜ] Oyundan çıkış yapılıyor...");

#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    private void PlayButtonClickSound()
    {
        if (buttonClickAudio != null)
        {
            buttonClickAudio.Play();
        }
    }
}
