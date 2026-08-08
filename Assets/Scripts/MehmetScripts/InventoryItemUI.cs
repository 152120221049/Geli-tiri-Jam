using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

/// <summary>
/// Envanterdeki eşya UI elemanı.
/// Sürükle-Bırak (Drag & Drop), 'R' ile döndürme, çift tıklama ile kullanım
/// ve hover/tıklama ile Tooltip tetiklemesi işlemlerini yönetir.
/// Yeni ve Eski Input System ile uyumludur.
/// </summary>
[RequireComponent(typeof(CanvasGroup))]
public class InventoryItemUI : MonoBehaviour,
    IBeginDragHandler, IDragHandler, IEndDragHandler, IDropHandler,
    IPointerClickHandler, IPointerEnterHandler, IPointerExitHandler
{
    [Header("UI Referansları")]
    [SerializeField] private Image iconImage;
    [SerializeField] private TextMeshProUGUI stackText;
    [SerializeField] private Image durabilityBar;
    [SerializeField] private Image durabilityBarBg;

    [Header("Ayarlar")]
    [SerializeField] private float doubleClickThreshold = 0.3f;
    [SerializeField] private float hoverDelay = 0.4f;

    /// <summary>Bu UI elemanının temsil ettiği envanter eşyası.</summary>
    public InventoryItem Item { get; private set; }

    // ── Dahili Referanslar ──
    private RectTransform rectTransform;
    private Canvas rootCanvas;
    private CanvasGroup canvasGroup;
    private Transform originalParent;
    private int originalSiblingIndex;
    private Vector2 originalAnchoredPos;

    // ── Drag State ──
    private bool isDragging;

    // ── Double Click ──
    private float lastClickTime;

    // ── Hover Tooltip ──
    private float hoverTimer;
    private bool isHovering;
    private bool tooltipShown;

    // ═══════════════════════════════════════════
    //  LIFECYCLE
    // ═══════════════════════════════════════════

    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        canvasGroup = GetComponent<CanvasGroup>();
        rootCanvas = GetComponentInParent<Canvas>();

        // Root canvas'ı bul
        if (rootCanvas != null)
        {
            Canvas[] canvases = GetComponentsInParent<Canvas>();
            if (canvases.Length > 0)
                rootCanvas = canvases[canvases.Length - 1]; // En üstteki canvas
        }
    }

    private void Update()
    {
        // Hover → tooltip delay
        if (isHovering && !tooltipShown && !isDragging)
        {
            hoverTimer += Time.unscaledDeltaTime;
            if (hoverTimer >= hoverDelay)
            {
                ShowTooltip();
            }
        }

        // Sürüklerken 'R' tuşuyla döndürme
        if (isDragging && InventoryInput.IsRotateKeyPressed())
        {
            RotateItem();
        }

        // Hover esnasında 1-4 rakam tuşları ile Hotbar'a hızlı gönderme
        if (isHovering && !isDragging && Item != null)
        {
            for (int i = 0; i < 4; i++)
            {
                if (InventoryInput.IsDigitKeyPressed(i))
                {
                    InventoryManager.Instance.QuickMoveToHotbar(Item, i);
                    break;
                }
            }
        }
    }

    // ═══════════════════════════════════════════
    //  SETUP & VISUALS
    // ═══════════════════════════════════════════

    /// <summary>Bu UI elemanını bir eşya ile ilişkilendirir ve görselleri günceller.</summary>
    public void Setup(InventoryItem item)
    {
        Item = item;
        UpdateVisuals();
    }

    private void EnsureIconImageBuilt()
    {
        Image rootImg = GetComponent<Image>();

        if (iconImage == null || iconImage == rootImg)
        {
            // Önce root dışındaki child Image'ları ara
            Image[] childImages = GetComponentsInChildren<Image>(true);
            foreach (var img in childImages)
            {
                if (img != rootImg)
                {
                    iconImage = img;
                    break;
                }
            }

            // Çocuklarda da bulunamadıysa "ItemIcon" adında yeni child Image oluştur
            if (iconImage == null || iconImage == rootImg)
            {
                Transform existingChild = transform.Find("ItemIcon");
                GameObject iconObj;
                if (existingChild != null)
                {
                    iconObj = existingChild.gameObject;
                }
                else
                {
                    iconObj = new GameObject("ItemIcon", typeof(RectTransform), typeof(Image));
                    iconObj.transform.SetParent(transform, false);
                }

                iconImage = iconObj.GetComponent<Image>();
            }
        }

        if (iconImage != null)
        {
            iconImage.color = Color.white;
        }

        // ── Adet metni (stackText) eksikse otomatik ekle ──
        if (stackText == null)
        {
            stackText = GetComponentInChildren<TMPro.TextMeshProUGUI>(true);
            if (stackText == null)
            {
                GameObject textObj = new GameObject("StackText", typeof(RectTransform), typeof(TMPro.TextMeshProUGUI));
                textObj.transform.SetParent(transform, false);

                RectTransform textRT = textObj.GetComponent<RectTransform>();
                textRT.anchorMin = Vector2.zero;
                textRT.anchorMax = Vector2.one;
                textRT.offsetMin = new Vector2(0, 2);
                textRT.offsetMax = new Vector2(-4, 0);

                stackText = textObj.GetComponent<TMPro.TextMeshProUGUI>();
                stackText.fontSize = 14f;
                stackText.fontStyle = TMPro.FontStyles.Bold;
                stackText.alignment = TMPro.TextAlignmentOptions.BottomRight;
                stackText.raycastTarget = false;
            }
        }
    }

    /// <summary>İkon, adet ve dayanıklılık görsellerini günceller.</summary>
    public void UpdateVisuals()
    {
        if (Item == null || Item.itemData == null) return;

        EnsureIconImageBuilt();

        // ── İkon ──
        if (iconImage != null)
        {
            iconImage.sprite = Item.itemData.icon;
            iconImage.enabled = Item.itemData.icon != null;
            iconImage.color = Color.white; // Görsel şeffaflığı önle
            iconImage.raycastTarget = false; // Tıklama tespiti kök (root) Görsel bileşeni tarafından yapılır

            RectTransform iconRT = iconImage.rectTransform;

            if (Item.isRotated)
            {
                // Döndürülmüş eşyalarda genislik ve yükseklik takas edilip -90 derece çevrilir
                iconRT.anchorMin = new Vector2(0.5f, 0.5f);
                iconRT.anchorMax = new Vector2(0.5f, 0.5f);
                iconRT.pivot = new Vector2(0.5f, 0.5f);
                iconRT.anchoredPosition = Vector2.zero;

                Vector2 parentSize = rectTransform.sizeDelta;
                iconRT.sizeDelta = new Vector2(parentSize.y, parentSize.x);
                iconRT.localRotation = Quaternion.Euler(0, 0, -90f);
            }
            else
            {
                // Normal eşyalarda ana boyuta tam esnetilir
                iconRT.anchorMin = Vector2.zero;
                iconRT.anchorMax = Vector2.one;
                iconRT.offsetMin = Vector2.zero;
                iconRT.offsetMax = Vector2.zero;
                iconRT.pivot = new Vector2(0.5f, 0.5f);
                iconRT.localRotation = Quaternion.identity;
            }
        }

        // ── Adet & Sadak (Quiver) Ok Sayısı Metni ──
        if (stackText != null)
        {
            if (Item.itemData.itemType == ItemType.Quiver && PlayerEquipmentController.Instance != null)
            {
                QuiverData qData = PlayerEquipmentController.Instance.GetQuiverData(Item);
                stackText.gameObject.SetActive(true);
                stackText.text = qData.storedArrowCount.ToString();

                if (qData.storedArrowCount <= 0)
                {
                    stackText.color = new Color(0.6f, 0.6f, 0.6f, 0.9f); // Gri (Boş Sadak)
                }
                else if (qData.storedArrowType != null && (qData.storedArrowType.itemName.Contains("Ateşli") || qData.storedArrowType.leavesFireArea))
                {
                    stackText.color = new Color(1f, 0.45f, 0.1f, 1f); // Ateş Turuncusu (Ateşli Ok)
                }
                else
                {
                    stackText.color = new Color(0.95f, 0.95f, 0.75f, 1f); // Sarımsı Beyaz (Normal Ok)
                }
            }
            else if (Item.currentStack > 1)
            {
                stackText.gameObject.SetActive(true);
                stackText.text = Item.currentStack.ToString();
                stackText.color = Color.white;
            }
            else
            {
                stackText.gameObject.SetActive(false);
            }
        }

        // ── Dayanıklılık Çubuğu (Yeşil → Sarı → Kırmızı) ──
        bool showDurability = Item.itemData.HasDurability && !Item.itemData.IsInfinite;

        if (durabilityBarBg != null)
            durabilityBarBg.gameObject.SetActive(showDurability);

        if (durabilityBar != null)
        {
            durabilityBar.gameObject.SetActive(showDurability);

            if (showDurability)
            {
                float percent = Item.DurabilityPercent;
                durabilityBar.fillAmount = percent;

                // Renk geçişi: yeşil(1.0) → sarı(0.5) → kırmızı(0.0)
                if (percent > 0.5f)
                    durabilityBar.color = Color.Lerp(Color.yellow, Color.green, (percent - 0.5f) * 2f);
                else
                    durabilityBar.color = Color.Lerp(Color.red, Color.yellow, percent * 2f);
            }
        }
    }

    // ═══════════════════════════════════════════
    //  DRAG & DROP
    // ═══════════════════════════════════════════

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (Item == null) return;

        isDragging = true;

        // Orijinal pozisyonu kaydet
        originalParent = transform.parent;
        originalSiblingIndex = transform.GetSiblingIndex();
        originalAnchoredPos = rectTransform.anchoredPosition;

        // Sürükleme görünümü
        canvasGroup.alpha = 0.7f;
        canvasGroup.blocksRaycasts = false;

        // En üst katmana taşı (diğer UI elemanlarının üstünde görünsün)
        transform.SetParent(rootCanvas.transform);
        transform.SetAsLastSibling();

        // Tooltip'i kapat
        if (ItemTooltipUI.Instance != null)
            ItemTooltipUI.Instance.Hide();

        tooltipShown = false;
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (!isDragging) return;
        rectTransform.anchoredPosition += eventData.delta / rootCanvas.scaleFactor;

        if (InventoryUI.Instance != null)
        {
            InventoryGridData targetGrid = InventoryUI.Instance.GetGridUnderPointer(eventData);
            if (targetGrid != null)
            {
                Vector2Int cell = InventoryUI.Instance.GetNearestCell(rectTransform, targetGrid, Item, Vector2Int.zero);
                InventoryUI.Instance.UpdateDragHighlight(Item, targetGrid, cell);
            }
            else
            {
                InventoryUI.Instance.ClearDragHighlight();
            }
        }
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        isDragging = false;
        canvasGroup.alpha = 1f;
        canvasGroup.blocksRaycasts = true;

        InventoryGridData targetGrid = (InventoryUI.Instance != null) ? InventoryUI.Instance.GetGridUnderPointer(eventData) : null;

        if (InventoryUI.Instance != null)
            InventoryUI.Instance.ClearDragHighlight();

        bool droppedOnInventoryUI = (targetGrid != null) || (eventData.pointerEnter != null && 
            (eventData.pointerEnter.GetComponentInParent<InventoryDropZone>() != null || 
             eventData.pointerEnter.GetComponentInParent<InventoryItemUI>() != null));

        if (targetGrid != null && Item != null && InventoryManager.Instance != null)
        {
            Vector2Int cell = InventoryUI.Instance.GetNearestCell(rectTransform, targetGrid, Item, Vector2Int.zero);
            InventoryManager.Instance.MoveItem(Item, targetGrid, cell.x, cell.y);
            return;
        }

        if (!droppedOnInventoryUI && InventoryManager.Instance != null && Item != null)
        {
            InventoryManager.Instance.DropItemToWorld(Item);
            return;
        }

        if (originalParent != null)
        {
            transform.SetParent(originalParent);
            transform.SetSiblingIndex(originalSiblingIndex);
            rectTransform.anchoredPosition = originalAnchoredPos;
        }

        if (InventoryManager.Instance != null)
            InventoryManager.Instance.NotifyInventoryChanged();
    }

    /// <summary>
    /// Eşya üzerine başka bir eşya bırakıldığında çağrılır.
    /// Eşyanın kapladığı alan üzerindeki tıklamaların hücreye düşmesini engeller.
    /// </summary>
    public void OnDrop(PointerEventData eventData)
    {
        if (Item == null || InventoryManager.Instance == null) return;

        InventoryGridData grid = InventoryManager.Instance.GetGridContaining(Item);
        if (grid == null) return;

        InventoryItemUI draggedItemUI = eventData.pointerDrag?.GetComponent<InventoryItemUI>();
        if (draggedItemUI == null || draggedItemUI.Item == null) return;

        InventoryItem draggedItem = draggedItemUI.Item;

        // Eşyanın sol üst hücresine en yakın yere taşımayı dene
        Vector2Int targetCell = new Vector2Int(Item.gridX, Item.gridY);
        if (InventoryUI.Instance != null)
        {
            targetCell = InventoryUI.Instance.GetNearestCell(draggedItemUI.GetComponent<RectTransform>(), grid, draggedItem, targetCell);
        }

        bool success = InventoryManager.Instance.MoveItem(draggedItem, grid, targetCell.x, targetCell.y);
        if (success)
        {
            Debug.Log($"✅ {draggedItem.itemData.itemName} → Grid[{targetCell.x},{targetCell.y}] taşındı.");
        }
        else
        {
            Debug.LogWarning($"❌ {draggedItem.itemData.itemName} → {Item.itemData.itemName} üzerine yerleştirilemedi!");
        }
    }

    /// <summary>Sürükleme sırasında eşyayı döndürür ve görsel boyutlarını günceller.</summary>
    private void RotateItem()
    {
        if (Item == null) return;
        Item.ToggleRotation();

        // sizeDelta boyutunu ters çevir
        if (rectTransform != null)
        {
            float oldW = rectTransform.sizeDelta.x;
            float oldH = rectTransform.sizeDelta.y;
            rectTransform.sizeDelta = new Vector2(oldH, oldW);
        }

        UpdateVisuals();
    }

    // ═══════════════════════════════════════════
    //  CLICK & DOUBLE CLICK
    // ═══════════════════════════════════════════

    public void OnPointerClick(PointerEventData eventData)
    {
        if (Item == null) return;
        if (eventData.dragging) return;

        // ── Sağ Tıklama → Envanter <-> Hotbar arası hızlı taşı ──
        if (eventData.button == PointerEventData.InputButton.Right)
        {
            if (InventoryManager.Instance != null)
            {
                if (InventoryManager.Instance.HotbarGrid.Contains(Item))
                    InventoryManager.Instance.QuickMoveToInventory(Item);
                else
                    InventoryManager.Instance.QuickMoveToHotbar(Item);

                if (ItemTooltipUI.Instance != null)
                    ItemTooltipUI.Instance.Hide();
            }
            return;
        }

        float timeSinceLastClick = Time.unscaledTime - lastClickTime;

        if (timeSinceLastClick <= doubleClickThreshold && timeSinceLastClick > 0f)
        {
            // ── Çift tıklama → Eşyayı kullan ──
            OnDoubleClick();
            lastClickTime = 0f;
        }
        else
        {
            // ── Tek tıklama → Tooltip'i sabitle (Kullan butonuna tıklanabilsin) ──
            lastClickTime = Time.unscaledTime;
            ShowTooltip(pin: true);
        }
    }

    private void OnDoubleClick()
    {
        if (Item != null && Item.CanUse())
        {
            InventoryManager.Instance.UseItem(Item);
            UpdateVisuals();
        }
    }

    // ═══════════════════════════════════════════
    //  HOVER & TOOLTIP
    // ═══════════════════════════════════════════

    public void OnPointerEnter(PointerEventData eventData)
    {
        isHovering = true;
        hoverTimer = 0f;
        tooltipShown = false;
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        isHovering = false;
        hoverTimer = 0f;
        tooltipShown = false;

        // Sabitlenmemişse gizle (Sabitlenmişse 'Kullan' butonuna basmak için açık kalır)
        if (ItemTooltipUI.Instance != null && !ItemTooltipUI.Instance.IsPinned)
            ItemTooltipUI.Instance.Hide();
    }

    private void ShowTooltip(bool pin = false)
    {
        if (Item == null) return;

        // Envanter açık değilse tooltip gösterme (hotbar'da oyun sırasında tooltip istemiyoruz)
        if (InventoryManager.Instance != null && !InventoryManager.Instance.IsInventoryOpen)
            return;

        tooltipShown = true;

        if (ItemTooltipUI.Instance != null)
            ItemTooltipUI.Instance.Show(Item, InventoryInput.GetMousePosition(), pin);
    }
}
