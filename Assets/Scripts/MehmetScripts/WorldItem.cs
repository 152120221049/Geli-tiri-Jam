using UnityEngine;

/// <summary>
/// Oyun dünyasında yerde duran, toplanabilir 2D eşya bileşeni.
/// IInteractable arayüzünü uygular. Oyuncu yaklaşıp 'E' tuşuna bastığında envantere eklenir.
/// Hafif yukarı-aşağı süzülme (floating animation) efekti içerir.
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class WorldItem : MonoBehaviour, IInteractable
{
    [Header("Eşya Bilgisi")]
    public ItemSO itemData;
    [Min(1)] public int count = 1;

    [Header("Animasyon & Görsel")]
    [SerializeField] private SpriteRenderer spriteRenderer;
    [SerializeField] private bool enableFloatingAnimation = true;
    [SerializeField] private float floatSpeed = 3.0f;
    [SerializeField] private float floatAmount = 0.15f;

    private Vector3 initialPosition;

    private void Awake()
    {
        if (spriteRenderer == null)
            spriteRenderer = GetComponentInChildren<SpriteRenderer>();

        // Collider'ı Trigger yap (fizik çakışması olmasın)
        Collider2D col = GetComponent<Collider2D>();
        if (col != null)
            col.isTrigger = true;

        initialPosition = transform.position;
    }

    private void Start()
    {
        UpdateVisuals();
    }

    private void Update()
    {
        // Yerde süzülme animasyonu
        if (enableFloatingAnimation)
        {
            float newY = initialPosition.y + Mathf.Sin(Time.time * floatSpeed) * floatAmount;
            transform.position = new Vector3(transform.position.x, newY, transform.position.z);
        }
    }

    /// <summary>Eşya ikonunu SpriteRenderer'a atar.</summary>
    public void UpdateVisuals()
    {
        if (itemData != null && spriteRenderer != null && itemData.icon != null)
        {
            spriteRenderer.sprite = itemData.icon;
        }
    }

    /// <summary>
    /// Eşyayı ayarlar ve görselini günceller (Dinamik yer eşyası spawn etmek için).
    /// </summary>
    public void Setup(ItemSO data, int amount = 1)
    {
        itemData = data;
        count = amount;
        UpdateVisuals();
    }

    // ═══════════════════════════════════════════
    //  IINTERACTABLE IMPLEMENTATION
    // ═══════════════════════════════════════════

    public string GetInteractPrompt()
    {
        if (itemData == null) return "[E] Eşya";
        return $"[E] {itemData.itemName} x{count}";
    }

    public bool CanInteract(Transform interactor)
    {
        return itemData != null;
    }

    public void Interact(Transform interactor)
    {
        if (itemData == null || InventoryManager.Instance == null) return;

        bool added = InventoryManager.Instance.AddItem(itemData, count);

        if (added)
        {
            Debug.Log($"📦 [DÜNYA EŞYASI] {itemData.itemName} x{count} toplandı!");
            Destroy(gameObject);
        }
        else
        {
            Debug.LogWarning($"❌ [DÜNYA EŞYASI] Envanter dolu! {itemData.itemName} toplanamadı.");
        }
    }
}
