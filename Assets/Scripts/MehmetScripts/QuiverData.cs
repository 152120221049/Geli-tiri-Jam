using UnityEngine;

/// <summary>
/// Sadak (Quiver) veri yöneticisi.
/// InventoryItem'a eklenerek ok depolama, tip doğrulama ve tüketim mantığını yönetir.
/// </summary>
[System.Serializable]
public class QuiverData
{
    /// <summary>Sadakta depolanan ok sayısı.</summary>
    public int storedArrowCount = 0;

    /// <summary>Sadakta depolanan ok tipi (ItemSO referansı). Null = boş sadak.</summary>
    public ItemSO storedArrowType;

    /// <summary>Sadak dolu mu?</summary>
    public bool IsFull(int maxCapacity) => storedArrowCount >= maxCapacity;

    /// <summary>Sadak boş mu?</summary>
    public bool IsEmpty => storedArrowCount <= 0;

    /// <summary>
    /// Sadağa ok yüklemeyi dener.
    /// Aynı tip ok olmalı veya sadak boş olmalıdır.
    /// </summary>
    /// <returns>Yükleme başarılı mı?</returns>
    public bool TryLoadArrow(ItemSO arrowItemData, int maxCapacity)
    {
        if (arrowItemData == null) return false;
        if (arrowItemData.itemType != ItemType.Arrow) return false;

        // Sadak doluysa reddet
        if (IsFull(maxCapacity))
        {
            Debug.LogWarning($"🏹 [SADAK] Sadak dolu! ({storedArrowCount}/{maxCapacity})");
            return false;
        }

        // Tip kontrolü — boş sadak her oku kabul eder, dolu sadak sadece aynı tipi
        if (storedArrowType != null && storedArrowType != arrowItemData)
        {
            Debug.LogWarning($"🏹 [SADAK] Sadakte '{storedArrowType.itemName}' var! Sadece aynı tip ok yüklenebilir. ('{arrowItemData.itemName}' reddedildi)");
            return false;
        }

        storedArrowType = arrowItemData;
        storedArrowCount++;

        if (InventoryManager.Instance != null)
            InventoryManager.Instance.NotifyInventoryChanged();

        Debug.Log($"🏹 [SADAK] Ok yüklendi: {arrowItemData.itemName} ({storedArrowCount}/{maxCapacity})");
        return true;
    }

    /// <summary>
    /// Sadaktan 1 ok tüketir ve ok tipini döndürür.
    /// </summary>
    /// <returns>Tüketilen ok tipi (null = boş).</returns>
    public ItemSO ConsumeArrow()
    {
        if (IsEmpty)
        {
            Debug.LogWarning("🏹 [SADAK] Sadak boş! Ok doldurmalısınız.");
            return null;
        }

        ItemSO arrowType = storedArrowType;
        storedArrowCount--;

        if (storedArrowCount <= 0)
        {
            storedArrowCount = 0;
            storedArrowType = null; // Boşalan sadak yeni tip ok kabul edebilir
        }

        if (InventoryManager.Instance != null)
            InventoryManager.Instance.NotifyInventoryChanged();

        Debug.Log($"🏹 [SADAK] Ok ateşlendi: {arrowType.itemName} (Kalan: {storedArrowCount})");
        return arrowType;
    }
}
