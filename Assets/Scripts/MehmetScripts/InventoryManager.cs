using UnityEngine;
using System;

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
    [SerializeField] private int hotbarSlots = 4;

    [Header("Hold-to-Open Ayarları")]
    [SerializeField] private float holdDuration = 1.5f;
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
        if (item == null || !item.CanUse()) return;

        bool depleted = item.Use();
        OnItemUsed?.Invoke(item);

        if (depleted)
        {
            RemoveItemCompletely(item);
        }

        OnInventoryChanged?.Invoke();
    }

    /// <summary>Eşyayı hem envanterden hem hotbar'dan tamamen kaldırır.</summary>
    private void RemoveItemCompletely(InventoryItem item)
    {
        InventoryGrid.RemoveItem(item);
        HotbarGrid.RemoveItem(item);
        OnItemRemoved?.Invoke(item);
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
    public bool MoveItem(InventoryItem item, InventoryGridData targetGrid, int targetX, int targetY)
    {
        InventoryGridData sourceGrid = GetGridContaining(item);
        if (sourceGrid == null) return false;

        // Orijinal pozisyonu kaydet (geri alma için)
        int origX = item.gridX;
        int origY = item.gridY;

        // Geçici olarak kaldır
        sourceGrid.RemoveItem(item);

        if (targetGrid.CanPlaceItem(item, targetX, targetY))
        {
            targetGrid.PlaceItem(item, targetX, targetY);
            OnInventoryChanged?.Invoke();
            return true;
        }
        else
        {
            // Yerleştirilemedi — orijinal pozisyona geri koy
            sourceGrid.PlaceItem(item, origX, origY);
            return false;
        }
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

        // 2x2 veya çoklu slot kısıtlaması kontrolü
        if (!HotbarGrid.CanPlaceItem(item, 0, 0) &&
            !HotbarGrid.CanPlaceItem(item, 1, 0) &&
            !HotbarGrid.CanPlaceItem(item, 2, 0))
        {
            Debug.LogWarning($"{item.itemData.itemName} Hotbar'a konulamaz (boyutu uygun değil)!");
            return false;
        }

        // Tercih edilen slot istendiyse dene
        if (preferredSlot >= 0 && preferredSlot < hotbarSlots)
        {
            if (MoveItem(item, HotbarGrid, preferredSlot, 0))
                return true;
        }

        // Aktif slotu dene
        if (MoveItem(item, HotbarGrid, ActiveHotbarSlot, 0))
            return true;

        // Herhangi bir boş slotu dene
        for (int i = 0; i < hotbarSlots; i++)
        {
            if (MoveItem(item, HotbarGrid, i, 0))
                return true;
        }

        return false;
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
}
