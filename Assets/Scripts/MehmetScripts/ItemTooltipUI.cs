using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Eşya detay paneli (Tooltip / Toolbox).
/// Fare ile eşya üzerine gelindiğinde veya tıklandığında gösterilir.
/// Eşya adı, açıklaması, dayanıklılık bilgisi ve "Kullan" butonu içerir.
/// Sahnede önceden kurulmuş UI yoksa otomatik şık bir panel oluşturur.
/// </summary>
public class ItemTooltipUI : MonoBehaviour
{
    private static ItemTooltipUI _instance;
    public static ItemTooltipUI Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = FindObjectOfType<ItemTooltipUI>();
                if (_instance == null)
                {
                    Canvas mainCanvas = FindObjectOfType<Canvas>();
                    GameObject go = new GameObject("ItemTooltipUI");
                    if (mainCanvas != null)
                        go.transform.SetParent(mainCanvas.transform, false);
                    _instance = go.AddComponent<ItemTooltipUI>();
                }
            }
            return _instance;
        }
        private set => _instance = value;
    }

    [Header("Panel Referansları")]
    [SerializeField] private GameObject tooltipPanel;
    [SerializeField] private TextMeshProUGUI itemNameText;
    [SerializeField] private TextMeshProUGUI descriptionText;
    [SerializeField] private TextMeshProUGUI durabilityText;
    [SerializeField] private TextMeshProUGUI stackText;
    [SerializeField] private Button useButton;
    [SerializeField] private TextMeshProUGUI useButtonText;

    [Header("Ayarlar")]
    [SerializeField] private Vector2 offset = new Vector2(15f, -15f);

    private InventoryItem currentItem;
    private RectTransform panelRect;
    private RectTransform canvasRect;
    private Canvas canvas;
    private CanvasGroup canvasGroup;
    private bool isPinned = false;
    private Vector2 pinnedScreenPos; // Sabitleme anındaki ekran pozisyonu

    /// <summary>Tooltip sabitlenmiş durumda mı (tıklama ile açılmış)?</summary>
    public bool IsPinned => isPinned;

    private void Awake()
    {
        Instance = this;
        InitCanvasAndPanel();
        Hide();
    }

    private void Update()
    {
        // Sabitlenmemişse farenin ucunda takip et
        if (tooltipPanel != null && tooltipPanel.activeSelf && !isPinned)
        {
            UpdatePosition(InventoryInput.GetMousePosition());
        }
    }

    private void InitCanvasAndPanel()
    {
        canvas = GetComponentInParent<Canvas>();
        if (canvas == null)
            canvas = FindObjectOfType<Canvas>();

        if (canvas != null)
            canvasRect = canvas.transform as RectTransform;

        EnsureUIBuilt();

        if (tooltipPanel != null)
        {
            panelRect = tooltipPanel.GetComponent<RectTransform>();
            EnsureCanvasOverride(tooltipPanel);
        }
    }

    /// <summary>
    /// Tooltip panelinin tüm eşya Canvas'larının ÜSTÜNDE çizilmesini garanti eder.
    /// Hem prebuilt hem auto-generated paneller için çalışır. Pivot'u sol üst (0,1) yapar.
    /// </summary>
    private void EnsureCanvasOverride(GameObject panel)
    {
        Canvas c = panel.GetComponent<Canvas>();
        if (c == null)
            c = panel.AddComponent<Canvas>();
        c.overrideSorting = true;
        c.sortingOrder = 100;

        if (panel.GetComponent<UnityEngine.UI.GraphicRaycaster>() == null)
            panel.AddComponent<UnityEngine.UI.GraphicRaycaster>();

        canvasGroup = panel.GetComponent<CanvasGroup>();
        if (canvasGroup == null)
            canvasGroup = panel.AddComponent<CanvasGroup>();

        // Pivot'u (0, 1) yani Sol-Üst köşe yap → panel farenin ucunun sağ-altına doğru açılır
        RectTransform rt = panel.GetComponent<RectTransform>();
        if (rt != null)
        {
            rt.pivot = new Vector2(0f, 1f);
        }
    }

    private void EnsureUIBuilt()
    {
        if (tooltipPanel != null) return;

        if (canvas == null)
        {
            canvas = FindObjectOfType<Canvas>();
            if (canvas == null) return;
        }
        canvasRect = canvas.transform as RectTransform;

        // Şık koyu füme panel oluştur
        GameObject panelObj = new GameObject("TooltipPanel", typeof(RectTransform), typeof(Image), typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));
        panelObj.transform.SetParent(canvas.transform, false);
        panelRect = panelObj.GetComponent<RectTransform>();

        // Rect & Pivot
        panelRect.anchorMin = new Vector2(0, 1);
        panelRect.anchorMax = new Vector2(0, 1);
        panelRect.pivot = new Vector2(0, 1);
        panelRect.sizeDelta = new Vector2(220f, 0f);

        // Arka plan rengi
        Image bgImg = panelObj.GetComponent<Image>();
        bgImg.color = new Color(0.08f, 0.1f, 0.14f, 0.95f);

        // Outline
        Outline outline = panelObj.AddComponent<Outline>();
        outline.effectColor = new Color(0.3f, 0.6f, 0.9f, 0.7f);
        outline.effectDistance = new Vector2(1, -1);

        // Vertical Layout Group
        VerticalLayoutGroup vlg = panelObj.GetComponent<VerticalLayoutGroup>();
        vlg.padding = new RectOffset(12, 12, 10, 10);
        vlg.spacing = 6f;
        vlg.childControlWidth = true;
        vlg.childControlHeight = true;
        vlg.childForceExpandWidth = true;
        vlg.childForceExpandHeight = false;

        ContentSizeFitter csf = panelObj.GetComponent<ContentSizeFitter>();
        csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        csf.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;

        // 1. Eşya Adı
        itemNameText = CreateText(panelObj.transform, "ItemTitle", 15, FontStyles.Bold, new Color(1f, 0.85f, 0.3f));

        // 2. Açıklama
        descriptionText = CreateText(panelObj.transform, "ItemDesc", 12, FontStyles.Italic, new Color(0.85f, 0.85f, 0.85f));

        // 3. Adet
        stackText = CreateText(panelObj.transform, "ItemStack", 12, FontStyles.Normal, new Color(0.7f, 0.9f, 1f));

        // 4. Dayanıklılık
        durabilityText = CreateText(panelObj.transform, "ItemDurability", 12, FontStyles.Normal, new Color(0.5f, 1f, 0.5f));

        // 5. Kullan Butonu
        GameObject btnObj = new GameObject("UseButton", typeof(RectTransform), typeof(Image), typeof(Button), typeof(LayoutElement));
        btnObj.transform.SetParent(panelObj.transform, false);

        Image btnImg = btnObj.GetComponent<Image>();
        btnImg.color = new Color(0.2f, 0.55f, 0.85f, 1f);

        useButton = btnObj.GetComponent<Button>();
        useButtonText = CreateText(btnObj.transform, "ButtonText", 13, FontStyles.Bold, Color.white);
        useButtonText.alignment = TextAlignmentOptions.Center;

        LayoutElement le = btnObj.GetComponent<LayoutElement>();
        le.preferredHeight = 26f;

        tooltipPanel = panelObj;
        if (useButton != null)
            useButton.onClick.AddListener(OnUseClicked);
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

    private void Start()
    {
        if (useButton != null)
            useButton.onClick.AddListener(OnUseClicked);
    }

    /// <summary>
    /// Tooltip panelini gösterir ve belirtilen eşya bilgilerini doldurur.
    /// </summary>
    public void Show(InventoryItem item, Vector2 screenPosition)
    {
        if (item == null || item.itemData == null) return;

        currentItem = item;

        // ── Eşya Adı ──
        if (itemNameText != null)
            itemNameText.text = item.itemData.itemName;

        // ── Açıklama ──
        if (descriptionText != null)
            descriptionText.text = item.itemData.description;

        // ── Adet Bilgisi ──
        if (stackText != null)
        {
            if (item.itemData.maxStack > 1)
            {
                stackText.gameObject.SetActive(true);
                stackText.text = $"Adet: {item.currentStack}/{item.itemData.maxStack}";
            }
            else
            {
                stackText.gameObject.SetActive(false);
            }
        }

        // ── Dayanıklılık Bilgisi ──
        if (durabilityText != null)
        {
            if (item.itemData.HasDurability)
            {
                durabilityText.gameObject.SetActive(true);
                durabilityText.text = $"Dayanıklılık: {item.currentDurability}/{item.itemData.maxDurability}";
            }
            else if (item.itemData.itemType == ItemType.KeyItem)
            {
                durabilityText.gameObject.SetActive(true);
                durabilityText.text = "Dayanıklılık: ∞";
            }
            else
            {
                durabilityText.gameObject.SetActive(false);
            }
        }

        // ── Kullan Butonu ──
        if (useButton != null)
        {
            bool canUse = item.CanUse();
            useButton.gameObject.SetActive(canUse);

            if (canUse && useButtonText != null)
            {
                switch (item.itemData.itemType)
                {
                    case ItemType.Consumable:
                        useButtonText.text = "Tüket";
                        break;
                    case ItemType.WeaponTool:
                        useButtonText.text = "Kullan";
                        break;
                    case ItemType.KeyItem:
                        useButtonText.text = "Kullan";
                        break;
                    default:
                        useButtonText.text = "Kullan";
                        break;
                }
            }
        }

        // Raycast bloklama kontrolü: Sadece sabitlenmişse (tıklanmışsa) raycast alır.
        // Hover modundayken raycast almaz → alttaki ItemUI'a OnPointerExit tetikletmez!
        if (canvasGroup == null && tooltipPanel != null)
            canvasGroup = tooltipPanel.GetComponent<CanvasGroup>();

        if (canvasGroup != null)
        {
            canvasGroup.blocksRaycasts = isPinned;
            canvasGroup.interactable = isPinned;
        }

        tooltipPanel.SetActive(true);
        tooltipPanel.transform.SetAsLastSibling(); // Sibling sırasında da en sonda olsun
        UpdatePosition(screenPosition);
    }

    /// <summary>
    /// Tooltip panelini gösterir ve sabitlenme durumunu ayarlar.
    /// pin = true ise fareyi takip etmez, tıklanabilir kalır.
    /// </summary>
    public void Show(InventoryItem item, Vector2 screenPosition, bool pin)
    {
        isPinned = pin;
        if (pin) pinnedScreenPos = screenPosition;
        Show(item, screenPosition);
    }

    /// <summary>Tooltip pozisyonunu günceller (fare takibi için).</summary>
    public void UpdatePosition(Vector2 screenPosition)
    {
        if (tooltipPanel == null || !tooltipPanel.activeSelf || canvasRect == null) return;

        Camera cam = (canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay) ? canvas.worldCamera : null;

        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, screenPosition + offset, cam, out Vector2 localPoint))
        {
            panelRect.anchoredPosition = localPoint;
            ClampToCanvas();
        }
    }

    /// <summary>Tooltip'in Canvas dışına taşmamasını sağlar.</summary>
    private void ClampToCanvas()
    {
        if (canvasRect == null || panelRect == null) return;

        Vector3[] panelCorners = new Vector3[4];
        Vector3[] canvasCorners = new Vector3[4];
        panelRect.GetWorldCorners(panelCorners);
        canvasRect.GetWorldCorners(canvasCorners);

        Vector3 pos = panelRect.position;

        // Sağ taşma
        if (panelCorners[2].x > canvasCorners[2].x)
            pos.x -= (panelCorners[2].x - canvasCorners[2].x);

        // Sol taşma
        if (panelCorners[0].x < canvasCorners[0].x)
            pos.x += (canvasCorners[0].x - panelCorners[0].x);

        // Üst taşma
        if (panelCorners[1].y > canvasCorners[1].y)
            pos.y -= (panelCorners[1].y - canvasCorners[1].y);

        // Alt taşma
        if (panelCorners[0].y < canvasCorners[0].y)
            pos.y += (canvasCorners[0].y - panelCorners[0].y);

        panelRect.position = pos;
    }

    /// <summary>Tooltip panelini gizler.</summary>
    public void Hide()
    {
        if (tooltipPanel != null)
            tooltipPanel.SetActive(false);
        currentItem = null;
        isPinned = false;
    }

    /// <summary>Tooltip şu anda görünür mü?</summary>
    public bool IsVisible => tooltipPanel != null && tooltipPanel.activeSelf;

    /// <summary>"Kullan" butonuna tıklandığında çağrılır.</summary>
    private void OnUseClicked()
    {
        if (currentItem == null) return;

        InventoryManager.Instance.UseItem(currentItem);

        // Eşya tükendiyse veya kırıldıysa tooltip'i kapat
        if (currentItem.IsBroken || currentItem.IsEmpty)
        {
            Hide();
        }
        else
        {
            // Bilgileri güncelle — pozisyonu DEĞİŞTİRME, sabitlenen yerde kalsın
            Show(currentItem, pinnedScreenPos, true);
        }
    }
}
