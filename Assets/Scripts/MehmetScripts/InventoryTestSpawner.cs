using UnityEngine;

/// <summary>
/// Envanter sistemini hızlıca test etmek için test eşyaları oluşturan ve envantere ekleyen test bileşeni.
/// Sahnedeki herhangi bir GameObject'e eklenebilir veya 'T' tuşuna basarak test eşyaları eklenebilir.
/// Eşya kullanımlarında konsola (Debug.Log) bilgilendirme mesajı yazdırır.
/// </summary>
public class InventoryTestSpawner : MonoBehaviour
{
    [Header("Test Ayarları")]
    [Tooltip("Başlangıçta otomatik test eşyaları eklensin mi?")]
    [SerializeField] private bool addTestItemsOnStart = true;

    [Tooltip("Klavyeden 'T' tuşuna basıldığında test eşyaları eklensin mi?")]
    [SerializeField] private bool addTestItemsOnTPress = true;

    private void Start()
    {
        // Eşya kullanım olayına abone ol
        if (InventoryManager.Instance != null)
        {
            InventoryManager.Instance.OnItemUsed += OnItemUsedCallback;
            InventoryManager.Instance.OnItemRemoved += OnItemRemovedCallback;
        }

        if (addTestItemsOnStart)
        {
            // Birkaç kare gecikmeyle ekle (Manager ve UI tam başlasın)
            Invoke(nameof(SpawnAllTestItems), 0.2f);
        }
    }

    private void Update()
    {
        if (addTestItemsOnTPress && InventoryInput.IsRotateKeyPressed()) // R harfi yerleşim için, T harfini aşağıda kontrol edelim
        {
            // T tuşu ile yeni test eşyası ekleme
#if ENABLE_INPUT_SYSTEM
            if (UnityEngine.InputSystem.Keyboard.current != null && UnityEngine.InputSystem.Keyboard.current.tKey.wasPressedThisFrame)
            {
                SpawnAllTestItems();
            }
#else
            if (Input.GetKeyDown(KeyCode.T))
            {
                SpawnAllTestItems();
            }
#endif
        }
    }

    private void OnDestroy()
    {
        if (InventoryManager.Instance != null)
        {
            InventoryManager.Instance.OnItemUsed -= OnItemUsedCallback;
            InventoryManager.Instance.OnItemRemoved -= OnItemRemovedCallback;
        }
    }

    /// <summary>Tüm farklı boyut ve tipteki test eşyalarını oluşturur ve envantere/hotbar'a ekler.</summary>
    [ContextMenu("Test Eşyalarını Ekle")]
    public void SpawnAllTestItems()
    {
        if (InventoryManager.Instance == null)
        {
            Debug.LogWarning("InventoryTestSpawner: InventoryManager sahnede bulunamadı!");
            return;
        }

        Debug.Log("🧪 [ENVANTER TEST] Test eşyaları oluşturuluyor...");

        // 1. Tüketilebilir Can İksiri (1x1, Max Stack 5)
        ItemSO potionData = CreateTestSO("Can İksiri", "Canı 50 puan yeniler.", 1, 1, ItemType.Consumable, maxStack: 5, color: new Color(0.9f, 0.2f, 0.2f));
        InventoryManager.Instance.AddItem(potionData, 3);

        // 2. Hafif Bıçak (1x2 dikey silah, Dayanıklılık 10)
        ItemSO daggerData = CreateTestSO("Avcı Bıçağı", "Dikey 1x2 küçük silah.", 1, 2, ItemType.WeaponTool, durability: 10, color: new Color(0.2f, 0.6f, 0.9f));
        InventoryManager.Instance.AddItem(daggerData, 1);

        // 3. Tabanca (2x1 yatay silah, Dayanıklılık 15)
        ItemSO pistolData = CreateTestSO("Tabanca", "Yatay 2x1 menzilli silah.", 2, 1, ItemType.WeaponTool, durability: 15, color: new Color(0.8f, 0.5f, 0.2f));
        InventoryManager.Instance.AddItem(pistolData, 1);

        // 4. Büyük Kalkan (2x2 kare ekipman, Dayanıklılık 20)
        ItemSO shieldData = CreateTestSO("Ahşap Kalkan", "Kare 2x2 savunma ekipmanı.", 2, 2, ItemType.WeaponTool, durability: 20, color: new Color(0.3f, 0.7f, 0.3f));
        InventoryManager.Instance.AddItem(shieldData, 1);

        // 5. Altın Anahtar (1x1 Anahtar Eşya, Sonsuz Dayanıklılık)
        ItemSO keyData = CreateTestSO("Altın Anahtar", "Kilitli kapıları açar. Asla kırılmaz/tükenmez.", 1, 1, ItemType.KeyItem, durability: -1, color: new Color(0.95f, 0.8f, 0.1f));
        InventoryManager.Instance.AddItem(keyData, 1);

        // 6. Demir Cevheri (1x1 Pasif Malzeme, Max Stack 10)
        ItemSO oreData = CreateTestSO("Demir Cevheri", "Pasif üretim malzemesi. Doğrudan kullanılamaz.", 1, 1, ItemType.Passive, maxStack: 10, color: new Color(0.5f, 0.5f, 0.5f));
        InventoryManager.Instance.AddItem(oreData, 5);

        Debug.Log("✅ [ENVANTER TEST] Test eşyaları başarıyla eklendi! (Klavyeden 'T' tuşuna basarak tekrar ekleyebilirsiniz)");
    }

