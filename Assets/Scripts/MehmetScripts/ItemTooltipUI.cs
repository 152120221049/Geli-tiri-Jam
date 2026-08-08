using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Eşya detay paneli (Tooltip / Toolbox).
/// Fare ile eşya üzerine gelindiğinde veya tıklandığında gösterilir.
/// Eşya adı, açıklaması, dayanıklılık bilgisi ve "Kullan" butonu içerir.
/// </summary>
public class ItemTooltipUI : MonoBehaviour
{
    public static ItemTooltipUI Instance { get; private set; }

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

    private void Awake()
    {
        Instance = this;
        panelRect = tooltipPanel.GetComponent<RectTransform>();
        canvas = GetComponentInParent<Canvas>();
        if (canvas != null)
            canvasRect = canvas.transform as RectTransform;

        Hide();
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

        tooltipPanel.SetActive(true);
        UpdatePosition(screenPosition);
    }

    /// <summary>Tooltip pozisyonunu günceller (fare takibi için).</summary>
    public void UpdatePosition(Vector2 screenPosition)
    {
        if (!tooltipPanel.activeSelf || canvasRect == null) return;

        Vector2 localPoint;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            canvasRect,
            screenPosition + offset,
            canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera,
            out localPoint);

        panelRect.localPosition = localPoint;

        // Ekran sınırlarına kenetleme
        ClampToCanvas();
    }

    /// <summary>Tooltip'in Canvas dışına taşmamasını sağlar.</summary>
    private void ClampToCanvas()
    {
        if (canvasRect == null) return;

        Vector3[] panelCorners = new Vector3[4];
        Vector3[] canvasCorners = new Vector3[4];
        panelRect.GetWorldCorners(panelCorners);
        canvasRect.GetWorldCorners(canvasCorners);

        // panelCorners: 0=bottomLeft, 1=topLeft, 2=topRight, 3=bottomRight
        // canvasCorners: aynı sıra

        Vector3 pos = panelRect.localPosition;

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

        panelRect.localPosition = pos;
    }

    /// <summary>Tooltip panelini gizler.</summary>
    public void Hide()
    {
        if (tooltipPanel != null)
            tooltipPanel.SetActive(false);
        currentItem = null;
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
            // Bilgileri güncelle (adet/dayanıklılık değişmiş olabilir)
            Show(currentItem, InventoryInput.GetMousePosition());
        }
    }
}
