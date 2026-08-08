using UnityEngine;

/// <summary>
/// Eşya kategori tipleri.
/// </summary>
public enum ItemType
{
    Consumable,   // Tüketilebilir: kullanımda adet azalır (iksir, yiyecek)
    WeaponTool,   // Silah/Alet: kullanımda dayanıklılık azalır (kılıç, kazma)
    KeyItem,      // Anahtar Eşya: etkileşim için kullanılır, asla tükenmez
    Passive       // Pasif/Malzeme: doğrudan kullanılamaz, sadece taşınır (taş, odun)
}

/// <summary>
/// Eşya tanım verileri. Inspector'dan CreateAssetMenu ile oluşturulur.
/// Her eşya tipi için grid boyutu, istiflenme limiti ve dayanıklılık bilgisi içerir.
/// </summary>
[CreateAssetMenu(fileName = "New Item", menuName = "Inventory/Item")]
public class ItemSO : ScriptableObject
{
    [Header("Temel Bilgiler")]
    public string itemName = "Yeni Eşya";

    [TextArea(2, 4)]
    public string description = "";

    public Sprite icon;

    [Header("Grid Boyutu")]
    [Min(1)] public int gridWidth = 1;
    [Min(1)] public int gridHeight = 1;

    [Header("Eşya Tipi & Özellikler")]
    public ItemType itemType = ItemType.Passive;

    [Min(1)] public int maxStack = 1;

    [Header("Dayanıklılık")]
    [Tooltip("-1 = Sonsuz dayanıklılık (KeyItem için). 0 = Dayanıklılık yok.")]
    public int maxDurability = 0;

    /// <summary>Eşyanın sonlu dayanıklılığı var mı?</summary>
    public bool HasDurability => maxDurability > 0;

    /// <summary>Eşyanın sonsuz dayanıklılığı var mı? (KeyItem)</summary>
    public bool IsInfinite => maxDurability < 0;

    /// <summary>Döndürme durumuna göre efektif genişlik.</summary>
    public int EffectiveWidth(bool rotated) => rotated ? gridHeight : gridWidth;

    /// <summary>Döndürme durumuna göre efektif yükseklik.</summary>
    public int EffectiveHeight(bool rotated) => rotated ? gridWidth : gridHeight;
}
