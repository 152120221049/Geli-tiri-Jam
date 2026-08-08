using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// Grid hücresine bırakma hedefi.
/// Sürüklenen eşya bu hücreye bırakıldığında InventoryManager üzerinden taşıma işlemi gerçekleştirir.
/// Boş hücreye tıklandığında açık Tooltip'i kapatır.
/// </summary>
public class InventoryDropZone : MonoBehaviour, IDropHandler, IPointerClickHandler
{
    private InventoryGridData targetGrid;
    public InventoryGridData TargetGrid => targetGrid;
    private int cellX;
    private int cellY;

    /// <summary>Bu drop zone'u belirli bir grid hücresi ile ilişkilendirir.</summary>
    public void Setup(InventoryGridData grid, int x, int y)
    {
        targetGrid = grid;
        cellX = x;
        cellY = y;
    }

    /// <summary>
    /// Sürüklenen eşya bu hücreye bırakıldığında çağrılır.
    /// Eşyayı hedef grid'in bu pozisyonuna taşımayı dener.
    /// </summary>
    public void OnDrop(PointerEventData eventData)
    {
        if (targetGrid == null) return;

        InventoryItemUI draggedItemUI = eventData.pointerDrag?.GetComponent<InventoryItemUI>();
        if (draggedItemUI == null || draggedItemUI.Item == null) return;

        InventoryItem item = draggedItemUI.Item;

        // 1.5) Ok → Sadak (Arrow → Quiver) doldurma kontrolü
        InventoryItem existingItemAtCell = targetGrid.GetItemAt(cellX, cellY);
        if (item.itemData.itemType == ItemType.Arrow && existingItemAtCell != null && existingItemAtCell.itemData.itemType == ItemType.Quiver)
        {
            if (PlayerEquipmentController.Instance != null)
            {
                QuiverData qData = PlayerEquipmentController.Instance.GetQuiverData(existingItemAtCell);
                bool loaded = qData.TryLoadArrow(item.itemData, existingItemAtCell.itemData.maxArrowCapacity);
                if (loaded)
                {
                    InventoryManager.Instance.RemoveItemCompletely(item);
                    return;
                }
            }
        }

        // 1.6) Zırh Slot Kısıtı Kontrolü
        if (item.itemData.itemType == ItemType.Armor && PlayerArmorSystem.Instance != null)
        {
            if (!PlayerArmorSystem.Instance.CanAddArmorItem(item.itemData))
            {
                return;
            }
        }

        // 1) Sürüklenen görselin sol üst köşesine en yakın hücreyi hesapla
        Vector2Int targetCell = new Vector2Int(cellX, cellY);
        if (InventoryUI.Instance != null)
        {
            targetCell = InventoryUI.Instance.GetNearestCell(draggedItemUI.GetComponent<RectTransform>(), targetGrid, item, targetCell);
        }

        // 2) Önce hesaplanan sol üst hücreye yerleştirmeyi dene
        bool success = InventoryManager.Instance.MoveItem(item, targetGrid, targetCell.x, targetCell.y);

        // 3) Başarısız olursa doğrudan fare altındaki hücreye yerleştirmeyi dene
        if (!success && (targetCell.x != cellX || targetCell.y != cellY))
        {
            success = InventoryManager.Instance.MoveItem(item, targetGrid, cellX, cellY);
        }

        if (success)
        {
            Debug.Log($"✅ {item.itemData.itemName} → Grid[{targetCell.x},{targetCell.y}] taşındı.");
        }
        else
        {
            Debug.LogWarning($"❌ {item.itemData.itemName} → Grid[{targetCell.x},{targetCell.y}] sığmadı / engellendi.");
        }
    }

    /// <summary>
    /// Boş hücreye tıklandığında açık olan Tooltip'i kapatır.
    /// </summary>
    public void OnPointerClick(PointerEventData eventData)
    {
        // Bu hücrede eşya yoksa, tooltip'i kapat
        if (targetGrid != null && targetGrid.GetItemAt(cellX, cellY) == null)
        {
            if (ItemTooltipUI.Instance != null)
                ItemTooltipUI.Instance.Hide();
        }
    }
}
