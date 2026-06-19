using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class SettingsManager : MonoBehaviour
{
    public static SettingsManager Instance { get; private set; }

    [Header("Audio UI Elements")]
    public Slider musicVolumeSlider;
    public Slider sfxVolumeSlider;
    public Slider voiceVolumeSlider;

    [Header("Graphics UI Elements")]
    public TMP_Dropdown qualityDropdown;
    public Toggle fullscreenToggle;
    public TMP_Dropdown resolutionDropdown;

    [Header("Controls UI Elements")]
    public Slider sensitivitySlider;

    private Resolution[] resolutions;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        LoadSettings();
        InitializeResolutionDropdown();
    }

    // ==========================================
    // 1. CÀI ĐẶT ÂM THANH (AUDIO SETTINGS)
    // ==========================================
    public void SetMusicVolume(float volume)
    {
        PlayerPrefs.SetFloat("MusicVolume", volume);
        PlayerPrefs.Save();
        // Áp dụng âm lượng nhạc nền ở đây nếu có hệ thống MusicManager
    }

    public void SetSFXVolume(float volume)
    {
        PlayerPrefs.SetFloat("SFXVolume", volume);
        PlayerPrefs.Save();
        // CarAudio hoặc các nguồn âm thanh hiệu ứng khác sẽ đọc giá trị này
    }

    public void SetVoiceVolume(float volume)
    {
        PlayerPrefs.SetFloat("VoiceVolume", volume);
        PlayerPrefs.Save();
        // ExamManager sẽ đọc giá trị này để điều chỉnh âm lượng giọng đọc
    }

    // ==========================================
    // 2. CÀI ĐẶT ĐỒ HỌA (GRAPHICS SETTINGS)
    // ==========================================
    public void SetQuality(int qualityIndex)
    {
        QualitySettings.SetQualityLevel(qualityIndex);
        PlayerPrefs.SetInt("QualityIndex", qualityIndex);
        PlayerPrefs.Save();
    }

    public void SetFullscreen(bool isFullscreen)
    {
        Screen.fullScreen = isFullscreen;
        PlayerPrefs.SetInt("Fullscreen", isFullscreen ? 1 : 0);
        PlayerPrefs.Save();
    }

    private void InitializeResolutionDropdown()
    {
        if (resolutionDropdown == null) return;

        resolutions = Screen.resolutions;
        resolutionDropdown.ClearOptions();

        List<string> options = new List<string>();
        int currentResolutionIndex = 0;

        for (int i = 0; i < resolutions.Length; i++)
        {
            string option = resolutions[i].width + " x " + resolutions[i].height + " @" + resolutions[i].refreshRateRatio.value.ToString("0") + "Hz";
            options.Add(option);

            if (resolutions[i].width == Screen.currentResolution.width &&
                resolutions[i].height == Screen.currentResolution.height)
            {
                currentResolutionIndex = i;
            }
        }

        resolutionDropdown.AddOptions(options);

        // Đọc cấu hình độ phân giải đã lưu
        int savedResIndex = PlayerPrefs.GetInt("ResolutionIndex", currentResolutionIndex);
        if (savedResIndex < resolutions.Length)
        {
            resolutionDropdown.value = savedResIndex;
            resolutionDropdown.RefreshShownValue();
            SetResolution(savedResIndex);
        }
    }

    public void SetResolution(int resolutionIndex)
    {
        if (resolutions == null || resolutionIndex >= resolutions.Length) return;

        Resolution resolution = resolutions[resolutionIndex];
        Screen.SetResolution(resolution.width, resolution.height, Screen.fullScreen);
        
        PlayerPrefs.SetInt("ResolutionIndex", resolutionIndex);
        PlayerPrefs.Save();
    }

    // ==========================================
    // 3. CÀI ĐẶT ĐIỀU KHIỂN (CONTROLS SETTINGS)
    // ==========================================
    public void SetSensitivity(float sensitivity)
    {
        PlayerPrefs.SetFloat("MouseSensitivity", sensitivity);
        PlayerPrefs.Save();
        // CameraController sẽ đọc giá trị này để điều chỉnh độ nhạy xoay chuột
    }

    // ==========================================
    // 4. LOAD & ĐỒNG BỘ TRẠNG THÁI UI (INITIALIZE)
    // ==========================================
    private void LoadSettings()
    {
        // Load Audio
        float musicVol = PlayerPrefs.GetFloat("MusicVolume", 0.75f);
        float sfxVol = PlayerPrefs.GetFloat("SFXVolume", 0.8f);
        float voiceVol = PlayerPrefs.GetFloat("VoiceVolume", 1.0f);

        if (musicVolumeSlider != null) musicVolumeSlider.value = musicVol;
        if (sfxVolumeSlider != null) sfxVolumeSlider.value = sfxVol;
        if (voiceVolumeSlider != null) voiceVolumeSlider.value = voiceVol;

        // Load Graphics Quality
        int qualityIndex = PlayerPrefs.GetInt("QualityIndex", QualitySettings.GetQualityLevel());
        QualitySettings.SetQualityLevel(qualityIndex);
        if (qualityDropdown != null)
        {
            qualityDropdown.value = qualityIndex;
            qualityDropdown.RefreshShownValue();
        }

        // Load Fullscreen
        bool isFullscreen = PlayerPrefs.GetInt("Fullscreen", Screen.fullScreen ? 1 : 0) == 1;
        Screen.fullScreen = isFullscreen;
        if (fullscreenToggle != null) fullscreenToggle.isOn = isFullscreen;

        // Load Sensitivity
        float sens = PlayerPrefs.GetFloat("MouseSensitivity", 3.0f);
        if (sensitivitySlider != null) sensitivitySlider.value = sens;
    }
}
