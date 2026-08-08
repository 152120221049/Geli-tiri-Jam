using UnityEngine;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using TMPro;

public class Dialogs : MonoBehaviour
{
    [Header("Data")]
    [Tooltip("data.json dosyasını Inspector'dan buraya sürükleyin")]
    public TextAsset dialogDataFile;

    [Header("UI Referansları")]
    public TextMeshProUGUI dialogTextDisplay;

    // Veri yapımız: [NpcTag] -> [Tipi (npcDialog/playerDialog)] -> [ID] -> [Metin]
    private Dictionary<string, Dictionary<string, Dictionary<string, string>>> parsedData = new Dictionary<string, Dictionary<string, Dictionary<string, string>>>();

    void Start()
    {
        if (dialogDataFile != null)
        {
            ParseJsonManually(dialogDataFile.text);
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

        // Satır satır okuyarak regex ile ayıklıyoruz
        string[] lines = json.Split(new[] { '\r', '\n' }, System.StringSplitOptions.RemoveEmptyEntries);
        
        foreach (string line in lines)
        {
            // npcDialog veya playerDialog bloğuna girdik mi?
            if (line.Contains("\"npcDialog\"")) { currentType = "npcDialog"; continue; }
            if (line.Contains("\"playerDialog\"")) { currentType = "playerDialog"; continue; }
            
            // Yeni bir NPC tag'ı mı başlıyor? (Örn: "npc1": { )
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

            // Diyalog metinlerini yakala (Örn: "1": "Greetings...") veya ("1A": "Hello...")
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
    /// npcTag vererek NPC'nin ilk cümlesi ("1") ile konuşmayı başlatır.
    /// </summary>
    public void StartDialog(string npcTag)
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

        // Başlangıç diyalog ID'sini "1" olarak varsayıyoruz
        ShowNpcDialog(npcTag, "1");
    }

    /// <summary>
    /// Belirtilen NPC'nin belirtilen ID'li diyaloğunu gösterir.
    /// </summary>
    public void ShowNpcDialog(string npcTag, string dialogId)
    {
        if (parsedData.ContainsKey(npcTag) && parsedData[npcTag]["npcDialog"].ContainsKey(dialogId))
        {
            string text = parsedData[npcTag]["npcDialog"][dialogId];
            
            // Ekrana yansıt
            if (dialogTextDisplay != null) 
                dialogTextDisplay.text = text;
            
            Debug.Log($"[NPC - {npcTag}]: {text}");

            // Bu cümlenin ardından oyuncunun verebileceği cevaplar (1A, 1B vb.) var mı kontrol et
            CheckPlayerOptions(npcTag, dialogId);
        }
        else
        {
            Debug.LogWarning($"Diyalog bulunamadı: NPC={npcTag}, ID={dialogId}");
        }
    }

    /// <summary>
    /// Oyuncunun o anki NPC cümlesine verebileceği cevapları bulur.
    /// </summary>
    private void CheckPlayerOptions(string npcTag, string npcDialogId)
    {
        if (!parsedData.ContainsKey(npcTag) || !parsedData[npcTag].ContainsKey("playerDialog")) return;

        var playerDialogs = parsedData[npcTag]["playerDialog"];
        bool hasOptions = false;

        foreach (var kvp in playerDialogs)
        {
            // Örneğin NPC "1" no'lu cümleyi kurduysa, oyuncunun seçenekleri "1A", "1B" şeklinde başlamalı.
            if (kvp.Key.StartsWith(npcDialogId) && kvp.Key.Length > npcDialogId.Length)
            {
                hasOptions = true;
                Debug.Log($"Oyuncu Seçeneği [{kvp.Key}]: {kvp.Value}");
            }
        }

        if (!hasOptions)
        {
            // Eğer oyuncunun seçeceği bir cevap yoksa, NPC'nin sonraki cümlesine (Örn: "1"den "2"ye) devam edip etmediğine bak.
            if (int.TryParse(npcDialogId, out int nextIdInt))
            {
                string nextId = (nextIdInt + 1).ToString();
                if (parsedData[npcTag]["npcDialog"].ContainsKey(nextId))
                {
                    Debug.Log($"Sonraki NPC cümlesi: [{nextId}] (Otomatik geçmek istersen ShowNpcDialog fonksiyonunu çağırabilirsin)");
                }
            }
        }
    }
}
