using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using System.IO;
using UnityEngine.UI;
using TMPro;

[InitializeOnLoad]
public class PracticeSettingsSetup
{
    static PracticeSettingsSetup()
    {
        if (!EditorPrefs.HasKey("PracticeSettingsSetup_Done_V4"))
        {
            EditorPrefs.SetBool("PracticeSettingsSetup_Done_V4", true);
            EditorApplication.delayCall += () => {
                SetupPracticeSettings(false);
            };
        }
    }

    [MenuItem("Tools/Setup Practice Settings")]
    public static void ManualSetup()
    {
        SetupPracticeSettings(true);
    }

    private static void SetupPracticeSettings(bool interactive)
    {
        string currentScene = EditorSceneManager.GetActiveScene().path;

        // 1. Open MainMenu scene to export Panel_Settings as a prefab
        string mainMenuPath = "Assets/Scenes/MainMenu.unity";
        if (!File.Exists(mainMenuPath))
        {
            if (interactive) EditorUtility.DisplayDialog("Error", "MainMenu scene not found!", "OK");
            return;
        }

        EditorSceneManager.OpenScene(mainMenuPath);
        GameObject mainMenuSettingsPanel = FindObjectIncludingInactive("Panel_Settings");
        if (mainMenuSettingsPanel == null)
        {
            if (interactive) EditorUtility.DisplayDialog("Error", "Panel_Settings not found in MainMenu scene!", "OK");
            return;
        }

        // Ensure Prefabs directory exists
        if (!Directory.Exists("Assets/Prefabs"))
        {
            Directory.CreateDirectory("Assets/Prefabs");
            AssetDatabase.Refresh();
        }

        // Save MainMenu's Settings Panel as a Prefab
        string prefabPath = "Assets/Prefabs/Panel_Settings.prefab";
        GameObject settingsPrefab = PrefabUtility.SaveAsPrefabAssetAndConnect(mainMenuSettingsPanel, prefabPath, InteractionMode.AutomatedAction);
        if (settingsPrefab == null)
        {
            if (interactive) EditorUtility.DisplayDialog("Error", "Failed to save Panel_Settings prefab!", "OK");
            return;
        }
        Debug.Log("[PracticeSettingsSetup] Saved Panel_Settings prefab successfully.");

        // 2. Open Practice scene
        string practicePath = "Assets/Scenes/Practice.unity";
        if (!File.Exists(practicePath))
        {
            if (interactive) EditorUtility.DisplayDialog("Error", "Practice scene not found!", "OK");
            return;
        }

        EditorSceneManager.OpenScene(practicePath);

        // Find Canvas or HUD root
        HUDController hud = Object.FindAnyObjectByType<HUDController>();
        if (hud == null)
        {
            if (interactive) EditorUtility.DisplayDialog("Error", "HUDController not found in Practice scene!", "OK");
            return;
        }

        // Find existing Panel_Settings in Practice
        GameObject practiceSettingsPanel = FindObjectIncludingInactive("Panel_Settings");
        Transform parentTransform = hud.transform;
        int siblingIndex = -1;

        if (practiceSettingsPanel != null)
        {
            parentTransform = practiceSettingsPanel.transform.parent;
            siblingIndex = practiceSettingsPanel.transform.GetSiblingIndex();
            // Delete old Settings Panel
            Object.DestroyImmediate(practiceSettingsPanel);
        }

        // Instantiate new Settings Panel from Prefab
        GameObject newSettingsPanel = (GameObject)PrefabUtility.InstantiatePrefab(settingsPrefab, parentTransform);
        newSettingsPanel.name = "Panel_Settings";
        newSettingsPanel.SetActive(false); // Inactive by default

        if (siblingIndex != -1)
        {
            newSettingsPanel.transform.SetSiblingIndex(siblingIndex);
        }

        // Reference it in HUDController
        hud.settingsPanel = newSettingsPanel;
        EditorUtility.SetDirty(hud);

        // 3. Setup SettingsManager in the scene
        // We will attach SettingsManager to the HUDController GameObject
        SettingsManager settingsManager = hud.gameObject.GetComponent<SettingsManager>();
        if (settingsManager == null)
        {
            settingsManager = hud.gameObject.AddComponent<SettingsManager>();
        }

        // Reset SettingsManager parameters so that AutoAssignSettingsManagerFields can work and then save them
        AutoAssignSettingsManagerFields(settingsManager, newSettingsPanel);
        EditorUtility.SetDirty(settingsManager);

        // 4. Hook up Apply button click event
        if (settingsManager.applyButton != null)
        {
            SerializedObject serializedButton = new SerializedObject(settingsManager.applyButton);
            SerializedProperty onClickProperty = serializedButton.FindProperty("m_OnClick");
            onClickProperty.FindPropertyRelative("m_PersistentCalls.m_Calls").ClearArray();
            serializedButton.ApplyModifiedProperties();
            UnityEditor.Events.UnityEventTools.AddPersistentListener(settingsManager.applyButton.onClick, settingsManager.ApplyGraphicsSettings);
            EditorUtility.SetDirty(settingsManager.applyButton);
        }

        // 5. Hook up Back button click event to HUDController.CloseSettings
        Button backButton = FindBackButton(newSettingsPanel);
        if (backButton != null)
        {
            SerializedObject serializedButton = new SerializedObject(backButton);
            SerializedProperty onClickProperty = serializedButton.FindProperty("m_OnClick");
            onClickProperty.FindPropertyRelative("m_PersistentCalls.m_Calls").ClearArray();
            serializedButton.ApplyModifiedProperties();
            UnityEditor.Events.UnityEventTools.AddPersistentListener(backButton.onClick, hud.CloseSettings);
            EditorUtility.SetDirty(backButton);
            Debug.Log("[PracticeSettingsSetup] Hooked up Back button to HUDController.CloseSettings.");
        }

        // Mark scenes dirty and save
        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        EditorSceneManager.SaveOpenScenes();

        // Restore original scene
        if (currentScene != practicePath && File.Exists(currentScene))
        {
            EditorSceneManager.OpenScene(currentScene);
        }

        if (interactive)
        {
            EditorUtility.DisplayDialog("Success", "Practice scene Settings setup completed successfully!", "OK");
        }
        else
        {
            Debug.Log("[PracticeSettingsSetup] Auto setup completed successfully.");
        }
    }