    /// <summary>Dinamik ScriptableObject ve renkli ikon sprite'ı üretir.</summary>
    private ItemSO CreateTestSO(string name, string desc, int width, int height, ItemType type, int maxStack = 1, int durability = 0, Color color = default)
    {
        ItemSO item = ScriptableObject.CreateInstance<ItemSO>();
        item.itemName = name;
        item.description = desc;
        item.gridWidth = width;
        item.gridHeight = height;
        item.itemType = type;
        item.maxStack = maxStack;
        item.maxDurability = durability;

        if (color == default) color = Color.cyan;
        item.icon = CreateColoredSprite(color, name);

        return item;
    }

    /// <summary>Görsel test ikonu için renkli sprite oluşturur.</summary>
    private Sprite CreateColoredSprite(Color color, string label)
    {
        int size = 64;
        Texture2D tex = new Texture2D(size, size);
        Color[] pixels = new Color[size * size];

        // İç dolgu ve kenarlık
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                bool isBorder = (x < 3 || x >= size - 3 || y < 3 || y >= size - 3);
                pixels[y * size + x] = isBorder ? Color.black : color;
            }
        }

        tex.SetPixels(pixels);
        tex.Apply();

        return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f));
    }

    // ═══════════════════════════════════════════
    //  EŞYA KULLANIM LOGLARI
    // ═══════════════════════════════════════════

    private void OnItemUsedCallback(InventoryItem item)
    {
        if (item == null || item.itemData == null) return;

        string typeStr = item.itemData.itemType.ToString();
        string detail = "";

        switch (item.itemData.itemType)
        {
            case ItemType.Consumable:
                detail = $"Kalan Adet: {item.currentStack}/{item.itemData.maxStack}";
                break;

            case ItemType.WeaponTool:
                detail = item.itemData.HasDurability
                    ? $"Kalan Dayanıklılık: {item.currentDurability}/{item.itemData.maxDurability}"
                    : "Dayanıklılık Sınırsız";
                break;

            case ItemType.KeyItem:
                detail = "Anahtar eşya kullanıldı (tükenmez).";
                break;

            case ItemType.Passive:
                detail = "Pasif eşya kullanılamaz.";
                break;
        }

        Debug.Log($"🎮 [EŞYA KULLANILDI] ➜ '{item.itemData.itemName}' [{typeStr}] | {detail}");
    }

    private void OnItemRemovedCallback(InventoryItem item)
    {
        if (item == null || item.itemData == null) return;
        Debug.Log($"🗑️ [EŞYA TÜKENDİ / KIRILDI] ➜ '{item.itemData.itemName}' envanterden kaldırıldı!");
    }
}
