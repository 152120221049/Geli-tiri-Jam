using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using TMPro;
using System.Collections.Generic;

/// <summary>
/// Unity Editör menüsünden (Tools -> Ana Menü Canvas'ı Oluştur) tek tıkla
/// eksiksiz, şık ve animasyonlu Ana Menü Canvas yapısını sahnede sıfırdan kurar.
/// </summary>
public class MainMenuUIBuilder : EditorWindow
{
    [MenuItem("Tools/Envanter & Etkileşim/Ana Menü Canvas'ı Oluştur")]
    public static void BuildMainMenuCanvas()
    {
        // 1. Sahnedeki eski MainMenuCanvas'ı kontrol et
        GameObject existingCanvas = GameObject.Find("MainMenuCanvas");
        if (existingCanvas != null)
        {
            if (!EditorUtility.DisplayDialog("Var Olan Canvas",
                "Sahnede zaten 'MainMenuCanvas' bulunuyor. Yeniden oluşturmak istiyor musunuz?",
                "Evet, Yeniden Oluştur", "İptal"))
            {
                return;
            }
            DestroyImmediate(existingCanvas);
        }

        // EventSystem Kontrolü
        UnityEngine.EventSystems.EventSystem es = FindObjectOfType<UnityEngine.EventSystems.EventSystem>();
        if (es == null)
        {
            GameObject esObj = new GameObject("EventSystem");
            es = esObj.AddComponent<UnityEngine.EventSystems.EventSystem>();
            Undo.RegisterCreatedObjectUndo(esObj, "Create EventSystem");
        }

#if ENABLE_INPUT_SYSTEM
        if (es.GetComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>() == null)
        {
            var oldMod = es.GetComponent<UnityEngine.EventSystems.StandaloneInputModule>();
            if (oldMod != null) DestroyImmediate(oldMod);
            es.gameObject.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
        }
#else
        if (es.GetComponent<UnityEngine.EventSystems.StandaloneInputModule>() == null)
        {
            es.gameObject.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
        }
#endif

        // 2. Ana Canvas Oluştur
        GameObject canvasObj = new GameObject("MainMenuCanvas");
        Undo.RegisterCreatedObjectUndo(canvasObj, "Create MainMenuCanvas");

        Canvas canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 100;

        CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;

        canvasObj.AddComponent<GraphicRaycaster>();

        // Yöneticileri Ekle
        MainMenuController menuController = canvasObj.AddComponent<MainMenuController>();
        SettingsManager settingsManager = canvasObj.AddComponent<SettingsManager>();

        // 3. Arka Plan Karartma Görseli
        GameObject bgObj = CreateUIObject("Background", canvasObj.transform);
        Image bgImg = bgObj.AddComponent<Image>();
        bgImg.color = new Color(0.08f, 0.09f, 0.12f, 0.98f);
        SetFullAnchor(bgObj.GetComponent<RectTransform>());

        // 4. ANA MENÜ PANELİ
        GameObject mainPanel = CreateUIObject("MainMenuPanel", canvasObj.transform);
        SetFullAnchor(mainPanel.GetComponent<RectTransform>());

        // Başlık Text
        TextMeshProUGUI titleText = CreateText(mainPanel.transform, "TitleText", "GELİŞTİRİ-MİA JAM", 48, FontStyles.Bold, new Color(1f, 0.85f, 0.3f));
        RectTransform titleRT = titleText.GetComponent<RectTransform>();
        titleRT.anchorMin = new Vector2(0.5f, 0.85f);
        titleRT.anchorMax = new Vector2(0.5f, 0.85f);
        titleRT.sizeDelta = new Vector2(800, 80);
        titleRT.anchoredPosition = Vector2.zero;

        // Subtitle Text
        TextMeshProUGUI subTitle = CreateText(mainPanel.transform, "SubTitleText", "Zindan Macerası & Envanter Dövüş Sistemi", 20, FontStyles.Italic, new Color(0.7f, 0.8f, 0.9f));
        RectTransform subRT = subTitle.GetComponent<RectTransform>();
        subRT.anchorMin = new Vector2(0.5f, 0.78f);
        subRT.anchorMax = new Vector2(0.5f, 0.78f);
        subRT.sizeDelta = new Vector2(800, 40);
        subRT.anchoredPosition = Vector2.zero;

        // Buton Konteyneri
        GameObject btnContainer = CreateUIObject("ButtonContainer", mainPanel.transform);
        RectTransform btnContainerRT = btnContainer.GetComponent<RectTransform>();
        btnContainerRT.anchorMin = new Vector2(0.5f, 0.45f);
        btnContainerRT.anchorMax = new Vector2(0.5f, 0.45f);
        btnContainerRT.sizeDelta = new Vector2(350, 320);
        btnContainerRT.anchoredPosition = Vector2.zero;

        VerticalLayoutGroup vlg = btnContainer.AddComponent<VerticalLayoutGroup>();
        vlg.spacing = 15;
        vlg.childControlWidth = true;
        vlg.childControlHeight = true;
        vlg.childForceExpandWidth = true;
        vlg.childForceExpandHeight = true;

        Button playBtn = CreateStyledButton(btnContainer.transform, "PlayButton", "OYUNA BAŞLA", new Color(0.18f, 0.55f, 0.32f));
        Button settingsBtn = CreateStyledButton(btnContainer.transform, "SettingsButton", "AYARLAR", new Color(0.22f, 0.32f, 0.45f));
        Button creditsBtn = CreateStyledButton(btnContainer.transform, "CreditsButton", "EMEĞİ GEÇENLER", new Color(0.35f, 0.28f, 0.45f));
        Button quitBtn = CreateStyledButton(btnContainer.transform, "QuitButton", "ÇIKIŞ", new Color(0.55f, 0.2f, 0.2f));

        // 5. AYARLAR PANELİ
        GameObject settingsPanel = CreateUIObject("SettingsPanel", canvasObj.transform);
        SetFullAnchor(settingsPanel.GetComponent<RectTransform>());

        TextMeshProUGUI setHeader = CreateText(settingsPanel.transform, "SettingsHeader", "AYARLAR", 36, FontStyles.Bold, new Color(1f, 0.85f, 0.3f));
        RectTransform setHeaderRT = setHeader.GetComponent<RectTransform>();
        setHeaderRT.anchorMin = new Vector2(0.5f, 0.9f);
        setHeaderRT.anchorMax = new Vector2(0.5f, 0.9f);
        setHeaderRT.sizeDelta = new Vector2(600, 60);

        // Ayarlar İçerik Paneli (Scroll & Layout)
        GameObject setContent = CreateUIObject("SettingsContent", settingsPanel.transform);
        RectTransform setContentRT = setContent.GetComponent<RectTransform>();
        setContentRT.anchorMin = new Vector2(0.5f, 0.5f);
        setContentRT.anchorMax = new Vector2(0.5f, 0.5f);
        setContentRT.sizeDelta = new Vector2(700, 520);
        setContentRT.anchoredPosition = new Vector2(0, 20);

        VerticalLayoutGroup setVLG = setContent.AddComponent<VerticalLayoutGroup>();
        setVLG.spacing = 10;
        setVLG.childControlWidth = true;
        setVLG.childControlHeight = false;
        setVLG.childForceExpandWidth = true;

        // --- SES AYARLARI ---
        CreateSectionHeader(setContent.transform, "🔊 SES AYARLARI");
        Slider masterSlider = CreateLabeledSlider(setContent.transform, "Master Volume", "Ana Ses Seviyesi");
        Slider musicSlider = CreateLabeledSlider(setContent.transform, "Music Volume", "Müzik Seviyesi");
        Slider sfxSlider = CreateLabeledSlider(setContent.transform, "SFX Volume", "Ses Efektleri (SFX)");

        // --- GÖRÜNTÜ AYARLARI ---
        CreateSectionHeader(setContent.transform, "📺 GÖRÜNTÜ AYARLARI");
        Toggle fsToggle = CreateLabeledToggle(setContent.transform, "FullscreenToggle", "Tam Ekran Modu");
        TMP_Dropdown resDropdown = CreateLabeledDropdown(setContent.transform, "ResolutionDropdown", "Ekran Çözünürlüğü");
        TMP_Dropdown qualDropdown = CreateLabeledDropdown(setContent.transform, "QualityDropdown", "Grafik Kalitesi");
        Toggle vsyncToggle = CreateLabeledToggle(setContent.transform, "VSyncToggle", "VSync (Dikey Eşitleme)");

        // Geri Butonu (Ayarlar)
        Button setBackBtn = CreateStyledButton(settingsPanel.transform, "SettingsBackButton", "GERİ [ESC]", new Color(0.35f, 0.35f, 0.4f));
        RectTransform setBackRT = setBackBtn.GetComponent<RectTransform>();
        setBackRT.anchorMin = new Vector2(0.5f, 0.08f);
        setBackRT.anchorMax = new Vector2(0.5f, 0.08f);
        setBackRT.sizeDelta = new Vector2(240, 50);

        // 6. CREDITS PANELİ
        GameObject creditsPanel = CreateUIObject("CreditsPanel", canvasObj.transform);
        SetFullAnchor(creditsPanel.GetComponent<RectTransform>());

        TextMeshProUGUI credHeader = CreateText(creditsPanel.transform, "CreditsHeader", "EMEĞİ GEÇENLER", 36, FontStyles.Bold, new Color(1f, 0.85f, 0.3f));
        RectTransform credHeaderRT = credHeader.GetComponent<RectTransform>();
        credHeaderRT.anchorMin = new Vector2(0.5f, 0.9f);
        credHeaderRT.anchorMax = new Vector2(0.5f, 0.9f);
        credHeaderRT.sizeDelta = new Vector2(600, 60);

        TextMeshProUGUI credBody = CreateText(creditsPanel.transform, "CreditsBody",
            "🏆 Geliştiri-Mia Game Jam 2026\n\n" +
            "🎮 Oyun Tasarımı & Programlama:\n" +
            "• Envanter & Izgara Mantığı (Minecraft / Tarkov Hibrit)\n" +
            "• Silah, Ok, Büyü & Zırh Sistemi\n" +
            "• Dinamik Nişan & Yörünge Çizgisi\n\n" +
            "🎨 Görsel & Sanat: Pixel Art Assets & Custom UI\n" +
            "🎵 Ses & Müzik: Jam Audio Library\n\n" +
            "Oynadığınız İçin Teşekkürler!", 18, FontStyles.Normal, Color.white);
        RectTransform credBodyRT = credBody.GetComponent<RectTransform>();
        credBodyRT.anchorMin = new Vector2(0.5f, 0.52f);
        credBodyRT.anchorMax = new Vector2(0.5f, 0.52f);
        credBodyRT.sizeDelta = new Vector2(700, 400);

        Button credBackBtn = CreateStyledButton(creditsPanel.transform, "CreditsBackButton", "GERİ [ESC]", new Color(0.35f, 0.35f, 0.4f));
        RectTransform credBackRT = credBackBtn.GetComponent<RectTransform>();
        credBackRT.anchorMin = new Vector2(0.5f, 0.08f);
        credBackRT.anchorMax = new Vector2(0.5f, 0.08f);
        credBackRT.sizeDelta = new Vector2(240, 50);

        // 7. SCRIPT ATAMALARI & EVENT BAĞLAMALARI
        SerializedObject soMenu = new SerializedObject(menuController);
        soMenu.FindProperty("mainMenuPanel").objectReferenceValue = mainPanel;
        soMenu.FindProperty("settingsPanel").objectReferenceValue = settingsPanel;
        soMenu.FindProperty("creditsPanel").objectReferenceValue = creditsPanel;
        soMenu.ApplyModifiedProperties();

        SerializedObject soSettings = new SerializedObject(settingsManager);
        soSettings.FindProperty("masterVolumeSlider").objectReferenceValue = masterSlider;
        soSettings.FindProperty("musicVolumeSlider").objectReferenceValue = musicSlider;
        soSettings.FindProperty("sfxVolumeSlider").objectReferenceValue = sfxSlider;
        soSettings.FindProperty("fullscreenToggle").objectReferenceValue = fsToggle;
        soSettings.FindProperty("resolutionDropdown").objectReferenceValue = resDropdown;
        soSettings.FindProperty("qualityDropdown").objectReferenceValue = qualDropdown;
        soSettings.FindProperty("vsyncToggle").objectReferenceValue = vsyncToggle;
        soSettings.ApplyModifiedProperties();

        // Buton Event Bağlamaları
        playBtn.onClick.AddListener(() => menuController.StartGame());
        settingsBtn.onClick.AddListener(() => menuController.ShowSettings());
        creditsBtn.onClick.AddListener(() => menuController.ShowCredits());
        quitBtn.onClick.AddListener(() => menuController.QuitGame());
        setBackBtn.onClick.AddListener(() => menuController.ShowMainMenu());
        credBackBtn.onClick.AddListener(() => menuController.ShowMainMenu());

        // İlk Panelleri Ayarla
        mainPanel.SetActive(true);
        settingsPanel.SetActive(false);
        creditsPanel.SetActive(false);

        Selection.activeGameObject = canvasObj;
        EditorUtility.DisplayDialog("Ana Menü Oluşturuldu",
            "✅ Ana Menü Canvas'ı ve tüm paneller (Ana Menü, Ayarlar, Credits) başarıyla inşa edildi!", "Tamam");
    }

