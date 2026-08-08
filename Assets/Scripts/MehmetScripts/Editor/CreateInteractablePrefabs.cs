#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System.IO;

/// <summary>
/// Unity Editor menüsü üzerinden tüm 26 oyun eşyasının ScriptableObject (.asset)
/// ve WorldItem Prefab (.prefab) dosyalarını otomatik üreten Editör aracı.
/// </summary>
public static class CreateInteractablePrefabs
{
    [MenuItem("Tools/Envanter & Etkileşim/Tüm 26 Eşya & Prefab'ları Oluştur")]
    public static void GenerateAllGameItemsAndPrefabs()
    {
        string prefabsFolder = "Assets/Prefabs/Interactables";
        string itemsFolder = "Assets/GameData/Items";

        if (!Directory.Exists(prefabsFolder)) Directory.CreateDirectory(prefabsFolder);
        if (!Directory.Exists(itemsFolder)) Directory.CreateDirectory(itemsFolder);

        AssetDatabase.Refresh();

        // 1. Normal Ok (2x1, Arrow)
        ItemSO arrowData = CreateOrLoadSO(itemsFolder, "NormalOk", "Normal Ok", "Yay ile ateşlenir. (2x1, İstiflenmez)", 2, 1, ItemType.Arrow, maxStack: 1, color: new Color(0.7f, 0.7f, 0.7f));
        arrowData.pierceCount = 1;

        // 2. Ateşli Ok (2x1, Arrow)
        ItemSO fireArrowData = CreateOrLoadSO(itemsFolder, "AtesliOk", "Ateşli Ok", "Yay ile ateşlenir. Çarptığı yeri yakar. (2x1)", 2, 1, ItemType.Arrow, maxStack: 1, color: new Color(1f, 0.4f, 0.1f));
        fireArrowData.pierceCount = 1;
        fireArrowData.leavesFireArea = true;

        // 3. Yay (3x1, WeaponTool)
        ItemSO bowData = CreateOrLoadSO(itemsFolder, "Yay", "Yay", "Sadak'tan ok çekerek nişan alınan yere ateşler. (3x1)", 3, 1, ItemType.WeaponTool, durability: 30, color: new Color(0.6f, 0.4f, 0.2f));

        // 4. Sadak / Quiver (3x1, Quiver)
        ItemSO quiverData = CreateOrLoadSO(itemsFolder, "Sadak", "Sadak (Quiver)", "Maksimum 10 ok depolar. Aynı tip oklar yüklenebilir. (3x1)", 3, 1, ItemType.Quiver, color: new Color(0.5f, 0.35f, 0.2f));
        quiverData.maxArrowCapacity = 10;

        // 5. Küçük Sopa (2x1, WeaponTool)
        ItemSO clubData = CreateOrLoadSO(itemsFolder, "KucukSopa", "Küçük Sopa", "Hızlı yakın dövüş silahı. Fırlatılabilir. (2x1)", 2, 1, ItemType.WeaponTool, durability: 15, color: new Color(0.65f, 0.5f, 0.3f));
        clubData.canMeleeAttack = true; clubData.meleeDamage = 10f; clubData.meleeRange = 1.1f; clubData.attackCooldown = 0.4f; clubData.swingArcAngle = 60f;
        clubData.isThrowable = true; clubData.throwStyle = ThrowStyle.Arc; clubData.throwDamage = 10f; clubData.throwDurabilityCost = 3;

        // 6. Asa (3x1, WeaponTool)
        ItemSO staffData = CreateOrLoadSO(itemsFolder, "Asa", "Asa", "Orta menzilli yakın dövüş silahı. Fırlatılamaz. (3x1)", 3, 1, ItemType.WeaponTool, durability: 25, color: new Color(0.4f, 0.6f, 0.8f));
        staffData.canMeleeAttack = true; staffData.meleeDamage = 12f; staffData.meleeRange = 1.6f; staffData.attackCooldown = 0.6f; staffData.swingArcAngle = 90f;

        // 7. Hançer (2x1, WeaponTool)
        ItemSO daggerData = CreateOrLoadSO(itemsFolder, "Hancer", "Hançer", "Çok hızlı bıçak. Düz hatta fırlatılabilir (3 pierce). (2x1)", 2, 1, ItemType.WeaponTool, durability: 20, color: new Color(0.3f, 0.7f, 0.9f));
        daggerData.canMeleeAttack = true; daggerData.meleeDamage = 5f; daggerData.meleeRange = 0.9f; daggerData.attackCooldown = 0.25f; daggerData.swingArcAngle = 45f;
        daggerData.isThrowable = true; daggerData.throwStyle = ThrowStyle.StraightLine; daggerData.throwDamage = 5f; daggerData.pierceCount = 3; daggerData.throwDurabilityCost = 3;

        // 8. Kılıç (2x1, WeaponTool)
        ItemSO swordData = CreateOrLoadSO(itemsFolder, "Kilic", "Kılıç", "Dengeli kılıç. Kavisli fırlatılabilir (2 pierce). (2x1)", 2, 1, ItemType.WeaponTool, durability: 35, color: new Color(0.8f, 0.8f, 0.85f));
        swordData.canMeleeAttack = true; swordData.meleeDamage = 15f; swordData.meleeRange = 1.4f; swordData.attackCooldown = 0.55f; swordData.swingArcAngle = 75f;
        swordData.isThrowable = true; swordData.throwStyle = ThrowStyle.Arc; swordData.throwDamage = 15f; swordData.pierceCount = 2; swordData.throwDurabilityCost = 3;

        // 9. Büyük Kılıç (4x1, WeaponTool)
        ItemSO greatswordData = CreateOrLoadSO(itemsFolder, "BuyukKilic", "Büyük Kılıç", "Yavaş ama çok güçlü iki elli dev kılıç. Fırlatılamaz. (4x1)", 4, 1, ItemType.WeaponTool, durability: 50, color: new Color(0.9f, 0.3f, 0.3f));
        greatswordData.canMeleeAttack = true; greatswordData.meleeDamage = 30f; greatswordData.meleeRange = 2.1f; greatswordData.attackCooldown = 0.9f; greatswordData.swingArcAngle = 110f;

        // 10. Ahşap Kalkan (2x2, Shield)
        ItemSO woodShieldData = CreateOrLoadSO(itemsFolder, "AhsapKalkan", "Ahşap Kalkan", "Envanterde bulununca pasif +5 Armor (can) verir. (2x2)", 2, 2, ItemType.Shield, durability: 40, color: new Color(0.55f, 0.35f, 0.15f));
        woodShieldData.armorValue = 5; woodShieldData.weightAccelTime = 0.15f; woodShieldData.weightDecelTime = 0.1f;

        // 11. Demir Kalkan (2x2, Shield)
        ItemSO ironShieldData = CreateOrLoadSO(itemsFolder, "DemirKalkan", "Demir Kalkan", "Envanterde bulununca pasif +12 Armor (can) verir. Ağır ivmelenme. (2x2)", 2, 2, ItemType.Shield, durability: 70, color: new Color(0.5f, 0.55f, 0.65f));
        ironShieldData.armorValue = 12; ironShieldData.weightAccelTime = 0.45f; ironShieldData.weightDecelTime = 0.35f;

        // 12. Okunabilir Parşömen (1x1, ReadableNote)
        ItemSO noteScrollData = CreateOrLoadSO(itemsFolder, "OkunabilirParsomen", "Okunabilir Parşömen", "Kullanıldığında NotUI okuma panelini açar. (1x1)", 1, 1, ItemType.ReadableNote, maxStack: 5, color: new Color(0.9f, 0.85f, 0.6f));

        // 13. Fireball Parşömeni (1x1, SpellScroll)
        ItemSO fireballScrollData = CreateOrLoadSO(itemsFolder, "FireballParsomeni", "Fireball Parşömeni", "Alev topu büyüsü fırlatır (60 AoE Hasar). (1x1)", 1, 1, ItemType.SpellScroll, maxStack: 3, color: new Color(1f, 0.3f, 0.1f));
        fireballScrollData.spellDamage = 60f; fireballScrollData.spellAoeRadius = 2f;

        // 14. Lightning Parşömeni (1x1, SpellScroll)
        ItemSO lightningScrollData = CreateOrLoadSO(itemsFolder, "LightningParsomeni", "Lightning Parşömeni", "Düz hatta geçen yıldırım büyüsü fırlatır (40 Hasar). (1x1)", 1, 1, ItemType.SpellScroll, maxStack: 3, color: new Color(0.3f, 0.7f, 1f));
        lightningScrollData.spellDamage = 40f;

        // 15. Can İksiri (1x1, Consumable)
        ItemSO hpPotionData = CreateOrLoadSO(itemsFolder, "CanIksiri", "Can İksiri", "Önce içilir (+50 HP), ardından boş şişe yavaşlatıcı olarak fırlatılır. (1x1)", 1, 1, ItemType.Consumable, maxStack: 5, color: new Color(0.9f, 0.2f, 0.2f));
        hpPotionData.appliesSlowOnHit = true;

        // 16. Patlayıcı İksir (1x1, ThrowableFlask)
        ItemSO murkyVialData = CreateOrLoadSO(itemsFolder, "PatlayiciIksir", "Patlayıcı İksir (Murky Vial)", "Fırlatılır, patlayıp alev alanı bırakır. (1x1)", 1, 1, ItemType.ThrowableFlask, maxStack: 5, color: new Color(0.2f, 0.8f, 0.3f));
        murkyVialData.leavesFireArea = true; murkyVialData.throwDamage = 40f;

        // 17. Deri Kask (1x1, Armor)
        ItemSO leatherHelm = CreateOrLoadSO(itemsFolder, "DeriKask", "Deri Kask", "+3 Armor (can). Hafif zırh. (1x1)", 1, 1, ItemType.Armor, durability: 30, color: new Color(0.6f, 0.4f, 0.25f));
        leatherHelm.armorValue = 3; leatherHelm.armorSlotType = ArmorSlotType.Helmet; leatherHelm.weightAccelTime = 0.15f;

        // 18. Deri Göğüslük (2x2, Armor)
        ItemSO leatherChest = CreateOrLoadSO(itemsFolder, "DeriGogusluk", "Deri Göğüslük", "+6 Armor (can). Hafif zırh. (2x2)", 2, 2, ItemType.Armor, durability: 40, color: new Color(0.65f, 0.45f, 0.25f));
        leatherChest.armorValue = 6; leatherChest.armorSlotType = ArmorSlotType.Chestplate; leatherChest.weightAccelTime = 0.15f;

        // 19. Deri Bacaklık (2x1, Armor)
        ItemSO leatherLegs = CreateOrLoadSO(itemsFolder, "DeriBacaklik", "Deri Bacaklık", "+4 Armor (can). Hafif zırh. (2x1)", 2, 1, ItemType.Armor, durability: 35, color: new Color(0.55f, 0.35f, 0.2f));
        leatherLegs.armorValue = 4; leatherLegs.armorSlotType = ArmorSlotType.Leggings; leatherLegs.weightAccelTime = 0.15f;

        // 20. Demir Kask (1x1, Armor)
        ItemSO ironHelm = CreateOrLoadSO(itemsFolder, "DemirKask", "Demir Kask", "+7 Armor (can). Ağır zırh. (1x1)", 1, 1, ItemType.Armor, durability: 60, color: new Color(0.5f, 0.55f, 0.6f));
        ironHelm.armorValue = 7; ironHelm.armorSlotType = ArmorSlotType.Helmet; ironHelm.weightAccelTime = 0.45f; ironHelm.weightDecelTime = 0.35f;

        // 21. Demir Göğüslük (2x3, Armor)
        ItemSO ironChest = CreateOrLoadSO(itemsFolder, "DemirGogusluk", "Demir Göğüslük", "+15 Armor (can). Ağır zırh. (2x3)", 2, 3, ItemType.Armor, durability: 80, color: new Color(0.45f, 0.5f, 0.55f));
        ironChest.armorValue = 15; ironChest.armorSlotType = ArmorSlotType.Chestplate; ironChest.weightAccelTime = 0.45f; ironChest.weightDecelTime = 0.35f;

        // 22. Demir Bacaklık (2x2, Armor)
        ItemSO ironLegs = CreateOrLoadSO(itemsFolder, "DemirBacaklik", "Demir Bacaklık", "+9 Armor (can). Ağır zırh. (2x2)", 2, 2, ItemType.Armor, durability: 70, color: new Color(0.4f, 0.45f, 0.5f));
        ironLegs.armorValue = 9; ironLegs.armorSlotType = ArmorSlotType.Leggings; ironLegs.weightAccelTime = 0.45f; ironLegs.weightDecelTime = 0.35f;

        // 23. Anahtarlar (1x1, KeyItem)
        ItemSO key1 = CreateOrLoadSO(itemsFolder, "Anahtar1", "Anahtar 1", "Bölüm 1 kilitli kapılarını açar.", 1, 1, ItemType.KeyItem, color: new Color(0.9f, 0.8f, 0.2f));
        ItemSO key2 = CreateOrLoadSO(itemsFolder, "Anahtar2", "Anahtar 2", "Bölüm 2 kilitli kapılarını açar.", 1, 1, ItemType.KeyItem, color: new Color(0.75f, 0.85f, 0.95f));
        ItemSO key3 = CreateOrLoadSO(itemsFolder, "Anahtar3", "Anahtar 3", "Bölüm 3 kilitli kapılarını açar.", 1, 1, ItemType.KeyItem, color: new Color(0.85f, 0.4f, 0.85f));

        // 24. Mum (1x1, QuestItem)
        ItemSO candleData = CreateOrLoadSO(itemsFolder, "Mum", "Mum", "Fırlatılabilir ışık kaynağı — karanlık alanları aydınlatır.", 1, 1, ItemType.QuestItem, maxStack: 10, color: new Color(1f, 0.95f, 0.5f));
        candleData.isThrowable = true; candleData.throwStyle = ThrowStyle.Arc; candleData.createsLightOnLand = true; candleData.throwDamage = 2f;

        // 25. Taş (1x1, QuestItem)
        ItemSO stoneData = CreateOrLoadSO(itemsFolder, "Tas", "Taş", "Fırlatılabilir taş — 5 hasar verir ve dikkat dağıtır.", 1, 1, ItemType.QuestItem, maxStack: 10, color: new Color(0.5f, 0.5f, 0.5f));
        stoneData.isThrowable = true; stoneData.throwStyle = ThrowStyle.Arc; stoneData.throwDamage = 5f;

        // 26. Görev Eşyaları (1x1, QuestItem)
        ItemSO questData = CreateOrLoadSO(itemsFolder, "GorevEsyasi", "Görev Nesnesi", "Özel görev hedef eşyası.", 1, 1, ItemType.QuestItem, maxStack: 5, color: new Color(0.3f, 0.9f, 0.6f));

        // WorldItem Prefab'ları üret
        CreatePrefabForSO(prefabsFolder, "WorldItem_Kilic", swordData);
        CreatePrefabForSO(prefabsFolder, "WorldItem_CanIksiri", hpPotionData);
        CreatePrefabForSO(prefabsFolder, "WorldItem_GumusAnahtar", key1);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log($"✅ [PREFAB CREATOR] Tüm 26 oyun eşyasının ScriptableObject (.asset) dosyaları '{itemsFolder}' klasöründe oluşturuldu!");
    }

