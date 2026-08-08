using UnityEngine;
using UnityEngine.UI;
using TMPro;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

/// <summary>
/// Oyuncunun etrafındaki IInteractable nesneleri algılayan ve etkileşimi yöneten bileşen.
/// En yakın etkileşimli nesnenin üzerinde şık bir "[E] Can İksiri Al" UI yazısı gösterir.
/// 'E' tuşuna basıldığında nesneyle etkileşime geçer.
/// </summary>
public class PlayerInteractor : MonoBehaviour
{
    [Header("Etkileşim Ayarları")]
    [Tooltip("Etkileşim algılama menzili (Yarıçap)")]
    [SerializeField] private float interactRadius = 2.0f;

    [Tooltip("Etkileşim tuşu")]
    [SerializeField] private KeyCode interactKey = KeyCode.E;

    [Tooltip("Etkileşim yapılabilir katmanlar (Boş bırakılırsa tümü kontrol edilir)")]
    [SerializeField] private LayerMask interactableMask = ~0;

    [Header("UI Görünüm")]
    [Tooltip("Nesne üstündeki UI offset'i (ör. Y: 0.8)")]
    [SerializeField] private Vector3 promptOffset = new Vector3(0f, 0.8f, 0f);

    private IInteractable currentInteractable;
    private MonoBehaviour currentInteractableMono;

    // Procedural Floating Prompt UI
    private GameObject promptCanvasObj;
    private TextMeshProUGUI promptText;
    private Image promptBgImage;

    private void Start()
    {
        CreatePromptUI();
    }

    private void Update()
    {
        FindNearestInteractable();
        UpdatePromptUI();
        HandleInteractionInput();
    }

    /// <summary>Karaktere en yakın IInteractable nesneyi bulur.</summary>
    private void FindNearestInteractable()
    {
        Collider2D[] colliders = Physics2D.OverlapCircleAll(transform.position, interactRadius, interactableMask);

        float minDistance = float.MaxValue;
        IInteractable nearest = null;
        MonoBehaviour nearestMono = null;

        foreach (var col in colliders)
        {
            if (col.gameObject == gameObject) continue;

            IInteractable interactable = col.GetComponent<IInteractable>();
            if (interactable == null)
                interactable = col.GetComponentInParent<IInteractable>();

            if (interactable != null && interactable.CanInteract(transform))
            {
                float dist = Vector2.Distance(transform.position, col.transform.position);
                if (dist < minDistance)
                {
                    minDistance = dist;
                    nearest = interactable;
                    nearestMono = interactable as MonoBehaviour;
                }
            }
        }

        currentInteractable = nearest;
        currentInteractableMono = nearestMono;
    }

    /// <summary>Giriş tuşu kontrolü.</summary>
    private void HandleInteractionInput()
    {
        if (currentInteractable == null) return;

        // Envanter açıkken etkileşim yapılmasın
        if (InventoryManager.Instance != null && InventoryManager.Instance.IsInventoryOpen)
            return;

        bool keyPressed = false;

#if ENABLE_INPUT_SYSTEM
        if (Keyboard.current != null && Keyboard.current.eKey.wasPressedThisFrame)
            keyPressed = true;
#else
        if (Input.GetKeyDown(interactKey))
            keyPressed = true;
#endif

        if (keyPressed)
        {
            currentInteractable.Interact(transform);
        }
    }

    /// <summary>Etkileşim UI göstergesini günceller.</summary>
    private void UpdatePromptUI()
    {
        if (currentInteractable != null && currentInteractableMono != null)
        {
            if (promptCanvasObj != null)
            {
                promptCanvasObj.SetActive(true);

                // Nesnenin tepesine yerleştir
                Vector3 worldPos = currentInteractableMono.transform.position + promptOffset;
                promptCanvasObj.transform.position = worldPos;

                if (promptText != null)
                {
                    promptText.text = currentInteractable.GetInteractPrompt();
                }
            }
        }
        else
        {
            if (promptCanvasObj != null)
                promptCanvasObj.SetActive(false);
        }
    }

    /// <summary>World-Space şık prompt UI oluşturur.</summary>
    private void CreatePromptUI()
    {
        promptCanvasObj = new GameObject("InteractionPromptUI");
        
        Canvas canvas = promptCanvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        canvas.sortingOrder = 50;

        RectTransform canvasRT = promptCanvasObj.GetComponent<RectTransform>();
        canvasRT.sizeDelta = new Vector2(200f, 40f);
        canvasRT.localScale = new Vector3(0.01f, 0.01f, 0.01f); // World-space ölçekleme

        // Koyu şeffaf arka plan
        GameObject bgObj = new GameObject("Bg", typeof(RectTransform), typeof(Image));
        bgObj.transform.SetParent(promptCanvasObj.transform, false);
        
        RectTransform bgRT = bgObj.GetComponent<RectTransform>();
        bgRT.anchorMin = Vector2.zero;
        bgRT.anchorMax = Vector2.one;
        bgRT.offsetMin = Vector2.zero;
        bgRT.offsetMax = Vector2.zero;

        promptBgImage = bgObj.GetComponent<Image>();
        promptBgImage.color = new Color(0.1f, 0.12f, 0.16f, 0.85f);

        Outline outline = bgObj.AddComponent<Outline>();
        outline.effectColor = new Color(1f, 0.85f, 0.3f, 0.9f);
        outline.effectDistance = new Vector2(1, -1);

        // Metin
        GameObject textObj = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
        textObj.transform.SetParent(promptCanvasObj.transform, false);

        RectTransform textRT = textObj.GetComponent<RectTransform>();
        textRT.anchorMin = Vector2.zero;
        textRT.anchorMax = Vector2.one;
        textRT.offsetMin = new Vector2(6, 2);
        textRT.offsetMax = new Vector2(-6, -2);

        promptText = textObj.GetComponent<TextMeshProUGUI>();
        promptText.fontSize = 18f;
        promptText.fontStyle = FontStyles.Bold;
        promptText.alignment = TextAlignmentOptions.Center;
        promptText.color = Color.white;
        promptText.raycastTarget = false;

        promptCanvasObj.SetActive(false);
    }

    private void OnDrawGizmosSelected()
    {
        // Etkileşim menzilini Scene görünümünde çiz
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, interactRadius);
    }
}