    // ═══════════════════════════════════════════
    //  HELPER METODLAR
    // ═══════════════════════════════════════════

    private static GameObject CreateUIObject(string name, Transform parent)
    {
        GameObject go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        return go;
    }

    private static void SetFullAnchor(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
    }

    private static TextMeshProUGUI CreateText(Transform parent, string name, string text, float size, FontStyles style, Color color)
    {
        GameObject go = CreateUIObject(name, parent);
        TextMeshProUGUI tmp = go.AddComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.fontSize = size;
        tmp.fontStyle = style;
        tmp.color = color;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.raycastTarget = false;
        return tmp;
    }

    private static Button CreateStyledButton(Transform parent, string name, string label, Color baseColor)
    {
        GameObject btnObj = CreateUIObject(name, parent);
        Image img = btnObj.AddComponent<Image>();
        img.color = baseColor;

        Button btn = btnObj.AddComponent<Button>();
        ColorBlock colors = btn.colors;
        colors.normalColor = baseColor;
        colors.highlightedColor = baseColor * 1.25f;
        colors.pressedColor = baseColor * 0.8f;
        btn.colors = colors;

        Outline outline = btnObj.AddComponent<Outline>();
        outline.effectColor = new Color(1f, 0.85f, 0.3f, 0.7f);
        outline.effectDistance = new Vector2(2, -2);

        TextMeshProUGUI txt = CreateText(btnObj.transform, "Label", label, 18, FontStyles.Bold, Color.white);
        SetFullAnchor(txt.GetComponent<RectTransform>());

        return btn;
    }

