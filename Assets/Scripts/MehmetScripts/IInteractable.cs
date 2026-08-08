using UnityEngine;

/// <summary>
/// Oyuncunun dünyada etkileşime geçebileceği nesneler için arayüz (Interface).
/// Eşyalar (WorldItem), Sandıklar (Chest), NPC'ler ve Kapılar bu arayüzü uygular.
/// </summary>
public interface IInteractable
{
    /// <summary>Etkileşim başladığında UI'da görünecek açıklama metni (ör. "[E] Can İksiri Al").</summary>
    string GetInteractPrompt();

    /// <summary>Şu an etkileşime geçilebilir mi?</summary>
    bool CanInteract(Transform interactor);

    /// <summary>Etkileşim gerçekleştiğinde çalışacak mantık.</summary>
    void Interact(Transform interactor);
}
