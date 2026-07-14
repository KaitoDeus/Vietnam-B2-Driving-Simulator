using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class SettingsManager : MonoBehaviour
{
    public static SettingsManager Instance { get; private set; }

    [System.Serializable]
    public struct CustomResolution
    {
        public int width;
        public int height;
        public string label;
    }

    [Header("Audio UI Elements")]
    public Slider musicVolumeSlider;
    public TMP_Text musicVolumeText;
    public Slider sfxVolumeSlider;
    public TMP_Text sfxVolumeText;
    public Slider voiceVolumeSlider;
    public TMP_Text voiceVolumeText;

    [Header("Graphics UI Elements")]
    public TMP_Dropdown qualityDropdown;
    public Toggle fullscreenToggle;
    public TMP_Dropdown resolutionDropdown;
    public Button applyButton;

    [Header("Controls UI Elements")]
    public Slider sensitivitySlider;

    [Header("Custom Hardcoded Resolutions")]
    public List<CustomResolution> customResolutions = new List<CustomResolution>()
    {
        new CustomResolution { width = 3840, height = 2160, label = "3840 x 2160" },
        new CustomResolution { width = 2560, height = 1440, label = "2560 x 1440" },
        new CustomResolution { width = 1920, height = 1080, label = "1920 x 1080" },
        new CustomResolution { width = 1600, height = 900,  label = "1600 x 900" },
        new CustomResolution { width = 1366, height = 768,  label = "1366 x 768" },
        new CustomResolution { width = 1280, height = 720,  label = "1280 x 720" },
        new CustomResolution { width = 1024, height = 768,  label = "1024 x 768" }
    };

    // Runtime active resolutions filtered by monitor capability
    private List<CustomResolution> activeResolutions = new List<CustomResolution>();

    // Graphics Pending States
    private int pendingQualityIndex;
    private bool pendingFullscreen;
    private int pendingResolutionIndex;

    private GameObject graphicsTabPanel;
    private bool isShowingFeedback = false;

    private void Awake()
    {
        Instance = this;
        AutoFindReferences();
        LoadSettings();
    }

    private void Start()
    {
        AutoFindReferences();
        InitializeResolutionDropdown();
        CreateApplyButtonProgrammatically();
        RegisterListeners();
    }

    public void AutoFindReferences()
    {
        GameObject settingsPanel = FindObjectIncludingInactive("Panel_Settings");
        if (settingsPanel == null) return;

        // Auto find sliders
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
            {
                if (musicVolumeSlider == null) musicVolumeSlider = sl;
            }
            else if (searchContext.Contains("sfx") || searchContext.Contains("amthanh") || searchContext.Contains("âm thanh") || searchContext.Contains("effects") || searchContext.Contains("hieuung") || searchContext.Contains("hiệu ứng"))
            {
                if (sfxVolumeSlider == null) sfxVolumeSlider = sl;
            }
            else if (searchContext.Contains("voice") || searchContext.Contains("giong") || searchContext.Contains("giọng") || searchContext.Contains("huongdan") || searchContext.Contains("hướng dẫn"))
            {
                if (voiceVolumeSlider == null) voiceVolumeSlider = sl;
            }
            else if (searchContext.Contains("sens") || searchContext.Contains("nhay") || searchContext.Contains("nhạy"))
            {
                if (sensitivitySlider == null) sensitivitySlider = sl;
            }
        }

        // Auto find texts (labels / percentage values)
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
            {
                if (musicVolumeText == null) musicVolumeText = txt;
            }
            else if (searchContext.Contains("sfx") || searchContext.Contains("amthanh") || searchContext.Contains("âm thanh") || searchContext.Contains("effects") || searchContext.Contains("hieuung") || searchContext.Contains("hiệu ứng"))
            {
                if (sfxVolumeText == null) sfxVolumeText = txt;
            }
            else if (searchContext.Contains("voice") || searchContext.Contains("giong") || searchContext.Contains("giọng") || searchContext.Contains("huongdan") || searchContext.Contains("hướng dẫn"))
            {
                if (voiceVolumeText == null) voiceVolumeText = txt;
            }
        }

        // Auto find dropdowns
        TMP_Dropdown[] dropdowns = settingsPanel.GetComponentsInChildren<TMP_Dropdown>(true);
        foreach (var dd in dropdowns)
        {
            string ddName = dd.name.ToLower();
            if (ddName.Contains("res") || ddName.Contains("dophan") || ddName.Contains("phân giải"))
            {
                if (resolutionDropdown == null) resolutionDropdown = dd;
            }
            else if (ddName.Contains("qual") || ddName.Contains("chatluong") || ddName.Contains("chất lượng") || ddName.Contains("preset"))
            {
                if (qualityDropdown == null) qualityDropdown = dd;
            }
        }

        // Auto find toggles
        Toggle[] toggles = settingsPanel.GetComponentsInChildren<Toggle>(true);
        foreach (var tg in toggles)
        {
            string tgName = tg.name.ToLower();
            if (tgName.Contains("full") || tgName.Contains("toanman") || tgName.Contains("toàn màn hình"))
            {
                if (fullscreenToggle == null) fullscreenToggle = tg;
            }
        }

        // Auto find apply button
        if (applyButton == null)
        {
            Transform applyTrans = settingsPanel.transform.Find("Btn_Apply") ?? settingsPanel.transform.Find("ApplyButton");
            if (applyTrans != null)
            {
                applyButton = applyTrans.GetComponent<Button>();
            }
            else
            {
                Button[] panelButtons = settingsPanel.GetComponentsInChildren<Button>(true);
                foreach (var btn in panelButtons)
                {
                    string btnName = btn.name.ToLower();
                    if (btnName.Contains("apply") || btnName.Contains("apdung") || btnName.Contains("áp dụng"))
                    {
                        applyButton = btn;
                        break;
                    }
                }
            }
        }
    }

    private void OnEnable()
    {
        if (activeResolutions == null || activeResolutions.Count == 0)
        {
            InitializeResolutionDropdown();
        }
        SyncUIWithSavedSettings();
        DefaultToAudioTab();
    }

    private void Update()
    {
        CheckPendingChanges();
    }

    private void RegisterListeners()
    {
        // 1. Audio listeners (Apply and save immediately)
        if (musicVolumeSlider != null)
        {
            musicVolumeSlider.onValueChanged.RemoveAllListeners();
            musicVolumeSlider.onValueChanged.AddListener(val => {
                SetMusicVolume(val);
                UpdateMusicVolumeText(val);
            });
        }

        if (sfxVolumeSlider != null)
        {
            sfxVolumeSlider.onValueChanged.RemoveAllListeners();
            sfxVolumeSlider.onValueChanged.AddListener(val => {
                SetSFXVolume(val);
                UpdateSFXVolumeText(val);
            });
        }

        if (voiceVolumeSlider != null)
        {
            voiceVolumeSlider.onValueChanged.RemoveAllListeners();
            voiceVolumeSlider.onValueChanged.AddListener(val => {
                SetVoiceVolume(val);
                UpdateVoiceVolumeText(val);
            });
        }

        // 2. Control listeners (Apply and save immediately)
        if (sensitivitySlider != null)
        {
            sensitivitySlider.onValueChanged.RemoveAllListeners();
            sensitivitySlider.onValueChanged.AddListener(SetSensitivity);
        }

        // 3. Graphics listeners (Only update pending state, click Apply to save)
        if (qualityDropdown != null)
        {
            qualityDropdown.onValueChanged.RemoveAllListeners();
            qualityDropdown.onValueChanged.AddListener(val => {
                pendingQualityIndex = val;
                CheckPendingChanges();
            });
        }

        if (fullscreenToggle != null)
        {
            fullscreenToggle.onValueChanged.RemoveAllListeners();
            fullscreenToggle.onValueChanged.AddListener(val => {
                pendingFullscreen = val;
                CheckPendingChanges();
            });
        }

        if (resolutionDropdown != null)
        {
            resolutionDropdown.onValueChanged.RemoveAllListeners();
            resolutionDropdown.onValueChanged.AddListener(val => {
                pendingResolutionIndex = val;
                CheckPendingChanges();
            });
        }

        // 4. Apply button listener (Reset onClick completely to avoid persistent listener cloning)
        if (applyButton != null)
        {
            applyButton.onClick = new Button.ButtonClickedEvent();
            applyButton.onClick.AddListener(ApplyGraphicsSettings);
        }
    }

    // ==========================================
    // 1. CÀI ĐẶT ÂM THANH (AUDIO SETTINGS)
    // ==========================================
    public void SetMusicVolume(float volume)
    {
        PlayerPrefs.SetFloat("MusicVolume", volume);
        PlayerPrefs.Save();
        AudioListener.volume = volume;
    }

    public void SetSFXVolume(float volume)
    {
        PlayerPrefs.SetFloat("SFXVolume", volume);
        PlayerPrefs.Save();

        // Cập nhật real-time âm lượng các hiệu ứng âm thanh (SFX) trong màn chơi
        if (ExamManager.Instance != null)
        {
            ExamManager.Instance.UpdateAudioVolumes();
        }

        CarController car = Object.FindAnyObjectByType<CarController>();
        if (car != null)
        {
            car.UpdateAudioVolumes();
        }

        CarAudio carAudio = Object.FindAnyObjectByType<CarAudio>();
        if (carAudio != null)
        {
            carAudio.UpdateVolumeSettings();
        }
    }

    public void SetVoiceVolume(float volume)
    {
        PlayerPrefs.SetFloat("VoiceVolume", volume);
        PlayerPrefs.Save();

        // Cập nhật real-time giọng đọc hướng dẫn thi
        if (ExamManager.Instance != null)
        {
            ExamManager.Instance.UpdateAudioVolumes();
        }
    }

    private void UpdateMusicVolumeText(float volume)
    {
        if (musicVolumeText != null)
        {
            musicVolumeText.text = Mathf.RoundToInt(volume * 100) + "%";
        }
    }

    private void UpdateSFXVolumeText(float volume)
    {
        if (sfxVolumeText != null)
        {
            sfxVolumeText.text = Mathf.RoundToInt(volume * 100) + "%";
        }
    }

    private void UpdateVoiceVolumeText(float volume)
    {
        if (voiceVolumeText != null)
        {
            voiceVolumeText.text = Mathf.RoundToInt(volume * 100) + "%";
        }
    }

    // ==========================================
    // 2. CÀI ĐẶT ĐỒ HỌA (GRAPHICS SETTINGS)
    // ==========================================
    public void ApplyGraphicsSettings()
    {
        PlayerPrefs.SetInt("QualityIndex", pendingQualityIndex);
        PlayerPrefs.SetInt("Fullscreen", pendingFullscreen ? 1 : 0);
        PlayerPrefs.SetInt("ResolutionIndex", pendingResolutionIndex);
        
        if (activeResolutions != null && activeResolutions.Count > 0 && pendingResolutionIndex < activeResolutions.Count)
        {
            CustomResolution targetRes = activeResolutions[pendingResolutionIndex];
            PlayerPrefs.SetInt("SavedResWidth", targetRes.width);
            PlayerPrefs.SetInt("SavedResHeight", targetRes.height);
        }
        PlayerPrefs.Save();

        QualitySettings.SetQualityLevel(pendingQualityIndex);

        bool isWebGL = false;
#if UNITY_WEBGL
        isWebGL = true;
#endif
#if UNITY_EDITOR
        MainMenuController mainMenu = Object.FindAnyObjectByType<MainMenuController>();
        if (mainMenu != null && mainMenu.simulateWebGLInEditor)
        {
            isWebGL = true;
        }
#endif

        if (isWebGL)
        {
            Screen.fullScreen = pendingFullscreen;
            Debug.Log($"[SettingsManager] Đã áp dụng cài đặt WebGL: Fullscreen={pendingFullscreen}, Quality={pendingQualityIndex}");
        }
        else
        {
            if (activeResolutions != null && activeResolutions.Count > 0 && pendingResolutionIndex < activeResolutions.Count)
            {
                CustomResolution targetRes = activeResolutions[pendingResolutionIndex];
                FullScreenMode mode = pendingFullscreen ? FullScreenMode.FullScreenWindow : FullScreenMode.Windowed;
                
                Screen.SetResolution(targetRes.width, targetRes.height, mode);
                Debug.Log($"[SettingsManager] Đã áp dụng cài đặt: {targetRes.width}x{targetRes.height}, Mode: {mode}, Quality: {pendingQualityIndex}");
            }
        }

        StartCoroutine(ShowApplyFeedback());
    }

    private void CheckPendingChanges()
    {
        if (applyButton == null) return;
        if (isShowingFeedback) return; // Giữ nút hiển thị khi đang chạy Feedback (ĐÃ ÁP DỤNG!)

        // 1. Kiểm tra xem tab Đồ họa có đang active hay không
        bool isGraphicsTabActive = false;
        if (graphicsTabPanel == null)
        {
            graphicsTabPanel = FindGraphicsTabPanel();
        }

        if (graphicsTabPanel != null)
        {
            isGraphicsTabActive = graphicsTabPanel.activeInHierarchy;
        }

        // 2. Kiểm tra thay đổi ở Fullscreen và Độ phân giải
        bool savedFullscreen = PlayerPrefs.GetInt("Fullscreen", Screen.fullScreen ? 1 : 0) == 1;

        int savedW = PlayerPrefs.GetInt("SavedResWidth", Screen.width);
        int savedH = PlayerPrefs.GetInt("SavedResHeight", Screen.height);

        int currentResIndex = 0;
        if (activeResolutions != null)
        {
            for (int i = 0; i < activeResolutions.Count; i++)
            {
                if (activeResolutions[i].width == savedW &&
                    activeResolutions[i].height == savedH)
                {
                    currentResIndex = i;
                    break;
                }
            }
        }
        int savedResolutionIndex = PlayerPrefs.GetInt("ResolutionIndex", currentResIndex);

        // Nút Áp dụng chỉ hiện khi có thay đổi ở Fullscreen HOẶC Resolution
        bool hasChanges = (pendingFullscreen != savedFullscreen) ||
                          (pendingResolutionIndex != savedResolutionIndex);

        // Nút chỉ xuất hiện (active) khi và chỉ khi đang ở Tab Đồ họa VÀ có thay đổi
        bool shouldShow = isGraphicsTabActive && hasChanges;
        if (applyButton.gameObject.activeSelf != shouldShow)
        {
            applyButton.gameObject.SetActive(shouldShow);
        }
    }

    private System.Collections.IEnumerator ShowApplyFeedback()
    {
        if (applyButton == null) yield break;
        isShowingFeedback = true;

        TMP_Text tmpTxt = applyButton.GetComponentInChildren<TMP_Text>();
        Text legacyText = applyButton.GetComponentInChildren<Text>();

        string originalText = "";
        if (tmpTxt != null)
        {
            originalText = tmpTxt.text;
            tmpTxt.text = "ĐÃ ÁP DỤNG!";
        }
        else if (legacyText != null)
        {
            originalText = legacyText.text;
            legacyText.text = "ĐÃ ÁP DỤNG!";
        }

        yield return new WaitForSecondsRealtime(1.5f);

        if (tmpTxt != null) tmpTxt.text = originalText;
        else if (legacyText != null) legacyText.text = originalText;

        isShowingFeedback = false;
        CheckPendingChanges();
    }

    private void SyncUIWithSavedSettings()
    {
        pendingQualityIndex = PlayerPrefs.GetInt("QualityIndex", QualitySettings.GetQualityLevel());
        pendingFullscreen = PlayerPrefs.GetInt("Fullscreen", Screen.fullScreen ? 1 : 0) == 1;

        int savedW = PlayerPrefs.GetInt("SavedResWidth", Screen.width);
        int savedH = PlayerPrefs.GetInt("SavedResHeight", Screen.height);

        int currentResIndex = 0;
        if (activeResolutions != null)
        {
            for (int i = 0; i < activeResolutions.Count; i++)
            {
                if (activeResolutions[i].width == savedW &&
                    activeResolutions[i].height == savedH)
                {
                    currentResIndex = i;
                    break;
                }
            }
        }
        pendingResolutionIndex = PlayerPrefs.GetInt("ResolutionIndex", currentResIndex);

        if (qualityDropdown != null)
        {
            qualityDropdown.value = pendingQualityIndex;
            qualityDropdown.RefreshShownValue();
        }
        if (fullscreenToggle != null)
        {
            fullscreenToggle.isOn = pendingFullscreen;
        }
        if (resolutionDropdown != null && activeResolutions != null && pendingResolutionIndex < activeResolutions.Count)
        {
            resolutionDropdown.value = pendingResolutionIndex;
            resolutionDropdown.RefreshShownValue();
        }

        CheckPendingChanges();
    }

    private string GetAspectRatioString(int width, int height)
    {
        int gcd = GetGCD(width, height);
        int aspectWidth = width / gcd;
        int aspectHeight = height / gcd;

        // Điều chỉnh các tỉ lệ đặc biệt
        if (aspectWidth == 8 && aspectHeight == 5) return "16:10";
        if (aspectWidth == 43 && aspectHeight == 18) return "21:9";
        if (aspectWidth == 64 && aspectHeight == 27) return "21:9";

        return $"{aspectWidth}:{aspectHeight}";
    }

    private int GetGCD(int a, int b)
    {
        while (b != 0)
        {
            int temp = b;
            b = a % b;
            a = temp;
        }
        return a;
    }

    private void InitializeResolutionDropdown()
    {
        if (resolutionDropdown == null) return;

        bool isWebGL = false;
#if UNITY_WEBGL
        isWebGL = true;
#endif
#if UNITY_EDITOR
        MainMenuController mainMenu = Object.FindAnyObjectByType<MainMenuController>();
        if (mainMenu != null && mainMenu.simulateWebGLInEditor)
        {
            isWebGL = true;
        }
#endif

        if (isWebGL)
        {
            resolutionDropdown.gameObject.SetActive(false);
            
            Transform parentRow = resolutionDropdown.transform.parent;
            if (parentRow != null)
            {
                string pName = parentRow.name.ToLower();
                if (pName.Contains("res") || pName.Contains("dophan") || pName.Contains("phân giải") || pName.Contains("row"))
                {
                    parentRow.gameObject.SetActive(false);
                }
                else
                {
                    TMP_Text labelText = parentRow.GetComponentInChildren<TMP_Text>();
                    if (labelText != null && (labelText.text.Contains("phân giải") || labelText.text.Contains("Resolution")))
                    {
                        labelText.gameObject.SetActive(false);
                    }
                }
            }
            return;
        }

        // 1. Tăng độ nhạy cuộn chuột (Scroll Sensitivity) của Dropdown
        ScrollRect scrollRect = resolutionDropdown.GetComponentInChildren<ScrollRect>(true);
        if (scrollRect != null)
        {
            scrollRect.scrollSensitivity = 25f; // Mặc định của Unity là 1f (rất chậm), 25f giúp cuộn mượt mà
        }

        // 2. Xác định độ phân giải cao nhất được hỗ trợ bởi màn hình hiện tại
        int maxSupportedWidth = Screen.currentResolution.width;
        int maxSupportedHeight = Screen.currentResolution.height;
        if (Screen.resolutions != null && Screen.resolutions.Length > 0)
        {
            foreach (var res in Screen.resolutions)
            {
                if (res.width > maxSupportedWidth) maxSupportedWidth = res.width;
                if (res.height > maxSupportedHeight) maxSupportedHeight = res.height;
            }
        }

        // 3. Lọc danh sách Custom Resolutions để chỉ giữ lại các cấu hình màn hình hỗ trợ được
        activeResolutions.Clear();
        foreach (var customRes in customResolutions)
        {
            if (customRes.width <= maxSupportedWidth && customRes.height <= maxSupportedHeight)
            {
                CustomResolution updatedRes = customRes;
                updatedRes.label = $"{customRes.width} x {customRes.height}";
                activeResolutions.Add(updatedRes);
            }
        }

        // 4. Đảm bảo độ phân giải hiện hành của máy người dùng luôn có mặt
        bool currentResExists = false;
        int currentW = Screen.width;
        int currentH = Screen.height;
        foreach (var res in activeResolutions)
        {
            if (res.width == currentW && res.height == currentH)
            {
                currentResExists = true;
                break;
            }
        }

        if (!currentResExists)
        {
            CustomResolution nativeRes = new CustomResolution
            {
                width = currentW,
                height = currentH,
                label = $"{currentW} x {currentH}"
            };
            activeResolutions.Add(nativeRes);
        }

        // 5. Sắp xếp danh sách giảm dần theo chiều rộng để hiển thị trực quan
        activeResolutions.Sort((a, b) => b.width.CompareTo(a.width));

        // 6. Cập nhật các tùy chọn vào UI Dropdown
        resolutionDropdown.ClearOptions();
        List<string> options = new List<string>();
        int currentResolutionIndex = 0;

        for (int i = 0; i < activeResolutions.Count; i++)
        {
            options.Add(activeResolutions[i].label);

            if (activeResolutions[i].width == currentW &&
                activeResolutions[i].height == currentH)
            {
                currentResolutionIndex = i;
            }
        }

        resolutionDropdown.AddOptions(options);

        // 7. Đồng bộ với cài đặt đã lưu
        int savedW = PlayerPrefs.GetInt("SavedResWidth", currentW);
        int savedH = PlayerPrefs.GetInt("SavedResHeight", currentH);

        int savedResIndex = currentResolutionIndex;
        for (int i = 0; i < activeResolutions.Count; i++)
        {
            if (activeResolutions[i].width == savedW && activeResolutions[i].height == savedH)
            {
                savedResIndex = i;
                break;
            }
        }

        if (savedResIndex < activeResolutions.Count)
        {
            pendingResolutionIndex = savedResIndex;
            resolutionDropdown.value = savedResIndex;
            resolutionDropdown.RefreshShownValue();

            // Load fullscreen state directly from PlayerPrefs to prevent uninitialized false values from resetting window mode
            pendingFullscreen = PlayerPrefs.GetInt("Fullscreen", Screen.fullScreen ? 1 : 0) == 1;
            FullScreenMode mode = pendingFullscreen ? FullScreenMode.FullScreenWindow : FullScreenMode.Windowed;
            if (Screen.width != activeResolutions[savedResIndex].width || Screen.height != activeResolutions[savedResIndex].height || Screen.fullScreenMode != mode)
            {
                Screen.SetResolution(activeResolutions[savedResIndex].width, activeResolutions[savedResIndex].height, mode);
            }
        }

        CheckPendingChanges();
    }

    private GameObject FindGraphicsTabPanel()
    {
        if (resolutionDropdown != null) return resolutionDropdown.transform.parent.gameObject;
        if (fullscreenToggle != null) return fullscreenToggle.transform.parent.gameObject;
        if (qualityDropdown != null) return qualityDropdown.transform.parent.gameObject;

        return FindObjectIncludingInactive("Panel_Graphics_Content");
    }

    private GameObject FindObjectIncludingInactive(string name)
    {
        var rootObjects = UnityEngine.SceneManagement.SceneManager.GetActiveScene().GetRootGameObjects();
        foreach (var root in rootObjects)
        {
            Transform[] allChildren = root.GetComponentsInChildren<Transform>(true);
            foreach (var child in allChildren)
            {
                if (child.name.Trim() == name)
                {
                    return child.gameObject;
                }
            }
        }
        return null;
    }

    private void DefaultToAudioTab()
    {
        GameObject audioPanel = FindObjectIncludingInactive("Panel_Audio_Content");
        GameObject graphicsPanel = FindObjectIncludingInactive("Panel_Graphics_Content");
        GameObject controlsPanel = FindObjectIncludingInactive("Panel_Controls_Content");

        if (audioPanel != null) audioPanel.SetActive(true);
        if (graphicsPanel != null) graphicsPanel.SetActive(false);
        if (controlsPanel != null) controlsPanel.SetActive(false);

        // Kích hoạt click giả lập trên Tab Âm Thanh để cập nhật UI/Màu sắc của Tab
        GameObject tabNav = FindObjectIncludingInactive("Tab_Navigation");
        if (tabNav != null)
        {
            Button audioTabBtn = tabNav.transform.Find("Btn_Audio")?.GetComponent<Button>() ??
                                 tabNav.transform.Find("AudioTab")?.GetComponent<Button>() ??
                                 tabNav.transform.Find("Btn_Tab_Audio")?.GetComponent<Button>();
            
            if (audioTabBtn == null)
            {
                Button[] buttons = tabNav.GetComponentsInChildren<Button>(true);
                foreach (var btn in buttons)
                {
                    string btnName = btn.name.ToLower();
                    if (btnName.Contains("audio") || btnName.Contains("amthanh") || btnName.Contains("âm thanh"))
                    {
                        audioTabBtn = btn;
                        break;
                    }
                }
            }

            if (audioTabBtn != null)
            {
                audioTabBtn.onClick.Invoke();
            }
        }
    }

    private void CreateApplyButtonProgrammatically()
    {
        if (applyButton != null) return;

        GameObject settingsPanel = FindObjectIncludingInactive("Panel_Settings");
        if (settingsPanel == null) return;

        Button existingApplyButton = null;
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
                }
            }
        }

        GameObject applyGo = null;
        if (existingApplyButton != null)
        {
            applyButton = existingApplyButton;
            applyGo = existingApplyButton.gameObject;
        }
        else
        {
            Transform graphicsContentParent = null;
            if (resolutionDropdown != null) graphicsContentParent = resolutionDropdown.transform.parent;
            else if (fullscreenToggle != null) graphicsContentParent = fullscreenToggle.transform.parent;
            else if (qualityDropdown != null) graphicsContentParent = qualityDropdown.transform.parent;

            if (graphicsContentParent == null) return;

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

            if (backButton != null)
            {
                applyGo = Instantiate(backButton.gameObject, graphicsContentParent);
                applyGo.name = "ApplyButton";
                applyButton = applyGo.GetComponent<Button>();

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
                    if (fullscreenToggle != null)
                    {
                        RectTransform toggleRT = fullscreenToggle.GetComponent<RectTransform>();
                        // Fallback position logic
                        applyRT.anchoredPosition = toggleRT != null ? toggleRT.anchoredPosition + new Vector2(0f, -65f) : new Vector2(0f, -100f);
                    }
                    else
                    {
                        applyRT.anchoredPosition = new Vector2(0f, -100f);
                    }
                }
            }
        }

        if (applyButton != null)
        {
            // Reset onClick hoàn toàn để xoá bỏ listener Quay Lại được clone từ Inspector
            applyButton.onClick = new Button.ButtonClickedEvent();
            applyButton.onClick.AddListener(ApplyGraphicsSettings);

            TMP_Text tmpTxt = applyButton.GetComponentInChildren<TMP_Text>();
            if (tmpTxt != null) tmpTxt.text = "ÁP DỤNG";

            Text legacyText = applyButton.GetComponentInChildren<Text>();
            if (legacyText != null) legacyText.text = "ÁP DỤNG";

            CheckPendingChanges();
        }
    }

    // ==========================================
    // 3. CÀI ĐẶT ĐIỀU KHIỂN (CONTROLS SETTINGS)
    // ==========================================
    public void SetSensitivity(float sensitivity)
    {
        PlayerPrefs.SetFloat("MouseSensitivity", sensitivity);
        PlayerPrefs.Save();

        // Cập nhật real-time độ nhạy chuột của camera
        CameraController camCtrl = Object.FindAnyObjectByType<CameraController>();
        if (camCtrl != null)
        {
            camCtrl.UpdateSensitivitySettings();
        }
    }

    // ==========================================
    // 4. LOAD & ĐỒNG BỘ TRẠNG THÁI UI (INITIALIZE)
    // ==========================================
    private void LoadSettings()
    {
        float musicVol = PlayerPrefs.GetFloat("MusicVolume", 1.0f);
        float sfxVol = PlayerPrefs.GetFloat("SFXVolume", 1.0f);
        float voiceVol = PlayerPrefs.GetFloat("VoiceVolume", 1.0f);

        AudioListener.volume = musicVol;

        if (musicVolumeSlider != null)
        {
            musicVolumeSlider.value = musicVol;
            UpdateMusicVolumeText(musicVol);
        }
        if (sfxVolumeSlider != null)
        {
            sfxVolumeSlider.value = sfxVol;
            UpdateSFXVolumeText(sfxVol);
        }
        if (voiceVolumeSlider != null)
        {
            voiceVolumeSlider.value = voiceVol;
            UpdateVoiceVolumeText(voiceVol);
        }

        pendingQualityIndex = PlayerPrefs.GetInt("QualityIndex", QualitySettings.GetQualityLevel());
        QualitySettings.SetQualityLevel(pendingQualityIndex);
        if (qualityDropdown != null)
        {
            qualityDropdown.value = pendingQualityIndex;
            qualityDropdown.RefreshShownValue();
        }

        pendingFullscreen = PlayerPrefs.GetInt("Fullscreen", Screen.fullScreen ? 1 : 0) == 1;
        FullScreenMode mode = pendingFullscreen ? FullScreenMode.FullScreenWindow : FullScreenMode.Windowed;
        if (Screen.fullScreenMode != mode)
        {
            Screen.fullScreenMode = mode;
        }
        if (fullscreenToggle != null) fullscreenToggle.isOn = pendingFullscreen;

        float sens = PlayerPrefs.GetFloat("MouseSensitivity", 3.0f);
        if (sensitivitySlider != null) sensitivitySlider.value = sens;
    }
}
