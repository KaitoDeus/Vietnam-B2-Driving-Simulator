using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class HUDController : MonoBehaviour
{
    [Header("Top Left Panel")]
    public TMP_Text scoreText;
    public TMP_Text timerText;

    [Header("Top Right Panel")]
    public TMP_Text stepTitleText;
    public TMP_Text stepDescText;

    [Header("Bottom Left (Dashboard)")]
    public TMP_Text carNumberText;
    public TMP_Text leftBlinkerText;
    public TMP_Text rightBlinkerText;
    public TMP_Text lowBeamText;
    public TMP_Text highBeamText;
    public TMP_Text hazardText;

    [Header("Bottom Right (Speedometer)")]
    public RectTransform speedometerNeedle;
    public TMP_Text digitalSpeedText;
    public float maxSpeed = 100f; // Vận tốc tối đa hiển thị trên đồng hồ
    public float zeroSpeedAngle = 120f;  // Góc quay của kim khi tốc độ = 0
    public float maxSpeedAngle = -120f; // Góc quay của kim khi tốc độ = maxSpeed

    [Header("Pause Menu")]
    public GameObject pausePanel;
    public GameObject settingsPanel;
    private bool isPaused = false;

    [Header("Notifications")]
    public NotificationController notificationPanel;

    [Header("Colors (Aesthetics)")]
    public Color activeColor = new Color(0.12f, 0.8f, 0.2f);      // Xanh lá sáng
    public Color inactiveColor = new Color(0.4f, 0.4f, 0.4f);    // Xám
    public Color warningColor = new Color(1.0f, 0.6f, 0.0f);     // Cam/Vàng xi-nhan
    public Color blueLightColor = new Color(0.0f, 0.5f, 1.0f);    // Xanh dương đèn pha
    public Color errorColor = new Color(1.0f, 0.2f, 0.2f);        // Đỏ lỗi/cảnh báo

    private CarController targetCar;

    private void Awake()
    {
        // Tự động tìm kiếm các tham chiếu UI tại runtime nếu chưa được gán trong Inspector
        Transform canvasTrans = transform;
        
        if (scoreText == null) scoreText = FindComponentInDescendants<TMP_Text>(canvasTrans, "Txt_ScoreVal");
        if (timerText == null) timerText = FindComponentInDescendants<TMP_Text>(canvasTrans, "Txt_TimeVal");
        
        if (stepTitleText == null) stepTitleText = FindComponentInDescendants<TMP_Text>(canvasTrans, "Txt_StepTitle");
        if (stepDescText == null) stepDescText = FindComponentInDescendants<TMP_Text>(canvasTrans, "Txt_StepDesc");
        
        if (carNumberText == null)
        {
            carNumberText = FindComponentInDescendants<TMP_Text>(canvasTrans, "Txt_CarNumber");
            if (carNumberText == null) carNumberText = FindComponentInDescendants<TMP_Text>(canvasTrans, "Badge_CarNumber");
        }
        
        if (leftBlinkerText == null) leftBlinkerText = FindComponentInDescendants<TMP_Text>(canvasTrans, "Txt_BlinkerL");
        if (rightBlinkerText == null) rightBlinkerText = FindComponentInDescendants<TMP_Text>(canvasTrans, "Txt_BlinkerR");
        if (lowBeamText == null) lowBeamText = FindComponentInDescendants<TMP_Text>(canvasTrans, "Txt_LightCos");
        if (highBeamText == null) highBeamText = FindComponentInDescendants<TMP_Text>(canvasTrans, "Txt_LightPha");
        if (hazardText == null) hazardText = FindComponentInDescendants<TMP_Text>(canvasTrans, "Txt_Hazard");
        
        if (pausePanel == null)
        {
            Transform pauseTrans = FindDeepChild(canvasTrans, "Panel_Pause");
            if (pauseTrans != null) pausePanel = pauseTrans.gameObject;
        }

        if (settingsPanel == null)
        {
            Transform settingsTrans = FindDeepChild(canvasTrans, "Panel_Settings");
            if (settingsTrans != null) settingsPanel = settingsTrans.gameObject;
        }

        if (notificationPanel == null)
        {
            Transform notifTrans = FindDeepChild(canvasTrans, "Panel_Notification");
            if (notifTrans != null)
            {
                notificationPanel = notifTrans.GetComponent<NotificationController>();
            }
            else
            {
                Transform rootTrans = canvasTrans.Find("HUD_Root");
                Transform parentTrans = (rootTrans != null) ? rootTrans : canvasTrans;
                notificationPanel = NotificationController.Create(parentTrans);
            }
        }
    }

    private T FindComponentInDescendants<T>(Transform parent, string name) where T : Component
    {
        Transform child = FindDeepChild(parent, name);
        if (child != null)
        {
            return child.GetComponent<T>();
        }
        return null;
    }

    private Transform FindDeepChild(Transform parent, string name)
    {
        if (parent.name == name) return parent;
        for (int i = 0; i < parent.childCount; i++)
        {
            Transform result = FindDeepChild(parent.GetChild(i), name);
            if (result != null) return result;
        }
        return null;
    }

    private void Start()
    {
        FindCarInstance();
        
        if (pausePanel != null)
        {
            pausePanel.SetActive(false);
        }

        if (settingsPanel != null)
        {
            settingsPanel.SetActive(false);
        }
    }

    private void Update()
    {
        // Tự động tìm xe nếu xe hiện tại bị mất hoặc chưa gán
        if (targetCar == null)
        {
            FindCarInstance();
        }

        UpdateExamStats();
        UpdateDashboard();
        UpdateSpeedometer();

        // Phím ESC để Pause nhanh
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (isPaused && settingsPanel != null && settingsPanel.activeSelf)
            {
                CloseSettings();
            }
            else
            {
                TogglePause();
            }
        }
    }

    private void FindCarInstance()
    {
        targetCar = FindFirstObjectByType<CarController>();
    }

    private void UpdateExamStats()
    {
        ExamManager em = ExamManager.Instance;
        if (em == null) return;

        // Cập nhật Điểm số
        if (scoreText != null)
        {
            scoreText.text = em.currentScore.ToString();
            // Đổi màu điểm số: Dưới 80 điểm (báo động đỏ)
            scoreText.color = em.currentScore >= 80 ? activeColor : errorColor;
        }

        // Cập nhật Thời gian còn lại (MM:SS)
        if (timerText != null)
        {
            int minutes = Mathf.FloorToInt(em.totalExamTime / 60F);
            int seconds = Mathf.FloorToInt(em.totalExamTime % 60F);
            timerText.text = string.Format("{0:00}:{1:00}", minutes, seconds);

            // Đổi màu thời gian: Dưới 1 phút chữ hóa đỏ
            timerText.color = em.totalExamTime > 60f ? Color.white : errorColor;
        }

        // Cập nhật Bài thi hiện tại và mô tả
        UpdateStepTexts(em.currentStep);
    }

    private void UpdateStepTexts(ExamStep step)
    {
        if (stepTitleText == null || stepDescText == null) return;

        switch (step)
        {
            case ExamStep.None:
                stepTitleText.text = "Chuẩn bị thi";
                stepDescText.text = "Thắt dây an toàn, nổ máy (phím I) và chờ hiệu lệnh xuất phát.";
                break;
            case ExamStep.XuatPhat:
                stepTitleText.text = "Bài 1: Xuất phát";
                stepDescText.text = "Vào số 1 (phím 1) và cho xe di chuyển qua vạch xuất phát.";
                break;
            case ExamStep.DungNhuongDuongDiBo:
                stepTitleText.text = "Bài 2: Dừng xe nhường đường";
                stepDescText.text = "Dừng xe trước vạch trắng nhường đường cho người đi bộ (khoảng cách quy định).";
                break;
            case ExamStep.DungAndKhoiHanhNgangDoc:
                stepTitleText.text = "Bài 3: Dừng & Khởi hành ngang dốc";
                stepDescText.text = "Dừng xe trên dốc đúng vị trí, không trôi dốc và khởi hành qua dốc trong 30 giây.";
                break;
            case ExamStep.VetBanhXeAndDuongVuongGoc:
                stepTitleText.text = "Bài 4: Qua vệt bánh xe";
                stepDescText.text = "Lái bánh xe bên phụ đi qua vệt bánh xe và đường hẹp vuông góc. Tránh đè vạch.";
                break;
            case ExamStep.QuaNgaTuDenTinHieu:
                stepTitleText.text = "Bài 5: Qua ngã tư có đèn";
                stepDescText.text = "Dừng trước vạch khi đèn đỏ, di chuyển qua ngã tư khi đèn xanh và tuân thủ xi-nhan.";
                break;
            case ExamStep.DuongVongQuanhCo:
                stepTitleText.text = "Bài 6: Đường vòng quanh co";
                stepDescText.text = "Lái xe đi qua đường chữ S uốn lượn liên tục mà không đè lên vạch giới hạn.";
                break;
            case ExamStep.GhepDocVaoNoiDo:
                stepTitleText.text = "Bài 7: Ghép dọc vào nơi đỗ";
                stepDescText.text = "Lùi xe ghép dọc vào chuồng dọc, nghe tín hiệu nhận bài rồi đánh xe đi ra ngoài.";
                break;
            case ExamStep.TamDungNoiDuongSat:
                stepTitleText.text = "Bài 8: Tạm dừng nơi đường sắt";
                stepDescText.text = "Dừng xe đúng khoảng cách trước vạch giới hạn có đường sắt chạy qua.";
                break;
            case ExamStep.ThayDoiSoDuongBang:
                stepTitleText.text = "Bài 9: Thay đổi số trên đường bằng";
                stepDescText.text = "Tăng lên số 2 (tốc độ > 24 km/h) và giảm về số 1 (tốc độ < 20 km/h) đúng biển báo.";
                break;
            case ExamStep.GhepNgangVaoNoiDo:
                stepTitleText.text = "Bài 10: Ghép xe ngang vào nơi đỗ";
                stepDescText.text = "Ghép xe song song vào chuồng ngang bên lề đường, nghe tín hiệu nhận bài rồi đi ra.";
                break;
            case ExamStep.KetThuc:
                stepTitleText.text = "Bài 11: Kết thúc";
                stepDescText.text = "Bật xi-nhan phải (E) trước khi lái xe đi qua vạch kết thúc bài thi sát hạch.";
                break;
        }
    }

    private void UpdateDashboard()
    {
        if (targetCar == null) return;

        // Cập nhật hiển thị hộp số/gear (D -> 1, N -> N, R -> R) ở ô kế bên Số xe
        if (carNumberText != null)
        {
            switch (targetCar.currentGear)
            {
                case CarController.GearState.N:
                    carNumberText.text = "N";
                    break;
                case CarController.GearState.D:
                    carNumberText.text = "1";
                    break;
                case CarController.GearState.R:
                    carNumberText.text = "R";
                    break;
            }
        }

        // Tính toán trạng thái nhấp nháy (Blink) bằng tần số 0.5s
        bool isBlinking = (Time.time % 1.0f) < 0.5f;

        // 1. Xi-nhan trái
        if (leftBlinkerText != null)
        {
            if (targetCar.isLeftBlinkerOn || targetCar.isHazardOn)
            {
                leftBlinkerText.color = isBlinking ? warningColor : inactiveColor;
            }
            else
            {
                leftBlinkerText.color = inactiveColor;
            }
        }

        // 2. Xi-nhan phải
        if (rightBlinkerText != null)
        {
            if (targetCar.isRightBlinkerOn || targetCar.isHazardOn)
            {
                rightBlinkerText.color = isBlinking ? warningColor : inactiveColor;
            }
            else
            {
                rightBlinkerText.color = inactiveColor;
            }
        }

        // 3. Đèn Pha
        if (highBeamText != null)
        {
            highBeamText.color = targetCar.isHighBeamOn ? blueLightColor : inactiveColor;
        }

        // 4. Đèn Cos
        if (lowBeamText != null)
        {
            lowBeamText.color = targetCar.isLowBeamOn ? activeColor : inactiveColor;
        }

        // 5. Khẩn cấp
        if (hazardText != null)
        {
            if (targetCar.isHazardOn)
            {
                hazardText.color = isBlinking ? errorColor : inactiveColor;
            }
            else
            {
                hazardText.color = inactiveColor;
            }
        }
    }

    private void UpdateSpeedometer()
    {
        if (targetCar == null)
        {
            if (digitalSpeedText != null) digitalSpeedText.text = "0";
            if (speedometerNeedle != null) speedometerNeedle.localRotation = Quaternion.Euler(0, 0, zeroSpeedAngle);
            return;
        }

        float speed = targetCar.CurrentSpeed;
        
        // Cập nhật số phần trăm kỹ thuật số
        if (digitalSpeedText != null)
        {
            digitalSpeedText.text = Mathf.RoundToInt(speed).ToString();
        }

        // Xoay kim đồng hồ tốc độ
        if (speedometerNeedle != null)
        {
            float speedRatio = Mathf.Clamp01(speed / maxSpeed);
            float currentAngle = Mathf.Lerp(zeroSpeedAngle, maxSpeedAngle, speedRatio);
            speedometerNeedle.localRotation = Quaternion.Euler(0, 0, currentAngle);
        }
    }

    public void TogglePause()
    {
        isPaused = !isPaused;
        Time.timeScale = isPaused ? 0f : 1f;

        if (isPaused)
        {
            if (pausePanel != null) pausePanel.SetActive(true);
            if (settingsPanel != null) settingsPanel.SetActive(false);

            // Hiện con trỏ chuột khi tạm dừng
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
        else
        {
            if (pausePanel != null) pausePanel.SetActive(false);
            if (settingsPanel != null) settingsPanel.SetActive(false);

            // Ẩn con trỏ chuột khi tiếp tục game (nếu muốn)
            // Cursor.visible = false;
        }

        // Tạm dừng/Bật tiếp âm thanh thi sát hạch
        if (ExamManager.Instance != null)
        {
            if (ExamManager.Instance.voiceSource != null)
            {
                if (isPaused) ExamManager.Instance.voiceSource.Pause();
                else ExamManager.Instance.voiceSource.UnPause();
            }
            if (ExamManager.Instance.sfxSource != null)
            {
                if (isPaused) ExamManager.Instance.sfxSource.Pause();
                else ExamManager.Instance.sfxSource.UnPause();
            }
        }
    }

    // ==========================================
    // CÁC HÀM CLICK CHO BUTTON TRÊN PAUSE MENU
    // ==========================================

    public void ResumeGame()
    {
        if (isPaused)
        {
            TogglePause();
        }
    }

    public void RestartGame()
    {
        Time.timeScale = 1f;
        UnityEngine.SceneManagement.SceneManager.LoadScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene().buildIndex);
    }

    public void OpenSettings()
    {
        if (settingsPanel != null) settingsPanel.SetActive(true);
        if (pausePanel != null) pausePanel.SetActive(false);
    }

    public void CloseSettings()
    {
        if (settingsPanel != null) settingsPanel.SetActive(false);
        if (pausePanel != null) pausePanel.SetActive(true);
    }

    public void GoToMainMenu()
    {
        Time.timeScale = 1f;
        UnityEngine.SceneManagement.SceneManager.LoadScene("MainMenu");
    }

    // ==========================================
    // CÁC HÀM HIỂN THỊ THÔNG BÁO (NOTIFICATION)
    // ==========================================

    public void ShowNotification(string message, bool isSuccess, float duration = 3f)
    {
        if (notificationPanel != null)
        {
            var state = isSuccess ? NotificationController.NotificationState.Success : NotificationController.NotificationState.Warning;
            notificationPanel.Show(message, state, duration);
        }
        else
        {
            Debug.LogWarning($"[HUDController] Chưa gán notificationPanel! Tin nhắn: {message}");
        }
    }

    public void ShowWarningNotification(string message, float duration = 3f)
    {
        ShowNotification(message, false, duration);
    }

    public void ShowSuccessNotification(string message, float duration = 3f)
    {
        ShowNotification(message, true, duration);
    }
}
