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
    public TMP_Text seatbeltText;

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
    public ResultPanelController resultPanel;
    
    [Header("Countdown")]
    public TMP_Text countdownText;

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
        if (seatbeltText == null) seatbeltText = FindComponentInDescendants<TMP_Text>(canvasTrans, "Txt_Seatbelt");
        
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

        // Tìm kiếm các thực thể trong Canvas của Scene để tránh tham chiếu nhầm vào Prefab
        Transform notifTrans = FindDeepChild(canvasTrans, "Panel_Notification");
        if (notifTrans != null)
        {
            notificationPanel = notifTrans.GetComponent<NotificationController>();
        }
        else if (notificationPanel == null || !notificationPanel.gameObject.scene.IsValid())
        {
            Transform rootTrans = canvasTrans.Find("HUD_Root");
            Transform parentTrans = (rootTrans != null) ? rootTrans : canvasTrans;
            notificationPanel = NotificationController.Create(parentTrans);
        }

        Transform resultTrans = FindDeepChild(canvasTrans, "Panel_Result");
        if (resultTrans != null)
        {
            resultPanel = resultTrans.GetComponent<ResultPanelController>();
        }

        if (countdownText == null)
        {
            countdownText = FindComponentInDescendants<TMP_Text>(canvasTrans, "Txt_Countdown");
        }
        if (countdownText == null)
        {
            GameObject countdownGo = new GameObject("Txt_Countdown");
            countdownGo.transform.SetParent(canvasTrans, false);
            
            RectTransform rect = countdownGo.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = new Vector2(0f, 150f);
            rect.sizeDelta = new Vector2(300f, 100f);
            
            countdownText = countdownGo.AddComponent<TextMeshProUGUI>();
            countdownText.fontSize = 72;
            countdownText.fontWeight = FontWeight.Bold;
            countdownText.alignment = TextAlignmentOptions.Center;
            countdownText.color = errorColor;
            countdownText.text = "";
            countdownText.outlineColor = Color.black;
            countdownText.outlineWidth = 0.2f;
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

        // Đảm bảo âm thanh toàn cục không bị pause khi bắt đầu cảnh mới
        AudioListener.pause = false;

        // Hiển thị Popup hướng dẫn phím lái xe
        InstructionsPopup.Create(transform);
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
            if (InstructionsPopup.IsActive) return;

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
        targetCar = Object.FindAnyObjectByType<CarController>();
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

        // Hiển thị trạng thái xi-nhan xuất phát trên HUD (góc phải)
        if (em.currentStep == ExamStep.None)
        {
            if (targetCar != null)
            {
                if (targetCar.isLeftBlinkerOn)
                {
                    stepDescText.text = "<color=#1FA659><b>[ĐÃ BẬT XI-NHAN TRÁI]</b></color> Nổ máy (phím T) & di chuyển vào vạch xuất phát.";
                }
                else
                {
                    stepDescText.text = "Nổ máy (phím T) & <color=#FF9900><b>bật xi-nhan trái (phím Q)</b></color> trước khi xuất phát.";
                }
            }
        }
        else if (em.currentStep == ExamStep.XuatPhat)
        {
            if (targetCar != null)
            {
                if (!em.XuatPhatMovingChecked)
                {
                    if (targetCar.isLeftBlinkerOn)
                    {
                        stepDescText.text = "<color=#1FA659><b>[ĐÃ BẬT XI-NHAN TRÁI]</b></color> Nhấn ga (W) qua vạch xuất phát.";
                    }
                    else
                    {
                        stepDescText.text = "<color=#FF9900><b>[CẦN BẬT XI-NHAN TRÁI (Q)]</b></color> Bật xi-nhan trái trước khi xuất phát!";
                    }
                }
                else
                {
                    if (em.HasDeductedForNoBlinker)
                    {
                        stepDescText.text = "<color=#E74C3C><b>[BỊ TRỪ 5 ĐIỂM]</b></color> Quên bật xi-nhan trái khi xuất phát. Di chuyển sang Bài 2.";
                    }
                    else if (targetCar.isLeftBlinkerOn && !em.XuatPhatBlinkerOffChecked)
                    {
                        stepDescText.text = "<color=#0078D4><b>[ĐÃ QUA VẠCH XUẤT PHÁT]</b></color> Hãy <color=#FF9900><b>TẮT XI-NHAN TRÁI (phím Q)</b></color> để tiếp tục bài thi.";
                    }
                    else
                    {
                        stepDescText.text = "<color=#1FA659><b>[XUẤT PHÁT HỢP LỆ]</b></color> Tiến vào Bài 2: Dừng xe nhường đường.";
                    }
                }
            }
        }

        // Cập nhật nhắc nhở khu vực thi cho các bài có Vùng Trigger riêng (Bài 2, 3, 7, 8, 10)
        if (em.IsZoneRequiredForStep(em.currentStep) && !em.IsStepTimerActive)
        {
            switch (em.currentStep)
            {
                case ExamStep.DungNhuongDuongDiBo:
                    stepDescText.text = "<color=#FF9900><b>[Chờ vào vị trí]</b></color> Di chuyển vào <color=#0078D4>vùng vạch dừng đi bộ</color> để kích hoạt tính giờ.";
                    break;
                case ExamStep.DungAndKhoiHanhNgangDoc:
                    stepDescText.text = "<color=#FF9900><b>[Chờ vào vị trí]</b></color> Di chuyển lên <color=#0078D4>vùng vạch dừng trên dốc</color> để kích hoạt tính giờ.";
                    break;
                case ExamStep.GhepDocVaoNoiDo:
                    stepDescText.text = "<color=#FF9900><b>[Chờ vào vị trí]</b></color> Lùi xe vào <color=#0078D4>khu vực chuồng đỗ dọc</color> để kích hoạt đếm ngược 120s.";
                    break;
                case ExamStep.TamDungNoiDuongSat:
                    stepDescText.text = "<color=#FF9900><b>[Chờ vào vị trí]</b></color> Di chuyển tới <color=#0078D4>vùng dừng đường sắt</color> để kích hoạt tính giờ 30s.";
                    break;
                case ExamStep.GhepNgangVaoNoiDo:
                    stepDescText.text = "<color=#FF9900><b>[Chờ vào vị trí]</b></color> Lùi xe vào <color=#0078D4>khu vực chuồng đỗ ngang</color> để kích hoạt đếm ngược 120s.";
                    break;
            }
        }
        else
        {
            float limit = em.GetTimeLimitForStep(em.currentStep);
            float elapsed = Time.time - em.GetStepStartTime();
            int stepRemaining = Mathf.Max(0, Mathf.CeilToInt(limit - elapsed));
            string timerColor = stepRemaining <= 15 ? "#E74C3C" : (stepRemaining <= 30 ? "#FF9900" : "#0078D4");

            // Hiển thị tiến trình dừng nhường đường đi bộ trên HUD
            if (em.currentStep == ExamStep.DungNhuongDuongDiBo)
            {
                if (em.HasPedestrianStoppedLongEnough)
                {
                    stepDescText.text = $"<color=#1FA659><b>Đạt 2s!</b> Tiếp tục di chuyển sang bài tiếp theo. <color=#0078D4>(Còn: {stepRemaining}s)</color></color>";
                }
                else if (em.PedestrianStopDuration > 0f)
                {
                    stepDescText.text = $"<color=#0078D4><b>Đang dừng:</b> {em.PedestrianStopDuration:F1}s/2.0s. Giữ phanh! (Bài thi còn: {stepRemaining}s)</color>";
                }
                else
                {
                    stepDescText.text = $"<color={timerColor}><b>[Đang đếm ngược: {stepRemaining}s]</b></color> Dừng trước vạch trắng và giữ đứng yên 2 giây.";
                }
            }
            // Hiển thị tiến trình dừng dốc (Đề-pa) trên HUD
            else if (em.currentStep == ExamStep.DungAndKhoiHanhNgangDoc)
            {
                if (em.HasSlopeStoppedLongEnough)
                {
                    stepDescText.text = $"<color=#1FA659><b>Đạt 5s!</b> Hãy khởi hành vượt dốc ngay. <color=#0078D4>(Còn: {stepRemaining}s)</color></color>";
                }
                else if (em.SlopeStopDuration > 0f)
                {
                    stepDescText.text = $"<color=#0078D4><b>Đang dừng:</b> {em.SlopeStopDuration:F1}s/5.0s. Giữ phanh! (Bài thi còn: {stepRemaining}s)</color>";
                }
                else
                {
                    stepDescText.text = $"<color={timerColor}><b>[Đang đếm ngược: {stepRemaining}s]</b></color> Dừng trên dốc đúng vị trí và giữ đứng yên 5 giây.";
                }
            }
            // Hiển thị đếm ngược cho Bài 7 (Ghép dọc)
            else if (em.currentStep == ExamStep.GhepDocVaoNoiDo)
            {
                if (em.HasParkingStoppedLongEnough)
                {
                    stepDescText.text = $"<color=#1FA659><b>[ĐÃ NHẬN BÀI]</b></color> Đã ghép xe thành công! Hãy lái xe đi ra khỏi chuồng. <color=#0078D4>(Còn: {stepRemaining}s)</color>";
                }
                else if (em.ParkingStopDuration > 0f)
                {
                    stepDescText.text = $"<color=#0078D4><b>Đang dừng đỗ:</b> {em.ParkingStopDuration:F1}s/2.0s. Giữ xe đứng yên trong chuồng! (Còn: {stepRemaining}s)</color>";
                }
                else
                {
                    stepDescText.text = $"<color={timerColor}><b>[Đang đếm ngược: {stepRemaining}s]</b></color> Lùi xe vào chuồng dọc đúng quy định và giữ đứng yên 2s.";
                }
            }
            // Hiển thị đếm ngược cho Bài 8 (Đường sắt)
            else if (em.currentStep == ExamStep.TamDungNoiDuongSat)
            {
                if (em.HasRailwayStoppedLongEnough)
                {
                    stepDescText.text = $"<color=#1FA659><b>Đạt 2s!</b> Tiếp tục di chuyển sang bài tiếp theo. <color=#0078D4>(Còn: {stepRemaining}s)</color></color>";
                }
                else if (em.RailwayStopDuration > 0f)
                {
                    stepDescText.text = $"<color=#0078D4><b>Đang dừng:</b> {em.RailwayStopDuration:F1}s/2.0s. Giữ phanh! (Bài thi còn: {stepRemaining}s)</color>";
                }
                else
                {
                    stepDescText.text = $"<color={timerColor}><b>[Đang đếm ngược: {stepRemaining}s]</b></color> Dừng trước vạch đường sắt và giữ xe đứng yên 2s.";
                }
            }
            // Hiển thị đếm ngược cho Bài 10 (Ghép ngang)
            else if (em.currentStep == ExamStep.GhepNgangVaoNoiDo)
            {
                if (em.HasParkingStoppedLongEnough)
                {
                    stepDescText.text = $"<color=#1FA659><b>[ĐÃ NHẬN BÀI]</b></color> Đã ghép xe thành công! Hãy lái xe đi ra khỏi chuồng. <color=#0078D4>(Còn: {stepRemaining}s)</color>";
                }
                else if (em.ParkingStopDuration > 0f)
                {
                    stepDescText.text = $"<color=#0078D4><b>Đang dừng đỗ:</b> {em.ParkingStopDuration:F1}s/2.0s. Giữ xe đứng yên trong chuồng! (Còn: {stepRemaining}s)</color>";
                }
                else
                {
                    stepDescText.text = $"<color={timerColor}><b>[Đang đếm ngược: {stepRemaining}s]</b></color> Lái xe ghép vào chuồng ngang đúng quy định và giữ đứng yên 2s.";
                }
            }
            // Hiển thị tiến trình & tốc độ cho Bài 9 (Thay đổi số)
            else if (em.currentStep == ExamStep.ThayDoiSoDuongBang)
            {
                float curSpeed = targetCar != null ? targetCar.CurrentSpeed : 0f;
                if (em.step9Segment == 2)
                {
                    string speedStatus = curSpeed < 20f ? "<color=#1FA659>[ĐẠT <20km/h]</color>" : "<color=#E74C3C>[CẦN <20km/h!]</color>";
                    stepDescText.text = $"<color={timerColor}><b>[Còn {stepRemaining}s]</b></color> {speedStatus} Giảm tốc từ biển 2 -> biển 3 (rẽ trái). Vận tốc: <b>{curSpeed:F1} km/h</b>";
                }
                else
                {
                    string speedStatus = curSpeed > 20f ? "<color=#1FA659>[ĐẠT >20km/h]</color>" : "<color=#FF9900>[CẦN >20km/h!]</color>";
                    stepDescText.text = $"<color={timerColor}><b>[Còn {stepRemaining}s]</b></color> {speedStatus} Tăng tốc từ biển 1 -> biển 2. Vận tốc: <b>{curSpeed:F1} km/h</b>";
                }
            }
            // Hiển thị đếm ngược cho các bài thi khác nếu có thời gian quy định
            else if (limit < 9999f && em.currentStep != ExamStep.None && em.currentStep != ExamStep.XuatPhat)
            {
                string baseDesc = GetBaseDescForStep(em.currentStep);
                stepDescText.text = $"<color={timerColor}><b>[Đang đếm ngược: {stepRemaining}s]</b></color> {baseDesc}";
            }
        }

        // Bỏ hiển thị đếm ngược ở giữa màn hình theo yêu cầu
        if (countdownText != null)
        {
            countdownText.gameObject.SetActive(false);
        }
    }

    private void UpdateStepTexts(ExamStep step)
    {
        if (stepTitleText == null || stepDescText == null) return;

        switch (step)
        {
            case ExamStep.None:
                stepTitleText.text = "Chuẩn bị thi";
                stepDescText.text = "Thắt dây an toàn, nổ máy (phím T) và chờ xuất phát.";
                break;
            case ExamStep.XuatPhat:
                stepTitleText.text = "Bài 1: Xuất phát";
                stepDescText.text = "Bật xi-nhan trái, nhấn ga (W) qua vạch xuất phát.";
                break;
            case ExamStep.DungNhuongDuongDiBo:
                stepTitleText.text = "Bài 2: Dừng xe nhường đường";
                stepDescText.text = "Dừng trước vạch trắng và giữ đứng yên 2 giây.";
                break;
            case ExamStep.DungAndKhoiHanhNgangDoc:
                stepTitleText.text = "Bài 3: Dừng & Khởi hành ngang dốc";
                stepDescText.text = "Dừng trên dốc, giữ yên 5 giây và vượt dốc (<30s).";
                break;
            case ExamStep.VetBanhXeAndDuongVuongGoc:
                stepTitleText.text = "Bài 4: Qua vệt bánh xe";
                stepDescText.text = "Lái bánh xe bên phụ qua vệt bánh xe, tránh đè vạch.";
                break;
            case ExamStep.QuaNgaTuDenTinHieu:
                stepTitleText.text = "Bài 5: Qua ngã tư có đèn";
                stepDescText.text = "Dừng khi đèn đỏ, đi khi đèn xanh và xi-nhan đúng.";
                break;
            case ExamStep.DuongVongQuanhCo:
                stepTitleText.text = "Bài 6: Đường vòng quanh co";
                stepDescText.text = "Lái xe qua đường chữ S uốn lượn, không đè vạch.";
                break;
            case ExamStep.GhepDocVaoNoiDo:
                stepTitleText.text = "Bài 7: Ghép dọc vào nơi đỗ";
                stepDescText.text = "Lùi xe vào chuồng dọc đúng quy định và đi ra.";
                break;
            case ExamStep.TamDungNoiDuongSat:
                stepTitleText.text = "Bài 8: Tạm dừng nơi đường sắt";
                stepDescText.text = "Dừng trước vạch đường sắt và giữ xe đứng yên.";
                break;
            case ExamStep.ThayDoiSoDuongBang:
                stepTitleText.text = "Bài 9: Thay đổi tốc độ";
                if (ExamManager.Instance != null && ExamManager.Instance.step9Segment == 2)
                    stepDescText.text = "Giảm tốc < 20 km/h từ biển 2 đến biển 3 (rẽ trái).";
                else
                    stepDescText.text = "Tăng tốc > 20 km/h từ biển 1 đến biển 2.";
                break;
            case ExamStep.GhepNgangVaoNoiDo:
                stepTitleText.text = "Bài 10: Ghép xe ngang";
                stepDescText.text = "Lái xe ghép vào chuồng ngang đúng quy định và đi ra.";
                break;
            case ExamStep.KetThuc:
                stepTitleText.text = "Bài 11: Kết thúc";
                stepDescText.text = "Bật xi-nhan phải và lái xe qua vạch kết thúc.";
                break;
        }
    }

    private string GetBaseDescForStep(ExamStep step)
    {
        switch (step)
        {
            case ExamStep.XuatPhat: return "Bật xi-nhan trái, nhấn ga (W) qua vạch xuất phát.";
            case ExamStep.DungNhuongDuongDiBo: return "Dừng trước vạch trắng và giữ đứng yên 2 giây.";
            case ExamStep.DungAndKhoiHanhNgangDoc: return "Dừng trên dốc, giữ yên 5 giây và vượt dốc.";
            case ExamStep.VetBanhXeAndDuongVuongGoc: return "Lái bánh xe bên phụ qua vệt bánh xe, tránh đè vạch.";
            case ExamStep.QuaNgaTuDenTinHieu: return "Dừng khi đèn đỏ, đi khi đèn xanh và xi-nhan đúng.";
            case ExamStep.DuongVongQuanhCo: return "Lái xe qua đường chữ S uốn lượn, không đè vạch.";
            case ExamStep.GhepDocVaoNoiDo: return "Lùi xe vào chuồng dọc đúng quy định và đi ra.";
            case ExamStep.TamDungNoiDuongSat: return "Dừng trước vạch đường sắt và giữ xe đứng yên.";
            case ExamStep.ThayDoiSoDuongBang:
                if (ExamManager.Instance != null && ExamManager.Instance.step9Segment == 2)
                    return "Giảm tốc < 20 km/h từ biển 2 đến biển 3 (rẽ trái).";
                return "Tăng tốc > 20 km/h từ biển 1 đến biển 2.";
            case ExamStep.GhepNgangVaoNoiDo: return "Lái xe ghép vào chuồng ngang đúng quy định và đi ra.";
            default: return "";
        }
    }

    private void UpdateDashboard()
    {
        if (targetCar == null) return;

        // Cập nhật hiển thị hộp số/gear (D -> D1/D2/..., N -> N, R -> R) ở ô kế bên Số xe
        if (carNumberText != null)
        {
            switch (targetCar.currentGear)
            {
                case CarController.GearState.N:
                    carNumberText.text = "N";
                    break;
                case CarController.GearState.D:
                    carNumberText.text = "D" + targetCar.currentAutomaticGear;
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

            AudioListener.pause = true; // Mute tất cả âm thanh khi tạm dừng
        }
        else
        {
            if (pausePanel != null) pausePanel.SetActive(false);
            if (settingsPanel != null) settingsPanel.SetActive(false);

            // Ẩn con trỏ chuột khi tiếp tục game (nếu muốn)
            // Cursor.visible = false;

            AudioListener.pause = false; // Bật lại âm thanh khi tiếp tục
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
        AudioListener.pause = false; // Bật lại âm thanh
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
        AudioListener.pause = false; // Bật lại âm thanh
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

    // ==========================================
    // CÁC HÀM HIỂN THỊ MÀN HÌNH KẾT QUẢ (RESULT SCREEN)
    // ==========================================

    public void ShowResultScreen(bool isPass, string finalStepName, int finalScore, List<ExamManager.DeductionRecord> mistakes)
    {
        // Kiểm tra xem tham chiếu có hợp lệ trong scene không, nếu không thì tìm lại thực thể trong scene
        if (resultPanel == null || resultPanel.gameObject == null || !resultPanel.gameObject.scene.IsValid())
        {
            Transform resultTrans = FindDeepChild(transform, "Panel_Result");
            if (resultTrans != null)
            {
                resultPanel = resultTrans.GetComponent<ResultPanelController>();
            }
        }

        if (resultPanel != null)
        {
            // Kích hoạt tất cả các object cha (ví dụ HUD_Root, HUD_Canvas) nếu chúng bị tắt
            Transform parentTrans = resultPanel.transform.parent;
            while (parentTrans != null)
            {
                if (!parentTrans.gameObject.activeSelf)
                {
                    parentTrans.gameObject.SetActive(true);
                }
                parentTrans = parentTrans.parent;
            }

            resultPanel.Setup(isPass, finalStepName, finalScore, mistakes);
        }
        else
        {
            Debug.LogWarning("[HUDController] Chưa gán resultPanel!");
        }
    }
}

