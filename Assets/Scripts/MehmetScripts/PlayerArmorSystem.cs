using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Zırh yönetim sistemi.
/// Envanterdeki Armor/Shield eşyalarını tarar ve:
/// 1. Aynı ArmorSlotType'dan sadece 1 tane bulunmasını sağlar (ikinci reddedilir).
/// 2. Toplam zırh ağırlık eğrisini hesaplar (hızlanma/yavaşlama süreleri).
/// 3. CharacterController'a ağırlık eğrisi uygular.
/// </summary>
public class PlayerArmorSystem : MonoBehaviour
{
    public static PlayerArmorSystem Instance { get; private set; }

    /// <summary>Anlık hesaplanan toplam Armor değeri (bonus can).</summary>
    public int TotalArmorValue { get; private set; }

    /// <summary>Anlık hesaplanan ivmelenme süresi (saniye).</summary>
    public float CurrentAccelTime { get; private set; }

    /// <summary>Anlık hesaplanan yavaşlama süresi (saniye).</summary>
    public float CurrentDecelTime { get; private set; }

    [Header("Varsayılan Ağırlık (Zırhsız)")]
    [SerializeField] private float defaultAccelTime = 0.05f;
    [SerializeField] private float defaultDecelTime = 0.05f;

    [Header("UI Görünüm Referansları")]
    [SerializeField] private TMPro.TextMeshProUGUI armorText;
    [SerializeField] private UnityEngine.UI.Text legacyArmorText;

    [Header("Kalkan UI Görseli")]
    [SerializeField] private UnityEngine.UI.Image armorShieldImage;
    [SerializeField] private Sprite defaultEmptyShieldSprite;
    [SerializeField] private Sprite woodenShieldSprite;
    [SerializeField] private Sprite ironShieldSprite;

