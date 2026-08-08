#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System.IO;
using System.Text.RegularExpressions;

/// <summary>
/// Unity'nin otomatik oluşturduğu Assets/_Recovery/0.unity dosyasındaki Git merge conflict
/// imleçlerini (<<<<<<<, =======, >>>>>>>) otomatik temizleyen Editör bileşeni.
/// </summary>
[InitializeOnLoad]
public static class FixRecoverySceneConflict
{
    static FixRecoverySceneConflict()
    {
        CleanRecoveryScene();
    }

    [MenuItem("Tools/Envanter & Etkileşim/Recovery Sahne Conflict'lerini Temizle")]
    public static void CleanRecoveryScene()
    {
        string recoveryPath = "Assets/_Recovery/0.unity";
        if (!File.Exists(recoveryPath)) return;

        try
        {
            string content = File.ReadAllText(recoveryPath);

            if (!content.Contains("<<<<<<<")) return;

            Debug.Log("🛠️ [RECOVERY FIX] Assets/_Recovery/0.unity içindeki Git conflict işaretleri temizleniyor...");

            // Git conflict bloklarını yakala ve HEAD kısmını koru
            string pattern = @"<<<<<<<[^\n]*\r?\n(.*?)\r?\n=======\r?\n(.*?)\r?\n>>>>>>>[^\n]*";
            string fixedContent = Regex.Replace(content, pattern, "$1", RegexOptions.Singleline);

            // Eğer hala kalıntı çizgi varsa temizle
            string[] lines = fixedContent.Split(new[] { "\r\n", "\n" }, System.StringSplitOptions.None);
            var cleanLines = new System.Collections.Generic.List<string>();

            foreach (var line in lines)
            {
                if (line.StartsWith("<<<<<<<") || line.StartsWith("=======") || line.StartsWith(">>>>>>>"))
                    continue;
                cleanLines.Add(line);
            }

            File.WriteAllLines(recoveryPath, cleanLines);
            AssetDatabase.ImportAsset(recoveryPath);

            Debug.Log("✅ [RECOVERY FIX] Assets/_Recovery/0.unity dosyası başarıyla onarıldı!");
        }
        catch (System.Exception ex)
        {
            Debug.LogWarning($"⚠️ [RECOVERY FIX WARNING] {ex.Message}");
        }
    }
}
#endif