    private static void CreateSectionHeader(Transform parent, string title)
    {
        GameObject go = CreateUIObject($"Header_{title}", parent);
        go.AddComponent<LayoutElement>().preferredHeight = 35;
        TextMeshProUGUI tmp = go.AddComponent<TextMeshProUGUI>();
        tmp.text = title;
        tmp.fontSize = 20;
        tmp.fontStyle = FontStyles.Bold;
        tmp.color = new Color(1f, 0.85f, 0.3f);
        tmp.alignment = TextAlignmentOptions.Left;
    }

    private static Slider CreateLabeledSlider(Transform parent, string name, string labelText)
    {
        GameObject row = CreateUIObject($"Row_{name}", parent);
        row.AddComponent<LayoutElement>().preferredHeight = 40;

        HorizontalLayoutGroup hlg = row.AddComponent<HorizontalLayoutGroup>();
        hlg.childControlWidth = false;
        hlg.childControlHeight = true;

        TextMeshProUGUI lbl = CreateText(row.transform, "Label", labelText, 16, FontStyles.Normal, Color.white);
        lbl.alignment = TextAlignmentOptions.Left;
        lbl.GetComponent<RectTransform>().sizeDelta = new Vector2(250, 40);

        GameObject sliderObj = CreateUIObject(name, row.transform);
        sliderObj.GetComponent<RectTransform>().sizeDelta = new Vector2(380, 40);

        Slider slider = sliderObj.AddComponent<Slider>();

        // Background
        GameObject bg = CreateUIObject("Background", sliderObj.transform);
        Image bgImg = bg.AddComponent<Image>();
        bgImg.color = new Color(0.2f, 0.2f, 0.25f);
        RectTransform bgRT = bg.GetComponent<RectTransform>();
        bgRT.anchorMin = new Vector2(0, 0.25f);
        bgRT.anchorMax = new Vector2(1, 0.75f);
        bgRT.offsetMin = Vector2.zero;
        bgRT.offsetMax = Vector2.zero;

        // Fill Area & Fill
        GameObject fillArea = CreateUIObject("Fill Area", sliderObj.transform);
        RectTransform faRT = fillArea.GetComponent<RectTransform>();
        faRT.anchorMin = new Vector2(0, 0.25f);
        faRT.anchorMax = new Vector2(1, 0.75f);
        faRT.offsetMin = Vector2.zero;
        faRT.offsetMax = Vector2.zero;

        GameObject fill = CreateUIObject("Fill", fillArea.transform);
        Image fillImg = fill.AddComponent<Image>();
        fillImg.color = new Color(0.2f, 0.7f, 1f);
        SetFullAnchor(fill.GetComponent<RectTransform>());

        slider.fillRect = fill.GetComponent<RectTransform>();

        // Handle
        GameObject handleArea = CreateUIObject("Handle Slide Area", sliderObj.transform);
        SetFullAnchor(handleArea.GetComponent<RectTransform>());

        GameObject handle = CreateUIObject("Handle", handleArea.transform);
        Image handleImg = handle.AddComponent<Image>();
        handleImg.color = Color.white;
        RectTransform handleRT = handle.GetComponent<RectTransform>();
        handleRT.sizeDelta = new Vector2(20, 30);

        slider.handleRect = handleRT;
        slider.targetGraphic = handleImg;

        return slider;
    }

