using UnityEngine;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using TMPro;

public class Dialogs : MonoBehaviour
{
    // Singleton yapısı sayesinde diğer scriptlerden (NPCInteractable) kolayca erişilir
    public static Dialogs Instance;

    [Header("Data")]
    [Tooltip("data.json dosyasını Inspector'dan buraya sürükleyin")]
    public TextAsset dialogDataFile;

    [Header("Player UI Referansları")]
    [Tooltip("Oyuncunun kendi konuşma baloncuğu (NPC'ninki gibi). Atanmazsa otomatik bulunmaya çalışılır.")]
    public GameObject playerBubbleObj;
    
    [Tooltip("Oyuncunun kendi metin objesi (3D TextMeshPro). Atanmazsa otomatik bulunmaya çalışılır.")]
    public TextMeshPro playerTextDisplay;

    // Veri yapımız: [NpcTag] -> [Tipi (npcDialog/playerDialog)] -> [ID] -> [Metin]
    private Dictionary<string, Dictionary<string, Dictionary<string, string>>> parsedData = new Dictionary<string, Dictionary<string, Dictionary<string, string>>>();

    // Durum takibi
    private bool isDialogActive = false;
    private string currentNpcTag = "";
    private string currentNpcDialogId = "";
    
    private bool waitingForOption = false;
    private bool showingPlayerText = false;

    // Şu an aktif olan NPC'nin lokal UI referansları
    private TextMeshPro currentTextDisplay;
    private GameObject currentBubbleObj;

    // Klavyeden seçilebilecek aktif opsiyonlar
    private List<KeyValuePair<string, string>> activeOptions = new List<KeyValuePair<string, string>>();

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public bool IsDialogActive()
    {
        return isDialogActive;
    }

    void Start()
    {
        if (dialogDataFile != null)
        {
            ParseJsonManually(dialogDataFile.text);
        }

        // Eğer Inspector'dan Player Text atanmadıysa, otomatik bul
        if (playerTextDisplay == null)
        {
            playerTextDisplay = GetComponentInChildren<TextMeshPro>(true);
        }

        // Eğer Player Bubble atanmadıysa Text'in parent'ını al
        if (playerBubbleObj == null && playerTextDisplay != null && playerTextDisplay.transform.parent != null)
        {
            if (playerTextDisplay.transform.parent != this.transform)
            {
                playerBubbleObj = playerTextDisplay.transform.parent.gameObject;
            }
        }

        // Oyun başlarken oyuncunun baloncuğunu gizle
        if (playerBubbleObj != null)
        {
            playerBubbleObj.SetActive(false);
        }
    }

