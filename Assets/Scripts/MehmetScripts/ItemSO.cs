using UnityEngine;

/// <summary>
/// Eşya kategori tipleri.
/// </summary>
public enum ItemType
{
    Consumable,      // Tüketilebilir: kullanımda adet azalır (iksir, yiyecek)
    WeaponTool,      // Silah/Alet: kullanımda dayanıklılık azalır (kılıç, kazma)
    KeyItem,         // Anahtar Eşya: etkileşim için kullanılır, asla tükenmez
    Passive,         // Pasif/Malzeme: doğrudan kullanılamaz, sadece taşınır (taş, odun)
    Arrow,           // Ok: Sadak'a (Quiver) doldurulur, tek başına kullanılamaz
    Quiver,          // Sadak: Ok deposu, yay ile birlikte çalışır
    Shield,          // Kalkan: Envanterde bulundurulunca pasif zırh (bonus can) verir
    Armor,           // Zırh: Envanterde bulundurulunca pasif zırh (bonus can) + ağırlık eğrisi
    SpellScroll,     // Büyü Parşömeni: Kullanıldığında büyü fırlatır, adet azalır
    ReadableNote,    // Okunabilir Parşömen/Not: NoteUI açar
    ThrowableFlask,  // Fırlatılabilir Şişe: Hedefe fırlatılır, çarpınca efekt
    QuestItem        // Görev Eşyası: Özel amaçlı (Mum, Taş vb.)
}

/// <summary>
/// Zırh yuva tipleri — envanterde aynı yuvadan yalnızca 1 adet bulunabilir.
/// </summary>
public enum ArmorSlotType
{
    None,
    Helmet,       // Kask
    Chestplate,   // Göğüslük
    Leggings      // Bacaklık
}

/// <summary>
/// Fırlatma stili.
/// </summary>
public enum ThrowStyle
{
    None,          // Fırlatılamaz
    StraightLine,  // Düz çizgi (Hançer, Oklar)
    Arc            // Parabolik kavis (Sopa, Kılıç, Şişe, Taş, Mum)
}

/// <summary>
/// Eşya tanım verileri. Inspector'dan CreateAssetMenu ile oluşturulur.
/// Her eşya tipi için grid boyutu, istiflenme limiti, dayanıklılık,
/// savaş istatistikleri, zırh değerleri ve büyü bilgisi içerir.
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

    // ═══════════════════════════════════════════
    //  SAVAŞ & SİLAH İSTATİSTİKLERİ
    // ═══════════════════════════════════════════

    [Header("Yakın Dövüş (Melee)")]
    [Tooltip("Yakın dövüş hasar değeri")]
    public float meleeDamage = 0f;

    [Tooltip("Yakın dövüş menzili (birim)")]
    public float meleeRange = 1.0f;

    [Tooltip("Saldırı bekleme süresi (saniye)")]
    public float attackCooldown = 0.5f;

    [Tooltip("Savurma açısı (derece)")]
    public float swingArcAngle = 60f;

    [Tooltip("Yakın dövüş saldırısı yapabilir mi?")]
    public bool canMeleeAttack = false;

    [Header("Fırlatma (Throw)")]
    [Tooltip("Fırlatılabilir mi? (Sağ tık basılı tut → bırak)")]
    public bool isThrowable = false;

    [Tooltip("Fırlatma stili")]
    public ThrowStyle throwStyle = ThrowStyle.None;

    [Tooltip("Fırlatma hasarı")]
    public float throwDamage = 0f;

    [Tooltip("Fırlatınca kaç düşmandan geçer (pierce)")]
    public int pierceCount = 0;

    [Tooltip("Fırlatma başına dayanıklılık maliyeti")]
    public int throwDurabilityCost = 3;

    [Tooltip("Fırlatma gücü / hızı")]
    public float throwForce = 10f;

    // ═══════════════════════════════════════════
    //  ZIRH & KALKAN
    // ═══════════════════════════════════════════

    [Header("Zırh & Kalkan")]
    [Tooltip("Bu eşyanın envanterde bulunmasıyla kazanılan bonus can (zırh puanı)")]
    public int armorValue = 0;

    [Tooltip("Zırh yuva tipi (aynı yuvadan sadece 1 adet taşınabilir)")]
    public ArmorSlotType armorSlotType = ArmorSlotType.None;

    [Tooltip("Ağırlık — İvmelenme süresi (saniye). 0 = etki yok")]
    public float weightAccelTime = 0f;

    [Tooltip("Ağırlık — Yavaşlama süresi (saniye). 0 = etki yok")]
    public float weightDecelTime = 0f;

    // ═══════════════════════════════════════════
    //  SADAK (QUİVER)
    // ═══════════════════════════════════════════

    [Header("Sadak (Quiver)")]
    [Tooltip("Sadak'ın taşıyabileceği maksimum ok sayısı")]
    public int maxArrowCapacity = 10;

    // ═══════════════════════════════════════════
    //  BÜYÜ PARŞÖMEN
    // ═══════════════════════════════════════════

    [Header("Büyü (Spell Scroll)")]
    [Tooltip("Büyü hasarı")]
    public float spellDamage = 0f;

    [Tooltip("Büyü AoE yarıçapı (0 = tek hedef)")]
    public float spellAoeRadius = 0f;

    [Tooltip("Zincir atlama sayısı (Lightning için, 0 = zincir yok)")]
    public int chainCount = 0;

    [Tooltip("Büyü menzili (LineRenderer için)")]
    public float spellRange = 8f;

    // ═══════════════════════════════════════════
    //  MUM & TAŞ (QUEST ITEM)
    // ═══════════════════════════════════════════

    [Header("Görev Eşyası Özellikleri")]
    [Tooltip("Çarpınca yavaşlatma efekti uygular mı?")]
    public bool appliesSlowOnHit = false;

    [Tooltip("Çarptığı yerde ışık/ateş alanı bırakır mı?")]
    public bool leavesFireArea = false;

    [Tooltip("Çarptığı yerde ışık kaynağı oluşturur mu? (Mum)")]
    public bool createsLightOnLand = false;

    // ═══════════════════════════════════════════
    //  HELPER PROPERTIES
    // ═══════════════════════════════════════════

    /// <summary>Eşyanın sonlu dayanıklılığı var mı?</summary>
    public bool HasDurability => maxDurability > 0;

    /// <summary>Eşyanın sonsuz dayanıklılığı var mı? (KeyItem)</summary>
    public bool IsInfinite => maxDurability < 0;

    /// <summary>Döndürme durumuna göre efektif genişlik.</summary>
    public int EffectiveWidth(bool rotated) => rotated ? gridHeight : gridWidth;

    /// <summary>Döndürme durumuna göre efektif yükseklik.</summary>
    public int EffectiveHeight(bool rotated) => rotated ? gridWidth : gridHeight;
}