    /// <summary>Zırh değeri değiştiğinde tetiklenir.</summary>
    public event System.Action<int> OnArmorChanged;

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
        if (InventoryManager.Instance != null)
        {
            InventoryManager.Instance.OnInventoryChanged += RecalculateArmor;
        }
        RecalculateArmor();
    }

    private void OnDestroy()
    {
        if (InventoryManager.Instance != null)
        {
            InventoryManager.Instance.OnInventoryChanged -= RecalculateArmor;
        }
    }

    /// <summary>
    /// Envantere yeni bir Armor eşyası eklenmeye çalışıldığında
    /// aynı ArmorSlotType'dan zaten var mı kontrol eder.
    /// </summary>
    /// <returns>Eklenebilir mi?</returns>
    public bool CanAddArmorItem(ItemSO newArmorData)
    {
        if (newArmorData == null) return true;
        if (InventoryManager.Instance == null) return true;

        // 1) Zırh Slot Tipi Kontrolü (Helmet, Chestplate, Leggings)
        if (newArmorData.itemType == ItemType.Armor && newArmorData.armorSlotType != ArmorSlotType.None)
        {
            ArmorSlotType targetSlot = newArmorData.armorSlotType;

            foreach (var item in InventoryManager.Instance.HotbarGrid.GetAllItems())
            {
                if (item.itemData != null && item.itemData.itemType == ItemType.Armor
                    && item.itemData.armorSlotType == targetSlot
                    && item.itemData != newArmorData)
                {
                    Debug.LogWarning($"🛡️ [ZIRH] Zaten bir {targetSlot} zırhınız var! Önce mevcut zırhı çıkarmalısınız.");
                    return false;
                }
            }

            foreach (var item in InventoryManager.Instance.InventoryGrid.GetAllItems())
            {
                if (item.itemData != null && item.itemData.itemType == ItemType.Armor
                    && item.itemData.armorSlotType == targetSlot
                    && item.itemData != newArmorData)
                {
                    Debug.LogWarning($"🛡️ [ZIRH] Zaten bir {targetSlot} zırhınız var! Önce mevcut zırhı çıkarmalısınız.");
                    return false;
                }
            }
        }

        // 2) Kalkan Kontrolü (Ahşap Kalkan, Demir Kalkan — Tek tip kalkan taşınabilir)
        if (newArmorData.itemType == ItemType.Shield)
        {
            foreach (var item in InventoryManager.Instance.HotbarGrid.GetAllItems())
            {
                if (item.itemData != null && item.itemData.itemType == ItemType.Shield
                    && item.itemData != newArmorData)
                {
                    Debug.LogWarning($"🛡️ [KALKAN] Zaten bir kalkanınız var! Tek bir kalkan taşıyabilirsiniz.");
                    return false;
                }
            }

            foreach (var item in InventoryManager.Instance.InventoryGrid.GetAllItems())
            {
                if (item.itemData != null && item.itemData.itemType == ItemType.Shield
                    && item.itemData != newArmorData)
                {
                    Debug.LogWarning($"🛡️ [KALKAN] Zaten bir kalkanınız var! Tek bir kalkan taşıyabilirsiniz.");
                    return false;
                }
            }
        }

        return true;
    }

    /// <summary>
    /// Envanterdeki tüm Armor ve Shield eşyalarını tarayarak
    /// toplam Armor değerini ve ağırlık eğrilerini hesaplar.
    /// </summary>
    public void RecalculateArmor()
    {
        int totalArmor = 0;
        float totalAccel = 0f;
        float totalDecel = 0f;
        int armorPieceCount = 0;

        var allItems = new List<InventoryItem>();

        if (InventoryManager.Instance != null)
        {
            allItems.AddRange(InventoryManager.Instance.HotbarGrid.GetAllItems());
            allItems.AddRange(InventoryManager.Instance.InventoryGrid.GetAllItems());
        }

        foreach (var item in allItems)
        {
            if (item.itemData == null) continue;

            if (item.itemData.itemType == ItemType.Armor || item.itemData.itemType == ItemType.Shield)
            {
                totalArmor += item.itemData.armorValue;

                if (item.itemData.weightAccelTime > 0f || item.itemData.weightDecelTime > 0f)
                {
                    totalAccel += item.itemData.weightAccelTime;
                    totalDecel += item.itemData.weightDecelTime;
                    armorPieceCount++;
                }
            }
        }

        TotalArmorValue = totalArmor;

        // UI Text güncellemesi
        if (armorText != null)
            armorText.text = $"Zırh: {TotalArmorValue}";
        if (legacyArmorText != null)
            legacyArmorText.text = $"Zırh: {TotalArmorValue}";

        // Kalkan Görseli Güncellemesi
        if (armorShieldImage != null)
        {
            InventoryItem activeShield = null;
            foreach (var item in allItems)
            {
                if (item.itemData != null && item.itemData.itemType == ItemType.Shield)
                {
                    activeShield = item;
                    break;
                }
            }

            if (activeShield == null)
            {
                if (defaultEmptyShieldSprite != null)
                    armorShieldImage.sprite = defaultEmptyShieldSprite;
            }
            else
            {
                string sName = activeShield.itemData.itemName;
                if (sName.Contains("Ahşap") && woodenShieldSprite != null)
                {
                    armorShieldImage.sprite = woodenShieldSprite;
                }
                else if (sName.Contains("Demir") && ironShieldSprite != null)
                {
                    armorShieldImage.sprite = ironShieldSprite;
                }
                else if (activeShield.itemData.icon != null)
                {
                    armorShieldImage.sprite = activeShield.itemData.icon;
                }
            }
        }

        OnArmorChanged?.Invoke(TotalArmorValue);

        // Ağırlık eğrisi: zırh parçalarının toplanmış ivme süreleri
        if (armorPieceCount > 0)
        {
            CurrentAccelTime = defaultAccelTime + totalAccel;
            CurrentDecelTime = defaultDecelTime + totalDecel;
        }
        else
        {
            CurrentAccelTime = defaultAccelTime;
            CurrentDecelTime = defaultDecelTime;
        }
    }
}
