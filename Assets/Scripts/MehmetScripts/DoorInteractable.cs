using UnityEngine;
using UnityEngine.SceneManagement;

public enum DoorTransitionType
{
    SameSceneTeleport,  // Aynı sahnede başka bir odaya/konuma ışınla
    LoadScene,           // Yeni bir Unity sahnesi yükle
    ToggleDoorStateOnly  // Sadece kapıyı aç/kapat (geçiş yapma)
}

/// <summary>
/// Oyun dünyasındaki kapılar, geçitler ve oda geçişleri için etkileşimli bileşen.
/// IInteractable arayüzünü uygular. Anahtar kontrolü yapabilir ve aynı sahnede oda geçişi
/// veya sahne yüklemesi sağlayabilir.
/// </summary>
public class DoorInteractable : MonoBehaviour, IInteractable
{
    [Header("Kapı Bilgileri")]
    [SerializeField] private string doorName = "Ahşap Kapı";
    [SerializeField] private bool isLocked = false;
    [Tooltip("Kapıyı açmak için gereken anahtar (Envanterde bulunması yeterlidir, harcanmaz)")]
    [SerializeField] private ItemSO requiredKey;
    [SerializeField] private bool isOpen = false;

    [Header("Geçiş Ayarları")]
    [SerializeField] private DoorTransitionType transitionType = DoorTransitionType.SameSceneTeleport;

    [Tooltip("Aynı sahnede geçiş yapılacak hedef nokta (Transform)")]
    [SerializeField] private Transform targetSpawnTransform;

    [Tooltip("Eğer Transform atanmadıysa hedef Dünya Koordinatı")]
    [SerializeField] private Vector3 targetSpawnPosition;

    [Tooltip("Eğer LoadScene seçildiyse yüklenecek Sahne Adı")]
    [SerializeField] private string targetSceneName = "";

    [Header("Görsel & Bileşenler")]
    [SerializeField] private SpriteRenderer spriteRenderer;
    [SerializeField] private Sprite doorSpriteClosed;
    [SerializeField] private Sprite doorSpriteOpen;
    [SerializeField] private Collider2D doorCollider;

    private void Awake()
    {
        if (spriteRenderer == null)
            spriteRenderer = GetComponentInChildren<SpriteRenderer>();

        if (doorCollider == null)
            doorCollider = GetComponent<Collider2D>();

        UpdateVisuals();
    }

    private void UpdateVisuals()
    {
        if (spriteRenderer != null)
        {
            if (isOpen && doorSpriteOpen != null)
                spriteRenderer.sprite = doorSpriteOpen;
            else if (!isOpen && doorSpriteClosed != null)
                spriteRenderer.sprite = doorSpriteClosed;
        }

        // Kapı açıldığında engelleyen Collider kapatılır (fiziksel olarak kapıdan yürüyüp geçebilmek için)
        if (doorCollider != null)
        {
            doorCollider.enabled = !isOpen;
        }
    }

    // ═══════════════════════════════════════════
    //  IINTERACTABLE IMPLEMENTATION
    // ═══════════════════════════════════════════

    public string GetInteractPrompt()
    {
        if (isOpen)
        {
            return $"[E] {doorName} (Geçiş Yap)";
        }

        if (isLocked && requiredKey != null)
        {
            bool hasKey = (InventoryManager.Instance != null && InventoryManager.Instance.HasItem(requiredKey));
            return hasKey
                ? $"[E] {doorName} Aç (Gereken: {requiredKey.itemName})"
                : $"[E] {doorName} Kilitli (Gereken: {requiredKey.itemName})";
        }

        return $"[E] {doorName} Aç";
    }

    public bool CanInteract(Transform interactor)
    {
        return true;
    }

    public void Interact(Transform interactor)
    {
        if (interactor == null) return;

        // 1) Kilitli mi & Anahtar Var mı kontrol et
        if (isLocked && requiredKey != null)
        {
            bool hasKey = (InventoryManager.Instance != null && InventoryManager.Instance.HasItem(requiredKey));

            if (!hasKey)
            {
                Debug.LogWarning($"🔒 [KAPI] {doorName} kilitli! Envanterinizde '{requiredKey.itemName}' olmalı.");
                return;
            }

            // Anahtar var → Kilidi aç (Anahtar HARCANMAZ, envanterde bulunması yeterli)
            isLocked = false;
            Debug.Log($"🔑 [KAPI] {requiredKey.itemName} kullanılarak {doorName} kilidi açıldı!");
        }

        // 2) Kapıyı Aç ve Görselleri Güncelle
        isOpen = true;
        UpdateVisuals();

        // 3) Geçiş Türüne Göre İşlem Yap
        switch (transitionType)
        {
            case DoorTransitionType.SameSceneTeleport:
                Vector3 destPos = (targetSpawnTransform != null) ? targetSpawnTransform.position : targetSpawnPosition;
                interactor.position = destPos;
                Debug.Log($"🚪 [KAPI LEPORT] Oyuncu aynı sahnede {destPos} konumuna ışınlandı.");
                break;

            case DoorTransitionType.LoadScene:
                if (!string.IsNullOrEmpty(targetSceneName))
                {
                    Debug.Log($"🚪 [SAHNE YÜKLE] '{targetSceneName}' sahnesine geçiliyor...");
                    SceneManager.LoadScene(targetSceneName);
                }
                else
                {
                    Debug.LogWarning("🚪 [KAPI ERROR] Sahne yükleme seçili ancak targetSceneName boş!");
                }
                break;

            case DoorTransitionType.ToggleDoorStateOnly:
                if (doorCollider != null)
                    doorCollider.enabled = !isOpen;
                Debug.Log($"🚪 [KAPI] {doorName} açıldı.");
                break;
        }
    }
}
