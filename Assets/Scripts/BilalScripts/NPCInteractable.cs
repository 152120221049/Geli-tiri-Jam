using UnityEngine;
using TMPro; // TextMeshPro için
using UnityEngine.InputSystem; // YENİ INPUT SYSTEM

[RequireComponent(typeof(Collider2D))] // Çarpışma algılayabilmek için bir Collider zorunlu
public class NPCInteractable : MonoBehaviour
{
    [Header("Input (Yeni Sistem)")]
    [Tooltip("Etkileşimi başlatacak Input Action'ı buraya sürükleyin (Örn: Interact)")]
    public InputActionReference interactAction;

    [Header("Dialog Ayarları")]
    [Tooltip("data.json içindeki karakter tag'i (Örn: npc1)")]
    public string npcTag = "npc1";
    
    [Tooltip("Bu NPC ile en fazla kaç kez konuşulabilir? Sınırsız ise 0 veya eksi yapın.")]
    public int maxInteractions = 1;

    [Header("Local UI Referansları")]
    [Tooltip("Görseldeki Dialog objesi (Baloncuk arkaplanı vs.) Otomatik bulunamazsa sürükleyin.")]
    public GameObject dialogBubbleObj;
    
    [Tooltip("Görseldeki Text objesi (3D TextMeshPro). Otomatik bulunamazsa sürükleyin.")]
    public TextMeshPro dialogText;

    [Header("Etkileşim Görseli")]
    [Tooltip("Oyuncu etkileşim alanına girdiğinde tepede çıkacak 'Q' veya 'E' tuşunu temsil eden obje")]
    public GameObject interactPromptObj;

    private int currentInteractions = 0;
    private bool isPlayerInRange = false;
    private Transform currentPlayer = null;

    private void OnEnable()
    {
        if (interactAction != null)
        {
            interactAction.action.Enable();
        }
    }

    private void OnDisable()
    {
        if (interactAction != null)
        {
            interactAction.action.Disable();
        }
    }

    private void Start()
    {
        // Başlangıçta tuş yazısını gizle
        if (interactPromptObj != null)
        {
            interactPromptObj.SetActive(false);
        }

        // Eğer TextMeshPro Inspector'dan atanmadıysa, alt objelerde otomatik ara
        if (dialogText == null)
        {
            dialogText = GetComponentInChildren<TextMeshPro>(true);
        }

        // Eğer Bubble objesi atanmadıysa, Text'in bir üst objesini (parent) Bubble olarak varsay
        if (dialogBubbleObj == null && dialogText != null && dialogText.transform.parent != null)
        {
            if (dialogText.transform.parent != this.transform)
            {
                dialogBubbleObj = dialogText.transform.parent.gameObject;
            }
        }
    }

    private void Update()
    {
        bool interactPressed = false;

        // Gönderdiğin resimdeki Action (Interact) kullanılıyor
        if (interactAction != null)
        {
            interactPressed = interactAction.action.WasPressedThisFrame();
        }
        else
        {
            // Eğer InputAction atanmamışsa, InputSystem üzerinden E veya Q tuşu ile yedek (fallback) sağla
            if (Keyboard.current != null)
            {
                interactPressed = Keyboard.current.eKey.wasPressedThisFrame || Keyboard.current.qKey.wasPressedThisFrame;
            }
            if (Gamepad.current != null && !interactPressed)
            {
                interactPressed = Gamepad.current.buttonNorth.wasPressedThisFrame || Gamepad.current.buttonWest.wasPressedThisFrame;
            }
        }

        // Oyuncu alandaysa ve Interact tuşuna basarsa konuşmayı başlat
        if (isPlayerInRange && interactPressed)
        {
            TryInteract();
        }
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        // Alana "Player" tag'li bir obje girdiyse
        if (collision.CompareTag("Player"))
        {
            isPlayerInRange = true;
            currentPlayer = collision.transform;

            // Eğer hala konuşma hakkımız varsa ve başka bir diyalog aktif değilse etkileşim tuşunu göster
            bool anotherDialogActive = (currentPlayer.GetComponent<Dialogs>() != null && currentPlayer.GetComponent<Dialogs>().IsDialogActive()) || 
                                       (Dialogs.Instance != null && Dialogs.Instance.IsDialogActive());

            if (CanInteract() && interactPromptObj != null && !anotherDialogActive)
            {
                interactPromptObj.SetActive(true);
            }
        }
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        // "Player" alandan çıktıysa
        if (collision.CompareTag("Player"))
        {
            isPlayerInRange = false;
            currentPlayer = null;

            // Alandan çıkınca etkileşim tuşunu gizle
            if (interactPromptObj != null)
            {
                interactPromptObj.SetActive(false);
            }
        }
    }
    
    private bool CanInteract()
    {
        if (maxInteractions > 0 && currentInteractions >= maxInteractions)
            return false;
        
        return true;
    }

    private void TryInteract()
    {
        // Maksimum konuşma sınırına ulaşıldı mı?
        if (!CanInteract())
            return;

        // Oyuncunun üzerindeki Dialogs scriptini bul
        Dialogs playerDialogs = currentPlayer.GetComponent<Dialogs>();

        // Eğer sistemde halihazırda aktif bir diyalog varsa yenisini başlatma
        if (playerDialogs != null && playerDialogs.IsDialogActive()) return;
        if (Dialogs.Instance != null && Dialogs.Instance.IsDialogActive()) return;

        currentInteractions++;

        // Etkileşim başlayınca tuş yazısını hemen gizle
        if (interactPromptObj != null)
        {
            interactPromptObj.SetActive(false);
        }

        if (playerDialogs != null)
        {
            playerDialogs.StartDialog(npcTag, dialogText, dialogBubbleObj);
        }
        else
        {
            // Eğer oyuncunun üstünde yoksa, Singleton olarak bulmaya çalış
            if (Dialogs.Instance != null)
            {
                Dialogs.Instance.StartDialog(npcTag, dialogText, dialogBubbleObj);
            }
            else
            {
                Debug.LogError("Oyuncu üzerinde veya sahnede Dialogs scripti bulunamadı!");
            }
        }
    }
}
