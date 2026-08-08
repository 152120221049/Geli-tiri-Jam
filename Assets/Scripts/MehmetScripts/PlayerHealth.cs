using UnityEngine;
using UnityEngine.UI;
using System;

/// <summary>
/// Oyuncu sağlık sistemi.
/// Temel can (baseHealth) + Zırh bonus canı (armorHP) = toplam dayanıklılık.
/// Hasar önce zırh canından düşer, ardından temel candan.
/// Zırh bozulunca (dayanıklılık bitince) bonus can da kaybolur.
/// </summary>
public class PlayerHealth : MonoBehaviour
{
    public static PlayerHealth Instance { get; private set; }

    [Header("Sağlık Ayarları")]
    [SerializeField] private float maxBaseHealth = 100f;
    [SerializeField] private float currentBaseHealth;

    /// <summary>Zırh parçalarından gelen toplam bonus can.</summary>
    private float armorBonusHP = 0f;
    private float currentArmorHP = 0f;

    /// <summary>Toplam mevcut can (base + armor).</summary>
    public float CurrentHealth => currentBaseHealth + currentArmorHP;

    /// <summary>Toplam maksimum can (base + armor bonus).</summary>
    public float MaxHealth => maxBaseHealth + armorBonusHP;

    /// <summary>Temel can yüzdesi (0-1).</summary>
    public float BaseHealthPercent => maxBaseHealth > 0 ? currentBaseHealth / maxBaseHealth : 0f;

    /// <summary>Toplam can yüzdesi (0-1).</summary>
    public float TotalHealthPercent => MaxHealth > 0 ? CurrentHealth / MaxHealth : 0f;

    public bool IsDead => currentBaseHealth <= 0f;

    // Events
    public event Action<float, float> OnHealthChanged;  // (currentTotal, maxTotal)
    public event Action OnDeath;

