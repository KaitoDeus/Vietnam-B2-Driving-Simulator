using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using TMPro;
using UnityEngine.UI;

public class SettingsMenuEditor : EditorWindow
{
    [MenuItem("Tools/Setup Apply Button")]
    public static void SetupApplyButton()
    {
        // 1. Kiểm tra Cảnh đang mở
        if (UnityEngine.SceneManagement.SceneManager.GetActiveScene().name != "MainMenu")
        {
            EditorUtility.DisplayDialog("Lỗi", "Vui lòng mở Scene MainMenu trước khi chạy Tool!", "OK");
            return;
        }

        // 2. Tìm SettingsManager và Panel_Settings trong cảnh
        SettingsManager settingsManager = Object.FindObjectOfType<SettingsManager>();
        if (settingsManager == null)
        {
            EditorUtility.DisplayDialog("Lỗi", "Không tìm thấy component SettingsManager trong cảnh đang hoạt động!", "OK");
            return;
        }

        GameObject settingsPanel = FindObjectIncludingInactive("Panel_Settings");
        if (settingsPanel == null)
        {
            EditorUtility.DisplayDialog("Lỗi", "Không tìm thấy Panel_Settings trong cảnh MainMenu!", "OK");
            return;
        }

        // 3. Tự động tìm và liên kết các tham chiếu bị thiếu trong SettingsManager
        TMP_Dropdown[] dropdowns = settingsPanel.GetComponentsInChildren<TMP_Dropdown>(true);
        foreach (var dd in dropdowns)
        {
            string ddName = dd.name.ToLower();
            if (settingsManager.resolutionDropdown == null && (ddName.Contains("res") || ddName.Contains("dophan") || ddName.Contains("phân giải")))
            {
                settingsManager.resolutionDropdown = dd;
                EditorUtility.SetDirty(settingsManager);
                Debug.Log("[SettingsMenuEditor] Tự động gán ResolutionDropdown: " + dd.name);
            }
            else if (settingsManager.qualityDropdown == null && (ddName.Contains("qual") || ddName.Contains("chatluong") || ddName.Contains("chất lượng") || ddName.Contains("preset")))
            {
                settingsManager.qualityDropdown = dd;
                EditorUtility.SetDirty(settingsManager);
                Debug.Log("[SettingsMenuEditor] Tự động gán QualityDropdown: " + dd.name);
            }
        }

        Toggle[] toggles = settingsPanel.GetComponentsInChildren<Toggle>(true);
        foreach (var tg in toggles)
        {
            string tgName = tg.name.ToLower();
            if (settingsManager.fullscreenToggle == null && (tgName.Contains("full") || tgName.Contains("toanman") || tgName.Contains("toàn màn hình")))
            {
                settingsManager.fullscreenToggle = tg;
                EditorUtility.SetDirty(settingsManager);
                Debug.Log("[SettingsMenuEditor] Tự động gán FullscreenToggle: " + tg.name);
            }
        }

        Slider[] sliders = settingsPanel.GetComponentsInChildren<Slider>(true);
        foreach (var sl in sliders)
        {
            string slName = sl.name.ToLower();
            if (settingsManager.sensitivitySlider == null && (slName.Contains("sens") || slName.Contains("nhay") || slName.Contains("nhạy")))
            {
                settingsManager.sensitivitySlider = sl;
                EditorUtility.SetDirty(settingsManager);
                Debug.Log("[SettingsMenuEditor] Tự động gán SensitivitySlider: " + sl.name);
            }
        }

        // 4. Xác định Tab Đồ họa chứa nội dung
        Transform graphicsContentParent = null;
        if (settingsManager.resolutionDropdown != null) graphicsContentParent = settingsManager.resolutionDropdown.transform.parent;
        else if (settingsManager.fullscreenToggle != null) graphicsContentParent = settingsManager.fullscreenToggle.transform.parent;
        else if (settingsManager.qualityDropdown != null) graphicsContentParent = settingsManager.qualityDropdown.transform.parent;

        if (graphicsContentParent == null)
        {
            EditorUtility.DisplayDialog("Lỗi", "Không tìm thấy Panel chứa nội dung Đồ họa (Graphics Content)!", "OK");
            return;
        }

        // 5. Dọn dẹp các nút ApplyButton được tạo sai vị trí trước đó (ví dụ ở Panel_About)
        Button[] allButtons = Object.FindObjectsOfType<Button>(true);
        foreach (var btn in allButtons)
        {
            if (btn.name == "ApplyButton" && btn.transform.parent != graphicsContentParent)
            {
                Undo.DestroyObjectImmediate(btn.gameObject);
                Debug.Log("[SettingsMenuEditor] Đã dọn dẹp nút ApplyButton ở sai vị trí: " + btn.transform.parent.name);
            }
        }

        // 6. Tìm nút Quay lại cụ thể trong Panel_Settings để làm mẫu
        Button backButton = settingsPanel.transform.Find("Btn_Back")?.GetComponent<Button>();
        if (backButton == null)
        {
            Button[] panelButtons = settingsPanel.GetComponentsInChildren<Button>(true);
            foreach (var btn in panelButtons)
            {
                string btnName = btn.name.ToLower();
                if (btnName.Contains("back") || btnName.Contains("quaylai") || btnName.Contains("quay_lai"))
                {
                    backButton = btn;
                    break;
                }
            }
        }

        if (backButton == null)
        {
            EditorUtility.DisplayDialog("Lỗi", "Không tìm thấy nút Quay lại mẫu (Btn_Back) trong Panel_Settings!", "OK");
            return;
        }

        // 7. Tìm hoặc tạo nút ApplyButton
        Button existingApplyButton = null;
        if (settingsPanel != null)
        {
            Transform applyTrans = settingsPanel.transform.Find("Btn_Apply") ?? settingsPanel.transform.Find("ApplyButton");
            if (applyTrans != null)
            {
                existingApplyButton = applyTrans.GetComponent<Button>();
            }
            else
            {
                Button[] panelButtons = settingsPanel.GetComponentsInChildren<Button>(true);
                foreach (var btn in panelButtons)
                {
                    string btnName = btn.name.ToLower();
                    if (btnName.Contains("apply") || btnName.Contains("apdung") || btnName.Contains("áp dụng"))
                    {
                        existingApplyButton = btn;
                        break;
                    }
                }
            }
        }

        GameObject applyGo = null;
        Button applyBtn = null;

        if (existingApplyButton != null)
        {
            applyGo = existingApplyButton.gameObject;
            applyBtn = existingApplyButton;
            Debug.Log("[SettingsMenuEditor] Đã tìm thấy nút ApplyButton có sẵn. Đang cấu hình logic...");
        }
        else
        {
            // Nhân bản nút Quay lại mẫu, chuyển sang làm con của Graphics Content
            GameObject sourcePrefab = PrefabUtility.GetCorrespondingObjectFromSource(backButton.gameObject);
            if (sourcePrefab != null)
            {
                applyGo = (GameObject)PrefabUtility.InstantiatePrefab(sourcePrefab, graphicsContentParent);
            }
            
            if (applyGo == null)
            {
                applyGo = Instantiate(backButton.gameObject, graphicsContentParent);
            }
            
            applyGo.name = "ApplyButton";
            applyBtn = applyGo.GetComponent<Button>();
            Undo.RegisterCreatedObjectUndo(applyGo, "Tạo nút Áp dụng đồ họa");

            // 9. Căn chỉnh kích thước và vị trí cho nút (Chỉ áp dụng với nút tự động sinh ra)
            RectTransform backRT = backButton.GetComponent<RectTransform>();
            RectTransform applyRT = applyGo.GetComponent<RectTransform>();

            applyRT.anchorMin = new Vector2(0.5f, 0.5f);
            applyRT.anchorMax = new Vector2(0.5f, 0.5f);
            applyRT.pivot = new Vector2(0.5f, 0.5f);
            applyRT.sizeDelta = backRT.sizeDelta;

            UnityEngine.UI.LayoutGroup layoutGroup = graphicsContentParent.GetComponent<UnityEngine.UI.LayoutGroup>();
            if (layoutGroup != null)
            {
                applyGo.transform.SetAsLastSibling();
            }
            else
            {
                if (settingsManager.fullscreenToggle != null)
                {
                    RectTransform toggleRT = settingsManager.fullscreenToggle.GetComponent<RectTransform>();
                    applyRT.anchoredPosition = toggleRT.anchoredPosition + new Vector2(0f, -65f);
                }
                else
                {
                    applyRT.anchoredPosition = new Vector2(0f, -100f);
                }
            }
        }

        // 8. Thay đổi text nhãn hiển thị thành "ÁP DỤNG"
        TMP_Text tmpText = applyGo.GetComponentInChildren<TMP_Text>();
        if (tmpText != null)
        {
            tmpText.text = "ÁP DỤNG";
            EditorUtility.SetDirty(tmpText);
        }
        Text legText = applyGo.GetComponentInChildren<Text>();
        if (legText != null)
        {
            legText.text = "ÁP DỤNG";
            EditorUtility.SetDirty(legText);
        }

        // 10. Thiết lập Persistent Event cho onClick tại môi trường Editor
        SerializedObject serializedButton = new SerializedObject(applyBtn);
        SerializedProperty onClickProperty = serializedButton.FindProperty("m_OnClick");
        onClickProperty.FindPropertyRelative("m_PersistentCalls.m_Calls").ClearArray();
        serializedButton.ApplyModifiedProperties();

        // Gán sự kiện thực tế tới hàm SettingsManager.ApplyGraphicsSettings
        UnityEditor.Events.UnityEventTools.AddPersistentListener(applyBtn.onClick, settingsManager.ApplyGraphicsSettings);

        // 11. Liên kết nút mới vào trường public trong SettingsManager
        settingsManager.applyButton = applyBtn;

        // Lưu thay đổi đối tượng
        EditorUtility.SetDirty(settingsManager);
        EditorUtility.SetDirty(applyBtn);
        EditorUtility.SetDirty(applyGo);

        // Lưu thay đổi Scene
        EditorSceneManager.MarkSceneDirty(UnityEngine.SceneManagement.SceneManager.GetActiveScene());
        EditorSceneManager.SaveOpenScenes();

        EditorUtility.DisplayDialog("Thành công", "Nút 'Áp dụng' đã được đặt trong Tab Đồ họa của Panel_Settings thành công!", "Tuyệt vời");
    }

