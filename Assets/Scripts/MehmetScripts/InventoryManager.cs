using UnityEngine;
using System;
using System.Collections.Generic;

/// <summary>
/// Ana envanter ve Hotbar yöneticisi.
/// Hold-to-Open zamanlayıcı, Minecraft tarzı Hotbar seçimi (numara + scroll + sol tık),
/// eşya ekleme/taşıma/kullanma işlemlerini yönetir.
/// </summary>
public class InventoryManager : MonoBehaviour
{
    public static InventoryManager Instance { get; private set; }

    [Header("Envanter Grid Ayarları")]
    [SerializeField] private int inventoryWidth = 6;
    [SerializeField] private int inventoryHeight = 4;

    [Header("Hotbar Ayarları")]
    [SerializeField] private int hotbarSlots = 5;

    [Header("Hold-to-Open Ayarları")]
    [SerializeField] private float holdDuration = 0.7f;
    [SerializeField] private KeyCode inventoryKey1 = KeyCode.I;
    [SerializeField] private KeyCode inventoryKey2 = KeyCode.E;

    // ── Grid Verileri ──
    public InventoryGridData InventoryGrid { get; private set; }
    public InventoryGridData HotbarGrid { get; private set; }

    // ── Durum ──
    public bool IsInventoryOpen { get; private set; }
    public int ActiveHotbarSlot { get; private set; } = 0;
    public int HotbarSlotCount => hotbarSlots;

    // ── Hold-to-Open ──
    private float holdTimer = 0f;
    private bool isHolding = false;

    /// <summary>Oyuncu envanteri açmak/kapatmak için tuşa basılı tutuyor mu?</summary>
    public bool IsHoldingToOpen => isHolding;

    /// <summary>Hold-to-Open ilerleme yüzdesi (0–1).</summary>
    public float HoldProgress => isHolding ? Mathf.Clamp01(holdTimer / holdDuration) : 0f;

    // ── Olaylar (Events) ──
    /// <summary>Envanter açıldığında/kapandığında tetiklenir.</summary>
    public event Action OnInventoryToggled;

    /// <summary>Aktif Hotbar slotu değiştiğinde tetiklenir.</summary>
    public event Action<int> OnActiveSlotChanged;

    /// <summary>Bir eşya kullanıldığında tetiklenir.</summary>
    public event Action<InventoryItem> OnItemUsed;

    /// <summary>Bir eşya envanterden tamamen silindiğinde tetiklenir.</summary>
    public event Action<InventoryItem> OnItemRemoved;

    /// <summary>Envanter verileri değiştiğinde tetiklenir (UI yenileme sinyali).</summary>
    public event Action OnInventoryChanged;

    /// <summary>OnInventoryChanged event'ini dışarıdan tetiklemek için çağrılır.</summary>
    public void NotifyInventoryChanged() => OnInventoryChanged?.Invoke();

    // ═══════════════════════════════════════════
    //  LIFECYCLE
    // ═══════════════════════════════════════════

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        if (hotbarSlots < 5) hotbarSlots = 5;

