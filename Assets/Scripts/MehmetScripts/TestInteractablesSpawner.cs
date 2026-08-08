using UnityEngine;

/// <summary>
/// Etkileşimli sistemleri (Notlar, Yerdeki Eşyalar ve Kapılar) sahnede anında test etmek için
/// otomatik nesneler ve prefablara dönüştürülebilir GameObjects oluşturan spawner bileşeni.
/// </summary>
public class TestInteractablesSpawner : MonoBehaviour
{
    [Header("Otomatik Oluşturma Ayarları")]
    [SerializeField] private bool spawnOnStart = true;
    [SerializeField] private Vector3 startOffset = new Vector3(-3f, 0f, 0f);

    private void Start()
    {
        if (spawnOnStart)
        {
            Invoke(nameof(SpawnAllInteractables), 0.3f);
        }
    }

    [ContextMenu("Test Etkileşimlilerini Sahneye Ekle")]
    public void SpawnAllInteractables()
    {
        Debug.Log("🧪 [ETKİLEŞİM TEST] Test etkileşimlileri (Not, Anahtar, İksir, Kilitli Kapı) oluşturuluyor...");

        // 1. Gümüş Anahtar ItemSO ve WorldItem
        ItemSO keyData = ScriptableObject.CreateInstance<ItemSO>();
        keyData.itemName = "Gümüş Anahtar";
        keyData.description = "Zindan kapısını açan antik bir anahtar.";
        keyData.gridWidth = 1;
        keyData.gridHeight = 1;
        keyData.itemType = ItemType.KeyItem;
        keyData.icon = CreateColoredSprite(new Color(0.8f, 0.85f, 0.9f), "Key");

        GameObject keyObj = CreateBaseWorldObject("WorldItem_SilverKey", startOffset + new Vector3(0f, 0f, 0f), new Color(0.9f, 0.85f, 0.2f));
        WorldItem wKey = keyObj.AddComponent<WorldItem>();
        wKey.Setup(keyData, 1);

        // 2. Can İksiri WorldItem (x3)
        ItemSO potionData = ScriptableObject.CreateInstance<ItemSO>();
        potionData.itemName = "Can İksiri";
        potionData.description = "Sağlığı 50 yeniler.";
        potionData.gridWidth = 1;
        potionData.gridHeight = 1;
        potionData.itemType = ItemType.Consumable;
        potionData.maxStack = 5;
        potionData.icon = CreateColoredSprite(new Color(0.9f, 0.2f, 0.2f), "Potion");

        GameObject potionObj = CreateBaseWorldObject("WorldItem_Potion", startOffset + new Vector3(2f, 0f, 0f), new Color(0.9f, 0.2f, 0.2f));
        WorldItem wPotion = potionObj.AddComponent<WorldItem>();
        wPotion.Setup(potionData, 3);

        // 3. Okunabilir Not / Mektup (NoteInteractable)
        GameObject noteObj = CreateBaseWorldObject("Note_GizemliMektup", startOffset + new Vector3(4f, 0f, 0f), new Color(0.85f, 0.75f, 0.5f));
        NoteInteractable noteComp = noteObj.AddComponent<NoteInteractable>();

        // System.Reflection ile private alanlara test değerleri aktar
        var titleField = typeof(NoteInteractable).GetField("noteTitle", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var contentField = typeof(NoteInteractable).GetField("noteContent", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

        if (titleField != null) titleField.SetValue(noteComp, "Gizemli Mektup");
        if (contentField != null) contentField.SetValue(noteComp, "Sevgili Maceracı,\n\nZindanın doğu kapısı kilitlidir. Kapıyı açmak için yerdeki Gümüş Anahtar'ı envanterine almalısın.\n\nAnahtarı harcamana gerek kalmadan kapıyı 'E' tuşu ile açabilirsin.\n\nİyi şanslar!");

        // 4. Hedef Oda Teleport Noktası
        GameObject spawnPointObj = new GameObject("TargetRoom_SpawnPoint");
        spawnPointObj.transform.position = startOffset + new Vector3(12f, 0f, 0f);

        // 5. Kilitli Kapı (DoorInteractable)
        GameObject doorObj = CreateBaseWorldObject("Door_ZindanKapisi", startOffset + new Vector3(7f, 0f, 0f), new Color(0.5f, 0.3f, 0.1f), new Vector2(0.8f, 1.8f));
        DoorInteractable doorComp = doorObj.AddComponent<DoorInteractable>();

        var dNameField = typeof(DoorInteractable).GetField("doorName", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var dLockedField = typeof(DoorInteractable).GetField("isLocked", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var dKeyField = typeof(DoorInteractable).GetField("requiredKey", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var dTargetField = typeof(DoorInteractable).GetField("targetSpawnTransform", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

        if (dNameField != null) dNameField.SetValue(doorComp, "Zindan Kapısı");
        if (dLockedField != null) dLockedField.SetValue(doorComp, true);
        if (dKeyField != null) dKeyField.SetValue(doorComp, keyData);
        if (dTargetField != null) dTargetField.SetValue(doorComp, spawnPointObj.transform);

        Debug.Log("✅ [ETKİLEŞİM TEST] Test nesneleri sahneye eklendi:\n1) Gümüş Anahtar\n2) Can İksiri (x3)\n3) Gizemli Mektup\n4) Kilitli Zindan Kapısı (Hedef Oda: +12 X)");
    }

    private GameObject CreateBaseWorldObject(string name, Vector3 position, Color color, Vector2 size = default)
    {
        if (size == default) size = new Vector2(0.8f, 0.8f);

        GameObject obj = new GameObject(name);
        obj.transform.position = position;

        SpriteRenderer sr = obj.AddComponent<SpriteRenderer>();
        sr.sprite = CreateSquareSprite(color);

        BoxCollider2D col = obj.AddComponent<BoxCollider2D>();
        col.size = size;
        col.isTrigger = true;

        return obj;
    }

    private Sprite CreateSquareSprite(Color color)
    {
        int texSize = 32;
        Texture2D tex = new Texture2D(texSize, texSize);
        Color[] pixels = new Color[texSize * texSize];

        for (int y = 0; y < texSize; y++)
        {
            for (int x = 0; x < texSize; x++)
            {
                bool isBorder = (x < 2 || x >= texSize - 2 || y < 2 || y >= texSize - 2);
                pixels[y * texSize + x] = isBorder ? Color.black : color;
            }
        }

        tex.SetPixels(pixels);
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, texSize, texSize), new Vector2(0.5f, 0.5f));
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
                pixels[y * size + x] = isBorder ? Color.black : color;
            }
        }

        tex.SetPixels(pixels);
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f));
    }
}
