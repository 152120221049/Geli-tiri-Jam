using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

/// <summary>
/// Envanter ve Hotbar UI yöneticisi.
/// Grid hücrelerini ve Hotbar slotlarını oluşturur, eşya UI elemanlarını yönetir,
/// aktif slot vurgusunu günceller ve Hold-to-Open dolum çubuğunu gösterir.
/// </summary>
public class InventoryUI : MonoBehaviour
{
    [Header("Panel Referansları")]
    [SerializeField] private GameObject inventoryPanel;
    [SerializeField] private RectTransform inventoryGridContainer;
    [SerializeField] private RectTransform hotbarContainer;

    [Header("Hold-to-Open")]
    [SerializeField] private GameObject holdProgressRoot;
    [SerializeField] private Image holdProgressBar;
    [Tooltip("Karakterin Transform'u (Boş bırakılırsa 'Player' tag'li obje veya CharacterController otomatik bulunur)")]
    [SerializeField] private Transform playerTransform;
    [Tooltip("Karakterin başının ne kadar üstünde görüneceği offset'i (ör. Y: 1.5)")]
    [SerializeField] private Vector3 playerOffset = new Vector3(0f, 1.5f, 0f);

    [Header("Prefab'lar")]
    [Tooltip("Grid hücresi prefab'ı (Image bileşeni olan bir UI elemanı)")]
    [SerializeField] private GameObject cellPrefab;

    [Tooltip("Eşya UI prefab'ı (InventoryItemUI bileşeni olan bir UI elemanı)")]
    [SerializeField] private GameObject itemUIPrefab;

    [Header("Grid Görünüm Ayarları")]
    [SerializeField] private float cellSize = 64f;
    [SerializeField] private float hotbarCellSize = 48f;
    [SerializeField] private float cellSpacing = 4f;

    [Header("Hotbar Vurgu Renkleri")]
    [SerializeField] private Color activeSlotColor = new Color(1f, 0.84f, 0f, 0.8f);     // Altın sarısı
    [SerializeField] private Color normalSlotColor = new Color(0.3f, 0.3f, 0.3f, 0.5f);  // Koyu gri

    // ── Dahili Referanslar ──
    private InventoryManager manager;

    // Grid hücre Image'ları
    private Image[,] inventoryCellImages;
    private Image[] hotbarCellImages;

    // Aktif eşya UI elemanları
    private List<InventoryItemUI> activeItemUIs = new List<InventoryItemUI>();

    // Hotbar vurgu çerçeveleri
    private Outline[] hotbarOutlines;

    public static InventoryUI Instance { get; private set; }

    private void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
        manager = InventoryManager.Instance;

        if (manager == null)
        {
            Debug.LogError("InventoryUI: InventoryManager bulunamadı!");
            return;
        }

        // Hold Progress Bar'ın koda tam uyumlu dolum tipini garanti et
        if (holdProgressBar != null)
        {
            holdProgressBar.type = Image.Type.Filled;

            // Eğer Image sprite'ı boşsa (None), dolum görünümünün çalışması için varsayılan sprite oluştur
            if (holdProgressBar.sprite == null)
            {
                Texture2D whiteTex = Texture2D.whiteTexture;
                holdProgressBar.sprite = Sprite.Create(whiteTex, new Rect(0, 0, whiteTex.width, whiteTex.height), new Vector2(0.5f, 0.5f));
            }
        }

        // Oyuncu Transform'unu otomatik bul
        FindPlayerTransform();

        BuildInventoryGrid();
        BuildHotbar();
        RefreshAllItems();

        // Event'lere abone ol
        manager.OnInventoryToggled += OnInventoryToggled;
        manager.OnActiveSlotChanged += OnActiveSlotChanged;
        manager.OnInventoryChanged += RefreshAllItems;

        // Başlangıçta envanter kapalı
        if (inventoryPanel != null)
            inventoryPanel.SetActive(false);