        InventoryGrid = new InventoryGridData(inventoryWidth, inventoryHeight);
        HotbarGrid = new InventoryGridData(hotbarSlots, 1);
    }

    private void Update()
    {
        HandleHoldToOpen();

        if (!IsInventoryOpen)
        {
            HandleHotbarSelection();
            HandleHotbarUse();
        }
    }

    // ═══════════════════════════════════════════
    //  HOLD-TO-OPEN
    // ═══════════════════════════════════════════

    private void HandleHoldToOpen()
    {
        bool keyHeld = InventoryInput.IsInventoryKeyHeld(inventoryKey1, inventoryKey2);

        if (keyHeld)
        {
            if (!isHolding)
            {
                isHolding = true;
                holdTimer = 0f;
            }

            holdTimer += Time.unscaledDeltaTime;

            if (holdTimer >= holdDuration)
            {
                ToggleInventory();
                isHolding = false;
                holdTimer = 0f;
            }
        }
        else if (isHolding)
        {
            // Tuş erken bırakıldı — sıfırla
            isHolding = false;
            holdTimer = 0f;
        }
    }

    /// <summary>Envanteri açar/kapatır.</summary>
    public void ToggleInventory()
    {
        IsInventoryOpen = !IsInventoryOpen;
        OnInventoryToggled?.Invoke();
    }

    /// <summary>Envanteri açar.</summary>
    public void OpenInventory()
    {
        if (IsInventoryOpen) return;
        IsInventoryOpen = true;
        OnInventoryToggled?.Invoke();
    }

    /// <summary>Envanteri kapatır (UI Kapat Butonu için).</summary>
    public void CloseInventory()
    {
        if (!IsInventoryOpen) return;
        IsInventoryOpen = false;
        OnInventoryToggled?.Invoke();

        if (ItemTooltipUI.Instance != null)
            ItemTooltipUI.Instance.Hide();
    }

    // ═══════════════════════════════════════════
    //  HOTBAR SEÇİMİ (Numara Tuşları + Scroll)
    // ═══════════════════════════════════════════

    private void HandleHotbarSelection()
    {
        // Numara tuşları (1–4)
        for (int i = 0; i < hotbarSlots; i++)
        {
            if (InventoryInput.IsDigitKeyPressed(i))
            {
                SetActiveHotbarSlot(i);
                return;
            }
        }

        // Fare tekerleği ile slot değiştirme
        float scroll = InventoryInput.GetScrollDeltaY();
        if (Mathf.Abs(scroll) > 0.01f)
        {
            int newSlot = ActiveHotbarSlot + (scroll > 0f ? -1 : 1);

            // Wrap around
            if (newSlot < 0) newSlot = hotbarSlots - 1;
            if (newSlot >= hotbarSlots) newSlot = 0;

            SetActiveHotbarSlot(newSlot);
        }
    }

    private void SetActiveHotbarSlot(int slotIndex)
    {
        if (slotIndex < 0 || slotIndex >= hotbarSlots) return;
        ActiveHotbarSlot = slotIndex;
        OnActiveSlotChanged?.Invoke(slotIndex);
    }

    // ═══════════════════════════════════════════
    //  HOTBAR KULLANIMI (Sol Tık)
    // ═══════════════════════════════════════════

    private void HandleHotbarUse()
    {
        if (InventoryInput.IsLeftClickDown())
        {
            // UI üzerinde tıklandıysa hotbar kullanma
            if (UnityEngine.EventSystems.EventSystem.current != null &&
                UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject())
                return;

            UseActiveHotbarItem();
        }
    }

    /// <summary>Aktif Hotbar slotundaki eşyayı kullanır.</summary>
    public void UseActiveHotbarItem()
    {
        InventoryItem item = GetActiveHotbarItem();
        if (item == null) return;
        UseItem(item);
    }

    /// <summary>
    /// Aktif Hotbar slotundaki eşyayı döndürür.
    /// Çoklu slot eşyalarda, kapladığı herhangi bir slottaki eşyayı bulur.
    /// </summary>
    public InventoryItem GetActiveHotbarItem()
    {
        return HotbarGrid.GetItemAtSlot(ActiveHotbarSlot);
    }

    // ═══════════════════════════════════════════
    //  EŞYA KULLANIMI
    // ═══════════════════════════════════════════

    /// <summary>
    /// Eşyayı kullanır. Tipe göre adet/dayanıklılık düşürür.
    /// Tükenen/kırılan eşyalar otomatik kaldırılır.
    /// </summary>
    public void UseItem(InventoryItem item)
    {
        if (item == null) return;

        // Anahtar eşyaları (KeyItem) doğrudan kullanılamaz — envanterden kullanmaya basılırsa yere atılır
        if (item.itemData != null && item.itemData.itemType == ItemType.KeyItem)
        {
            DropItemToWorld(item);
            return;
        }

        // Kalkanlar ve Zırhlar envanterde bulunduğu an pasif etkilerini verirler — Hotbar'a kuşanılmazlar
        if (item.itemData != null && (item.itemData.itemType == ItemType.Shield || item.itemData.itemType == ItemType.Armor))
        {
            Debug.Log($"🛡️ '{item.itemData.itemName}' pasif etki verir, Hotbar'a konulmasına gerek yoktur.");
            return;
        }

        // ── OK (ARROW) → KULLAN DENİNCE SADAĞA YÜKLE ──
        if (item.itemData != null && item.itemData.itemType == ItemType.Arrow)
        {
            InventoryItem quiverItem = FindFirstQuiver();
            if (quiverItem != null && PlayerEquipmentController.Instance != null)
            {
                QuiverData qData = PlayerEquipmentController.Instance.GetQuiverData(quiverItem);
                bool loaded = qData.TryLoadArrow(item.itemData, quiverItem.itemData.maxArrowCapacity);
                if (loaded)
                {
                    RemoveItemCompletely(item);
                    if (ItemTooltipUI.Instance != null)
                        ItemTooltipUI.Instance.Hide();
                    return;
                }
            }
            else
            {
                Debug.LogWarning("🏹 [SADAK HATA] Ok doldurabilmek için envanterinizde yer olan bir Sadak (Quiver) olmalıdır!");
                return;
            }
        }

        // ── EĞER EŞYA ANA ENVANTERDEYSE VE HOTBAR'DA DEĞİLSE → HOTBAR'A KUŞAN (EQUIP) ──
        if (InventoryGrid.Contains(item) && !HotbarGrid.Contains(item))
        {
            bool equipped = QuickMoveToHotbar(item);
            if (equipped)
            {
                Debug.Log($"⚔️ [EQUIP] '{item.itemData.itemName}' Hotbar'a kuşandı!");
                if (ItemTooltipUI.Instance != null)
                    ItemTooltipUI.Instance.Hide();
                return;
            }
        }

        if (!item.CanUse()) return;

        bool depleted = item.Use();
        OnItemUsed?.Invoke(item);

        if (depleted)
        {
            RemoveItemCompletely(item);
        }

        OnInventoryChanged?.Invoke();
    }

    /// <summary>Eşyayı hem envanterden hem hotbar'dan tamamen kaldırır.</summary>
    public void RemoveItemCompletely(InventoryItem item)
    {
        InventoryGrid.RemoveItem(item);
        HotbarGrid.RemoveItem(item);
        OnItemRemoved?.Invoke(item);
    }

    /// <summary>Eşyayı dünyada oyuncunun yakınına yere atar (WorldItem olarak spawn eder).</summary>
    public bool DropItemToWorld(InventoryItem item)
    {
        if (item == null || item.itemData == null) return false;

        Transform pTrans = null;
        GameObject pObj = GameObject.FindGameObjectWithTag("Player");
        if (pObj != null) pTrans = pObj.transform;

        Vector3 dropPos = (pTrans != null) 
            ? pTrans.position + new Vector3(UnityEngine.Random.Range(-0.6f, 0.6f), 0.2f, 0) 
            : Vector3.zero;

        // WorldItem GameObject oluştur
        GameObject worldItemObj = new GameObject($"WorldItem_{item.itemData.itemName}");
        worldItemObj.transform.position = dropPos;

        SpriteRenderer sr = worldItemObj.AddComponent<SpriteRenderer>();
        sr.sprite = item.itemData.icon;
        sr.sortingOrder = 5;

        BoxCollider2D col = worldItemObj.AddComponent<BoxCollider2D>();
        col.isTrigger = true;

        WorldItem wItem = worldItemObj.AddComponent<WorldItem>();
        wItem.Setup(item.itemData, item.currentStack);

        RemoveItemCompletely(item);
        OnInventoryChanged?.Invoke();
        Debug.Log($"🗑️ {item.itemData.itemName} x{item.currentStack} yere atıldı!");
        return true;
    }

    // ═══════════════════════════════════════════
    //  EŞYA YÖNETİMİ
    // ═══════════════════════════════════════════

    /// <summary>
    /// Yeni eşya ekler. Önce mevcut istiflere, sonra Hotbar'a, son olarak envantere yerleştirir.
    /// </summary>
    /// <returns>Ekleme başarılı mı.</returns>
    public bool AddItem(ItemSO itemData, int amount = 1)
    {
        int remaining = amount;

        // 1) Mevcut istiflere eklemeyi dene
        if (itemData.maxStack > 1)
        {
            int stackedInHotbar = HotbarGrid.TryStackItem(itemData, remaining);
            remaining -= stackedInHotbar;

            if (remaining > 0)
            {
                int stackedInInventory = InventoryGrid.TryStackItem(itemData, remaining);
                remaining -= stackedInInventory;
            }
        }

        // 2) Kalan miktar için yeni eşya oluştur
        while (remaining > 0)
        {
            int stackSize = Mathf.Min(remaining, itemData.maxStack);
            InventoryItem newItem = new InventoryItem(itemData, stackSize);

            if (HotbarGrid.AutoPlaceItem(newItem))
            {
                remaining -= stackSize;
            }
            else if (InventoryGrid.AutoPlaceItem(newItem))
            {
                remaining -= stackSize;
            }
            else
            {
                Debug.LogWarning($"Envanter dolu! {itemData.itemName} x{remaining} eklenemedi.");
                OnInventoryChanged?.Invoke();
                return false;
            }
        }

        OnInventoryChanged?.Invoke();
        return true;
    }

    /// <summary>
    /// Eşyayı bir grid'den diğerine (veya aynı grid içinde) taşır.
    /// </summary>
    /// <summary>
    /// Eşyayı bir grid'den diğerine (veya aynı grid içinde) taşır veya hedef yerdeki eşya ile TAKAS (Swap) yapar.
    /// Hiçbir eşya kaybolmaz; yerinden edilen eşya orijinal yerine veya boş alana otomatik taşınır.
    /// </summary>
    public bool MoveItem(InventoryItem item, InventoryGridData targetGrid, int targetX, int targetY)
    {
        if (item == null || targetGrid == null) return false;

        InventoryGridData sourceGrid = GetGridContaining(item);
        if (sourceGrid == null) return false;

        // Aynı pozisyon ise işlem yapma
        if (sourceGrid == targetGrid && item.gridX == targetX && item.gridY == targetY)
            return true;

        // Kaynak eşyanın orijinal konumunu kaydet
        int itemOrigX = item.gridX;
        int itemOrigY = item.gridY;

        // 1) Doğrudan Boş Hücreye Taşıma Denemesi
        sourceGrid.RemoveItem(item);

        if (targetGrid.CanPlaceItem(item, targetX, targetY))
        {
            targetGrid.PlaceItem(item, targetX, targetY);
            OnInventoryChanged?.Invoke();
            return true;
        }

        // 2) Hedefteki eşya(lar) ile TAKAS (Swap) veya Deplasman Denemesi
        InventoryItem targetItem = targetGrid.GetItemAt(targetX, targetY);

        if (targetItem != null && targetItem != item)
        {
            // Hedef eşyanın orijinal konumunu kaydet
            int targetOrigX = targetItem.gridX;
            int targetOrigY = targetItem.gridY;
            InventoryGridData targetItemSourceGrid = GetGridContaining(targetItem);

            if (targetItemSourceGrid != null)
            {
                targetItemSourceGrid.RemoveItem(targetItem);

                // Şimdi item hedef konuma sığıyor mu?
                if (targetGrid.CanPlaceItem(item, targetX, targetY))
                {
                    // targetItem kaynak konuma sığıyor mu?
                    if (sourceGrid.CanPlaceItem(targetItem, itemOrigX, itemOrigY))
                    {
                        targetGrid.PlaceItem(item, targetX, targetY);
                        sourceGrid.PlaceItem(targetItem, itemOrigX, itemOrigY);
                        OnInventoryChanged?.Invoke();
                        return true;
                    }
                    // Birebir konuma sığmıyorsa envanterde boş bir alana otomatik yerleşebilir mi?
                    else if (sourceGrid.AutoPlaceItem(targetItem) || InventoryGrid.AutoPlaceItem(targetItem))
                    {
                        targetGrid.PlaceItem(item, targetX, targetY);
                        OnInventoryChanged?.Invoke();
                        return true;
                    }
                }

                // Takas sığmadı — targetItem'ı orijinal yerine geri koy
                targetItemSourceGrid.PlaceItem(targetItem, targetOrigX, targetOrigY);
            }
        }

        // 3) Tüm işlemler başarısız — item'ı orijinal pozisyonuna geri yerleştir
        sourceGrid.PlaceItem(item, itemOrigX, itemOrigY);
        return false;
    }

    /// <summary>Eşyanın hangi grid'de olduğunu döndürür.</summary>
    public InventoryGridData GetGridContaining(InventoryItem item)
    {
        if (InventoryGrid.Contains(item)) return InventoryGrid;
        if (HotbarGrid.Contains(item)) return HotbarGrid;
        return null;
    }

    /// <summary>Eşyayı envanterden Hotbar'a hızlıca taşır (Sağ tık veya 1-4 tuşları).</summary>
    public bool QuickMoveToHotbar(InventoryItem item, int preferredSlot = -1)
    {
        if (item == null) return false;

        // 1) Tercih edilen slot verilmişse ve TAMAMEN BOŞSA yerleştir
        if (preferredSlot >= 0 && preferredSlot < hotbarSlots)
        {
            if (HotbarGrid.CanPlaceItem(item, preferredSlot, 0))
            {
                MoveItem(item, HotbarGrid, preferredSlot, 0);
                return true;
            }
        }

        // 2) Hotbar'daki tüm boş slotları sırayla tara (yalnızca tamamen boş yerleşebilen yerler)
        for (int i = 0; i < hotbarSlots; i++)
        {
            if (HotbarGrid.CanPlaceItem(item, i, 0))
            {
                MoveItem(item, HotbarGrid, i, 0);
                return true;
            }
        }

        // 3) 'Kullan' ile otomatik kuşanmada takas yapılmaz; boş yer yoksa reddet
        Debug.LogWarning($"⚠️ [HOTBAR DOLU] Hotbar'da '{item.itemData.itemName}' için sığacak boş yer yok!");
        return false;
    }

    /// <summary>Envanterde veya Hotbar'da belirtilen eşyadan en az belirtilen miktarda var mı kontrol eder.</summary>
    public bool HasItem(ItemSO itemData, int amount = 1)
    {
        if (itemData == null) return false;
        int count = 0;

        foreach (var item in HotbarGrid.GetAllItems())
        {
            if (item.itemData == itemData)
            {
                count += item.currentStack;
                if (count >= amount) return true;
            }
        }

        foreach (var item in InventoryGrid.GetAllItems())
        {
            if (item.itemData == itemData)
            {
                count += item.currentStack;
                if (count >= amount) return true;
            }
        }

        return count >= amount;
    }

    /// <summary>Envanterden ve Hotbar'dan belirtilen eşyadan istenen miktarda eksiltir.</summary>
    public bool RemoveItem(ItemSO itemData, int amount = 1)
    {
        if (!HasItem(itemData, amount)) return false;
        int remainingToRemove = amount;

        // Önce Hotbar'dan eksilt
        var hotbarItems = new List<InventoryItem>(HotbarGrid.GetAllItems());
        foreach (var item in hotbarItems)
        {
            if (item.itemData == itemData)
            {
                int deduct = Mathf.Min(remainingToRemove, item.currentStack);
                item.currentStack -= deduct;
                remainingToRemove -= deduct;

                if (item.currentStack <= 0)
                {
                    HotbarGrid.RemoveItem(item);
                }

                if (remainingToRemove <= 0) break;
            }
        }

        // Kalan varsa envanterden eksilt
        if (remainingToRemove > 0)
        {
            var invItems = new List<InventoryItem>(InventoryGrid.GetAllItems());
            foreach (var item in invItems)
            {
                if (item.itemData == itemData)
                {
                    int deduct = Mathf.Min(remainingToRemove, item.currentStack);
                    item.currentStack -= deduct;
                    remainingToRemove -= deduct;

                    if (item.currentStack <= 0)
                    {
                        InventoryGrid.RemoveItem(item);
                    }

                    if (remainingToRemove <= 0) break;
                }
            }
        }

        OnInventoryChanged?.Invoke();
        return true;
    }

    /// <summary>Eşyayı Hotbar'dan Envantere hızlıca geri taşır (Sağ tık).</summary>
    public bool QuickMoveToInventory(InventoryItem item)
    {
        if (item == null || !HotbarGrid.Contains(item)) return false;

        HotbarGrid.RemoveItem(item);
        if (InventoryGrid.AutoPlaceItem(item))
        {
            OnInventoryChanged?.Invoke();
            return true;
        }
        else
        {
            // Envanter doluysa hotbar'daki yerine geri koy
            HotbarGrid.AutoPlaceItem(item);
            return false;
        }
    }

    /// <summary>Envanterde veya Hotbar'daki ilk Sadak (Quiver) nesnesini bulur.</summary>
    public InventoryItem FindFirstQuiver()
    {
        foreach (var i in HotbarGrid.GetAllItems())
            if (i.itemData != null && i.itemData.itemType == ItemType.Quiver) return i;

        foreach (var i in InventoryGrid.GetAllItems())
            if (i.itemData != null && i.itemData.itemType == ItemType.Quiver) return i;

        return null;
    }
}