    private static void AutoAssignSettingsManagerFields(SettingsManager settingsManager, GameObject settingsPanel)
    {
        // Sliders
        Slider[] sliders = settingsPanel.GetComponentsInChildren<Slider>(true);
        foreach (var sl in sliders)
        {
            string searchContext = sl.name.ToLower();
            Transform t = sl.transform.parent;
            while (t != null && t != settingsPanel.transform)
            {
                searchContext += " " + t.name.ToLower();
                t = t.parent;
            }

            if (searchContext.Contains("music") || searchContext.Contains("nhac") || searchContext.Contains("nhạc") || searchContext.Contains("master") || searchContext.Contains("tong") || searchContext.Contains("tổng"))
                settingsManager.musicVolumeSlider = sl;
            else if (searchContext.Contains("sfx") || searchContext.Contains("amthanh") || searchContext.Contains("âm thanh") || searchContext.Contains("effects") || searchContext.Contains("hieuung") || searchContext.Contains("hiệu ứng"))
                settingsManager.sfxVolumeSlider = sl;
            else if (searchContext.Contains("voice") || searchContext.Contains("giong") || searchContext.Contains("giọng") || searchContext.Contains("huongdan") || searchContext.Contains("hướng dẫn"))
                settingsManager.voiceVolumeSlider = sl;
            else if (searchContext.Contains("sens") || searchContext.Contains("nhay") || searchContext.Contains("nhạy"))
                settingsManager.sensitivitySlider = sl;
        }

        // Texts
        TMP_Text[] texts = settingsPanel.GetComponentsInChildren<TMP_Text>(true);
        foreach (var txt in texts)
        {
            string txtName = txt.name.ToLower();
            if (!(txtName.Contains("val") || txtName.Contains("percent"))) continue;

            string searchContext = txtName;
            Transform t = txt.transform.parent;
            while (t != null && t != settingsPanel.transform)
            {
                searchContext += " " + t.name.ToLower();
                t = t.parent;
            }

            if (searchContext.Contains("music") || searchContext.Contains("nhac") || searchContext.Contains("nhạc") || searchContext.Contains("master") || searchContext.Contains("tong") || searchContext.Contains("tổng"))
                settingsManager.musicVolumeText = txt;
            else if (searchContext.Contains("sfx") || searchContext.Contains("amthanh") || searchContext.Contains("âm thanh") || searchContext.Contains("effects") || searchContext.Contains("hieuung") || searchContext.Contains("hiệu ứng"))
                settingsManager.sfxVolumeText = txt;
            else if (searchContext.Contains("voice") || searchContext.Contains("giong") || searchContext.Contains("giọng") || searchContext.Contains("huongdan") || searchContext.Contains("hướng dẫn"))
                settingsManager.voiceVolumeText = txt;
        }

        // Dropdowns
        TMP_Dropdown[] dropdowns = settingsPanel.GetComponentsInChildren<TMP_Dropdown>(true);
        foreach (var dd in dropdowns)
        {
            string name = dd.name.ToLower();
            if (name.Contains("res") || name.Contains("dophan") || name.Contains("phân giải"))
                settingsManager.resolutionDropdown = dd;
            else if (name.Contains("qual") || name.Contains("chatluong") || name.Contains("chất lượng") || name.Contains("preset"))
                settingsManager.qualityDropdown = dd;
        }

        // Toggles
        Toggle[] toggles = settingsPanel.GetComponentsInChildren<Toggle>(true);
        foreach (var tg in toggles)
        {
            string name = tg.name.ToLower();
            if (name.Contains("full") || name.Contains("toanman") || name.Contains("toàn màn hình"))
                settingsManager.fullscreenToggle = tg;
        }

        // Apply Button
        Transform applyTrans = settingsPanel.transform.Find("Btn_Apply") ?? settingsPanel.transform.Find("ApplyButton");
        if (applyTrans != null)
        {
            settingsManager.applyButton = applyTrans.GetComponent<Button>();
        }
        else
        {
            Button[] panelButtons = settingsPanel.GetComponentsInChildren<Button>(true);
            foreach (var btn in panelButtons)
            {
                string btnName = btn.name.ToLower();
                if (btnName.Contains("apply") || btnName.Contains("apdung") || btnName.Contains("áp dụng"))
                {
                    settingsManager.applyButton = btn;
                    break;
                }
            }
        }
    }

    private static Button FindBackButton(GameObject settingsPanel)
    {
        Transform backTrans = settingsPanel.transform.Find("Btn_Back") ?? settingsPanel.transform.Find("BackButton") ?? settingsPanel.transform.Find("Btn_QuayLai");
        if (backTrans != null) return backTrans.GetComponent<Button>();

        Button[] buttons = settingsPanel.GetComponentsInChildren<Button>(true);
        foreach (var btn in buttons)
        {
            string name = btn.name.ToLower();
            if (name.Contains("back") || name.Contains("quaylai") || name.Contains("quay_lai") || name.Contains("close") || name.Contains("dong"))
            {
                return btn;
            }
        }
        return null;
    }

    private static GameObject FindObjectIncludingInactive(string name)
    {
        GameObject[] allObjects = Resources.FindObjectsOfTypeAll<GameObject>();
        foreach (var obj in allObjects)
        {
            if (obj.name.Trim() == name && !EditorUtility.IsPersistent(obj))
            {
                if ((obj.hideFlags & HideFlags.HideAndDontSave) == 0)
                {
                    return obj;
                }
            }
        }
        return null;
    }
}
