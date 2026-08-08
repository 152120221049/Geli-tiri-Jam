using UnityEngine;
using UnityEngine.UI;
using TMPro;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

/// <summary>
/// Dünyadaki notları, mektupları ve yazıları okumak için UI yöneticisi.
/// Sahnede yoksa otomatik olarak şık bir koyu parşömen Not Paneli oluşturur.
/// Not açıkken oyuncu hareketini durdurur, 'E', 'ESC' veya buton ile kapatılır.
/// </summary>
public class NoteUI : MonoBehaviour
{
    public static NoteUI Instance { get; private set; }

    [Header("UI Referansları (Boş kalırsa otomatik oluşturulur)")]
    [SerializeField] private GameObject notePanel;
    [SerializeField] private TextMeshProUGUI titleText;
    [SerializeField] private TextMeshProUGUI contentText;
    [SerializeField] private Button closeButton;
    [SerializeField] private TextMeshProUGUI closeButtonText;

    public bool IsReadingNote => notePanel != null && notePanel.activeSelf;

    private Canvas canvas;
    private RectTransform panelRect;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        InitCanvasAndPanel();
        Hide();
    }

    private void Update()
    {
        if (!IsReadingNote) return;

        // 'E', 'ESC' veya 'Space' basılınca notu kapat
        bool closePressed = false;

#if ENABLE_INPUT_SYSTEM
        if (Keyboard.current != null && (Keyboard.current.eKey.wasPressedThisFrame || Keyboard.current.escapeKey.wasPressedThisFrame || Keyboard.current.spaceKey.wasPressedThisFrame))
            closePressed = true;
#else
        if (Input.GetKeyDown(KeyCode.E) || Input.GetKeyDown(KeyCode.Escape) || Input.GetKeyDown(KeyCode.Space))
            closePressed = true;
#endif

        if (closePressed)
        {
            Hide();
        }
    }

    private void InitCanvasAndPanel()
    {
        canvas = GetComponentInParent<Canvas>();
        if (canvas == null)
            canvas = FindObjectOfType<Canvas>();

        EnsureUIBuilt();
    }

    private void EnsureUIBuilt()
    {
        if (notePanel != null) return;

        if (canvas == null)
        {
            canvas = FindObjectOfType<Canvas>();
            if (canvas == null) return;
        }

        // Koyu Parşömen UI Paneli Oluştur
        GameObject panelObj = new GameObject("NoteReaderPanel", typeof(RectTransform), typeof(Image), typeof(CanvasGroup), typeof(Canvas), typeof(GraphicRaycaster));
        panelObj.transform.SetParent(canvas.transform, false);
        panelRect = panelObj.GetComponent<RectTransform>();

        // Merkeze hizala
        panelRect.anchorMin = new Vector2(0.5f, 0.5f);
        panelRect.anchorMax = new Vector2(0.5f, 0.5f);
        panelRect.pivot = new Vector2(0.5f, 0.5f);
        panelRect.sizeDelta = new Vector2(480f, 320f);
        panelRect.anchoredPosition = Vector2.zero;

        // Canvas Sorting Override (Her zaman tüm UI elemanlarının en üstünde)
        Canvas c = panelObj.GetComponent<Canvas>();
        c.overrideSorting = true;
        c.sortingOrder = 120;

        // Arka Plan Rengi (Koyu Füme / Parşömen)
        Image bgImg = panelObj.GetComponent<Image>();
        bgImg.color = new Color(0.12f, 0.14f, 0.18f, 0.95f);

        Outline outline = panelObj.AddComponent<Outline>();
        outline.effectColor = new Color(0.9f, 0.75f, 0.4f, 0.9f); // Altın sarısı çerçeve
        outline.effectDistance = new Vector2(3, -3);

        // Vertical Layout Group
        VerticalLayoutGroup vlg = panelObj.AddComponent<VerticalLayoutGroup>();
        vlg.padding = new RectOffset(20, 20, 20, 20);
        vlg.spacing = 12f;
        vlg.childControlWidth = true;
        vlg.childControlHeight = false;
        vlg.childForceExpandWidth = true;
        vlg.childForceExpandHeight = false;

        // 1. Başlık
        titleText = CreateText(panelObj.transform, "NoteTitle", 22, FontStyles.Bold, new Color(1f, 0.85f, 0.4f));
        titleText.alignment = TextAlignmentOptions.Center;

        // Çizgi Ayırıcı
        GameObject divider = new GameObject("Divider", typeof(RectTransform), typeof(Image));
        divider.transform.SetParent(panelObj.transform, false);
        divider.GetComponent<Image>().color = new Color(0.9f, 0.75f, 0.4f, 0.5f);
        divider.GetComponent<RectTransform>().sizeDelta = new Vector2(0, 2f);

        // 2. İçerik Metni
        contentText = CreateText(panelObj.transform, "NoteContent", 15, FontStyles.Normal, new Color(0.9f, 0.9f, 0.9f));
        contentText.alignment = TextAlignmentOptions.TopLeft;
        contentText.enableWordWrapping = true;

        LayoutElement contentLE = contentText.gameObject.AddComponent<LayoutElement>();
        contentLE.preferredHeight = 170f;

        // 3. Kapat Butonu
        GameObject btnObj = new GameObject("CloseButton", typeof(RectTransform), typeof(Image), typeof(Button), typeof(LayoutElement));
        btnObj.transform.SetParent(panelObj.transform, false);

        Image btnImg = btnObj.GetComponent<Image>();
        btnImg.color = new Color(0.8f, 0.25f, 0.25f, 0.9f); // Koyu Kırmızı

        closeButton = btnObj.GetComponent<Button>();
        closeButtonText = CreateText(btnObj.transform, "ButtonText", 14, FontStyles.Bold, Color.white);
        closeButtonText.text = "Kapat [ESC / E]";
        closeButtonText.alignment = TextAlignmentOptions.Center;

        LayoutElement btnLE = btnObj.GetComponent<LayoutElement>();
        btnLE.preferredHeight = 34f;

        closeButton.onClick.AddListener(Hide);
        notePanel = panelObj;
    }

    private TextMeshProUGUI CreateText(Transform parent, string objectName, float fontSize, FontStyles style, Color color)
    {
        GameObject textObj = new GameObject(objectName, typeof(RectTransform), typeof(TextMeshProUGUI));
        textObj.transform.SetParent(parent, false);

        TextMeshProUGUI tmp = textObj.GetComponent<TextMeshProUGUI>();
        tmp.fontSize = fontSize;
        tmp.fontStyle = style;
        tmp.color = color;
        tmp.raycastTarget = false;
        return tmp;
    }

    /// <summary>Notu gösterir ve içeriğini doldurur.</summary>
    public void ShowNote(string title, string content)
    {
        EnsureUIBuilt();

        if (titleText != null) titleText.text = title;
        if (contentText != null) contentText.text = content;

        if (notePanel != null)
            notePanel.SetActive(true);
    }

    /// <summary>Not panelini kapatır.</summary>
    public void Hide()
    {
        if (notePanel != null)
            notePanel.SetActive(false);
    }
}
