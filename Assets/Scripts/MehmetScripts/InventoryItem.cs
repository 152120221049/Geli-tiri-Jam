using UnityEngine;

/// <summary>
/// Envanterdeki canlı eşya nesnesi.
/// Her eşya örneği kendi adet, dayanıklılık, döndürme ve grid pozisyon bilgilerini taşır.
/// </summary>
[System.Serializable]
public class InventoryItem
{
    /// <summary>Eşyanın ScriptableObject tanım verisi.</summary>
    public ItemSO itemData;

    /// <summary>Mevcut istif adedi.</summary>
    public int currentStack;

    /// <summary>Kalan dayanıklılık. -1 = sonsuz.</summary>
    public int currentDurability;

    /// <summary>Eşya döndürülmüş mü? (genişlik ↔ yükseklik takası)</summary>
    public bool isRotated;

    /// <summary>Grid üzerindeki sol üst köşe X koordinatı. -1 = yerleştirilmemiş.</summary>
    public int gridX;

    /// <summary>Grid üzerindeki sol üst köşe Y koordinatı. -1 = yerleştirilmemiş.</summary>
    public int gridY;

    public InventoryItem(ItemSO data, int stack = 1)
    {
        itemData = data;
        currentStack = Mathf.Min(stack, data.maxStack);
        isRotated = false;
        gridX = -1;
        gridY = -1;

        // Dayanıklılık başlangıcı
        if (data.HasDurability)
            currentDurability = data.maxDurability;
        else if (data.IsInfinite)
            currentDurability = -1; // Sonsuz
        else
            currentDurability = 0; // Dayanıklılık yok
    }

    public InventoryItem(ItemSO data, int stack, int durability)
    {
        itemData = data;
        currentStack = Mathf.Min(stack, data.maxStack);
        isRotated = false;
        gridX = -1;
        gridY = -1;

        if (durability >= 0 || durability == -1)
            currentDurability = durability;
        else if (data.HasDurability)
            currentDurability = data.maxDurability;
        else if (data.IsInfinite)
            currentDurability = -1;
        else
            currentDurability = 0;
    }

    /// <summary>Döndürme durumuna göre efektif genişlik.</summary>
    public int EffectiveWidth => itemData.EffectiveWidth(isRotated);

    /// <summary>Döndürme durumuna göre efektif yükseklik.</summary>
    public int EffectiveHeight => itemData.EffectiveHeight(isRotated);

    /// <summary>Eşya kırılmış mı? (sonlu dayanıklılık 0'a ulaştı)</summary>
    public bool IsBroken => itemData.HasDurability && currentDurability <= 0;

    /// <summary>Eşya stoku tükenmiş mi?</summary>
    public bool IsEmpty => currentStack <= 0;

    /// <summary>
    /// Eşyanın kullanılıp kullanılamayacağını kontrol eder.
    /// Passive, Arrow ve Quiver eşyaları doğrudan kullanılamaz.
    /// Kırık veya stoku bitmiş eşyalar kullanılamaz.
    /// </summary>
    public bool CanUse()
    {
        if (itemData.itemType == ItemType.Passive) return false;
        if (itemData.itemType == ItemType.Quiver) return false;
        if (IsBroken) return false;
        if (IsEmpty) return false;
        return true;
    }

    /// <summary>
    /// Eşyayı kullanır. Tipe göre adet veya dayanıklılık düşürür.
    /// </summary>
    /// <returns>Eşya tamamen tükenmiş/kırılmış mı (envanterden silinmeli)</returns>
    public bool Use()
    {
        switch (itemData.itemType)
        {
            case ItemType.Consumable:
            case ItemType.ThrowableFlask:
            case ItemType.SpellScroll:
                currentStack--;
                return IsEmpty;

            case ItemType.WeaponTool:
                if (itemData.HasDurability)
                {
                    currentDurability--;
                    return IsBroken;
                }
                return false;

            case ItemType.KeyItem:
            case ItemType.Shield:
            case ItemType.Armor:
                // Sonsuz dayanıklılık — asla tükenmez (Shield/Armor hasarla kırılır)
                return false;

            case ItemType.ReadableNote:
                // Not okunur — tüketilmez
                return false;

            case ItemType.QuestItem:
                // Görev eşyaları fırlatılabilir olanlar için adet azalır
                if (itemData.isThrowable)
                {
                    currentStack--;
                    return IsEmpty;
                }
                return false;

            case ItemType.Passive:
            case ItemType.Arrow:
            case ItemType.Quiver:
            default:
                return false;
        }
    }

    /// <summary>
    /// Dayanıklılık yüzdesi (0–1 arası). Sonsuz veya dayanıklılıksız ise 1 döner.
    /// </summary>
    public float DurabilityPercent
    {
        get
        {
            if (!itemData.HasDurability || itemData.IsInfinite) return 1f;
            return (float)currentDurability / itemData.maxDurability;
        }
    }

    /// <summary>
    /// Eşyanın döndürme durumunu değiştirir (genişlik ↔ yükseklik).
    /// </summary>
    public void ToggleRotation()
    {
        isRotated = !isRotated;
    }
}
