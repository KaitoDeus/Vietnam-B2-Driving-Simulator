#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using UnityEngine.UI;
using TMPro;

public class BuildResultPanel : EditorWindow
{
    [MenuItem("Tools/Build HUD Result Screen")]
    public static void CreateResultScreen()
    {
        // 1. Tìm HUD_Canvas trong Scene
        GameObject canvasGo = GameObject.Find("HUD_Canvas");
        if (canvasGo == null)
        {
            Canvas canvas = Object.FindFirstObjectByType<Canvas>();
            if (canvas != null)
            {
                canvasGo = canvas.gameObject;
            }
            else
            {
                Debug.LogError("[Result Builder] Không tìm thấy Canvas nào trong Scene!");
                EditorUtility.DisplayDialog("Lỗi", "Không tìm thấy Canvas nào trong Scene để tạo màn hình kết quả!", "OK");
                return;
            }
        }

        // 2. Tìm HUD_Root dưới HUD_Canvas
        Transform rootTrans = canvasGo.transform.Find("HUD_Root");
        Transform parentTrans = (rootTrans != null) ? rootTrans : canvasGo.transform;

        // 3. Xóa Panel_Result cũ nếu tồn tại
        Transform oldPanel = parentTrans.Find("Panel_Result");
        if (oldPanel != null)
        {
            Undo.DestroyObjectImmediate(oldPanel.gameObject);
        }

        Undo.IncrementCurrentGroup();
        int groupIndex = Undo.GetCurrentGroup();

        // 4. Tạo Panel_Result mới (Phủ toàn bộ màn hình)
        GameObject panelGo = new GameObject("Panel_Result", typeof(RectTransform));
        Undo.RegisterCreatedObjectUndo(panelGo, "Create Result Panel");
        panelGo.transform.SetParent(parentTrans, false);

        RectTransform panelRect = panelGo.GetComponent<RectTransform>();
        panelRect.anchorMin = Vector2.zero; // Stretch all
        panelRect.anchorMax = Vector2.one;
        panelRect.offsetMin = Vector2.zero;
        panelRect.offsetMax = Vector2.zero;
        panelRect.pivot = new Vector2(0.5f, 0.5f);

        // Nền đen mờ (95% đục)
        Image bgImage = panelGo.AddComponent<Image>();
        bgImage.color = new Color(0.07f, 0.08f, 0.09f, 0.96f); // #121417 (Gần đen)

        CanvasGroup canvasGroup = panelGo.AddComponent<CanvasGroup>();
        canvasGroup.alpha = 1f; // Hiển thị trong Editor để chỉnh sửa

        ResultPanelController controller = panelGo.AddComponent<ResultPanelController>();

        // Load Built-in Sprites
        Sprite knobSprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Knob.psd");
        string maskGuid = "d13a968037340c242b5cb3cf21798363"; // UIMask.psd
        string maskPath = AssetDatabase.GUIDToAssetPath(maskGuid);
        Sprite uiMaskSprite = AssetDatabase.LoadAssetAtPath<Sprite>(maskPath);

        // Load Font
        TMP_FontAsset mainFont = GetFontAsset();

        // 5. Tạo TopAccentBar (Dải màu trên đỉnh)
        GameObject topBarGo = new GameObject("TopAccentBar", typeof(RectTransform));
        topBarGo.transform.SetParent(panelGo.transform, false);
        RectTransform topBarRt = topBarGo.GetComponent<RectTransform>();
        topBarRt.anchorMin = new Vector2(0f, 1f); // Top stretch
        topBarRt.anchorMax = new Vector2(1f, 1f);
        topBarRt.pivot = new Vector2(0.5f, 1f);
        topBarRt.anchoredPosition = new Vector2(0f, 0f);
        topBarRt.sizeDelta = new Vector2(0f, 8f); // Cao 8px
        Image topBarImg = topBarGo.AddComponent<Image>();
        topBarImg.color = controller.passColor; // Mặc định xanh lá
        controller.topAccentBar = topBarImg;

        // 6. Tạo CenterContent (Vùng chứa căn giữa)
        GameObject centerGo = new GameObject("CenterContent", typeof(RectTransform));
        centerGo.transform.SetParent(panelGo.transform, false);
        RectTransform centerRt = centerGo.GetComponent<RectTransform>();
        centerRt.anchorMin = new Vector2(0.5f, 0.5f);
        centerRt.anchorMax = new Vector2(0.5f, 0.5f);
        centerRt.pivot = new Vector2(0.5f, 0.5f);
        centerRt.anchoredPosition = Vector2.zero;
        centerRt.sizeDelta = new Vector2(600f, 600f);

        // --- 6.2. StatusText (ĐẠT / KHÔNG ĐẠT) ---
        GameObject statusGo = new GameObject("Txt_Status", typeof(RectTransform));
        statusGo.transform.SetParent(centerRt, false);
        RectTransform statusRt = statusGo.GetComponent<RectTransform>();
        statusRt.anchorMin = new Vector2(0.5f, 0.5f);
        statusRt.anchorMax = new Vector2(0.5f, 0.5f);
        statusRt.pivot = new Vector2(0.5f, 0.5f);
        statusRt.anchoredPosition = new Vector2(0f, 180f);
        statusRt.sizeDelta = new Vector2(500f, 60f);
        TMP_Text statusTxt = statusGo.AddComponent<TextMeshProUGUI>();
        statusTxt.text = "ĐẠT";
        statusTxt.fontSize = 48f; // Kích thước chữ to, nổi bật làm tiêu đề chính
        statusTxt.fontStyle = FontStyles.Bold;
        statusTxt.alignment = TextAlignmentOptions.Center;
        statusTxt.color = controller.passColor;
        if (mainFont != null) statusTxt.font = mainFont;
        controller.statusText = statusTxt;

        // --- 6.3. StepNameText (Bài thi kết thúc) ---
        GameObject stepGo = new GameObject("Txt_StepName", typeof(RectTransform));
        stepGo.transform.SetParent(centerRt, false);
        RectTransform stepRt = stepGo.GetComponent<RectTransform>();
        stepRt.anchorMin = new Vector2(0.5f, 0.5f);
        stepRt.anchorMax = new Vector2(0.5f, 0.5f);
        stepRt.pivot = new Vector2(0.5f, 0.5f);
        stepRt.anchoredPosition = new Vector2(0f, 135f);
        stepRt.sizeDelta = new Vector2(500f, 25f);
        TMP_Text stepTxt = stepGo.AddComponent<TextMeshProUGUI>();
        stepTxt.text = "Bài 4: Qua vệt bánh xe";
        stepTxt.fontSize = 16f;
        stepTxt.alignment = TextAlignmentOptions.Center;
        stepTxt.color = new Color(0.6f, 0.64f, 0.7f); // Màu xám nhẹ tinh tế
        if (mainFont != null) stepTxt.font = mainFont;
        controller.stepNameText = stepTxt;

        // --- 6.4. ScoreText (Điểm đạt được) ---
        GameObject scoreGo = new GameObject("Txt_Score", typeof(RectTransform));
        scoreGo.transform.SetParent(centerRt, false);
        RectTransform scoreRt = scoreGo.GetComponent<RectTransform>();
        scoreRt.anchorMin = new Vector2(0.5f, 0.5f);
        scoreRt.anchorMax = new Vector2(0.5f, 0.5f);
        scoreRt.pivot = new Vector2(0.5f, 0.5f);
        scoreRt.anchoredPosition = new Vector2(0f, 75f);
        scoreRt.sizeDelta = new Vector2(500f, 75f);
        TMP_Text scoreTxt = scoreGo.AddComponent<TextMeshProUGUI>();
        scoreTxt.text = "<color=#2ECC71>85</color><size=60%><color=#FFFFFF>/100</color></size>";
        scoreTxt.fontSize = 62f;
        scoreTxt.fontStyle = FontStyles.Bold;
        scoreTxt.alignment = TextAlignmentOptions.Center;
        if (mainFont != null) scoreTxt.font = mainFont;
        controller.scoreText = scoreTxt;

        // --- 6.5. Panel_Mistakes (Bảng danh sách lỗi) ---
        GameObject mistakesGo = new GameObject("Panel_Mistakes", typeof(RectTransform));
        mistakesGo.transform.SetParent(centerRt, false);
        RectTransform mistakesRt = mistakesGo.GetComponent<RectTransform>();
        mistakesRt.anchorMin = new Vector2(0.5f, 0.5f);
        mistakesRt.anchorMax = new Vector2(0.5f, 0.5f);
        mistakesRt.pivot = new Vector2(0.5f, 0.5f);
        mistakesRt.anchoredPosition = new Vector2(0f, -95f);
        mistakesRt.sizeDelta = new Vector2(460f, 160f); // Rộng 460, Cao 160
        Image mistakesImg = mistakesGo.AddComponent<Image>();
        if (uiMaskSprite != null)
        {
            mistakesImg.sprite = uiMaskSprite;
            mistakesImg.type = Image.Type.Sliced;
        }
        mistakesImg.color = new Color(0.12f, 0.13f, 0.15f, 0.95f); // #1E2126 (Màu tối của card)

        // Title: Danh sách lỗi đã mắc
        GameObject mistakesTitleGo = new GameObject("Txt_Title", typeof(RectTransform));
        mistakesTitleGo.transform.SetParent(mistakesGo.transform, false);
        RectTransform mistakesTitleRt = mistakesTitleGo.GetComponent<RectTransform>();
        mistakesTitleRt.anchorMin = new Vector2(0f, 1f);
        mistakesTitleRt.anchorMax = new Vector2(1f, 1f);
        mistakesTitleRt.pivot = new Vector2(0.5f, 1f);
        mistakesTitleRt.anchoredPosition = new Vector2(15f, -12f);
        mistakesTitleRt.sizeDelta = new Vector2(-30f, 22f);
        TMP_Text mistakesTitleTxt = mistakesTitleGo.AddComponent<TextMeshProUGUI>();
        mistakesTitleTxt.text = "Danh sách lỗi đã mắc";
        mistakesTitleTxt.fontSize = 13f;
        mistakesTitleTxt.fontStyle = FontStyles.Bold;
        mistakesTitleTxt.color = new Color(0.49f, 0.54f, 0.6f); // #7D8A99
        if (mainFont != null) mistakesTitleTxt.font = mainFont;

        // Container danh sách lỗi
        GameObject containerGo = new GameObject("Container", typeof(RectTransform));
        containerGo.transform.SetParent(mistakesGo.transform, false);
        RectTransform containerRt = containerGo.GetComponent<RectTransform>();
        containerRt.anchorMin = Vector2.zero;
        containerRt.anchorMax = Vector2.one;
        containerRt.offsetMin = new Vector2(15f, 12f);
        containerRt.offsetMax = new Vector2(-15f, -38f); // Trừa lề trên cho tiêu đề
        
        VerticalLayoutGroup vLayout = containerGo.AddComponent<VerticalLayoutGroup>();
        vLayout.spacing = 6f;
        vLayout.childAlignment = TextAnchor.UpperLeft;
        vLayout.childControlWidth = true;
        vLayout.childControlHeight = true;
        vLayout.childForceExpandWidth = true;
        vLayout.childForceExpandHeight = false;
        controller.mistakesContainer = containerRt;

        // Template lỗi dòng đơn
        GameObject templateGo = new GameObject("MistakeItemTemplate", typeof(RectTransform));
        templateGo.transform.SetParent(containerRt, false);
        RectTransform templateRt = templateGo.GetComponent<RectTransform>();
        templateRt.sizeDelta = new Vector2(0f, 22f);

        HorizontalLayoutGroup hLayout = templateGo.AddComponent<HorizontalLayoutGroup>();
        hLayout.childAlignment = TextAnchor.MiddleLeft;
        hLayout.childControlWidth = true;
        hLayout.childControlHeight = true;
        hLayout.childForceExpandWidth = false;
        hLayout.childForceExpandHeight = false;

        // Tên lỗi (Trái)
        GameObject errTxtGo = new GameObject("Txt_Error", typeof(RectTransform));
        errTxtGo.transform.SetParent(templateGo.transform, false);
        RectTransform errTxtRt = errTxtGo.GetComponent<RectTransform>();
        errTxtRt.sizeDelta = new Vector2(350f, 22f);
        TMP_Text errTxt = errTxtGo.AddComponent<TextMeshProUGUI>();
        errTxt.text = "- Đi sai làn đường";
        errTxt.fontSize = 14f;
        errTxt.color = Color.white;
        if (mainFont != null) errTxt.font = mainFont;

        // Điểm trừ (Phải)
        GameObject deductTxtGo = new GameObject("Txt_Deduction", typeof(RectTransform));
        deductTxtGo.transform.SetParent(templateGo.transform, false);
        RectTransform deductTxtRt = deductTxtGo.GetComponent<RectTransform>();
        deductTxtRt.sizeDelta = new Vector2(60f, 22f);
        TMP_Text deductTxt = deductTxtGo.AddComponent<TextMeshProUGUI>();
        deductTxt.text = "-10";
        deductTxt.fontSize = 14f;
        deductTxt.alignment = TextAlignmentOptions.Right;
        deductTxt.color = controller.deductionColor;
        if (mainFont != null) deductTxt.font = mainFont;

        controller.mistakeItemTemplate = templateGo;

        // Text thông báo không lỗi
        GameObject noMistakesGo = new GameObject("Txt_NoMistakes", typeof(RectTransform));
        noMistakesGo.transform.SetParent(mistakesGo.transform, false);
        RectTransform noMistakesRt = noMistakesGo.GetComponent<RectTransform>();
        noMistakesRt.anchorMin = Vector2.zero;
        noMistakesRt.anchorMax = Vector2.one;
        noMistakesRt.offsetMin = new Vector2(15f, 15f);
        noMistakesRt.offsetMax = new Vector2(-15f, -38f);
        TMP_Text noMistakesTxt = noMistakesGo.AddComponent<TextMeshProUGUI>();
        noMistakesTxt.text = "Tuyệt vời! Bạn không phạm lỗi nào.";
        noMistakesTxt.fontSize = 14f;
        noMistakesTxt.alignment = TextAlignmentOptions.Center;
        noMistakesTxt.color = Color.white;
        if (mainFont != null) noMistakesTxt.font = mainFont;
        noMistakesTxt.gameObject.SetActive(false);
        controller.noMistakesText = noMistakesTxt;

        // --- 6.6. Nút Bấm Panel_Buttons ---
        GameObject buttonsGo = new GameObject("Panel_Buttons", typeof(RectTransform));
        buttonsGo.transform.SetParent(centerRt, false);
        RectTransform buttonsRt = buttonsGo.GetComponent<RectTransform>();
        buttonsRt.anchorMin = new Vector2(0.5f, 0.5f);
        buttonsRt.anchorMax = new Vector2(0.5f, 0.5f);
        buttonsRt.pivot = new Vector2(0.5f, 0.5f);
        buttonsRt.anchoredPosition = new Vector2(0f, -225f);
        buttonsRt.sizeDelta = new Vector2(460f, 45f);

        // Nút Thi Lại (Màu xanh dương)
        GameObject btnRestartGo = new GameObject("Btn_Restart", typeof(RectTransform));
        btnRestartGo.transform.SetParent(buttonsRt, false);
        RectTransform btnRestartRt = btnRestartGo.GetComponent<RectTransform>();
        btnRestartRt.anchorMin = new Vector2(0f, 0.5f);
        btnRestartRt.anchorMax = new Vector2(0.5f, 0.5f);
        btnRestartRt.pivot = new Vector2(0f, 0.5f);
        btnRestartRt.anchoredPosition = new Vector2(0f, 0f);
        btnRestartRt.sizeDelta = new Vector2(215f, 42f);
        Image btnRestartImg = btnRestartGo.AddComponent<Image>();
        if (uiMaskSprite != null)
        {
            btnRestartImg.sprite = uiMaskSprite;
            btnRestartImg.type = Image.Type.Sliced;
        }
        btnRestartImg.color = new Color(0.06f, 0.45f, 0.74f, 1f); // #0F75BD (Xanh dương)
        Button btnRestart = btnRestartGo.AddComponent<Button>();
        controller.restartButton = btnRestart;

        // Text của nút Thi Lại
        GameObject txtRestartGo = new GameObject("Txt_Label", typeof(RectTransform));
        txtRestartGo.transform.SetParent(btnRestartGo.transform, false);
        RectTransform txtRestartRt = txtRestartGo.GetComponent<RectTransform>();
        txtRestartRt.anchorMin = Vector2.zero;
        txtRestartRt.anchorMax = Vector2.one;
        txtRestartRt.offsetMin = Vector2.zero;
        txtRestartRt.offsetMax = Vector2.zero;
        TMP_Text txtRestart = txtRestartGo.AddComponent<TextMeshProUGUI>();
        txtRestart.text = "Thi lại";
        txtRestart.fontSize = 15f;
        txtRestart.fontStyle = FontStyles.Bold;
        txtRestart.alignment = TextAlignmentOptions.Center;
        txtRestart.color = Color.white;
        if (mainFont != null) txtRestart.font = mainFont;

        // Nút Về Menu (Màu xám viền)
        GameObject btnMenuGo = new GameObject("Btn_Menu", typeof(RectTransform));
        btnMenuGo.transform.SetParent(buttonsRt, false);
        RectTransform btnMenuRt = btnMenuGo.GetComponent<RectTransform>();
        btnMenuRt.anchorMin = new Vector2(1f, 0.5f);
        btnMenuRt.anchorMax = new Vector2(1f, 0.5f);
        btnMenuRt.pivot = new Vector2(1f, 0.5f);
        btnMenuRt.anchoredPosition = new Vector2(0f, 0f);
        btnMenuRt.sizeDelta = new Vector2(215f, 42f);
        Image btnMenuImg = btnMenuGo.AddComponent<Image>();
        if (uiMaskSprite != null)
        {
            btnMenuImg.sprite = uiMaskSprite;
            btnMenuImg.type = Image.Type.Sliced;
        }
        btnMenuImg.color = new Color(0.18f, 0.18f, 0.2f, 1f); // #2D2D33 (Xám đậm)
        Button btnMenu = btnMenuGo.AddComponent<Button>();
        controller.menuButton = btnMenu;

        // Text của nút Về Menu
        GameObject txtMenuGo = new GameObject("Txt_Label", typeof(RectTransform));
        txtMenuGo.transform.SetParent(btnMenuGo.transform, false);
        RectTransform txtMenuRt = txtMenuGo.GetComponent<RectTransform>();
        txtMenuRt.anchorMin = Vector2.zero;
        txtMenuRt.anchorMax = Vector2.one;
        txtMenuRt.offsetMin = Vector2.zero;
        txtMenuRt.offsetMax = Vector2.zero;
        TMP_Text txtMenu = txtMenuGo.AddComponent<TextMeshProUGUI>();
        txtMenu.text = "Về menu";
        txtMenu.fontSize = 15f;
        txtMenu.fontStyle = FontStyles.Bold;
        txtMenu.alignment = TextAlignmentOptions.Center;
        txtMenu.color = Color.white;
        if (mainFont != null) txtMenu.font = mainFont;

        // 7. Liên kết Panel vào HUDController
        HUDController hudCtrl = Object.FindFirstObjectByType<HUDController>();
        if (hudCtrl != null)
        {
            SerializedObject serializedHud = new SerializedObject(hudCtrl);
            // Chúng ta cần thêm trường resultPanel vào HUDController
            var prop = serializedHud.FindProperty("resultPanel");
            if (prop != null)
            {
                prop.objectReferenceValue = controller;
                serializedHud.ApplyModifiedProperties();
                Debug.Log("[Result Builder] Đã gán liên kết ResultPanelController vào HUDController.");
            }
            else
            {
                Debug.LogWarning("[Result Builder] HUDController chưa có biến resultPanel. Hãy khai báo nó trước.");
            }
        }

        // Đánh dấu bẩn để lưu Scene
        EditorUtility.SetDirty(panelGo);
        if (!Application.isPlaying)
        {
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(panelGo.scene);
        }

        Undo.CollapseUndoOperations(groupIndex);
        Debug.Log("[Result Builder] Tạo màn hình kết quả Panel_Result thành công!");
        EditorUtility.DisplayDialog("Thành công", "Đã dựng xong màn hình báo cáo kết quả thi sát hạch B2 cực kỳ sang trọng!", "OK");
    }

    private static TMP_FontAsset GetFontAsset()
    {
        string fontGuid = "8f586378b4e144a9851e7b34d9b748ee";
        string fontPath = AssetDatabase.GUIDToAssetPath(fontGuid);
        TMP_FontAsset mainFont = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(fontPath);
        if (mainFont == null)
        {
            mainFont = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/TextMesh Pro/Examples & Extras/Resources/Fonts & Materials/Roboto-Bold SDF.asset");
        }
        if (mainFont == null)
        {
            mainFont = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/TextMesh Pro/Resources/Fonts & Materials/LiberationSans SDF.asset");
        }
        return mainFont;
    }
}
#endif
