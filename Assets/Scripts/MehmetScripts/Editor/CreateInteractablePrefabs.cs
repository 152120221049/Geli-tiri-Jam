#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System.IO;

/// <summary>
/// Unity Editor menüsü üzerinden otomatik olarak Etkileşimli Prefab'lar (.prefab)
/// ve Test ScriptableObject (.asset) dosyaları oluşturan Editör aracı.
/// </summary>
public static class CreateInteractablePrefabs
{
    [MenuItem("Tools/Envanter & Etkileşim/Test Prefab'ları ve Eşyaları Oluştur")]
    public static void GeneratePrefabsAndTestScene()
    {
        string prefabsFolder = "Assets/Prefabs/Interactables";
        string itemsFolder = "Assets/GameData/Items";

        if (!Directory.Exists(prefabsFolder))
            Directory.CreateDirectory(prefabsFolder);

        if (!Directory.Exists(itemsFolder))
            Directory.CreateDirectory(itemsFolder);

        AssetDatabase.Refresh();

        // 1. Gümüş Anahtar ItemSO (.asset)
        ItemSO keyData = AssetDatabase.LoadAssetAtPath<ItemSO>($"{itemsFolder}/GumusAnahtar.asset");
        if (keyData == null)
        {
            keyData = ScriptableObject.CreateInstance<ItemSO>();
            keyData.itemName = "Gümüş Anahtar";
            keyData.description = "Zindan kapısını açan antik bir anahtar.";
            keyData.gridWidth = 1;
            keyData.gridHeight = 1;
            keyData.itemType = ItemType.KeyItem;
            keyData.icon = CreateColoredSprite(new Color(0.85f, 0.85f, 0.95f), "Key");
            AssetDatabase.CreateAsset(keyData, $"{itemsFolder}/GumusAnahtar.asset");
        }

        // 2. Can İksiri ItemSO (.asset)
        ItemSO potionData = AssetDatabase.LoadAssetAtPath<ItemSO>($"{itemsFolder}/CanIksiri.asset");
        if (potionData == null)
        {
            potionData = ScriptableObject.CreateInstance<ItemSO>();
            potionData.itemName = "Can İksiri";
            potionData.description = "Sağlığı 50 yeniler.";
            potionData.gridWidth = 1;
            potionData.gridHeight = 1;
            potionData.itemType = ItemType.Consumable;
            potionData.maxStack = 5;
            potionData.icon = CreateColoredSprite(new Color(0.9f, 0.2f, 0.2f), "Potion");
            AssetDatabase.CreateAsset(potionData, $"{itemsFolder}/CanIksiri.asset");
        }

        // 3. Prefab 1: WorldItem - Gümüş Anahtar
        GameObject keyGO = CreateBaseGameObject("WorldItem_GumusAnahtar", new Color(0.95f, 0.85f, 0.2f));
        WorldItem wKey = keyGO.AddComponent<WorldItem>();
        wKey.Setup(keyData, 1);
        SaveAsPrefab(keyGO, $"{prefabsFolder}/WorldItem_GumusAnahtar.prefab");

        // 4. Prefab 2: WorldItem - Can İksiri
        GameObject potionGO = CreateBaseGameObject("WorldItem_CanIksiri", new Color(0.9f, 0.2f, 0.2f));
        WorldItem wPotion = potionGO.AddComponent<WorldItem>();
        wPotion.Setup(potionData, 3);
        SaveAsPrefab(potionGO, $"{prefabsFolder}/WorldItem_CanIksiri.prefab");

        // 5. Prefab 3: Okunabilir Not (NoteInteractable)
        GameObject noteGO = CreateBaseGameObject("Note_GizemliMektup", new Color(0.85f, 0.75f, 0.5f));
        NoteInteractable noteComp = noteGO.AddComponent<NoteInteractable>();
        SetPrivateField(noteComp, "noteTitle", "Gizemli Mektup");
        SetPrivateField(noteComp, "noteContent", "Sevgili Maceracı,\n\nZindanın doğu kapısı kilitlidir. Kapıyı açmak için yerdeki Gümüş Anahtar'ı envanterine almalısın.\n\nAnahtarı harcamana gerek kalmadan kapıyı 'E' tuşu ile açabilirsin.");
        SaveAsPrefab(noteGO, $"{prefabsFolder}/Note_GizemliMektup.prefab");

        // 6. Prefab 4: Kilitli Kapı (DoorInteractable)
        GameObject doorGO = CreateBaseGameObject("Door_ZindanKapisi", new Color(0.5f, 0.3f, 0.1f), new Vector2(0.8f, 1.8f));
        DoorInteractable doorComp = doorGO.AddComponent<DoorInteractable>();
        SetPrivateField(doorComp, "doorName", "Zindan Kapısı");
        SetPrivateField(doorComp, "isLocked", true);
        SetPrivateField(doorComp, "requiredKey", keyData);
        SaveAsPrefab(doorGO, $"{prefabsFolder}/Door_ZindanKapisi.prefab");

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log("✅ [PREFAB CREATOR] Etkileşimli Prefab'lar ve ItemSO'lar başarıyla oluşturuldu:\n" +
                  $"📁 {prefabsFolder}/WorldItem_GumusAnahtar.prefab\n" +
                  $"📁 {prefabsFolder}/WorldItem_CanIksiri.prefab\n" +
                  $"📁 {prefabsFolder}/Note_GizemliMektup.prefab\n" +
                  $"📁 {prefabsFolder}/Door_ZindanKapisi.prefab");
    }

    private static GameObject CreateBaseGameObject(string name, Color color, Vector2 size = default)
    {
        if (size == default) size = new Vector2(0.8f, 0.8f);

        GameObject obj = new GameObject(name);
        SpriteRenderer sr = obj.AddComponent<SpriteRenderer>();
        sr.sprite = CreateSquareSprite(color);

        BoxCollider2D col = obj.AddComponent<BoxCollider2D>();
        col.size = size;
        col.isTrigger = true;

        return obj;
    }

    private static void SaveAsPrefab(GameObject go, string path)
    {
        PrefabUtility.SaveAsPrefabAsset(go, path);
        Object.DestroyImmediate(go);
    }

    private static void SetPrivateField(object target, string fieldName, object value)
    {
        var field = target.GetType().GetField(fieldName, System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        if (field != null) field.SetValue(target, value);
    }

    private static Sprite CreateSquareSprite(Color color)
    {
        int size = 32;
        Texture2D tex = new Texture2D(size, size);
        Color[] pixels = new Color[size * size];

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                bool isBorder = (x < 2 || x >= size - 2 || y < 2 || y >= size - 2);
                pixels[y * size + x] = isBorder ? Color.black : color;
            }
        }

        tex.SetPixels(pixels);
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f));
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