    /// <summary>
    /// Eklediğin iç içe Dictionary formatındaki JSON dosyasını (data.json) dış kütüphane olmadan parse eder.
    /// </summary>
    private void ParseJsonManually(string json)
    {
        parsedData.Clear();
        string currentNpc = "";
        string currentType = "";

        string[] lines = json.Split(new[] { '\r', '\n' }, System.StringSplitOptions.RemoveEmptyEntries);
        
        foreach (string line in lines)
        {
            if (line.Contains("\"npcDialog\"")) { currentType = "npcDialog"; continue; }
            if (line.Contains("\"playerDialog\"")) { currentType = "playerDialog"; continue; }
            
            var npcMatch = Regex.Match(line, @"\""([^\""]+)\""\s*:\s*\{");
            if (npcMatch.Success && !line.Contains("npcDialog") && !line.Contains("playerDialog"))
            {
                currentNpc = npcMatch.Groups[1].Value;
                if (!parsedData.ContainsKey(currentNpc))
                {
                    parsedData[currentNpc] = new Dictionary<string, Dictionary<string, string>>();
                    parsedData[currentNpc]["npcDialog"] = new Dictionary<string, string>();
                    parsedData[currentNpc]["playerDialog"] = new Dictionary<string, string>();
                }
                continue;
            }

            var dialogMatch = Regex.Match(line, @"\""([^\""]+)\""\s*:\s*\""([^\""]+)\""");
            if (dialogMatch.Success && !string.IsNullOrEmpty(currentNpc) && !string.IsNullOrEmpty(currentType))
            {
                string id = dialogMatch.Groups[1].Value;
                string text = dialogMatch.Groups[2].Value;
                parsedData[currentNpc][currentType][id] = text;
            }
        }
    }

    /// <summary>
    /// npcTag vererek NPC'nin ilk cümlesi ("1") ile konuşmayı başlatır. NPC'nin lokal Text ve Bubble objelerini alır.
    /// </summary>
    public void StartDialog(string npcTag, TextMeshPro textObj, GameObject bubbleObj)
    {
        if (dialogDataFile == null)
        {
            Debug.LogError("Dialog Data File atanmamış! Lütfen inspector'dan data.json dosyasını atayın.");
            return;
        }

        if (parsedData.Count == 0)
        {
            ParseJsonManually(dialogDataFile.text);
        }

        currentNpcTag = npcTag;
        currentTextDisplay = textObj;
        currentBubbleObj = bubbleObj;
        
        isDialogActive = true;
        waitingForOption = false;
        showingPlayerText = false;
        
        // Zamanı durdur
        Time.timeScale = 0f;

        // Player baloncuğu açıksa kapat
        if (playerBubbleObj != null) playerBubbleObj.SetActive(false);

        // NPC baloncuğunu aç
        if (currentBubbleObj != null) currentBubbleObj.SetActive(true);

        ShowNpcDialog("1");
    }

    /// <summary>
    /// Belirtilen NPC'nin belirtilen ID'li diyaloğunu gösterir.
    /// </summary>
    private void ShowNpcDialog(string dialogId)
    {
        currentNpcDialogId = dialogId;
        showingPlayerText = false;
        activeOptions.Clear();

        if (parsedData.ContainsKey(currentNpcTag) && parsedData[currentNpcTag]["npcDialog"].ContainsKey(dialogId))
        {
            string text = parsedData[currentNpcTag]["npcDialog"][dialogId];
            
            // Seçenekleri kontrol et ve metne ekle
            text += CheckPlayerOptions(dialogId);

            if (currentTextDisplay != null) 
                currentTextDisplay.text = text;
        }
        else
        {
            // Diyalog bittiyse kapat
            EndDialog();
        }
    }

    /// <summary>
    /// Oyuncunun o anki NPC cümlesine verebileceği cevapları bulur ve ekrana yazdırılmak üzere string olarak döndürür.
    /// Aynı zamanda activeOptions listesini doldurur.
    /// </summary>
    private string CheckPlayerOptions(string npcDialogId)
    {
        waitingForOption = false;
        activeOptions.Clear();

        if (!parsedData.ContainsKey(currentNpcTag) || !parsedData[currentNpcTag].ContainsKey("playerDialog")) return "";

        var playerDialogs = parsedData[currentNpcTag]["playerDialog"];

        foreach (var kvp in playerDialogs)
        {
            if (kvp.Key.StartsWith(npcDialogId) && kvp.Key.Length > npcDialogId.Length)
            {
                activeOptions.Add(kvp);
            }
        }

        if (activeOptions.Count > 0)
        {
            waitingForOption = true; // Oyuncunun klavyeden seçim yapmasını bekleyeceğiz
            
            string optionsString = "\n\n"; // NPC'nin cümlesinden sonra biraz boşluk bırak

            for (int i = 0; i < activeOptions.Count; i++)
            {
                var option = activeOptions[i];
                // 1- Seçenek 1
                // 2- Seçenek 2
                optionsString += $"<color=#f9a03f>{i + 1}-</color> {option.Value}\n";
            }

            return optionsString;
        }
        
        return "";
    }

    /// <summary>
    /// Klavyeden numara tuşuna basıldığında ilgili seçeneği işler.
    /// </summary>
    private void OnOptionSelected(int index)
    {
        if (index < 0 || index >= activeOptions.Count) return;

        var selectedOption = activeOptions[index];
        
        waitingForOption = false;
        showingPlayerText = true;

        // NPC'nin baloncuğunu gizleyip, Oyuncunun baloncuğunu gösteriyoruz
        if (currentBubbleObj != null) currentBubbleObj.SetActive(false);
        if (playerBubbleObj != null) playerBubbleObj.SetActive(true);

        if (playerTextDisplay != null)
        {
            playerTextDisplay.text = selectedOption.Value;
        }
        else if (currentTextDisplay != null) // Fallback: Eğer player texti yoksa npc'ninkine yaz
        {
            if (currentBubbleObj != null) currentBubbleObj.SetActive(true);
            currentTextDisplay.text = "<color=#a4e767>Oyuncu:</color>\n" + selectedOption.Value;
        }

        // Oyuncu cümlesini gösterdik, bir sonraki ID'yi belirliyoruz (Örn "1" idi, seçtiği "1A" oldu, şimdi "2" ye geçecek)
        if (int.TryParse(currentNpcDialogId, out int currentIdInt))
        {
            currentNpcDialogId = (currentIdInt + 1).ToString();
        }
    }

    /// <summary>
    /// Diyaloğu tamamen sonlandırır ve oyunu devam ettirir.
    /// </summary>
    private void EndDialog()
    {
        isDialogActive = false;
        Time.timeScale = 1f; // Zamanı geri başlat

        if (currentBubbleObj != null) currentBubbleObj.SetActive(false);
        if (playerBubbleObj != null) playerBubbleObj.SetActive(false);
        
        currentBubbleObj = null;
        currentTextDisplay = null;
        
        Debug.Log("Diyalog sona erdi, oyun devam ediyor.");
    }

    void Update()
    {
        if (!isDialogActive) return;

        // Eğer seçenek bekliyorsak klavyeden giriş kontrol et
        if (waitingForOption)
        {
            if (UnityEngine.InputSystem.Keyboard.current == null) return;

            if (UnityEngine.InputSystem.Keyboard.current.digit1Key.wasPressedThisFrame || UnityEngine.InputSystem.Keyboard.current.numpad1Key.wasPressedThisFrame) OnOptionSelected(0);
            else if (UnityEngine.InputSystem.Keyboard.current.digit2Key.wasPressedThisFrame || UnityEngine.InputSystem.Keyboard.current.numpad2Key.wasPressedThisFrame) OnOptionSelected(1);
            else if (UnityEngine.InputSystem.Keyboard.current.digit3Key.wasPressedThisFrame || UnityEngine.InputSystem.Keyboard.current.numpad3Key.wasPressedThisFrame) OnOptionSelected(2);
            else if (UnityEngine.InputSystem.Keyboard.current.digit4Key.wasPressedThisFrame || UnityEngine.InputSystem.Keyboard.current.numpad4Key.wasPressedThisFrame) OnOptionSelected(3);
            else if (UnityEngine.InputSystem.Keyboard.current.digit5Key.wasPressedThisFrame || UnityEngine.InputSystem.Keyboard.current.numpad5Key.wasPressedThisFrame) OnOptionSelected(4);
            
            // Seçenek bekleniyorken fare veya başka atlamaya izin verme
            return;
        }

        // Diyalog atlama kontrolü (Fare Sol Tık VEYA Yeni Sistem Tuşları)
        bool advancePressed = false;

        if (UnityEngine.InputSystem.Mouse.current != null && UnityEngine.InputSystem.Mouse.current.leftButton.wasPressedThisFrame) advancePressed = true;
        if (UnityEngine.InputSystem.Keyboard.current != null && (UnityEngine.InputSystem.Keyboard.current.eKey.wasPressedThisFrame || UnityEngine.InputSystem.Keyboard.current.enterKey.wasPressedThisFrame || UnityEngine.InputSystem.Keyboard.current.spaceKey.wasPressedThisFrame)) advancePressed = true;
        if (UnityEngine.InputSystem.Gamepad.current != null && (UnityEngine.InputSystem.Gamepad.current.buttonSouth.wasPressedThisFrame || UnityEngine.InputSystem.Gamepad.current.buttonNorth.wasPressedThisFrame)) advancePressed = true;

        if (advancePressed)
        {
            if (showingPlayerText)
            {
                // Zaten oyuncunun kendi seçtiği metin ekrandaydı, şimdi tıklanınca NPC'nin cevabına (sonraki cümleye) geç
                
                // Oyuncu baloncuğunu gizle, NPC baloncuğunu tekrar aç
                if (playerBubbleObj != null) playerBubbleObj.SetActive(false);
                if (currentBubbleObj != null) currentBubbleObj.SetActive(true);

                ShowNpcDialog(currentNpcDialogId);
            }
            else
            {
                // NPC'nin metni ekrandaydı ve seçenek yoktu. Sıradaki NPC cümlesine geç
                if (int.TryParse(currentNpcDialogId, out int currentIdInt))
                {
                    string nextId = (currentIdInt + 1).ToString();
                    ShowNpcDialog(nextId);
                }
                else
                {
                    EndDialog();
                }
            }
        }
    }
}
