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

    private void Start()
    {
        carController = GetComponent<CarController>();
        
        // Đồng bộ âm lượng SFX từ cài đặt
        float masterSFX = PlayerPrefs.GetFloat("SFXVolume", 0.8f);
        minVolume *= masterSFX;
        maxVolume *= masterSFX;

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
            startupSource.volume *= masterSFX;
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

        // Lấy vận tốc tuyệt đối (km/h) từ xe
        float speed = Mathf.Abs(carController.GetLocalForwardVelocity() * 3.6f);
        
        // Tính tỷ lệ tốc độ từ 0 đến 1
        float speedRatio = Mathf.Clamp01(speed / maxSpeedForPitch);

        // 1. Điều chỉnh Pitch (độ cao tần số) của âm thanh loop
        // Pitch tăng dần từ minPitch đến maxPitch tương ứng với tốc độ xe chạy
        engineLoopSource.pitch = Mathf.Lerp(minPitch, maxPitch, speedRatio);

        // 2. Điều chỉnh Volume (âm lượng) dựa trên việc nhấn ga
        // Khi người chơi ấn phím tiến (Vertical input > 0), âm lượng tăng mạnh để mô phỏng đạp ga
        float accelInput = Mathf.Clamp01(Input.GetAxis("Vertical"));
        float targetVolume = Mathf.Lerp(minVolume, maxVolume, accelInput * 0.4f + speedRatio * 0.6f);
        
        // Làm mượt sự thay đổi âm lượng giữa các frame
        engineLoopSource.volume = Mathf.MoveTowards(engineLoopSource.volume, targetVolume, Time.deltaTime * 2.5f);
    }
}