    private static Toggle CreateLabeledToggle(Transform parent, string name, string labelText)
    {
        GameObject row = CreateUIObject($"Row_{name}", parent);
        row.AddComponent<LayoutElement>().preferredHeight = 40;

        HorizontalLayoutGroup hlg = row.AddComponent<HorizontalLayoutGroup>();
        hlg.childControlWidth = false;
        hlg.childControlHeight = true;

        TextMeshProUGUI lbl = CreateText(row.transform, "Label", labelText, 16, FontStyles.Normal, Color.white);
        lbl.alignment = TextAlignmentOptions.Left;
        lbl.GetComponent<RectTransform>().sizeDelta = new Vector2(250, 40);

        GameObject toggleObj = CreateUIObject(name, row.transform);
        toggleObj.GetComponent<RectTransform>().sizeDelta = new Vector2(40, 40);

        Toggle toggle = toggleObj.AddComponent<Toggle>();

        Image bgImg = toggleObj.AddComponent<Image>();
        bgImg.color = new Color(0.2f, 0.2f, 0.25f);

        GameObject checkmark = CreateUIObject("Checkmark", toggleObj.transform);
        Image checkImg = checkmark.AddComponent<Image>();
        checkImg.color = new Color(0.2f, 0.9f, 0.4f);
        SetFullAnchor(checkmark.GetComponent<RectTransform>());

        toggle.graphic = checkImg;
        toggle.targetGraphic = bgImg;

        return toggle;
    }

