using UnityEngine;

[RequireComponent(typeof(CarController))]
public class CarAudio : MonoBehaviour
{
    [Header("Audio Sources")]
    [Tooltip("AudioSource để phát âm thanh nổ máy (One-shot)")]
    public AudioSource startupSource;
    [Tooltip("AudioSource để phát âm thanh động cơ chạy lặp (Loop)")]
    public AudioSource engineLoopSource;

    [Header("Audio Clips")]
    public AudioClip engineStartClip;
    public AudioClip engineLoopClip;

    [Header("Pitch Settings")]
    [Tooltip("Độ cao âm thanh tối thiểu (khi xe nổ máy không di chuyển)")]
    public float minPitch = 0.8f;
    [Tooltip("Độ cao âm thanh tối đa (khi xe chạy tốc độ tối đa)")]
    public float maxPitch = 2.2f;
    [Tooltip("Tốc độ xe (km/h) đạt tới để đạt pitch tối đa")]
    public float maxSpeedForPitch = 60f; 

    [Header("Volume Settings")]
    [Tooltip("Âm lượng tối thiểu khi nổ máy không nhấn ga")]
    public float minVolume = 0.25f;
    [Tooltip("Âm lượng tối đa khi nhấn ga kịch kim")]
    public float maxVolume = 0.8f;

    private CarController carController;
    private bool wasEngineRunningLastFrame = false;

    private float baseMinVolume;
    private float baseMaxVolume;
    private float baseStartupVolume;

    private void Start()
    {
        carController = GetComponent<CarController>();
        
        // Lưu lại âm lượng cơ sở ban đầu từ Inspector để phục vụ cập nhật real-time
        baseMinVolume = minVolume;
        baseMaxVolume = maxVolume;
        if (startupSource != null)
        {
            baseStartupVolume = startupSource.volume;
        }

        // Cấu hình nguồn phát lặp
        if (engineLoopSource != null)
        {
            engineLoopSource.clip = engineLoopClip;
            engineLoopSource.loop = true;
            engineLoopSource.playOnAwake = false;
        }

        // Cấu hình nguồn phát nổ máy
        if (startupSource != null)
        {
            startupSource.playOnAwake = false;
            startupSource.loop = false;
        }

        UpdateVolumeSettings();
    }

    public void UpdateVolumeSettings()
    {
        float masterSFX = PlayerPrefs.GetFloat("SFXVolume", 0.8f);
        minVolume = baseMinVolume * masterSFX;
        maxVolume = baseMaxVolume * masterSFX;
        if (startupSource != null)
        {
            startupSource.volume = baseStartupVolume * masterSFX;
        }
    }

    private void Update()
    {
        if (carController == null) return;

        bool isEngineOn = carController.isEngineOn;

        // Phát hiện sự thay đổi trạng thái nổ/tắt máy
        if (isEngineOn && !wasEngineRunningLastFrame)
        {
            StartEngine();
        }
        else if (!isEngineOn && wasEngineRunningLastFrame)
        {
            StopEngine();
        }

        if (isEngineOn)
        {
            UpdateEngineSound();
        }

        wasEngineRunningLastFrame = isEngineOn;
    }

    private void StartEngine()
    {
        if (startupSource != null && engineStartClip != null)
        {
            startupSource.clip = engineStartClip;
            startupSource.Play();
            
            // Chạy âm thanh loop sau khi nổ máy gần xong (độ trễ khoảng 85% thời lượng clip nổ máy)
            Invoke("PlayLoopDelayed", engineStartClip.length * 0.85f);
        }
        else
        {
            PlayLoopDelayed();
        }
    }

    private void PlayLoopDelayed()
    {
        if (carController != null && carController.isEngineOn && engineLoopSource != null && engineLoopClip != null)
        {
            if (!engineLoopSource.isPlaying)
            {
                engineLoopSource.Play();
            }
        }
    }

    private void StopEngine()
    {
        CancelInvoke("PlayLoopDelayed");
        if (startupSource != null) startupSource.Stop();
        if (engineLoopSource != null) engineLoopSource.Stop();
    }

    private void UpdateEngineSound()
    {
        if (engineLoopSource == null || !engineLoopSource.isPlaying) return;

        // Đọc thông số RPM trực tiếp từ hộp số tự động của CarController
        float rpm = carController.engineRPM;
        float maxRPM = carController.maxRPM;
        float minRPM = carController.minRPM;

        // Tính tỷ lệ vòng tua máy từ 0 đến 1
        float rpmRatio = Mathf.Clamp01((rpm - minRPM) / (maxRPM - minRPM));

        // 1. Điều chỉnh Pitch (độ cao tần số) theo vòng tua máy (RPM)
        // Độ cao sẽ nhấp nhô tăng giảm sinh động khi xe sang số (D1 -> D2 -> D3...)
        engineLoopSource.pitch = Mathf.Lerp(minPitch, maxPitch, rpmRatio);

        // 2. Điều chỉnh Volume (âm lượng) dựa trên ga (accelInput) và vòng tua máy
        float accelInput = Mathf.Clamp01(Input.GetAxis("Vertical"));
        float targetVolume = Mathf.Lerp(minVolume, maxVolume, accelInput * 0.5f + rpmRatio * 0.5f);
        
        // Làm mượt sự thay đổi âm lượng giữa các frame
        engineLoopSource.volume = Mathf.MoveTowards(engineLoopSource.volume, targetVolume, Time.deltaTime * 3.0f);
    }
}
