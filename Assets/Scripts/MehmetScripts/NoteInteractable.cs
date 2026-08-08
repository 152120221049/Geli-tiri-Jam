using UnityEngine;

/// <summary>
/// Oyun dünyasında okunabilir notlar, mektuplar veya tabelalar için etkileşimli bileşen.
/// IInteractable arayüzünü uygular. Oyuncu yaklaşıp 'E' bastığında NoteUI aracılığıyla notu açar.
/// </summary>
public class NoteInteractable : MonoBehaviour, IInteractable
{
    [Header("Not Bilgisi")]
    [SerializeField] private string noteTitle = "Eski Mektup";

    [TextArea(4, 10)]
    [SerializeField] private string noteContent = "Buraya okunacak metni yazınız...";

    [Header("Görsel (Opsiyonel)")]
    [SerializeField] private SpriteRenderer spriteRenderer;

    private void Awake()
    {
        if (spriteRenderer == null)
            spriteRenderer = GetComponentInChildren<SpriteRenderer>();

        Collider2D col = GetComponent<Collider2D>();
        if (col != null)
            col.isTrigger = true;
    }

    public string GetInteractPrompt()
    {
        return $"[E] {noteTitle} Oku";
    }

    public bool CanInteract(Transform interactor)
    {
        return !string.IsNullOrEmpty(noteContent);
    }

    public void Interact(Transform interactor)
    {
        if (NoteUI.Instance != null)
        {
            NoteUI.Instance.ShowNote(noteTitle, noteContent);
            Debug.Log($"📜 [NOT OKUNDU] {noteTitle}");
        }
    }
}