    [MenuItem("Tools/Debug Settings Hierarchy")]
    public static void DebugSettingsHierarchy()
    {
        GameObject settingsPanel = FindObjectIncludingInactive("Panel_Settings");
        if (settingsPanel == null)
        {
            Debug.LogError("Could not find Panel_Settings!");
            return;
        }
        
        string hierarchy = GetHierarchyString(settingsPanel.transform, "");
        Debug.Log("Settings Panel Hierarchy:\n" + hierarchy);
    }
    
    private static string GetHierarchyString(Transform current, string indent)
    {
        string result = indent + current.name + "\n";
        for (int i = 0; i < current.childCount; i++)
            result += GetHierarchyString(current.GetChild(i), indent + "  ");
        return result;
    }

    private static GameObject FindObjectIncludingInactive(string name)
    {
        GameObject[] allObjects = Resources.FindObjectsOfTypeAll<GameObject>();
        foreach (var obj in allObjects)
        {
            if (obj.name == name && !EditorUtility.IsPersistent(obj))
            {
                // Bỏ qua các đối tượng ẩn nội bộ của hệ thống Unity
                if ((obj.hideFlags & HideFlags.HideAndDontSave) == 0)
                {
                    return obj;
                }
            }
        }
        return null;
    }

    [MenuItem("Tools/Optimize Controls Panel Layout")]
    public static void OptimizeControlsPanelLayout()
    {
        // 1. Tìm Panel_Controls_Content
        GameObject controlsContent = FindObjectIncludingInactive("Panel_Controls_Content");
        if (controlsContent == null)
        {
            EditorUtility.DisplayDialog("Lỗi", "Không tìm thấy Panel_Controls_Content trong cảnh!", "OK");
            return;
        }

        Undo.RegisterCompleteObjectUndo(controlsContent, "Optimize Controls Layout");

        // 2. Thiết lập Vertical Layout Group của Panel_Controls_Content
        VerticalLayoutGroup layoutGroup = controlsContent.GetComponent<VerticalLayoutGroup>();
        if (layoutGroup != null)
        {
            // Bỏ chọn "Control Child Size - Height" và "Child Force Expand - Height" 
            // để người dùng có thể tự do chỉnh sửa chiều cao (Height) của từng dòng trong Inspector
            layoutGroup.childControlWidth = true;
            layoutGroup.childControlHeight = false; // Tách riêng điều chỉnh chiều cao
            layoutGroup.childForceExpandWidth = true;
            layoutGroup.childForceExpandHeight = false; // Tách riêng phân bổ chiều cao
            
            // Thiết lập khoảng cách dọc giữa các dòng hợp lý
            layoutGroup.spacing = 15f; 
            
            EditorUtility.SetDirty(layoutGroup);
            Debug.Log("[SettingsMenuEditor] Đã tối ưu Vertical Layout Group: Tắt tự động kiểm soát chiều cao để cho phép tự điều chỉnh.");
        }

        // 3. Quét các dòng con để đổi tên trực quan trong Hierarchy dựa trên nội dung hiển thị
        int renamedCount = 0;
        for (int i = 0; i < controlsContent.transform.childCount; i++)
        {
            Transform child = controlsContent.transform.GetChild(i);
            TMP_Text labelText = child.GetComponentInChildren<TMP_Text>();
            
            if (labelText != null)
            {
                string text = labelText.text.Trim();
                string newName = "Row_Control_";
                
                if (text.Contains("Di chuyển") || text.Contains("Move")) newName += "Move";
                else if (text.Contains("Phanh tay") || text.Contains("Handbrake")) newName += "Handbrake";
                else if (text.Contains("Xi-nhan Trái") || text.Contains("Left Blinker")) newName += "BlinkerLeft";
                else if (text.Contains("Xi-nhan Phải") || text.Contains("Right Blinker")) newName += "BlinkerRight";
                else if (text.Contains("Còi") || text.Contains("Horn")) newName += "Horn";
                else if (text.Contains("góc nhìn") || text.Contains("Camera")) newName += "SwitchCamera";
                else newName += text.Replace(" ", "");

                if (child.name != newName)
                {
                    Undo.RegisterCompleteObjectUndo(child.gameObject, "Rename Control Row");
                    child.name = newName;
                    renamedCount++;
                }
            }
        }

        // Lưu thay đổi
        EditorUtility.SetDirty(controlsContent);
        EditorSceneManager.MarkSceneDirty(UnityEngine.SceneManagement.SceneManager.GetActiveScene());
        EditorSceneManager.SaveOpenScenes();

        EditorUtility.DisplayDialog("Thành công", 
            $"Đã tối ưu bảng điều khiển:\n" +
            $"- Đã đổi tên {renamedCount} dòng để dễ quản lý trong Hierarchy.\n" +
            $"- Tách riêng kiểm soát chiều cao (Height): Giờ bạn có thể nhấp vào từng dòng và thay đổi chiều cao riêng lẻ tùy ý!", 
            "Tuyệt vời");
    }

