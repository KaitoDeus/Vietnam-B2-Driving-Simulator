#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.UI;
using TMPro;

public class ConfigureSettingsUI
{
    [MenuItem("Tools/Configure Settings UI")]
    public static void Setup()
    {
        Debug.Log("[ConfigureSettingsUI] Bắt đầu cấu hình giao diện Cài đặt...");

        var scene = EditorSceneManager.GetActiveScene();
        if (scene.name != "MainMenu")
        {
            if (EditorUtility.DisplayDialog("Chuyển Scene", "Bạn cần mở scene MainMenu để cấu hình. Mở ngay?", "Có", "Không"))
            {
                EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo();
                EditorSceneManager.OpenScene("Assets/Scenes/MainMenu.unity");
            }
            else
            {
                Debug.LogWarning("[ConfigureSettingsUI] Đã hủy cấu hình vì không ở scene MainMenu.");
                return;
            }
        }

        // ===== 1. TẠO HOẶC TÌM SettingsManager GameObject =====
        GameObject settingsManagerGo = GameObject.Find("SettingsManager");
        if (settingsManagerGo == null)
        {
            settingsManagerGo = new GameObject("SettingsManager");
            Undo.RegisterCreatedObjectUndo(settingsManagerGo, "Create SettingsManager");
            Debug.Log("[ConfigureSettingsUI] Đã tạo GameObject SettingsManager mới.");
        }

        SettingsManager[] allSettingsManagers = settingsManagerGo.GetComponents<SettingsManager>();
        SettingsManager settingsManager = null;
        
        if (allSettingsManagers.Length > 0)
        {
            settingsManager = allSettingsManagers[0];
            // Xóa các component trùng lặp thứ 2 trở đi nếu có
            for (int i = 1; i < allSettingsManagers.Length; i++)
            {
                Debug.LogWarning($"[ConfigureSettingsUI] Phát hiện component trùng lặp trên GameObject {settingsManagerGo.name}, đang tự động xóa component thứ {i + 1}...");
                Undo.DestroyObjectImmediate(allSettingsManagers[i]);
            }
        }
        else
        {
            settingsManager = Undo.AddComponent<SettingsManager>(settingsManagerGo);
            Debug.Log("[ConfigureSettingsUI] Đã thêm component SettingsManager duy nhất.");
        }

        // ===== 2. TÌM CÁC THÀNH PHẦN SLIDERS VÀ TEXT TRONG SCENE =====
        Slider musicSlider = FindComponentByName<Slider>("Sld_MasterVolume");
        Slider sfxSlider = FindComponentByName<Slider>("Sld_SFXEffects");
        Slider voiceSlider = FindComponentByName<Slider>("Sld_VoiceVolume");

        GameObject rowMaster = GameObject.Find("Row_MasterVolume");
        GameObject rowSFX = GameObject.Find("Row_SFXEffects");
        GameObject rowVoice = GameObject.Find("Row_VoiceVolume");

        TMP_Text musicText = FindPercentText(rowMaster);
        TMP_Text sfxText = FindPercentText(rowSFX);
        TMP_Text voiceText = FindPercentText(rowVoice);

        // ===== 3. SỬA LẠI LAYOUT CỦA SLIDERS BỊ LỆCH =====
        FixSliderLayout(musicSlider);
        FixSliderLayout(sfxSlider);
        FixSliderLayout(voiceSlider);

        // ===== 4. GÁN THAM CHIẾU VÀO SettingsManager =====
        settingsManager.musicVolumeSlider = musicSlider;
        settingsManager.musicVolumeText = musicText;

        settingsManager.sfxVolumeSlider = sfxSlider;
        settingsManager.sfxVolumeText = sfxText;

        settingsManager.voiceVolumeSlider = voiceSlider;
        settingsManager.voiceVolumeText = voiceText;

        // ===== 5. THIẾT LẬP GIÁ TRỊ MẶC ĐỊNH NGAY TRONG SCENE (EDIT MODE) =====
        if (musicSlider != null)
        {
            Undo.RecordObject(musicSlider, "Set Default Music Volume");
            musicSlider.value = 1.00f;
            Debug.Log("[ConfigureSettingsUI] Đã đặt mặc định slider Master Volume = 100%");
        }
        if (musicText != null)
        {
            Undo.RecordObject(musicText, "Set Default Music Text");
            musicText.text = "100%";
        }

        if (sfxSlider != null)
        {
            Undo.RecordObject(sfxSlider, "Set Default SFX Volume");
            sfxSlider.value = 1.00f;
            Debug.Log("[ConfigureSettingsUI] Đã đặt mặc định slider SFX Volume = 100%");
        }
        if (sfxText != null)
        {
            Undo.RecordObject(sfxText, "Set Default SFX Text");
            sfxText.text = "100%";
        }

        if (voiceSlider != null)
        {
            Undo.RecordObject(voiceSlider, "Set Default Voice Volume");
            voiceSlider.value = 1.00f;
            Debug.Log("[ConfigureSettingsUI] Đã đặt mặc định slider Voice Volume = 100%");
        }
        if (voiceText != null)
        {
            Undo.RecordObject(voiceText, "Set Default Voice Text");
            voiceText.text = "100%";
        }

        // Gán thêm Resolution và Fullscreen
        GameObject resGo = GameObject.Find("Ddp_Resolution");
        if (resGo != null)
        {
            settingsManager.resolutionDropdown = resGo.GetComponent<TMP_Dropdown>();
            Debug.Log("[ConfigureSettingsUI] Đã kết nối Resolution Dropdown.");
        }

        GameObject fsGo = GameObject.Find("Tgl_Fullscreen");
        if (fsGo != null)
        {
            settingsManager.fullscreenToggle = fsGo.GetComponent<Toggle>();
            Debug.Log("[ConfigureSettingsUI] Đã kết nối Fullscreen Toggle.");
        }

        // Đánh dấu Scene và Prefab dơ (Dirty) để Unity lưu lại thay đổi
        if (musicText != null) EditorUtility.SetDirty(musicText);
        if (sfxText != null) EditorUtility.SetDirty(sfxText);
        if (voiceText != null) EditorUtility.SetDirty(voiceText);
        
        EditorUtility.SetDirty(settingsManager);
        EditorSceneManager.MarkSceneDirty(scene);

        Debug.Log("[ConfigureSettingsUI] Cấu hình hoàn tất thành công!");
        EditorUtility.DisplayDialog("Cấu hình Settings UI", "Đã cấu hình, sửa lỗi layout và đồng bộ hiển thị 85% / 60% / 100% thành công!", "OK");
    }

