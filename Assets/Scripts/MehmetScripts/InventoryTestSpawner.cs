using UnityEngine;

/// <summary>
/// Tüm 26 silah, zırh, iksir, ok, sadak ve parşömen eşyalarını envanter sisteminde hızlıca test etmek için spawner.
/// 'T' tuşuna basıldığında tüm test eşyalarını oluşturup envantere yükler.
/// </summary>
public class InventoryTestSpawner : MonoBehaviour
{
    [Header("Test Ayarları")]
    [SerializeField] private bool addTestItemsOnStart = true;
    [SerializeField] private bool addTestItemsOnTPress = true;

    [Header("Özel Test Eşyaları (İsteğe Bağlı - Gerçek ItemSO'larınızı Ekleyebilirsiniz)")]
    [Tooltip("Eğer buraya kendi oluşturduğunuz ItemSO dosyalarını eklerseniz, kod sahte renkli eşyalar yerine sizin ikonlarınızı kullanır.")]
    [SerializeField] private System.Collections.Generic.List<ItemSO> customTestItems = new System.Collections.Generic.List<ItemSO>();

    private void Start()
    {
        if (InventoryManager.Instance != null)
        {
            InventoryManager.Instance.OnItemUsed += OnItemUsedCallback;
            InventoryManager.Instance.OnItemRemoved += OnItemRemovedCallback;
        }

        if (addTestItemsOnStart)
        {
            Invoke(nameof(SpawnAllTestItems), 0.2f);
        }
    }