    [MenuItem("Tools/Debug Controls Layout Detailed")]
    public static void DebugControlsLayoutDetailed()
    {
        GameObject controlsContent = FindObjectIncludingInactive("Panel_Controls_Content");
        if (controlsContent == null)
        {
            Debug.LogError("Could not find Panel_Controls_Content!");
            return;
        }

        System.Text.StringBuilder sb = new System.Text.StringBuilder();
        sb.AppendLine("=== DETAILED CONTROLS LAYOUT DEBUG ===");
        
        VerticalLayoutGroup layoutGroup = controlsContent.GetComponent<VerticalLayoutGroup>();
        if (layoutGroup != null)
        {
            sb.AppendLine($"VerticalLayoutGroup settings: spacing={layoutGroup.spacing}, childControlHeight={layoutGroup.childControlHeight}, childForceExpandHeight={layoutGroup.childForceExpandHeight}");
        }

        for (int i = 0; i < controlsContent.transform.childCount; i++)
        {
            Transform child = controlsContent.transform.GetChild(i);
            RectTransform rt = child.GetComponent<RectTransform>();
            sb.AppendLine($"Row {i}: Name='{child.name}'");
            if (rt != null)
            {
                sb.AppendLine($"  - RectTransform: sizeDelta={rt.sizeDelta}, anchoredPos={rt.anchoredPosition}, localScale={child.localScale}, pivot={rt.pivot}, anchors=({rt.anchorMin}, {rt.anchorMax})");
            }
            
            // Log children of this row
            for (int j = 0; j < child.childCount; j++)
            {
                Transform subChild = child.GetChild(j);
                RectTransform subRt = subChild.GetComponent<RectTransform>();
                TMP_Text txt = subChild.GetComponentInChildren<TMP_Text>();
                sb.AppendLine($"    * Child {j}: Name='{subChild.name}', active={subChild.gameObject.activeSelf}");
                if (subRt != null)
                {
                    sb.AppendLine($"      - sizeDelta={subRt.sizeDelta}, anchoredPos={subRt.anchoredPosition}, localScale={subChild.localScale}");
                }
                if (txt != null)
                {
                    sb.AppendLine($"      - Text Content: '{txt.text}'");
                }
            }
        }

        Debug.Log(sb.ToString());
    }