    private static ItemSO CreateOrLoadSO(string folder, string fileName, string itemName, string desc, int w, int h, ItemType type, int maxStack = 1, int durability = 0, Color color = default)
    {
        string path = $"{folder}/{fileName}.asset";
        ItemSO item = AssetDatabase.LoadAssetAtPath<ItemSO>(path);

        if (item == null)
        {
            item = ScriptableObject.CreateInstance<ItemSO>();
            AssetDatabase.CreateAsset(item, path);
        }

        item.itemName = itemName;
        item.description = desc;
        item.gridWidth = w;
        item.gridHeight = h;
        item.itemType = type;
        item.maxStack = maxStack;
        item.maxDurability = durability;

        if (color == default) color = Color.cyan;
        item.icon = CreateColoredSprite(color, itemName);

        EditorUtility.SetDirty(item);
        return item;
    }

    private static void CreatePrefabForSO(string folder, string prefabName, ItemSO data)
    {
        string path = $"{folder}/{prefabName}.prefab";
        GameObject go = new GameObject(prefabName);

        SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = data.icon;

        BoxCollider2D col = go.AddComponent<BoxCollider2D>();
        col.isTrigger = true;

        WorldItem wItem = go.AddComponent<WorldItem>();
        wItem.Setup(data, 1);

        PrefabUtility.SaveAsPrefabAsset(go, path);
        Object.DestroyImmediate(go);
    }

    private static Sprite CreateColoredSprite(Color color, string label)
    {
        int size = 64;
        Texture2D tex = new Texture2D(size, size);
        Color[] pixels = new Color[size * size];

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
}
#endif
