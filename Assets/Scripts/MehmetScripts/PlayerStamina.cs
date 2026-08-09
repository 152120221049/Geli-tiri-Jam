using UnityEngine;
using UnityEngine.UI;
using System;

public class PlayerStamina : MonoBehaviour
{
    public static PlayerStamina Instance { get; private set; }

    [Header("Stamina Ayarları")]
    [Tooltip("Maksimum stamina (enerji) miktarı")]
    [SerializeField] private float maxStamina = 100f;
    
    [Tooltip("Saniyede ne kadar stamina yenilenir")]
    [SerializeField] private float staminaRegenRate = 15f;

    [Tooltip("Stamina harcandıktan sonra yenilenmenin başlaması için gereken gecikme süresi (saniye)")]
    [SerializeField] private float regenDelay = 1.0f;

    [Tooltip("20 saniye koşabilmek için saniyede harcanan stamina (100 / 20 = 5)")]
    public float runCostPerSecond = 5f;

    [Header("UI Ayarları")]
    [Tooltip("Inspector'dan atanacak Stamina Bar (Image)")]
    [SerializeField] private Image staminaBarFill;

    public float CurrentStamina { get; private set; }
    public float MaxStamina => maxStamina;

    public event Action<float, float> OnStaminaChanged;

    private float regenTimer = 0f;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        CurrentStamina = maxStamina;
    }

    private void Start()
    {
        RefreshUI();
    }

    private void Update()
    {
        if (regenTimer > 0f)
        {
            regenTimer -= Time.deltaTime;
        }
        else if (CurrentStamina < maxStamina)
        {
            CurrentStamina += staminaRegenRate * Time.deltaTime;
            CurrentStamina = Mathf.Clamp(CurrentStamina, 0f, maxStamina);
            RefreshUI();
            OnStaminaChanged?.Invoke(CurrentStamina, maxStamina);
        }
    }

    /// <summary>
    /// Belirtilen miktarda stamina harcamayı dener (Kılıç sallamak vb. tek seferlik aksiyonlar için).
    /// Yeterli stamina yoksa false döner ve harcama yapmaz.
    /// </summary>
    public bool ConsumeStamina(float amount)
    {
        if (amount <= 0f) return true;

        if (CurrentStamina >= amount)
        {
            CurrentStamina -= amount;
            regenTimer = regenDelay;
            RefreshUI();
            OnStaminaChanged?.Invoke(CurrentStamina, maxStamina);
            return true;
        }
        
        Debug.LogWarning("🔋 Yeterli Stamina Yok!");
        return false;
    }

    /// <summary>
    /// Sürekli stamina harcar (Koşma gibi sürekli aksiyonlar için).
    /// </summary>
    public bool DrainStamina(float amountPerSecond)
    {
        if (amountPerSecond <= 0f) return true;

        float amount = amountPerSecond * Time.deltaTime;
        if (CurrentStamina > 0f)
        {
            CurrentStamina -= amount;
            CurrentStamina = Mathf.Max(0f, CurrentStamina);
            regenTimer = regenDelay;
            RefreshUI();
            OnStaminaChanged?.Invoke(CurrentStamina, maxStamina);
            return CurrentStamina > 0f;
        }
        return false;
    }

    /// <summary>
    /// Sadece staminanın yetip yetmediğini kontrol eder.
    /// </summary>
    public bool HasEnoughStamina(float amount)
    {
        return CurrentStamina >= amount;
    }

    public void RefreshUI()
    {
        if (staminaBarFill != null)
        {
            staminaBarFill.fillAmount = maxStamina > 0 ? CurrentStamina / maxStamina : 0f;
        }
    }
}