    private static T FindComponentByName<T>(string name) where T : Component
    {
        GameObject go = GameObject.Find(name);
        if (go != null)
        {
            return go.GetComponent<T>();
        }
        return null;
    }

    private static TMP_Text FindPercentText(GameObject rowGo)
    {
        if (rowGo == null) return null;
        Transform textInfo = rowGo.transform.Find("Row_Text_Info");
        if (textInfo == null) return null;

        // Tìm con có tên Txt_Percent (hoặc Txt_Percent )
        Transform percentTrans = textInfo.Find("Txt_Percent");
        if (percentTrans == null) percentTrans = textInfo.Find("Txt_Percent ");

        if (percentTrans == null)
        {
            // Dự phòng: Quét tất cả các thành phần TMP_Text trong con
            TMP_Text[] texts = textInfo.GetComponentsInChildren<TMP_Text>();
            foreach (var txt in texts)
            {
                if (txt.gameObject.name.Contains("Percent"))
                {
                    return txt;
                }
            }
            if (texts.Length > 1) return texts[1]; // Vị trí thứ 2 thường là % hiển thị
        }

        if (percentTrans != null)
        {
            return percentTrans.GetComponent<TMP_Text>();
        }
        return null;
    }

    private static void FixSliderLayout(Slider slider)
    {
        if (slider == null) return;

        Debug.Log($"[ConfigureSettingsUI] Đang sửa layout cho slider: {slider.name}...");
        Undo.RecordObject(slider.gameObject, "Fix Slider Layout");

        // 1. Sửa Handle Slide Area (Chiếm toàn bộ diện tích của Slider)
        Transform slideAreaTrans = slider.transform.Find("Handle Slide Area");
        if (slideAreaTrans != null)
        {
            RectTransform slideAreaRect = slideAreaTrans.GetComponent<RectTransform>();
            if (slideAreaRect != null)
            {
                Undo.RecordObject(slideAreaRect, "Fix Handle Slide Area");
                slideAreaRect.anchorMin = Vector2.zero;
                slideAreaRect.anchorMax = Vector2.one;
                slideAreaRect.anchoredPosition = Vector2.zero;
                slideAreaRect.sizeDelta = Vector2.zero;
                slideAreaRect.offsetMin = Vector2.zero;
                slideAreaRect.offsetMax = Vector2.zero;
                slideAreaRect.localScale = Vector3.one;
            }
        }

        // 2. Sửa Handle (Căn giữa theo anchor ngang, căng dọc, rộng 20, không lệch dọc)
        if (slider.handleRect != null)
        {
            Undo.RecordObject(slider.handleRect, "Fix Slider Handle");
            
            // Đảm bảo căng dọc (y từ 0 đến 1)
            float currentAnchorX = slider.handleRect.anchorMin.x;
            slider.handleRect.anchorMin = new Vector2(currentAnchorX, 0f);
            slider.handleRect.anchorMax = new Vector2(currentAnchorX, 1f);
            
            // Xóa toàn bộ offset lệch bằng cách set offsetMin/offsetMax trực tiếp
            // Rộng 20 (từ -10 đến 10), và khít chiều dọc (bottom = 0, top = 0)
            slider.handleRect.offsetMin = new Vector2(-10f, 0f);
            slider.handleRect.offsetMax = new Vector2(10f, 0f);
            
            slider.handleRect.anchoredPosition = new Vector2(0f, 0f);
            slider.handleRect.localScale = Vector3.one;
        }

        // 3. Sửa Fill Area (Chiếm toàn bộ diện tích của Slider)
        Transform fillAreaTrans = slider.transform.Find("Fill Area");
        if (fillAreaTrans != null)
        {
            RectTransform fillAreaRect = fillAreaTrans.GetComponent<RectTransform>();
            if (fillAreaRect != null)
            {
                Undo.RecordObject(fillAreaRect, "Fix Fill Area");
                fillAreaRect.anchorMin = Vector2.zero;
                fillAreaRect.anchorMax = Vector2.one;
                fillAreaRect.anchoredPosition = Vector2.zero;
                fillAreaRect.sizeDelta = Vector2.zero;
                fillAreaRect.offsetMin = Vector2.zero;
                fillAreaRect.offsetMax = Vector2.zero;
                fillAreaRect.localScale = Vector3.one;
            }
        }

        // 4. Sửa Fill
        if (slider.fillRect != null)
        {
            Undo.RecordObject(slider.fillRect, "Fix Slider Fill");
            
            slider.fillRect.anchorMin = Vector2.zero;
            slider.fillRect.anchorMax = new Vector2(slider.fillRect.anchorMax.x, 1f);
            slider.fillRect.offsetMin = Vector2.zero;
            slider.fillRect.offsetMax = Vector2.zero;
            slider.fillRect.anchoredPosition = Vector2.zero;
            slider.fillRect.localScale = Vector3.one;
        }

        Debug.Log($"[ConfigureSettingsUI] Hoàn tất sửa layout cho slider: {slider.name}");
    }
}
#endif