    private void Update()
    {
        if (addTestItemsOnTPress && InventoryInput.IsRotateKeyPressed())
        {
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

    [ContextMenu("Tüm Test Eşyalarını Ekle")]
    public void SpawnAllTestItems()
    {
        if (InventoryManager.Instance == null)
        {
            Debug.LogWarning("InventoryTestSpawner: InventoryManager sahnede bulunamadı!");
            return;
        }

        // Eğer kullanıcı Inspector'dan özel ItemSO'lar eklediyse onları yükle
        if (customTestItems != null && customTestItems.Count > 0)
        {
            Debug.Log("🧪 [ENVANTER TEST] Özel atanmış gerçek ItemSO'lar yükleniyor...");
            foreach (var itemSO in customTestItems)
            {
                if (itemSO != null)
                {
                    InventoryManager.Instance.AddItem(itemSO, 1);
                }
            }
            InventoryManager.Instance.NotifyInventoryChanged();
            return;
        }

        Debug.Log("🧪 [ENVANTER TEST] Tüm ekipman ve silah test eşyaları oluşturuluyor...");

        // 1. Kılıç (2x1 -> Hotbar Slot 0-1)
        ItemSO sword = CreateTestSO("Kılıç", "Dengeli kılıç. Sol Tık: Savurma, Sağ Tık Basılı: Nişan alıp fırlat (2 pierce).", 2, 1, ItemType.WeaponTool, durability: 35, color: new Color(0.85f, 0.85f, 0.9f));
        sword.canMeleeAttack = true; sword.meleeDamage = 15f; sword.meleeRange = 1.4f; sword.attackCooldown = 0.55f; sword.swingArcAngle = 75f;
        sword.isThrowable = true; sword.throwStyle = ThrowStyle.Arc; sword.throwDamage = 15f; sword.pierceCount = 2; sword.throwDurabilityCost = 3;

        InventoryItem swordItem = new InventoryItem(sword, 1);
        if (!InventoryManager.Instance.HotbarGrid.PlaceItem(swordItem, 0, 0))
            InventoryManager.Instance.InventoryGrid.AutoPlaceItem(swordItem);

        // 2. Can İksiri (1x1 -> Hotbar Slot 2)
        ItemSO hpPotion = CreateTestSO("Can İksiri", "Canı 50 yeniler, boş şişeyi yavaşlatıcı olarak fırlatır.", 1, 1, ItemType.Consumable, maxStack: 5, color: new Color(0.9f, 0.2f, 0.2f));
        hpPotion.appliesSlowOnHit = true;
        InventoryItem hpItem = new InventoryItem(hpPotion, 3);
        if (!InventoryManager.Instance.HotbarGrid.PlaceItem(hpItem, 2, 0))
            InventoryManager.Instance.InventoryGrid.AutoPlaceItem(hpItem);

        // 3. Hançer (2x1 -> Hotbar Slot 3-4)
        ItemSO dagger = CreateTestSO("Hançer", "Çok hızlı (0.25s). Düz hatta fırlatılır (3 pierce).", 2, 1, ItemType.WeaponTool, durability: 20, color: new Color(0.3f, 0.7f, 0.9f));
        dagger.canMeleeAttack = true; dagger.meleeDamage = 5f; dagger.meleeRange = 0.9f; dagger.attackCooldown = 0.25f; dagger.swingArcAngle = 45f;
        dagger.isThrowable = true; dagger.throwStyle = ThrowStyle.StraightLine; dagger.throwDamage = 5f; dagger.pierceCount = 3; dagger.throwDurabilityCost = 3;

        InventoryItem daggerItem = new InventoryItem(dagger, 1);
        if (!InventoryManager.Instance.HotbarGrid.PlaceItem(daggerItem, 3, 0))
            InventoryManager.Instance.InventoryGrid.AutoPlaceItem(daggerItem);

        // 4. Yay (3x1 -> Envanter Grid)
        ItemSO bow = CreateTestSO("Yay", "Sadak'tan ok çeker. Sol Tık: Ok ateşle.", 3, 1, ItemType.WeaponTool, durability: 30, color: new Color(0.65f, 0.45f, 0.2f));
        InventoryManager.Instance.AddItem(bow, 1);

        // 5. Sadak (3x1 -> Envanter Grid)
        ItemSO quiver = CreateTestSO("Sadak (Quiver)", "Maksimum 10 ok depolar.", 3, 1, ItemType.Quiver, color: new Color(0.55f, 0.35f, 0.15f));
        quiver.maxArrowCapacity = 10;
        InventoryItem quiverItem = new InventoryItem(quiver, 1);
        InventoryManager.Instance.InventoryGrid.AutoPlaceItem(quiverItem);

        // Sadağa 5 ok doldur
        if (PlayerEquipmentController.Instance != null)
        {
            ItemSO arrowSO = CreateTestSO("Normal Ok", "Sadak'a doldurulur.", 2, 1, ItemType.Arrow, maxStack: 1, color: new Color(0.7f, 0.7f, 0.7f));
            arrowSO.pierceCount = 1;
            QuiverData qData = PlayerEquipmentController.Instance.GetQuiverData(quiverItem);
            for (int i = 0; i < 5; i++)
            {
                qData.TryLoadArrow(arrowSO, 10);
            }
        }

        // 5. Patlayıcı İksir (1x1)
        ItemSO murkyVial = CreateTestSO("Patlayıcı İksir", "Fırlatılır, çarptığı yerde alev alanı bırakır.", 1, 1, ItemType.ThrowableFlask, maxStack: 5, color: new Color(0.2f, 0.8f, 0.3f));
        murkyVial.leavesFireArea = true; murkyVial.isThrowable = true; murkyVial.throwStyle = ThrowStyle.Arc; murkyVial.throwDamage = 40f;
        InventoryManager.Instance.AddItem(murkyVial, 2);

        // 6. Fireball Parşömeni (1x1)
        ItemSO fireballScroll = CreateTestSO("Fireball Parşömeni", "60 AoE ateş topu büyüsü fırlatır.", 1, 1, ItemType.SpellScroll, maxStack: 3, color: new Color(1f, 0.3f, 0.1f));
        fireballScroll.spellDamage = 60f; fireballScroll.spellAoeRadius = 2f;
        InventoryManager.Instance.AddItem(fireballScroll, 2);

        // 8. Ateşli Ok (2x1)
        ItemSO fireArrow = CreateTestSO("Ateşli Ok", "Sadağa yüklenir (2x1).", 2, 1, ItemType.Arrow, maxStack: 1, color: new Color(1f, 0.45f, 0.1f));
        fireArrow.pierceCount = 1; fireArrow.leavesFireArea = true;
        InventoryManager.Instance.AddItem(fireArrow, 1);

        // 9. Deri Kask (1x1)
        ItemSO leatherHelm = CreateTestSO("Deri Kask", "+3 Armor (can).", 1, 1, ItemType.Armor, durability: 30, color: new Color(0.6f, 0.4f, 0.25f));
        leatherHelm.armorValue = 3; leatherHelm.armorSlotType = ArmorSlotType.Helmet; leatherHelm.weightAccelTime = 0.15f;
        InventoryManager.Instance.AddItem(leatherHelm, 1);

        // 10. Mum (1x1)
        ItemSO candle = CreateTestSO("Mum", "Fırlatılabilir ışık kaynağı.", 1, 1, ItemType.QuestItem, maxStack: 10, color: new Color(1f, 0.95f, 0.5f));
        candle.isThrowable = true; candle.throwStyle = ThrowStyle.Arc; candle.createsLightOnLand = true; candle.throwDamage = 2f;
        InventoryManager.Instance.AddItem(candle, 3);

        // 11. Taş (1x1)
        ItemSO stone = CreateTestSO("Taş", "Fırlatılır (5 hasar).", 1, 1, ItemType.QuestItem, maxStack: 10, color: new Color(0.5f, 0.5f, 0.5f));
        stone.isThrowable = true; stone.throwStyle = ThrowStyle.Arc; stone.throwDamage = 5f;
        InventoryManager.Instance.AddItem(stone, 5);

        InventoryManager.Instance.NotifyInventoryChanged();
        Debug.Log("✅ [ENVANTER TEST] Tüm modern ekipmanlar başarıyla yüklendi!");
    }

    private ItemSO CreateTestSO(string name, string desc, int width, int height, ItemType type, int maxStack = 1, int durability = 0, Color color = default)
    {
        // Her aşamada öncelikle projede var olan gerçek ItemSO (.asset) dosyasını kontrol et
        ItemSO existingSO = TryFindExistingSO(name);
        if (existingSO != null)
        {
            // Eğer gerçek SO bulunduysa ama ikonu henüz atanmamışsa geçici desenli ikon ver
            if (existingSO.icon == null)
            {
                if (color == default) color = Color.cyan;
                existingSO.icon = CreateColoredSprite(color, name);
            }
            return existingSO;
        }

        // Projede var olan SO bulunamazsa geçici SO oluştur
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

    private ItemSO TryFindExistingSO(string name)
    {
#if UNITY_EDITOR
        // Projedeki tüm ItemSO asset'lerini tarayalım
        string[] guids = UnityEditor.AssetDatabase.FindAssets("t:ItemSO");
        foreach (string guid in guids)
        {
            string path = UnityEditor.AssetDatabase.GUIDToAssetPath(guid);
            ItemSO asset = UnityEditor.AssetDatabase.LoadAssetAtPath<ItemSO>(path);
            if (asset != null)
            {
                if (string.Equals(asset.itemName, name, System.StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(asset.name, name, System.StringComparison.OrdinalIgnoreCase) ||
                    CleanString(asset.name).Equals(CleanString(name), System.StringComparison.OrdinalIgnoreCase))
                {
                    return asset;
                }
            }
        }
#endif
        ItemSO res = Resources.Load<ItemSO>("Items/" + name);
        if (res != null) return res;

        return null;
    }

    private string CleanString(string str)
    {
        if (string.IsNullOrEmpty(str)) return "";
        return str.Replace("ı", "i").Replace("İ", "I").Replace("ğ", "g").Replace("Ğ", "G")
                  .Replace("ü", "u").Replace("Ü", "U").Replace("ş", "s").Replace("Ş", "S")
                  .Replace("ö", "o").Replace("Ö", "O").Replace("ç", "c").Replace("Ç", "C")
                  .Replace(" ", "").Replace("-", "").Replace("_", "").ToLower();
    }

    private Sprite CreateColoredSprite(Color color, string label)
    {
        int size = 64;
        Texture2D tex = new Texture2D(size, size);
        Color[] pixels = new Color[size * size];

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                bool isBorder = (x < 3 || x >= size - 3 || y < 3 || y >= size - 3);
                // İçi boş kalmasın diye çapraz ve iç çerçeve deseni ekle
                bool isDiagonal = (Mathf.Abs(x - y) <= 1) || (Mathf.Abs(x - (size - y)) <= 1);
                bool isInnerBorder = (x == 8 || x == size - 9 || y == 8 || y == size - 9);

                Color c = isBorder ? Color.black : ((isDiagonal || isInnerBorder) ? Color.white * 0.9f : color);
                pixels[y * size + x] = c;
            }
        }

        tex.SetPixels(pixels);
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f));
    }

    private void OnItemUsedCallback(InventoryItem item)
    {
        if (item == null || item.itemData == null) return;
        Debug.Log($"🎮 [EŞYA KULLANILDI] ➜ '{item.itemData.itemName}' [{item.itemData.itemType}]");
    }

    private void OnItemRemovedCallback(InventoryItem item)
    {
        if (item == null || item.itemData == null) return;
        Debug.Log($"🗑️ [EŞYA TÜKENDİ / KIRILDI] ➜ '{item.itemData.itemName}' envanterden kaldırıldı!");
    }
}
