using System.Collections.Generic;

/// <summary>
/// Grid tabanlı envanter matrisi yönetimi.
/// Hücre bazlı çakışma kontrolü, yerleştirme, kaldırma ve otomatik yerleşim sunar.
/// Hem ana envanter (WxH) hem de Hotbar (Nx1) için kullanılır.
/// </summary>
public class InventoryGridData
{
    public int width { get; private set; }
    public int height { get; private set; }

    /// <summary>Grid matrisi: her hücre hangi InventoryItem'a referans verir (null = boş).</summary>
    private InventoryItem[,] grid;

    /// <summary>Grid'deki tüm benzersiz eşyalar.</summary>
    private List<InventoryItem> items = new List<InventoryItem>();

    public InventoryGridData(int width, int height)
    {
        this.width = width;
        this.height = height;
        grid = new InventoryItem[width, height];
    }

    /// <summary>Grid'deki tüm benzersiz eşyaların kopyasını döndürür.</summary>
    public List<InventoryItem> GetAllItems()
    {
        return new List<InventoryItem>(items);
    }

    /// <summary>Belirli bir hücredeki eşyayı döndürür. Sınır dışı ise null döner.</summary>
    public InventoryItem GetItemAt(int x, int y)
    {
        if (x < 0 || x >= width || y < 0 || y >= height) return null;
        return grid[x, y];
    }

    /// <summary>
    /// Belirli bir slot indeksindeki eşyayı döndürür (Hotbar için: 1D index → 2D koordinat).
    /// Satır-öncelikli: slotIndex = y * width + x.
    /// </summary>
    public InventoryItem GetItemAtSlot(int slotIndex)
    {
        int x = slotIndex % width;
        int y = slotIndex / width;
        return GetItemAt(x, y);
    }

    /// <summary>
    /// Eşyanın belirtilen pozisyona yerleştirilebilirliğini kontrol eder.
    /// Sınır ve çakışma kontrolü yapar.
    /// </summary>
    public bool CanPlaceItem(InventoryItem item, int posX, int posY)
    {
        if (item == null || item.itemData == null) return false;

        int w = item.itemData.gridWidth;
        int h = item.itemData.gridHeight;

        if (item.isRotated)
        {
            int tmp = w; w = h; h = tmp;
        }

        // Hotbar (height == 1) kuralları:
        if (height == 1)
        {
            // 2x2, 2x3 gibi eni ve boyu 1'den büyük olan Zırh/Kalkan eşyaları Hotbar'a giremez
            if (item.itemData.gridWidth > 1 && item.itemData.gridHeight > 1)
                return false;

            // Dikey 1xN eşyalar Hotbar'da yatay Nx1 olarak yerleşir
            if (h > 1 && w == 1)
            {
                w = h; // Nx1
                h = 1;
            }
        }

        // Sınır kontrolü
        if (posX < 0 || posY < 0 || posX + w > width || posY + h > height)
            return false;

        // Çakışma kontrolü
        for (int x = posX; x < posX + w; x++)
        {
            for (int y = posY; y < posY + h; y++)
            {
                if (grid[x, y] != null && grid[x, y] != item)
                    return false;
            }
        }

        return true;
    }

    /// <summary>
    /// Eşyayı belirtilen pozisyona yerleştirir.
    /// Eğer eşya zaten grid'de ise, önce kaldırılır.
    /// </summary>
    /// <returns>Yerleştirme başarılı mı.</returns>
    public bool PlaceItem(InventoryItem item, int posX, int posY)
    {
        if (!CanPlaceItem(item, posX, posY)) return false;

        // Eğer eşya zaten bu grid'de ise, önce kaldır
        if (items.Contains(item))
            RemoveItem(item);

        int w = item.itemData.gridWidth;
        int h = item.itemData.gridHeight;

        if (item.isRotated)
        {
            int tmp = w; w = h; h = tmp;
        }

        if (height == 1 && h > 1 && w == 1)
        {
            w = h;
            h = 1;
        }

        for (int x = posX; x < posX + w; x++)
        {
            for (int y = posY; y < posY + h; y++)
            {
                grid[x, y] = item;
            }
        }

        item.gridX = posX;
        item.gridY = posY;
        items.Add(item);
        return true;
    }

    /// <summary>
    /// Eşyayı grid'den kaldırır. Kapladığı tüm hücreleri temizler.
    /// </summary>
    public void RemoveItem(InventoryItem item)
    {
        if (!items.Contains(item)) return;

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                if (grid[x, y] == item)
                    grid[x, y] = null;
            }
        }

        item.gridX = -1;
        item.gridY = -1;
        items.Remove(item);
    }

    /// <summary>
    /// Eşyayı otomatik olarak ilk uygun boş pozisyona yerleştirir (sol-üstten tarama).
    /// </summary>
    public bool AutoPlaceItem(InventoryItem item)
    {
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                if (CanPlaceItem(item, x, y))
                {
                    return PlaceItem(item, x, y);
                }
            }
        }
        return false;
    }

    /// <summary>
    /// Aynı türden istiflenebilir eşya varsa, üstüne eklemeyi dener.
    /// </summary>
    /// <returns>Eklenen miktar (0 = eklenemedi).</returns>
    public int TryStackItem(ItemSO itemData, int amount)
    {
        int remaining = amount;

        foreach (var existing in items)
        {
            if (remaining <= 0) break;
            if (existing.itemData != itemData) continue;
            if (existing.currentStack >= itemData.maxStack) continue;

            int canAdd = itemData.maxStack - existing.currentStack;
            int toAdd = remaining < canAdd ? remaining : canAdd;
            existing.currentStack += toAdd;
            remaining -= toAdd;
        }

        return amount - remaining;
    }

    /// <summary>Grid'i tamamen temizler.</summary>
    public void Clear()
    {
        for (int x = 0; x < width; x++)
            for (int y = 0; y < height; y++)
                grid[x, y] = null;
        items.Clear();
    }

    /// <summary>Grid'deki toplam eşya sayısını döndürür.</summary>
    public int ItemCount => items.Count;

    /// <summary>Belirtilen eşyanın bu grid'de olup olmadığını kontrol eder.</summary>
    public bool Contains(InventoryItem item) => items.Contains(item);
}