    // ═══════════════════════════════════════════
    //  PROCEDURAL HEALTH BAR UI
    // ═══════════════════════════════════════════
    private GameObject healthBarCanvas;
    private Image healthBarFill;
    private Image armorBarFill;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        currentBaseHealth = maxBaseHealth;
    }

    private void Start()
    {
        CreateHealthBarUI();
        RefreshUI();

        // Envanter değişikliklerinde zırh bonusunu yeniden hesapla
        if (InventoryManager.Instance != null)
        {
            InventoryManager.Instance.OnInventoryChanged += RecalculateArmorBonus;
        }

        RecalculateArmorBonus();
    }

    private void OnDestroy()
    {
        if (InventoryManager.Instance != null)
        {
            InventoryManager.Instance.OnInventoryChanged -= RecalculateArmorBonus;
        }
    }

    // ═══════════════════════════════════════════
    //  HASAR & İYİLEŞME
    // ═══════════════════════════════════════════

    /// <summary>
    /// Oyuncuya hasar verir. Önce zırh canından, sonra temel candan düşer.
    /// </summary>
    public void TakeDamage(float damage)
    {
        if (IsDead || damage <= 0f) return;

        float remaining = damage;

        // 1) Önce zırh bonus canından düş
        if (currentArmorHP > 0f)
        {
            float armorAbsorb = Mathf.Min(currentArmorHP, remaining);
            currentArmorHP -= armorAbsorb;
            remaining -= armorAbsorb;
        }

        // 2) Kalan hasar temel candan düşer
        if (remaining > 0f)
        {
            currentBaseHealth -= remaining;
            currentBaseHealth = Mathf.Max(0f, currentBaseHealth);
        }

        OnHealthChanged?.Invoke(CurrentHealth, MaxHealth);
        RefreshUI();

        Debug.Log($"💔 [HASAR] -{damage} hasar alındı! Can: {currentBaseHealth:F0}/{maxBaseHealth:F0} + Zırh: {currentArmorHP:F0}/{armorBonusHP:F0}");

        if (IsDead)
        {
            Debug.Log("💀 [ÖLÜM] Oyuncu öldü!");
            OnDeath?.Invoke();
        }
    }

    /// <summary>Temel canı iyileştirir (zırh canını değil).</summary>
    public void Heal(float amount)
    {
        if (IsDead || amount <= 0f) return;

        currentBaseHealth = Mathf.Min(currentBaseHealth + amount, maxBaseHealth);
        OnHealthChanged?.Invoke(CurrentHealth, MaxHealth);
        RefreshUI();

        Debug.Log($"💚 [İYİLEŞME] +{amount} can yenilendi! Can: {currentBaseHealth:F0}/{maxBaseHealth:F0}");
    }

    // ═══════════════════════════════════════════
    //  ZIRH BONUS CAN HESAPLAMA
    // ═══════════════════════════════════════════

    /// <summary>
    /// Envanterdeki tüm Armor ve Shield eşyalarından toplam bonus canı hesaplar.
    /// Envanter her değiştiğinde otomatik çağrılır.
    /// </summary>
    public void RecalculateArmorBonus()
    {
        if (InventoryManager.Instance == null) return;

        float newBonusHP = 0f;

        // Hotbar'daki zırh/kalkan eşyaları
        foreach (var item in InventoryManager.Instance.HotbarGrid.GetAllItems())
        {
            if (item.itemData != null &&
                (item.itemData.itemType == ItemType.Shield || item.itemData.itemType == ItemType.Armor))
            {
                newBonusHP += item.itemData.armorValue;
            }
        }

        // Envanterdeki zırh/kalkan eşyaları
        foreach (var item in InventoryManager.Instance.InventoryGrid.GetAllItems())
        {
            if (item.itemData != null &&
                (item.itemData.itemType == ItemType.Shield || item.itemData.itemType == ItemType.Armor))
            {
                newBonusHP += item.itemData.armorValue;
            }
        }

        float oldBonusHP = armorBonusHP;
        armorBonusHP = newBonusHP;

        // Bonus can arttıysa farkı current'a da ekle, azaldıysa currentArmorHP'yi clamp'le
        if (newBonusHP > oldBonusHP)
        {
            currentArmorHP += (newBonusHP - oldBonusHP);
        }
        else
        {
            currentArmorHP = Mathf.Min(currentArmorHP, newBonusHP);
        }

        OnHealthChanged?.Invoke(CurrentHealth, MaxHealth);
        RefreshUI();
    }

    // ═══════════════════════════════════════════
    //  PROCEDURAL UI
    // ═══════════════════════════════════════════

    private void CreateHealthBarUI()
    {
        // Ana Canvas
        healthBarCanvas = new GameObject("HealthBarUI");
        Canvas canvas = healthBarCanvas.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 90;
        healthBarCanvas.AddComponent<CanvasScaler>();
        healthBarCanvas.AddComponent<GraphicRaycaster>();

        // Arka plan çerçeve
        GameObject bgObj = new GameObject("HealthBar_BG", typeof(RectTransform), typeof(Image));
        bgObj.transform.SetParent(healthBarCanvas.transform, false);
        RectTransform bgRT = bgObj.GetComponent<RectTransform>();
        bgRT.anchorMin = new Vector2(0f, 1f);
        bgRT.anchorMax = new Vector2(0f, 1f);
        bgRT.pivot = new Vector2(0f, 1f);
        bgRT.anchoredPosition = new Vector2(20f, -20f);
        bgRT.sizeDelta = new Vector2(220f, 26f);
        bgObj.GetComponent<Image>().color = new Color(0.1f, 0.1f, 0.1f, 0.85f);

        Outline bgOutline = bgObj.AddComponent<Outline>();
        bgOutline.effectColor = new Color(0.3f, 0.3f, 0.3f, 0.9f);
        bgOutline.effectDistance = new Vector2(2, -2);

        // Zırh bar (mavi/gri — temel barın üstünde)
        GameObject armorObj = new GameObject("ArmorBar_Fill", typeof(RectTransform), typeof(Image));
        armorObj.transform.SetParent(bgObj.transform, false);
        RectTransform armorRT = armorObj.GetComponent<RectTransform>();
        armorRT.anchorMin = Vector2.zero;
        armorRT.anchorMax = Vector2.one;
        armorRT.offsetMin = new Vector2(3, 3);
        armorRT.offsetMax = new Vector2(-3, -3);
        armorBarFill = armorObj.GetComponent<Image>();
        armorBarFill.color = new Color(0.4f, 0.6f, 0.85f, 0.7f);
        armorBarFill.type = Image.Type.Filled;
        armorBarFill.fillMethod = Image.FillMethod.Horizontal;

        // Can bar (kırmızı/yeşil — temel can)
        GameObject fillObj = new GameObject("HealthBar_Fill", typeof(RectTransform), typeof(Image));
        fillObj.transform.SetParent(bgObj.transform, false);
        RectTransform fillRT = fillObj.GetComponent<RectTransform>();
        fillRT.anchorMin = Vector2.zero;
        fillRT.anchorMax = Vector2.one;
        fillRT.offsetMin = new Vector2(3, 3);
        fillRT.offsetMax = new Vector2(-3, -3);
        healthBarFill = fillObj.GetComponent<Image>();
        healthBarFill.color = new Color(0.8f, 0.2f, 0.2f, 0.9f);
        healthBarFill.type = Image.Type.Filled;
        healthBarFill.fillMethod = Image.FillMethod.Horizontal;
    }

    private void RefreshUI()
    {
        if (healthBarFill != null)
        {
            healthBarFill.fillAmount = BaseHealthPercent;
        }

        if (armorBarFill != null)
        {
            // Zırh barı toplam can üzerinden oranlanır
            armorBarFill.fillAmount = MaxHealth > 0 ? CurrentHealth / MaxHealth : 0f;
        }
    }
}
