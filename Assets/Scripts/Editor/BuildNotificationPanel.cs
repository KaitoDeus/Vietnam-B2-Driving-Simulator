using UnityEngine;
using UnityEditor;
using UnityEngine.UI;
using TMPro;

public class BuildNotificationPanel : EditorWindow
{
    [MenuItem("Tools/Build HUD Notification Panel")]
    public static void CreateNotificationPanel()
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
                Debug.LogError("[Notification Builder] Không tìm thấy Canvas nào trong Scene!");
                return;
            }
        }

        // 2. Tìm HUD_Root dưới HUD_Canvas
        Transform rootTrans = canvasGo.transform.Find("HUD_Root");
        Transform parentTrans = (rootTrans != null) ? rootTrans : canvasGo.transform;

        // 3. Xóa Panel_Notification cũ nếu tồn tại
        Transform oldPanel = parentTrans.Find("Panel_Notification");
        if (oldPanel != null)
        {
            Undo.DestroyObjectImmediate(oldPanel.gameObject);
        }

        // 4. Tạo Panel_Notification mới
        GameObject panelGo = new GameObject("Panel_Notification", typeof(RectTransform));
        Undo.RegisterCreatedObjectUndo(panelGo, "Create Notification Panel");
        panelGo.transform.SetParent(parentTrans, false);

        RectTransform panelRect = panelGo.GetComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(0.5f, 1f); // Top Center
        panelRect.anchorMax = new Vector2(0.5f, 1f);
        panelRect.pivot = new Vector2(0.5f, 0.5f);
        panelRect.anchoredPosition = new Vector2(0f, -220f); // Bên dưới nút Pause

        // 5. Thêm Image bo góc (Sliced)
        Image bgImage = panelGo.AddComponent<Image>();
        string maskGuid = "d13a968037340c242b5cb3cf21798363"; // UIMask.psd mặc định của Unity
        string maskPath = AssetDatabase.GUIDToAssetPath(maskGuid);
        Sprite roundedSprite = AssetDatabase.LoadAssetAtPath<Sprite>(maskPath);
        if (roundedSprite != null)
        {
            bgImage.sprite = roundedSprite;
            bgImage.type = Image.Type.Sliced;
        }
        else
        {
            Debug.LogWarning("[Notification Builder] Không tìm thấy sprite bo góc UIMask.psd. Hãy kéo sprite bo góc thủ công.");
        }
        // Màu mặc định cho trạng thái Warning (đỏ)
        bgImage.color = new Color(0.898f, 0.224f, 0.208f, 0.95f);

        // 6. Thêm CanvasGroup cho hiệu ứng mượt mà
        CanvasGroup canvasGroup = panelGo.AddComponent<CanvasGroup>();
        canvasGroup.alpha = 1f; // Hiển thị sẵn trong Editor để thiết kế

        // 7. Thêm HorizontalLayoutGroup để tự động co dãn theo chữ
        HorizontalLayoutGroup layout = panelGo.AddComponent<HorizontalLayoutGroup>();
        layout.padding = new RectOffset(20, 20, 8, 8);
        layout.childAlignment = TextAnchor.MiddleCenter;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = false;
        layout.childForceExpandHeight = false;

        // 8. Thêm ContentSizeFitter
        ContentSizeFitter fitter = panelGo.AddComponent<ContentSizeFitter>();
        fitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        // 9. Tạo Text con (Txt_Notification)
        GameObject textGo = new GameObject("Txt_Notification", typeof(RectTransform), typeof(TextMeshProUGUI));
        textGo.transform.SetParent(panelGo.transform, false);

        TextMeshProUGUI textComp = textGo.GetComponent<TextMeshProUGUI>();
        
        // Load TMPro Font Asset đồng bộ
        string fontGuid = "8f586378b4e144a9851e7b34d9b748ee";
        string fontPath = AssetDatabase.GUIDToAssetPath(fontGuid);
        TMP_FontAsset mainFont = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(fontPath);
        if (mainFont != null)
        {
            textComp.font = mainFont;
        }

        textComp.text = "ⓘ  Đi sai làn đường  ⓘ";
        textComp.fontSize = 16f;
        textComp.fontStyle = FontStyles.Bold;
        textComp.color = Color.white;
        textComp.alignment = TextAlignmentOptions.Center;

        // 10. Gán script Controller để quản lý 2 trạng thái
        NotificationController controller = panelGo.AddComponent<NotificationController>();
        controller.notificationText = textComp;
        controller.backgroundImage = bgImage;

        // Đánh dấu Scene thay đổi để lưu
        EditorUtility.SetDirty(panelGo);
        if (!Application.isPlaying)
        {
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(panelGo.scene);
        }

        Debug.Log("[Notification Builder] Đã xây dựng Panel_Notification thành công tại tọa độ Y: -220.");
    }
}