        UpdateHotbarHighlight();
    }

    private void FindPlayerTransform()
    {
        if (playerTransform != null) return;

        // 1) "Player" tag'ine sahip objeyi ara
        GameObject pObj = GameObject.FindGameObjectWithTag("Player");
        if (pObj != null)
        {
            playerTransform = pObj.transform;
            return;
        }

        // 2) CharacterController bileşenini ara
        var controller = FindObjectOfType<MemoScripts.CharacterController>();
        if (controller != null)
        {
            playerTransform = controller.transform;
        }
    }

    private void Update()
    {
        // Hold-to-Open progress bar güncelle
        UpdateHoldProgressBar();
    }

    private void OnDestroy()
    {
        if (manager != null)
        {
            manager.OnInventoryToggled -= OnInventoryToggled;
            manager.OnActiveSlotChanged -= OnActiveSlotChanged;
            manager.OnInventoryChanged -= RefreshAllItems;
        }
    }

    // ═══════════════════════════════════════════
    //  GRID OLUŞTURMA
    // ═══════════════════════════════════════════

    /// <summary>Envanter grid hücrelerini oluşturur veya var olanları bağlar.</summary>
    private void BuildInventoryGrid()
    {
        int w = manager.InventoryGrid.width;
        int h = manager.InventoryGrid.height;
        if (inventoryGridContainer == null) return;
        inventoryCellImages = new Image[w, h];

        // Önceki overlay'i kaldır (varsa)
        int prebuiltChildCount = inventoryGridContainer.childCount;

        bool hasPrebuilt = prebuiltChildCount >= w * h;
        bool hasGridLayout = inventoryGridContainer.GetComponent<UnityEngine.UI.GridLayoutGroup>() != null;

        if (!hasPrebuilt && !hasGridLayout && cellPrefab != null)
        {
            // Container boyutunu ayarla (GridLayoutGroup yoksa manuel hesapla)
            float totalWidth = w * cellSize + (w - 1) * cellSpacing;
            float totalHeight = h * cellSize + (h - 1) * cellSpacing;
            inventoryGridContainer.sizeDelta = new Vector2(totalWidth, totalHeight);
        }

        for (int y = 0; y < h; y++)
        {
            for (int x = 0; x < w; x++)
            {
                int index = y * w + x;
                GameObject cell;

                if (hasPrebuilt)
                {
                    // Var olan UI slotunu kullan
                    cell = inventoryGridContainer.GetChild(index).gameObject;
                }
                else if (cellPrefab != null)
                {
                    // Prefab'tan türet
                    cell = Instantiate(cellPrefab, inventoryGridContainer);
                    cell.name = $"InvCell_{x}_{y}";

                    if (!hasGridLayout)
                    {
                        RectTransform rt = cell.GetComponent<RectTransform>();
                        rt.sizeDelta = new Vector2(cellSize, cellSize);
                        rt.anchorMin = new Vector2(0, 1);
                        rt.anchorMax = new Vector2(0, 1);
                        rt.pivot = new Vector2(0, 1);
                        rt.anchoredPosition = new Vector2(
                            x * (cellSize + cellSpacing),
                            -y * (cellSize + cellSpacing));
                    }
                }
                else
                {
                    continue;
                }

                Image img = cell.GetComponent<Image>();
                if (img == null) img = cell.AddComponent<Image>();
                img.raycastTarget = true;
                inventoryCellImages[x, y] = img;

                // Drop hedefi ekle
                InventoryDropZone dropZone = cell.GetComponent<InventoryDropZone>();
                if (dropZone == null)
                    dropZone = cell.AddComponent<InventoryDropZone>();
                dropZone.Setup(manager.InventoryGrid, x, y);
            }
        }
    }

    /// <summary>Hotbar slotlarını oluşturur veya var olanları bağlar.</summary>
    private void BuildHotbar()
    {
        int slots = manager.HotbarSlotCount;
        hotbarCellImages = new Image[slots];
        hotbarOutlines = new Outline[slots];

        if (hotbarContainer == null) return;

        bool hasPrebuilt = hotbarContainer.childCount >= slots;

        if (!hasPrebuilt && cellPrefab != null)
        {
            float totalWidth = slots * hotbarCellSize + (slots - 1) * cellSpacing;
            hotbarContainer.sizeDelta = new Vector2(totalWidth, hotbarCellSize);
        }

        for (int i = 0; i < slots; i++)
        {
            GameObject cell;

            if (hasPrebuilt)
            {
                // Var olan UI slotunu kullan
                cell = hotbarContainer.GetChild(i).gameObject;
            }
            else if (cellPrefab != null)
            {
                // Prefab'tan türet
                cell = Instantiate(cellPrefab, hotbarContainer);
                cell.name = $"HotbarSlot_{i}";

                RectTransform rt = cell.GetComponent<RectTransform>();
                rt.sizeDelta = new Vector2(hotbarCellSize, hotbarCellSize);
                rt.anchorMin = new Vector2(0, 0.5f);
                rt.anchorMax = new Vector2(0, 0.5f);
                rt.pivot = new Vector2(0, 0.5f);
                rt.anchoredPosition = new Vector2(i * (hotbarCellSize + cellSpacing), 0);
            }
            else
            {
                continue;
            }

            Image img = cell.GetComponent<Image>();
            if (img == null) img = cell.AddComponent<Image>();
            img.raycastTarget = true;
            hotbarCellImages[i] = img;

            // Vurgu çerçevesi (Outline bileşeni)
            Outline outline = cell.GetComponent<Outline>();
            if (outline == null)
                outline = cell.AddComponent<Outline>();
            outline.effectColor = normalSlotColor;
            outline.effectDistance = new Vector2(3, -3);
            hotbarOutlines[i] = outline;

            // Drop hedefi ekle
            InventoryDropZone dropZone = cell.GetComponent<InventoryDropZone>();
            if (dropZone == null)
                dropZone = cell.AddComponent<InventoryDropZone>();
            dropZone.Setup(manager.HotbarGrid, i, 0);
        }
    }

    // ═══════════════════════════════════════════
    //  EŞYA UI YENİLEME
    // ═══════════════════════════════════════════

    /// <summary>Tüm eşya UI elemanlarını yeniden oluşturur.</summary>
    private void RefreshAllItems()
    {
        // Mevcut UI elemanlarını temizle
        foreach (var itemUI in activeItemUIs)
        {
            if (itemUI != null)
                Destroy(itemUI.gameObject);
        }
        activeItemUIs.Clear();

        if (manager == null) return;

        // Envanter eşyalarını ilgili hücre altında oluştur
        foreach (var item in manager.InventoryGrid.GetAllItems())
        {
            if (item.gridX >= 0 && item.gridY >= 0 &&
                inventoryCellImages != null &&
                item.gridX < inventoryCellImages.GetLength(0) &&
                item.gridY < inventoryCellImages.GetLength(1))
            {
                Image cellImg = inventoryCellImages[item.gridX, item.gridY];
                if (cellImg != null)
                {
                    CreateItemUI(item, cellImg.rectTransform, isHotbar: false);
                }
            }
        }

        // Hotbar eşyalarını ilgili slot altında oluştur
        foreach (var item in manager.HotbarGrid.GetAllItems())
        {
            if (item.gridX >= 0 &&
                hotbarCellImages != null &&
                item.gridX < hotbarCellImages.Length)
            {
                Image cellImg = hotbarCellImages[item.gridX];
                if (cellImg != null)
                {
                    CreateItemUI(item, cellImg.rectTransform, isHotbar: true);
                }
            }
        }
    }

    /// <summary>
    /// Belirtilen eşya için hedef hücresi altında UI elemanı oluşturur.
    /// Canvas overrideSorting kullanarak hücresel sıra farkını çözer ve eşyayı tüm hücrelerin üstünde çizer.
    /// </summary>
    private void CreateItemUI(InventoryItem item, RectTransform cellParent, bool isHotbar)
    {
        if (itemUIPrefab == null || cellParent == null) return;

        // Doğrudan hedef hücrenin child'ı yap (pozisyon her zaman kusursuz hücre sol üstü)
        GameObject uiObj = Instantiate(itemUIPrefab, cellParent);
        uiObj.name = $"ItemUI_{item.itemData.itemName}";

        RectTransform rt = uiObj.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0, 1);
        rt.anchorMax = new Vector2(0, 1);
        rt.pivot = new Vector2(0, 1);
        rt.anchoredPosition = Vector2.zero;

        // ── Canvas Sorting Override: Eşyanın diğer komşu hücrelerin ÜSTÜNDE çizilmesini sağla ──
        Canvas itemCanvas = uiObj.GetComponent<Canvas>();
        if (itemCanvas == null)
            itemCanvas = uiObj.AddComponent<Canvas>();

        itemCanvas.overrideSorting = true;
        itemCanvas.sortingOrder = 10; // Grid hücrelerinin (order 0) üstünde çizilir

        GraphicRaycaster raycaster = uiObj.GetComponent<GraphicRaycaster>();
        if (raycaster == null)
            uiObj.AddComponent<GraphicRaycaster>();

        // ── Full Raycast Target Image: Eşyanın kapladığı 2x2, 1x2 vb. TÜM alanı tıklanabilir/sürüklenebilir yapar ──
        Image rootImg = uiObj.GetComponent<Image>();
        if (rootImg == null)
            rootImg = uiObj.AddComponent<Image>();
        rootImg.color = Color.clear;
        rootImg.raycastTarget = true;

        // ── Gerçek hücre boyutunu referans hücreden oku ──
        float actualCellW = isHotbar ? (cellParent.rect.width > 0 ? cellParent.rect.width : hotbarCellSize) : cellParent.rect.width;
        float actualCellH = isHotbar ? (cellParent.rect.height > 0 ? cellParent.rect.height : hotbarCellSize) : cellParent.rect.height;

        // Eğer rect henüz hesaplanmamışsa fallback boyutları kullan
        if (actualCellW <= 0) actualCellW = isHotbar ? hotbarCellSize : cellSize;
        if (actualCellH <= 0 || isHotbar) actualCellH = actualCellW; // Hotbar slot yüksekliğini genişliğe kitle

        // GridLayoutGroup'tan gerçek spacing'i oku
        float actualSpacing = cellSpacing;
        Transform containerParent = cellParent.parent;
        if (containerParent != null)
        {
            GridLayoutGroup glg = containerParent.GetComponent<GridLayoutGroup>();
            if (glg != null)
            {
                actualSpacing = isHotbar ? glg.spacing.x : Mathf.Max(glg.spacing.x, glg.spacing.y);
            }
        }

        // Eşya boyutuna göre UI boyutu
        int w = item.EffectiveWidth;
        int h = item.EffectiveHeight;

        if (isHotbar && h > 1)
        {
            w = Mathf.Max(w, h);
            h = 1;
        }

        rt.sizeDelta = new Vector2(
            w * actualCellW + (w - 1) * actualSpacing,
            h * actualCellH + (h - 1) * actualSpacing);

        InventoryItemUI itemUI = uiObj.GetComponent<InventoryItemUI>();
        if (itemUI != null)
        {
            itemUI.Setup(item);
        }

        activeItemUIs.Add(itemUI);
    }

    // ═══════════════════════════════════════════
    //  HOTBAR VURGU
    // ═══════════════════════════════════════════

    /// <summary>Aktif Hotbar slotunun vurgu çerçevesini günceller.</summary>
    private void UpdateHotbarHighlight()
    {
        if (hotbarOutlines == null) return;

        for (int i = 0; i < hotbarOutlines.Length; i++)
        {
            if (hotbarOutlines[i] != null)
            {
                hotbarOutlines[i].effectColor = (i == manager.ActiveHotbarSlot)
                    ? activeSlotColor
                    : normalSlotColor;
            }
        }
    }

    // ═══════════════════════════════════════════
    //  HOLD-TO-OPEN PROGRESS BAR
    // ═══════════════════════════════════════════

    private void UpdateHoldProgressBar()
    {
        if (manager == null) return;

        float progress = manager.HoldProgress;
        bool isHolding = progress > 0f;

        if (holdProgressRoot != null)
        {
            holdProgressRoot.SetActive(isHolding);

            if (isHolding)
            {
                if (playerTransform == null)
                    FindPlayerTransform();

                // Oyuncunun başının üstünde gösterme (World → Screen space)
                if (playerTransform != null)
                {
                    Camera cam = Camera.main;
                    if (cam != null)
                    {
                        Vector3 worldPos = playerTransform.position + playerOffset;
                        Vector3 screenPos = cam.WorldToScreenPoint(worldPos);

                        RectTransform progressRect = holdProgressRoot.GetComponent<RectTransform>();
                        if (progressRect != null)
                        {
                            Canvas parentCanvas = GetComponentInParent<Canvas>();
                            if (parentCanvas != null && parentCanvas.renderMode != RenderMode.ScreenSpaceOverlay)
                            {
                                RectTransformUtility.ScreenPointToLocalPointInRectangle(
                                    parentCanvas.transform as RectTransform,
                                    screenPos,
                                    parentCanvas.worldCamera,
                                    out Vector2 localPoint);
                                progressRect.localPosition = localPoint;
                            }
                            else
                            {
                                progressRect.position = screenPos;
                            }
                        }
                    }
                }
            }
        }

        if (holdProgressBar != null)
        {
            holdProgressBar.fillAmount = progress;
        }
    }

    // ═══════════════════════════════════════════
    //  EVENT HANDLERS
    // ═══════════════════════════════════════════

    private void OnInventoryToggled()
    {
        if (inventoryPanel != null)
            inventoryPanel.SetActive(manager.IsInventoryOpen);

        RefreshAllItems();
    }

    private void OnActiveSlotChanged(int newSlot)
    {
        UpdateHotbarHighlight();
    }

    /// <summary>
    /// UI üzerindeki Kapat (X) butonunun OnClick olayına bağlanabilir.
    /// Envanteri ve açık tooltip'i kapatır.
    /// </summary>
    public void OnCloseButtonClicked()
    {
        if (manager != null)
            manager.CloseInventory();
    }

    /// <summary>
    /// Sürüklenen UI eşyası ekran üstündeyken, sol üst köşesine en yakın grid hücresinin (x,y) koordinatını bulur.
    /// Eşya boyutuna göre kenar sıkıştırması yaparak köşelerdeki sığmama sorunlarını engeller.
    /// </summary>
    public Vector2Int GetNearestCell(RectTransform draggedRect, InventoryGridData grid, InventoryItem item, Vector2Int fallbackCell)
    {
        if (draggedRect == null || grid == null) return fallbackCell;

        Vector3[] corners = new Vector3[4];
        draggedRect.GetWorldCorners(corners);
        Vector3 itemTopLeft = corners[1]; // Sol üst köşe

        Image[,] cellMatrix = (grid == manager.InventoryGrid) ? inventoryCellImages : null;
        Image[] hotbarArray = (grid == manager.HotbarGrid) ? hotbarCellImages : null;

        float minDistance = float.MaxValue;
        Vector2Int bestCell = fallbackCell;

        if (cellMatrix != null)
        {
            int w = grid.width;
            int h = grid.height;
            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    if (cellMatrix[x, y] != null)
                    {
                        Vector3[] cellCorners = new Vector3[4];
                        cellMatrix[x, y].rectTransform.GetWorldCorners(cellCorners);
                        Vector3 cellTopLeft = cellCorners[1];

                        float dist = Vector3.SqrMagnitude(itemTopLeft - cellTopLeft);
                        if (dist < minDistance)
                        {
                            minDistance = dist;
                            bestCell = new Vector2Int(x, y);
                        }
                    }
                }
            }

            // Sınır sıkıştırması: Çoklu slot eşyaların kenar/köşelerde taşmasını önler
            if (item != null)
            {
                int itemW = item.EffectiveWidth;
                int itemH = item.EffectiveHeight;
                bestCell.x = Mathf.Clamp(bestCell.x, 0, Mathf.Max(0, grid.width - itemW));
                bestCell.y = Mathf.Clamp(bestCell.y, 0, Mathf.Max(0, grid.height - itemH));
            }
        }
        else if (hotbarArray != null)
        {
            for (int i = 0; i < hotbarArray.Length; i++)
            {
                if (hotbarArray[i] != null)
                {
                    Vector3[] cellCorners = new Vector3[4];
                    hotbarArray[i].rectTransform.GetWorldCorners(cellCorners);
                    Vector3 cellTopLeft = cellCorners[1];

                    float dist = Vector3.SqrMagnitude(itemTopLeft - cellTopLeft);
                    if (dist < minDistance)
                    {
                        minDistance = dist;
                        bestCell = new Vector2Int(i, 0);
                    }
                }
            }
        }

        return bestCell;
    }
}