    private static TMP_Dropdown CreateLabeledDropdown(Transform parent, string name, string labelText)
    {
        GameObject row = CreateUIObject($"Row_{name}", parent);
        row.AddComponent<LayoutElement>().preferredHeight = 40;

        HorizontalLayoutGroup hlg = row.AddComponent<HorizontalLayoutGroup>();
        hlg.childControlWidth = false;
        hlg.childControlHeight = true;

        TextMeshProUGUI lbl = CreateText(row.transform, "Label", labelText, 16, FontStyles.Normal, Color.white);
        lbl.alignment = TextAlignmentOptions.Left;
        lbl.GetComponent<RectTransform>().sizeDelta = new Vector2(250, 40);

        GameObject ddObj = CreateUIObject(name, row.transform);
        ddObj.GetComponent<RectTransform>().sizeDelta = new Vector2(380, 40);

        Image bgImg = ddObj.AddComponent<Image>();
        bgImg.color = new Color(0.18f, 0.2f, 0.25f);

        TMP_Dropdown dd = ddObj.AddComponent<TMP_Dropdown>();

        TextMeshProUGUI label = CreateText(ddObj.transform, "Label", "Option", 15, FontStyles.Normal, Color.white);
        label.alignment = TextAlignmentOptions.Left;
        RectTransform labelRT = label.GetComponent<RectTransform>();
        labelRT.anchorMin = Vector2.zero;
        labelRT.anchorMax = Vector2.one;
        labelRT.offsetMin = new Vector2(10, 0);
        labelRT.offsetMax = new Vector2(-30, 0);

        dd.captionText = label;
        dd.targetGraphic = bgImg;

        // Template Panel
        GameObject template = CreateUIObject("Template", ddObj.transform);
        Image tempBg = template.AddComponent<Image>();
        tempBg.color = new Color(0.12f, 0.14f, 0.18f);
        ScrollRect sr = template.AddComponent<ScrollRect>();

        RectTransform tempRT = template.GetComponent<RectTransform>();
        tempRT.anchorMin = new Vector2(0, 0);
        tempRT.anchorMax = new Vector2(1, 0);
        tempRT.pivot = new Vector2(0.5f, 1f);
        tempRT.anchoredPosition = new Vector2(0, -2);
        tempRT.sizeDelta = new Vector2(0, 150);

        // Viewport
        GameObject viewport = CreateUIObject("Viewport", template.transform);
        Image vpImg = viewport.AddComponent<Image>();
        Mask vpMask = viewport.AddComponent<Mask>();
        vpMask.showMaskGraphic = false;
        SetFullAnchor(viewport.GetComponent<RectTransform>());

        // Content
        GameObject content = CreateUIObject("Content", viewport.transform);
        RectTransform contentRT = content.GetComponent<RectTransform>();
        contentRT.anchorMin = new Vector2(0, 1);
        contentRT.anchorMax = new Vector2(1, 1);
        contentRT.pivot = new Vector2(0.5f, 1);
        contentRT.sizeDelta = new Vector2(0, 28);

        // Item
        GameObject item = CreateUIObject("Item", content.transform);
        Toggle itemToggle = item.AddComponent<Toggle>();
        Image itemBg = item.AddComponent<Image>();
        itemBg.color = new Color(0.18f, 0.22f, 0.28f);

        RectTransform itemRT = item.GetComponent<RectTransform>();
        itemRT.anchorMin = new Vector2(0, 0.5f);
        itemRT.anchorMax = new Vector2(1, 0.5f);
        itemRT.sizeDelta = new Vector2(0, 28);

        TextMeshProUGUI itemText = CreateText(item.transform, "Item Text", "Option", 14, FontStyles.Normal, Color.white);
        SetFullAnchor(itemText.GetComponent<RectTransform>());

        itemToggle.targetGraphic = itemBg;
        itemToggle.isOn = true;

        sr.content = contentRT;
        sr.viewport = viewport.GetComponent<RectTransform>();

        dd.template = tempRT;
        dd.itemText = itemText;
        template.SetActive(false);

        return dd;
    }
}