    [MenuItem("Tools/Sync Controls Row Layouts from Template")]
    public static void SyncControlsRowLayouts()
    {
        GameObject controlsContent = FindObjectIncludingInactive("Panel_Controls_Content");
        if (controlsContent == null)
        {
            EditorUtility.DisplayDialog("Lỗi", "Không tìm thấy Panel_Controls_Content!", "OK");
            return;
        }

        if (controlsContent.transform.childCount < 2)
        {
            EditorUtility.DisplayDialog("Lỗi", "Cần ít nhất 2 dòng để copy template!", "OK");
            return;
        }

        Transform templateRow = controlsContent.transform.GetChild(1); // Dùng dòng thứ 2 (Phanh tay) làm mẫu
        RectTransform templateRT = templateRow.GetComponent<RectTransform>();
        
        if (templateRow.childCount < 2)
        {
            EditorUtility.DisplayDialog("Lỗi", "Dòng template không đủ 2 thành phần con!", "OK");
            return;
        }

        Transform templateLabel = templateRow.GetChild(0);
        Transform templateKeycap = templateRow.GetChild(1);

        Undo.RegisterCompleteObjectUndo(controlsContent, "Sync Control Rows Layout");

        int fixedCount = 0;
        for (int i = 0; i < controlsContent.transform.childCount; i++)
        {
            Transform row = controlsContent.transform.GetChild(i);
            if (row == templateRow) continue;

            Undo.RegisterCompleteObjectUndo(row.gameObject, "Sync Row RectTransform");
            
            // 1. Đồng bộ RectTransform của dòng chính
            RectTransform rowRT = row.GetComponent<RectTransform>();
            if (rowRT != null && templateRT != null)
            {
                row.localScale = Vector3.one;
                rowRT.sizeDelta = new Vector2(rowRT.sizeDelta.x, templateRT.sizeDelta.y);
                rowRT.pivot = templateRT.pivot;
                rowRT.anchorMin = templateRT.anchorMin;
                rowRT.anchorMax = templateRT.anchorMax;
                rowRT.anchoredPosition = new Vector2(templateRT.anchoredPosition.x, rowRT.anchoredPosition.y);
            }

            // 2. Đồng bộ các thành phần con (Label và Keycap)
            if (row.childCount >= 2)
            {
                Transform rowLabel = row.GetChild(0);
                Transform rowKeycap = row.GetChild(1);

                // Đồng bộ Label
                RectTransform rLabel = rowLabel.GetComponent<RectTransform>();
                RectTransform tLabel = templateLabel.GetComponent<RectTransform>();
                if (rLabel != null && tLabel != null)
                {
                    Undo.RegisterCompleteObjectUndo(rowLabel.gameObject, "Sync Label Layout");
                    rowLabel.localScale = Vector3.one;
                    rLabel.anchorMin = tLabel.anchorMin;
                    rLabel.anchorMax = tLabel.anchorMax;
                    rLabel.pivot = tLabel.pivot;
                    rLabel.sizeDelta = tLabel.sizeDelta;
                    rLabel.anchoredPosition = tLabel.anchoredPosition;
                }

                // Đồng bộ Keycap
                RectTransform rKeycap = rowKeycap.GetComponent<RectTransform>();
                RectTransform tKeycap = templateKeycap.GetComponent<RectTransform>();
                if (rKeycap != null && tKeycap != null)
                {
                    Undo.RegisterCompleteObjectUndo(rowKeycap.gameObject, "Sync Keycap Layout");
                    rowKeycap.localScale = Vector3.one;
                    rKeycap.anchorMin = tKeycap.anchorMin;
                    rKeycap.anchorMax = tKeycap.anchorMax;
                    rKeycap.pivot = tKeycap.pivot;
                    rKeycap.sizeDelta = tKeycap.sizeDelta;
                    rKeycap.anchoredPosition = tKeycap.anchoredPosition;
                }
                fixedCount++;
            }
        }

        // Thiết lập lại Vertical Layout Group để tự động giãn cách hoàn hảo
        VerticalLayoutGroup layoutGroup = controlsContent.GetComponent<VerticalLayoutGroup>();
        if (layoutGroup != null)
        {
            layoutGroup.childControlWidth = true;
            layoutGroup.childControlHeight = true; // Bật lại kiểm soát để đồng bộ thẳng hàng hoàn hảo
            layoutGroup.childForceExpandWidth = true;
            layoutGroup.childForceExpandHeight = false;
            layoutGroup.spacing = 15f;
            EditorUtility.SetDirty(layoutGroup);
        }

        EditorUtility.SetDirty(controlsContent);
        EditorSceneManager.MarkSceneDirty(UnityEngine.SceneManagement.SceneManager.GetActiveScene());
        EditorSceneManager.SaveOpenScenes();

        EditorUtility.DisplayDialog("Thành công", 
            $"Đã đồng bộ {fixedCount} dòng điều khiển thẳng hàng đẹp đẽ theo mẫu của dòng '{templateRow.name}'!", 
            "Tuyệt vời");
    }
}
