using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ExamManager : MonoBehaviour
{
    public static ExamManager Instance { get; private set; }

    [Header("Hệ thống Âm thanh")]
    public AudioSource voiceSource;       // Nguồn phát giọng đọc
    public AudioSource sfxSource;         // Nguồn phát tiếng bính boong, tít tít lỗi
    
    public AudioClip soundBinhBoong;      // Tiếng nhận bài thi
    public AudioClip soundTingTingError;  // Tiếng phạt lỗi

    [Header("Giọng đọc 11 bài thi (Tiếng Việt)")]
    public AudioClip voiceXuatPhat;
    public AudioClip voiceDungNhuongDiBo;
    public AudioClip voiceDePa;
    public AudioClip voiceVetBanhXe;
    public AudioClip voiceNgaTu;
    public AudioClip voiceChuS;
    public AudioClip voiceGhepDoc;
    public AudioClip voiceDuongSat;
    public AudioClip voiceTangGiamSo;
    public AudioClip voiceGhepNgang;
    public AudioClip voiceKetThuc;

    [Header("Giọng đọc trạng thái thi")]
    public AudioClip voiceStartExam;      // "Bắt đầu thi"
    public AudioClip voicePassExam;       // "Chúc mừng bạn đã thi đạt"
    public AudioClip voiceFailExam;       // "Bạn đã thi trượt"

    [Header("Trạng thái thi")]
    public ExamStep currentStep = ExamStep.None;
    public int currentScore = 100;
    public float totalExamTime = 1080f;   // 18 phút (giây)
    public bool isExamActive = false;

    [System.Serializable]
    public struct DeductionRecord
    {
        public string reason;
        public int points;
    }
    public List<DeductionRecord> deductionsList = new List<DeductionRecord>();

    private HashSet<ExamStep> completedSteps = new HashSet<ExamStep>();

    [Header("Cấu hình Trình tự thi")]
    private List<ExamStep> correctSequence = new List<ExamStep>()
    {
        ExamStep.XuatPhat,
        ExamStep.DungNhuongDuongDiBo,
        ExamStep.DungAndKhoiHanhNgangDoc,
        ExamStep.VetBanhXeAndDuongVuongGoc,
        ExamStep.QuaNgaTuDenTinHieu, // Lần 1
        ExamStep.DuongVongQuanhCo,
        ExamStep.QuaNgaTuDenTinHieu, // Lần 2
        ExamStep.GhepDocVaoNoiDo,
        ExamStep.QuaNgaTuDenTinHieu, // Lần 3
        ExamStep.TamDungNoiDuongSat,
        ExamStep.ThayDoiSoDuongBang,
        ExamStep.GhepNgangVaoNoiDo,
        ExamStep.QuaNgaTuDenTinHieu, // Lần 4
        ExamStep.KetThuc
    };

    private int sequenceIndex = 0;
    private CarController targetCar;

    // Các biến trạng thái để kiểm tra quy tắc bài thi
    private float xuatPhatStartTime = 0f;
    private float carMoveStartTime = 0f;
    private bool xuatPhatMovingChecked = false;
    private bool xuatPhatBlinkerOffChecked = false;
    private bool hasStartedWithBlinker = false;
    private bool hasDeductedForNoBlinker = false;

    private bool stoppedInPedestrianTrigger = false;
    private float pedestrianStopStartTime = 0f;
    private bool hasPedestrianStoppedLongEnough = false;
    
    private bool stoppedOnSlope = false;
    private float slopeEntryTime = 0f;
    private Vector3 slopeStopPosition;
    private float maxSlopeRollback = 0f;
    private bool hasDeductedForRollback = false;
    private float slopeStopDuration = 0f;
    private bool hasSlopeStoppedLongEnough = false;
    private bool isInsideCurrentTrigger = false;

    private int ngaTuVisitCount = 0;

    private bool stoppedAtRailway = false;
    private float railwayStopDuration = 0f;
    private bool hasRailwayStoppedLongEnough = false;
    private bool reachedTargetSpeed = false;
    private bool slowedDownAfterTargetSpeed = false;
    private bool parkedSuccessfully = false;
    private float parkingStopDuration = 0f;
    private bool hasParkingStoppedLongEnough = false;
    private bool hasPassedBridgeMid = false;
    private bool isCurrentStepValidated = false;

    [Header("Trạng thái Bài 9 (Thay đổi số trên đường bằng)")]
    public int step9Segment = 1; // 1: Từ Biển 1 -> Biển 2 (>20km/h), 2: Từ Biển 2 -> Biển 3 (<20km/h), 3: Qua Biển 3
    private float lastStep9DeductedTime = -999f;
    private bool step9Segment1SpeedPassed = false;
    private bool step9Segment2SpeedPassed = false;

    // Quản lý thời gian của từng bài thi riêng lẻ
    private float stepStartTime = 0f;
    private bool stepTimeLimitDeducted = false;

    // Trạng thái cho việc kiểm tra vượt đèn đỏ và đè vạch/ngược chiều
    private TrafficLight activeTrafficLight;
    private bool enteredStopLineOnRed = false;
    private bool hasDeductedOverStopLine = false;

    private float calibratedCorrectSide = 1f;
    private bool isLaneCalibrated = false;
    private bool isInWrongLane = false;
    private float wrongLaneEntryTime = 0f;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    private void Start()
    {
        UpdateAudioVolumes();
    }

    public float PedestrianStopDuration => pedestrianStopStartTime;
    public bool HasPedestrianStoppedLongEnough => hasPedestrianStoppedLongEnough;
    public float SlopeStopDuration => slopeStopDuration;
    public bool HasSlopeStoppedLongEnough => hasSlopeStoppedLongEnough;
    public float RailwayStopDuration => railwayStopDuration;
    public bool HasRailwayStoppedLongEnough => hasRailwayStoppedLongEnough;
    public float ParkingStopDuration => parkingStopDuration;
    public bool HasParkingStoppedLongEnough => hasParkingStoppedLongEnough;
    public bool IsInsideCurrentTrigger => isInsideCurrentTrigger;
    public bool XuatPhatMovingChecked => xuatPhatMovingChecked;
    public bool XuatPhatBlinkerOffChecked => xuatPhatBlinkerOffChecked;
    public bool HasStartedWithBlinker => hasStartedWithBlinker;
    public bool HasDeductedForNoBlinker => hasDeductedForNoBlinker;

    // Vùng xác định (Zone Trigger) cho Bài 2, 3, 7, 8, 10
    private bool isStepTimerActive = false;
    private bool isInsideZoneTrigger = false;
    private bool hasEnteredZone = false;

    public bool IsStepTimerActive => isStepTimerActive;
    public bool IsInsideZoneTrigger => isInsideZoneTrigger;
    public bool HasEnteredZone => hasEnteredZone;

    public bool IsZoneRequiredForStep(ExamStep step)
    {
        return step == ExamStep.DungNhuongDuongDiBo ||          // Bài 2
               step == ExamStep.DungAndKhoiHanhNgangDoc ||      // Bài 3
               step == ExamStep.GhepDocVaoNoiDo ||              // Bài 7
               step == ExamStep.TamDungNoiDuongSat ||           // Bài 8
               step == ExamStep.GhepNgangVaoNoiDo;              // Bài 10
    }

    public void SetZoneTriggerState(ExamStep step, bool isInside)
    {
        if (step == currentStep)
        {
            isInsideZoneTrigger = isInside;
            if (isInside && !isStepTimerActive)
            {
                ActivateZoneTimer(step);
            }
        }
    }

    public void ActivateZoneTimer(ExamStep step)
    {
        if (step == currentStep && !isStepTimerActive)
        {
            isStepTimerActive = true;
            hasEnteredZone = true;
            stepStartTime = Time.time;
            stepTimeLimitDeducted = false;

            Debug.Log($"[ExamManager] Xe đã vào đúng vùng bài thi {step}. Kích hoạt đếm ngược {GetTimeLimitForStep(step)}s!");

            var hud = Object.FindAnyObjectByType<HUDController>();
            if (hud != null)
            {
                hud.ShowSuccessNotification("Đã vào vùng bài thi! Bắt đầu đếm ngược thời gian.", 2.5f);
            }
        }
    }

    public void SetInsideTrigger(ExamStep step, bool isInside)
    {
        if (step == currentStep)
        {
            isInsideCurrentTrigger = isInside;
        }
    }

    public void StartExam()
    {
        StartCoroutine(StartExamDelayed());
    }

    public void UpdateAudioVolumes()
    {
        float voiceVol = PlayerPrefs.GetFloat("VoiceVolume", 1.0f);
        float sfxVol = PlayerPrefs.GetFloat("SFXVolume", 0.8f);
        if (voiceSource != null) voiceSource.volume = voiceVol;
        if (sfxSource != null) sfxSource.volume = sfxVol;
    }

    private IEnumerator StartExamDelayed()
    {
        yield return new WaitForSeconds(0.5f);
        PlayVoice(voiceStartExam);
        isExamActive = true;
        deductionsList.Clear();
        ResetStepTimer();
    }

    private void FindCar()
    {
        if (targetCar == null)
        {
            targetCar = Object.FindAnyObjectByType<CarController>();
        }
    }

    private void Update()
    {
        if (isExamActive && totalExamTime > 0)
        {
            totalExamTime -= Time.deltaTime;
            if (totalExamTime <= 0)
            {
                FailExam("Hết thời gian thi quy định!");
                return;
            }

            FindCar();
            if (targetCar != null)
            {
                // Kiểm tra điều kiện lật xe (Vehicle overturns) -> Đánh trượt lập tức
                if (Vector3.Dot(targetCar.transform.up, Vector3.up) < 0.3f)
                {
                    FailExam("Xe bị lật!");
                    return;
                }

                UpdateActiveStepChecks();
                CheckLaneViolation();
                CheckStepTimeLimit();
            }
        }
    }

    private void ResetStepTimer()
    {
        stepStartTime = Time.time;
        stepTimeLimitDeducted = false;
    }

    private void CheckStepTimeLimit()
    {
        if (stepTimeLimitDeducted) return;

        // Bài 2, 3, 7, 8, 10: Chỉ tính thời gian đếm ngược sau khi xe đã đi vào vùng trigger xác định (Zone Trigger)
        if (IsZoneRequiredForStep(currentStep) && !isStepTimerActive)
        {
            return;
        }

        float elapsed = Time.time - stepStartTime;
        float limit = GetTimeLimitForStep(currentStep);

        if (elapsed > limit)
        {
            if (currentStep == ExamStep.XuatPhat)
            {
                if (!targetCar.isEngineOn)
                {
                    FailExam("Không nổ máy xe khởi hành trong 30 giây!");
                }
                else if (targetCar.CurrentSpeed < 0.5f)
                {
                    FailExam("Không di chuyển khởi hành trong 30 giây!");
                }
                else
                {
                    FailExam("Quá thời gian xuất phát quy định!");
                }
                stepTimeLimitDeducted = true;
            }
            else if (currentStep == ExamStep.DungAndKhoiHanhNgangDoc)
            {
                FailExam("Quá 30 giây không khởi hành qua dốc!");
                stepTimeLimitDeducted = true;
            }
            else
            {
                DeductPoints(5, $"Quá thời gian bài thi: {GetStepNameVi(currentStep)}");
                stepTimeLimitDeducted = true;
            }
        }
    }

    public float GetStepStartTime() => stepStartTime;

    public float GetTimeLimitForStep(ExamStep step)
    {
        switch (step)
        {
            case ExamStep.XuatPhat: return 30f;
            case ExamStep.DungAndKhoiHanhNgangDoc: return 30f;
            case ExamStep.DungNhuongDuongDiBo: return 30f;
            case ExamStep.TamDungNoiDuongSat: return 30f;
            case ExamStep.ThayDoiSoDuongBang: return 60f;
            case ExamStep.VetBanhXeAndDuongVuongGoc: return 120f;
            case ExamStep.DuongVongQuanhCo: return 120f;
            case ExamStep.GhepDocVaoNoiDo: return 120f;
            case ExamStep.GhepNgangVaoNoiDo: return 120f;
            case ExamStep.QuaNgaTuDenTinHieu: return 45f;
            default: return 9999f;
        }
    }

    private string GetStepNameVi(ExamStep step)
    {
        switch (step)
        {
            case ExamStep.XuatPhat: return "Xuất phát";
            case ExamStep.DungNhuongDuongDiBo: return "Nhường đường đi bộ";
            case ExamStep.DungAndKhoiHanhNgangDoc: return "Dừng và khởi hành ngang dốc";
            case ExamStep.VetBanhXeAndDuongVuongGoc: return "Vệt bánh xe & đường vuông góc";
            case ExamStep.QuaNgaTuDenTinHieu: return "Qua ngã tư";
            case ExamStep.DuongVongQuanhCo: return "Đường vòng quanh co";
            case ExamStep.GhepDocVaoNoiDo: return "Ghép xe dọc";
            case ExamStep.TamDungNoiDuongSat: return "Dừng xe đường sắt";
            case ExamStep.ThayDoiSoDuongBang: return "Thay đổi số";
            case ExamStep.GhepNgangVaoNoiDo: return "Ghép xe ngang";
            case ExamStep.KetThuc: return "Kết thúc";
            default: return "Bài thi";
        }
    }

    private void UpdateActiveStepChecks()
    {
        switch (currentStep)
        {
            case ExamStep.XuatPhat:
                if (targetCar.isLeftBlinkerOn)
                {
                    hasStartedWithBlinker = true;
                }

                // Kiểm tra khi xe bắt đầu lăn bánh (khởi hành xuất phát)
                if (targetCar.CurrentSpeed >= 0.5f)
                {
                    if (!xuatPhatMovingChecked)
                    {
                        xuatPhatMovingChecked = true;
                        carMoveStartTime = Time.time;

                        // Xe bắt đầu di chuyển: Kiểm tra nếu chưa bật xi-nhan trái thì trừ 5 điểm
                        if (!hasStartedWithBlinker && !targetCar.isLeftBlinkerOn)
                        {
                            if (!hasDeductedForNoBlinker)
                            {
                                DeductPoints(5, "Không bật xi-nhan trái khi xuất phát");
                                hasDeductedForNoBlinker = true;
                            }
                        }
                    }
                }

                // Kiểm tra tắt xi-nhan trái kịp thời (5s tính từ lúc xe bắt đầu di chuyển)
                if (xuatPhatMovingChecked && !xuatPhatBlinkerOffChecked)
                {
                    if (!targetCar.isLeftBlinkerOn)
                    {
                        // Người chơi đã tắt xi-nhan trái hợp lệ
                        xuatPhatBlinkerOffChecked = true;
                    }
                    else if (Time.time - carMoveStartTime > 5f)
                    {
                        if (targetCar.isLeftBlinkerOn)
                        {
                            DeductPoints(5, "Không tắt xi-nhan trái kịp thời");
                        }
                        xuatPhatBlinkerOffChecked = true;
                    }
                }
                break;

            case ExamStep.DungNhuongDuongDiBo:
                if (targetCar.CurrentSpeed < 0.1f)
                {
                    stoppedInPedestrianTrigger = true;
                    pedestrianStopStartTime += Time.deltaTime;
                    if (pedestrianStopStartTime >= 2f && !hasPedestrianStoppedLongEnough)
                    {
                        hasPedestrianStoppedLongEnough = true;
                        var hud = Object.FindAnyObjectByType<HUDController>();
                        if (hud != null)
                        {
                            hud.ShowSuccessNotification("Đã dừng đủ thời gian! Hãy tiếp tục di chuyển.", 2f);
                        }
                    }
                }
                else
                {
                    if (!hasPedestrianStoppedLongEnough)
                    {
                        pedestrianStopStartTime = 0f;
                        stoppedInPedestrianTrigger = false;
                    }
                }
                break;

            case ExamStep.DungAndKhoiHanhNgangDoc:
                if (targetCar.CurrentSpeed < 0.1f)
                {
                    if (!stoppedOnSlope)
                    {
                        stoppedOnSlope = true;
                        slopeStopPosition = targetCar.transform.position;
                        maxSlopeRollback = 0f;
                    }

                    slopeStopDuration += Time.deltaTime;
                    if (slopeStopDuration >= 5f && !hasSlopeStoppedLongEnough)
                    {
                        hasSlopeStoppedLongEnough = true;
                        var hud = Object.FindAnyObjectByType<HUDController>();
                        if (hud != null)
                        {
                            hud.ShowSuccessNotification("Đã dừng đủ thời gian! Hãy khởi hành qua dốc.", 2f);
                        }
                    }
                }
                else
                {
                    if (!hasSlopeStoppedLongEnough)
                    {
                        slopeStopDuration = 0f;
                        stoppedOnSlope = false;
                    }
                }

                if (stoppedOnSlope)
                {
                    Vector3 diff = targetCar.transform.position - slopeStopPosition;
                    float dot = Vector3.Dot(diff, targetCar.transform.forward);
                    if (dot < 0f) // Xe trôi lùi
                    {
                        float rollbackDistance = Mathf.Abs(dot);
                        if (rollbackDistance > maxSlopeRollback)
                        {
                            maxSlopeRollback = rollbackDistance;
                        }

                        if (maxSlopeRollback > 1.0f)
                        {
                            FailExam("Để xe trôi dốc quá 1 mét!");
                        }
                        else if (maxSlopeRollback > 0.5f && !hasDeductedForRollback)
                        {
                            DeductPoints(5, "Để xe trôi dốc quá 50cm");
                            hasDeductedForRollback = true;
                        }
                    }
                }
                break;

            case ExamStep.VetBanhXeAndDuongVuongGoc:
                if (!hasPassedBridgeMid)
                {
                    // Sử dụng hệ thống bánh xe vật lý (bên phụ/bên phải) cán qua vùng gỗ quy định
                    WheelHit hit;
                    
                    // 1. Kiểm tra bánh trước bên phải (Front Right)
                    if (targetCar.frontRightCollider != null && targetCar.frontRightCollider.GetGroundHit(out hit))
                    {
                        if (hit.collider != null && hit.collider.gameObject.name.ToLower().Contains("roadstraightbridgemid"))
                        {
                            hasPassedBridgeMid = true;
                        }
                    }
                    
                    // 2. Kiểm tra bánh sau bên phải (Rear Right)
                    if (!hasPassedBridgeMid && targetCar.rearRightCollider != null && targetCar.rearRightCollider.GetGroundHit(out hit))
                    {
                        if (hit.collider != null && hit.collider.gameObject.name.ToLower().Contains("roadstraightbridgemid"))
                        {
                            hasPassedBridgeMid = true;
                        }
                    }
                }
                break;

            case ExamStep.TamDungNoiDuongSat:
                if (isInsideZoneTrigger && targetCar.CurrentSpeed < 0.1f)
                {
                    stoppedAtRailway = true;
                    railwayStopDuration += Time.deltaTime;
                    if (railwayStopDuration >= 2f && !hasRailwayStoppedLongEnough)
                    {
                        hasRailwayStoppedLongEnough = true;
                        var hud = Object.FindAnyObjectByType<HUDController>();
                        if (hud != null)
                        {
                            hud.ShowSuccessNotification("Đã dừng đủ thời gian! Hãy tiếp tục di chuyển.", 2f);
                        }
                    }
                }
                else
                {
                    if (!hasRailwayStoppedLongEnough)
                    {
                        railwayStopDuration = 0f;
                    }
                }
                break;

            case ExamStep.ThayDoiSoDuongBang:
                if (step9Segment == 1)
                {
                    if (!step9Segment1SpeedPassed)
                    {
                        // Từ Biển 1 đến Biển 2: Phải đạt tốc độ > 20 km/h
                        if (targetCar.CurrentSpeed > 20f)
                        {
                            step9Segment1SpeedPassed = true;
                            reachedTargetSpeed = true;
                        }
                        else if (Time.time - lastStep9DeductedTime >= 2.0f)
                        {
                            lastStep9DeductedTime = Time.time;
                            DeductPoints(5, "Tốc độ không đạt (>20km/h) giữa biển 1 và biển 2");
                        }
                    }
                }
                else if (step9Segment == 2)
                {
                    if (!step9Segment2SpeedPassed)
                    {
                        // Từ Biển 2 đến Biển 3 (rẽ trái): Phải giảm tốc < 20 km/h
                        if (targetCar.CurrentSpeed < 20f)
                        {
                            step9Segment2SpeedPassed = true;
                            slowedDownAfterTargetSpeed = true;
                        }
                        else if (Time.time - lastStep9DeductedTime >= 2.0f)
                        {
                            lastStep9DeductedTime = Time.time;
                            DeductPoints(5, "Tốc độ chưa giảm dưới 20km/h giữa biển 2 và biển 3");
                        }
                    }
                }
                break;

            case ExamStep.GhepDocVaoNoiDo:
            case ExamStep.GhepNgangVaoNoiDo:
                if (isInsideZoneTrigger && targetCar.CurrentSpeed < 0.1f)
                {
                    parkingStopDuration += Time.deltaTime;
                    if (parkingStopDuration >= 2f && !hasParkingStoppedLongEnough)
                    {
                        hasParkingStoppedLongEnough = true;
                        parkedSuccessfully = true;
                        PlaySFX(soundBinhBoong);
                        var hud = Object.FindAnyObjectByType<HUDController>();
                        if (hud != null)
                        {
                            hud.ShowSuccessNotification("Đã nhận bài đỗ xe thành công! Hãy lái xe đi ra.", 3f);
                        }
                    }
                }
                else
                {
                    if (!hasParkingStoppedLongEnough)
                    {
                        parkingStopDuration = 0f;
                    }
                }
                break;
            case ExamStep.QuaNgaTuDenTinHieu:
                CheckRedLightViolation();
                break;
        }
    }

    private void ValidatePreviousStepCompletion()
    {
        if (targetCar == null) return;
        if (isCurrentStepValidated) return;
        isCurrentStepValidated = true;

        switch (currentStep)
        {
            case ExamStep.XuatPhat:
                if (!hasStartedWithBlinker && !hasDeductedForNoBlinker)
                {
                    DeductPoints(5, "Không bật xi-nhan trái khi xuất phát");
                    hasDeductedForNoBlinker = true;
                }
                if (targetCar.isLeftBlinkerOn && !xuatPhatBlinkerOffChecked)
                {
                    DeductPoints(5, "Không tắt xi-nhan trái kịp thời");
                    xuatPhatBlinkerOffChecked = true;
                }
                break;

            case ExamStep.DungNhuongDuongDiBo:
                if (!stoppedInPedestrianTrigger)
                {
                    DeductPoints(5, "Không dừng xe nhường đường cho người đi bộ");
                }
                else if (!hasPedestrianStoppedLongEnough)
                {
                    DeductPoints(5, "Dừng xe chưa đủ 2 giây nhường đường cho người đi bộ");
                }
                break;

            case ExamStep.DungAndKhoiHanhNgangDoc:
                if (!stoppedOnSlope)
                {
                    FailExam("Không dừng xe trên dốc (Đề-pa)!");
                }
                else if (!hasSlopeStoppedLongEnough)
                {
                    DeductPoints(5, "Dừng xe chưa đủ 5 giây trên dốc (Đề-pa)");
                }
                break;

            case ExamStep.VetBanhXeAndDuongVuongGoc:
                if (!hasPassedBridgeMid)
                {
                    FailExam("Xe đi sai trình tự bài thi! Không đi qua vệt bánh xe đúng quy định.");
                }
                break;

            case ExamStep.GhepDocVaoNoiDo:
            case ExamStep.GhepNgangVaoNoiDo:
                if (!parkedSuccessfully)
                {
                    DeductPoints(5, "Không ghép xe đúng quy định (chưa dừng đỗ trong chuồng)");
                }
                else if (!hasParkingStoppedLongEnough)
                {
                    DeductPoints(5, "Dừng đỗ trong chuồng chưa đủ 2 giây");
                }
                break;

            case ExamStep.TamDungNoiDuongSat:
                if (!stoppedAtRailway)
                {
                    DeductPoints(5, "Không dừng xe nơi đường sắt chạy qua");
                }
                else if (!hasRailwayStoppedLongEnough)
                {
                    DeductPoints(5, "Dừng xe chưa đủ 2 giây nơi đường sắt chạy qua");
                }
                break;

            case ExamStep.ThayDoiSoDuongBang:
                if (!step9Segment1SpeedPassed && !reachedTargetSpeed)
                {
                    DeductPoints(5, "Không đạt tốc độ quy định (>20 km/h) giữa biển 1 và biển 2");
                }
                if (!step9Segment2SpeedPassed && !slowedDownAfterTargetSpeed)
                {
                    DeductPoints(5, "Không giảm tốc độ về quy định (<20 km/h) giữa biển 2 và biển 3");
                }
                break;
        }
    }

    private void InitializeStepStates(ExamStep step)
    {
        isInsideCurrentTrigger = true;

        if (IsZoneRequiredForStep(step))
        {
            isStepTimerActive = false;
            hasEnteredZone = false;
            isInsideZoneTrigger = false;
        }
        else
        {
            isStepTimerActive = true;
            hasEnteredZone = true;
            ResetStepTimer();
        }

        switch (step)
        {
            case ExamStep.XuatPhat:
                xuatPhatStartTime = Time.time;
                carMoveStartTime = 0f;
                xuatPhatMovingChecked = false;
                xuatPhatBlinkerOffChecked = false;
                hasStartedWithBlinker = targetCar != null && targetCar.isLeftBlinkerOn;
                hasDeductedForNoBlinker = false;
                break;

            case ExamStep.DungNhuongDuongDiBo:
                stoppedInPedestrianTrigger = false;
                hasPedestrianStoppedLongEnough = false;
                pedestrianStopStartTime = 0f;
                break;

            case ExamStep.DungAndKhoiHanhNgangDoc:
                stoppedOnSlope = false;
                slopeEntryTime = Time.time;
                slopeStopPosition = targetCar.transform.position;
                maxSlopeRollback = 0f;
                hasDeductedForRollback = false;
                slopeStopDuration = 0f;
                hasSlopeStoppedLongEnough = false;
                break;

            case ExamStep.VetBanhXeAndDuongVuongGoc:
                hasPassedBridgeMid = false;
                break;

            case ExamStep.QuaNgaTuDenTinHieu:
                ngaTuVisitCount++;
                activeTrafficLight = FindClosestTrafficLight();
                enteredStopLineOnRed = false;
                hasDeductedOverStopLine = false;

                if (ngaTuVisitCount == 3) // Lần 3: Rẽ trái
                {
                    if (!targetCar.isLeftBlinkerOn)
                    {
                        DeductPoints(5, "Không bật xi-nhan trái khi rẽ trái qua ngã tư");
                    }
                }
                else if (ngaTuVisitCount == 4) // Lần 4: Rẽ phải
                {
                    if (!targetCar.isRightBlinkerOn)
                    {
                        DeductPoints(5, "Không bật xi-nhan phải khi rẽ phải qua ngã tư");
                    }
                }
                break;

            case ExamStep.GhepDocVaoNoiDo:
            case ExamStep.GhepNgangVaoNoiDo:
                parkedSuccessfully = false;
                hasParkingStoppedLongEnough = false;
                parkingStopDuration = 0f;
                break;

            case ExamStep.TamDungNoiDuongSat:
                stoppedAtRailway = false;
                hasRailwayStoppedLongEnough = false;
                railwayStopDuration = 0f;
                break;

            case ExamStep.ThayDoiSoDuongBang:
                step9Segment = 1;
                lastStep9DeductedTime = Time.time; // Chờ 2s đệm khi vừa qua biển 1 trước khi kiểm tra tốc độ
                step9Segment1SpeedPassed = false;
                step9Segment2SpeedPassed = false;
                reachedTargetSpeed = false;
                slowedDownAfterTargetSpeed = false;
                break;

            case ExamStep.KetThuc:
                if (!targetCar.isRightBlinkerOn)
                {
                    DeductPoints(5, "Không bật xi-nhan phải khi kết thúc");
                }
                break;
        }
    }

    private TrafficLight FindClosestTrafficLight()
    {
        TrafficLight[] lights = Object.FindObjectsByType<TrafficLight>(FindObjectsInactive.Include);
        if (lights == null || lights.Length == 0) return null;

        TrafficLight closestLight = null;
        float minDist = float.MaxValue;
        Vector3 carPos = targetCar.transform.position;

        foreach (var light in lights)
        {
            float dist = Vector3.Distance(light.transform.position, carPos);
            if (dist < minDist)
            {
                minDist = dist;
                closestLight = light;
            }
        }
        return closestLight;
    }

    private void CheckRedLightViolation()
    {
        if (targetCar == null) return;
        
        if (activeTrafficLight == null)
        {
            activeTrafficLight = FindClosestTrafficLight();
        }
        
        if (activeTrafficLight == null) return;

        float dist = Vector3.Distance(activeTrafficLight.transform.position, targetCar.transform.position);

        if (activeTrafficLight.CurrentState != TrafficLightState.Red)
        {
            enteredStopLineOnRed = false;
        }
        else
        {
            // Xe đi quá vạch dừng (< 15m) khi đèn đang đỏ
            if (dist < 15f && dist >= 0f)
            {
                if (!enteredStopLineOnRed && !hasDeductedOverStopLine)
                {
                    enteredStopLineOnRed = true;
                }
            }
            else
            {
                if (dist >= 17f)
                {
                    enteredStopLineOnRed = false;
                }
            }
        }

        if (enteredStopLineOnRed)
        {
            if (targetCar.CurrentSpeed > 3f)
            {
                FailExam("Vượt đèn đỏ ngã tư (Lỗi trực tiếp trượt)!");
            }
            else if (targetCar.CurrentSpeed < 0.1f && !hasDeductedOverStopLine)
            {
                DeductPoints(5, "Dừng xe quá vạch giới hạn ngã tư khi có đèn đỏ");
                hasDeductedOverStopLine = true;
                enteredStopLineOnRed = false;
            }
        }
    }

    private void CalibrateLaneDetection()
    {
        if (targetCar == null) FindCar();
        if (targetCar == null) return;

        RaycastHit hit;
        Vector3 rayStart = targetCar.transform.position + Vector3.up * 1.5f;
        if (Physics.Raycast(rayStart, Vector3.down, out hit, 5f))
        {
            GameObject road = hit.collider.gameObject;
            string roadName = road.name.ToLower();
            if (roadName.Contains("road") || roadName.Contains("straight"))
            {
                Vector3 localPos = road.transform.InverseTransformPoint(targetCar.transform.position);
                calibratedCorrectSide = Mathf.Sign(localPos.x);
                isLaneCalibrated = true;
                Debug.Log($"[Lane Calibration] Road: {road.name}, localX: {localPos.x}, calibratedCorrectSide: {calibratedCorrectSide}");
            }
        }
    }

    private void CheckLaneViolation()
    {
        if (targetCar == null || !isExamActive) return;

        if (currentStep == ExamStep.GhepDocVaoNoiDo || 
            currentStep == ExamStep.GhepNgangVaoNoiDo || 
            currentStep == ExamStep.VetBanhXeAndDuongVuongGoc || 
            currentStep == ExamStep.DuongVongQuanhCo || 
            currentStep == ExamStep.QuaNgaTuDenTinHieu)
        {
            isInWrongLane = false;
            return;
        }

        if (!isLaneCalibrated)
        {
            CalibrateLaneDetection();
            return;
        }

        RaycastHit hit;
        Vector3 rayStart = targetCar.transform.position + Vector3.up * 1.5f;
        if (Physics.Raycast(rayStart, Vector3.down, out hit, 5f))
        {
            GameObject road = hit.collider.gameObject;
            string roadName = road.name.ToLower();

            if (roadName.Contains("straight") || roadName.Contains("road"))
            {
                Vector3 localPos = road.transform.InverseTransformPoint(targetCar.transform.position);
                float localX = localPos.x;

                float dot = Vector3.Dot(targetCar.transform.forward, road.transform.forward);
                float directionSign = (dot >= 0f) ? 1f : -1f;
                float expectedSign = calibratedCorrectSide * directionSign;

                if (Mathf.Sign(localX) != Mathf.Sign(expectedSign) && Mathf.Abs(localX) > 0.8f)
                {
                    if (!isInWrongLane)
                    {
                        isInWrongLane = true;
                        wrongLaneEntryTime = Time.time;
                        DeductPoints(5, "Đi đè vạch phân làn (lấn làn ngược chiều)");
                    }
                    else
                    {
                        if (targetCar.CurrentSpeed > 1f && (Time.time - wrongLaneEntryTime > 3f))
                        {
                            FailExam("Đi ngược chiều đường (Lỗi trực tiếp trượt)!");
                        }
                    }
                }
                else
                {
                    if (Mathf.Sign(localX) == Mathf.Sign(expectedSign) || Mathf.Abs(localX) < 0.2f)
                    {
                        isInWrongLane = false;
                    }
                }
            }
        }
    }

    // Hàm gọi từ trigger báo kết thúc bài thi (ExamEndTrigger)
    public void TriggerStepEnd(ExamStep step)
    {
        if (!isExamActive) return;
        if (currentStep != step) return;
        if (isCurrentStepValidated) return;

        int scoreBefore = currentScore;
        ValidatePreviousStepCompletion();

        // Hiển thị thông báo hoàn thành bài thi đúng luật ngay lập tức
        if (currentScore == scoreBefore)
        {
            var hud = Object.FindAnyObjectByType<HUDController>();
            if (hud != null)
            {
                hud.ShowSuccessNotification($"Hoàn thành Bài: {GetStepNameVi(step)} đúng luật!", 3.0f);
            }
        }
    }

    // Hàm gọi khi xe chạm vào Trigger của một bài thi mới
    public void EnterExamStep(ExamStep newStep)
    {
        if (!isExamActive) return;

        FindCar();
        if (targetCar == null) return;

        // Nếu xe chạm lại chính bài thi hiện tại, bỏ qua
        if (currentStep == newStep) return;

        // Kiểm tra trình tự thi sát hạch B2 thực tế
        if (sequenceIndex < correctSequence.Count)
        {
            ExamStep expectedStep = correctSequence[sequenceIndex];
            
            if (newStep != expectedStep)
            {
                // Đi sai trình tự (Skip required checkpoint) -> Trượt ngay lập tức
                FailExam($"Xe đi sai trình tự bài thi! Yêu cầu: {expectedStep}, nhưng lại đi vào: {newStep}");
                return;
            }
        }
        else
        {
            return;
        }

        // Kiểm tra bài thi cũ trước khi chuyển
        ExamStep prevStep = currentStep;
        int scoreBefore = currentScore;
        bool wasActiveBefore = isExamActive;

        bool alreadyValidated = isCurrentStepValidated;
        if (!alreadyValidated)
        {
            ValidatePreviousStepCompletion();
        }

        // Chuyển sang bài mới
        currentStep = newStep;
        sequenceIndex++;
        completedSteps.Add(newStep);
        isCurrentStepValidated = false; // Reset trạng thái xác thực cho bài mới

        // Hiển thị thông báo hoàn thành bài thi cũ đúng luật (nếu chưa được báo bởi ExamEndTrigger)
        if (wasActiveBefore && isExamActive && prevStep != ExamStep.None && !alreadyValidated)
        {
            if (currentScore == scoreBefore)
            {
                var hud = Object.FindAnyObjectByType<HUDController>();
                if (hud != null)
                {
                    hud.ShowSuccessNotification($"Hoàn thành Bài: {GetStepNameVi(prevStep)} đúng luật!", 3.0f);
                }
            }
        }

        Debug.Log($"Bắt đầu Bài Thi: {newStep} (Bước {sequenceIndex}/{correctSequence.Count})");

        // 1. Phát tiếng bính boong nhận bài
        PlaySFX(soundBinhBoong);

        // 2. Phát giọng nói tiếng Việt hướng dẫn
        AudioClip clipToPlay = GetVoiceClipForStep(newStep);
        if (clipToPlay != null)
        {
            StartCoroutine(PlayVoiceWithDelay(clipToPlay, 0.8f));
        }

        // 3. Khởi tạo trạng thái kiểm tra luật cho bài mới
        InitializeStepStates(newStep);

        // 4. Nếu là bài kết thúc, tự động hoàn thành thi sau khi giọng đọc xong
        if (newStep == ExamStep.KetThuc)
        {
            float delay = 0.8f + (clipToPlay != null ? clipToPlay.length : 2f) + 1.5f;
            StartCoroutine(CompleteExamAfterDelay(delay));
        }
    }

    public void SetStep9Segment(int segment)
    {
        if (!isExamActive || currentStep != ExamStep.ThayDoiSoDuongBang) return;
        if (step9Segment == segment) return; // Tránh reset lại timer nếu xe vẫn đang di chuyển trong cùng 1 phân đoạn

        step9Segment = segment;
        lastStep9DeductedTime = Time.time; // Chờ 2s đệm khi vừa qua biển mới trước khi kiểm tra trừ điểm

        var hud = Object.FindAnyObjectByType<HUDController>();
        if (hud != null)
        {
            if (segment == 1)
            {
                hud.ShowSuccessNotification("Vào đoạn Tăng tốc (Biển 1 -> Biển 2): Yêu cầu tốc độ > 20 km/h!", 3f);
            }
            else if (segment == 2)
            {
                hud.ShowSuccessNotification("Vào đoạn Giảm tốc (Biển 2 -> Biển 3): Yêu cầu tốc độ < 20 km/h!", 3f);
            }
            else if (segment == 3)
            {
                hud.ShowSuccessNotification("Đã qua Biển 3 (Rẽ trái): Đã hoàn thành Bài 9!", 3f);
            }
        }
    }

    private IEnumerator CompleteExamAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        if (isExamActive && currentScore >= 80)
        {
            PassExam();
        }
    }

    private AudioClip GetVoiceClipForStep(ExamStep step)
    {
        switch (step)
        {
            case ExamStep.XuatPhat: return voiceXuatPhat;
            case ExamStep.DungNhuongDuongDiBo: return voiceDungNhuongDiBo;
            case ExamStep.DungAndKhoiHanhNgangDoc: return voiceDePa;
            case ExamStep.VetBanhXeAndDuongVuongGoc: return voiceVetBanhXe;
            case ExamStep.QuaNgaTuDenTinHieu: return voiceNgaTu;
            case ExamStep.DuongVongQuanhCo: return voiceChuS;
            case ExamStep.GhepDocVaoNoiDo: return voiceGhepDoc;
            case ExamStep.TamDungNoiDuongSat: return voiceDuongSat;
            case ExamStep.ThayDoiSoDuongBang: return voiceTangGiamSo;
            case ExamStep.GhepNgangVaoNoiDo: return voiceGhepNgang;
            case ExamStep.KetThuc: return voiceKetThuc;
            default: return null;
        }
    }

    private void PlaySFX(AudioClip clip)
    {
        if (sfxSource != null && clip != null)
        {
            sfxSource.PlayOneShot(clip);
        }
    }

    private void PlayVoice(AudioClip clip)
    {
        if (voiceSource != null && clip != null)
        {
            voiceSource.Stop();
            voiceSource.clip = clip;
            voiceSource.Play();
        }
    }

    private IEnumerator PlayVoiceWithDelay(AudioClip clip, float delay)
    {
        yield return new WaitForSeconds(delay);
        if (isExamActive)
        {
            PlayVoice(clip);
        }
    }

    public void DeductPoints(int points, string reason)
    {
        if (!isExamActive) return;

        currentScore -= points;
        deductionsList.Add(new DeductionRecord { reason = reason, points = points });
        PlaySFX(soundTingTingError);
        Debug.Log($"Bị trừ {points} điểm. Lý do: {reason}. Điểm hiện tại: {currentScore}");

        // Hiển thị thông báo màu đỏ (Warning) lên HUD
        var hud = Object.FindAnyObjectByType<HUDController>();
        if (hud != null)
        {
            hud.ShowWarningNotification($"Trừ {points}đ: {reason}", 4.0f);
        }

        if (currentScore < 80)
        {
            FailExam("Điểm số dưới 80 điểm!");
        }
    }

    public void FailExam(string reason)
    {
        if (!isExamActive) return;

        isExamActive = false;
        deductionsList.Add(new DeductionRecord { reason = $"Đánh trượt: {reason}", points = 0 });
        PlayVoice(voiceFailExam);
        Debug.Log($"THI TRƯỢT! Lý do: {reason}");

        // Hiển thị thông báo cảnh báo trượt lên HUD
        var hud = Object.FindAnyObjectByType<HUDController>();
        if (hud != null)
        {
            hud.ShowWarningNotification($"THI TRƯỢT: {reason}", 6.0f);
        }

        StartCoroutine(ShowResultScreenDelayed(false, 2.0f));
    }

    public void PassExam()
    {
        if (!isExamActive) return;

        isExamActive = false;
        PlayVoice(voicePassExam);
        Debug.Log("CHÚC MỪNG BẠN ĐÃ THI ĐẠT!");

        // Hiển thị thông báo xanh lá (Success) lên HUD
        var hud = Object.FindAnyObjectByType<HUDController>();
        if (hud != null)
        {
            hud.ShowSuccessNotification("CHÚC MỪNG BẠN ĐÃ THI ĐẠT!", 6.0f);
        }

        StartCoroutine(ShowResultScreenDelayed(true, 2.0f));
    }

    private IEnumerator ShowResultScreenDelayed(bool isPass, float delay)
    {
        yield return new WaitForSeconds(delay);
        var hud = Object.FindAnyObjectByType<HUDController>();
        if (hud != null)
        {
            string finalStepName = (currentStep == ExamStep.None || currentStep == ExamStep.KetThuc) 
                ? "Bài 11: Kết thúc" 
                : GetFormattedStepName(currentStep);
            hud.ShowResultScreen(isPass, finalStepName, currentScore, deductionsList);
        }
    }

    public string GetFormattedStepName(ExamStep step)
    {
        switch (step)
        {
            case ExamStep.XuatPhat: return "Bài 1: Xuất phát";
            case ExamStep.DungNhuongDuongDiBo: return "Bài 2: Nhường đường đi bộ";
            case ExamStep.DungAndKhoiHanhNgangDoc: return "Bài 3: Dừng & Khởi hành ngang dốc";
            case ExamStep.VetBanhXeAndDuongVuongGoc: return "Bài 4: Qua vệt bánh xe";
            case ExamStep.QuaNgaTuDenTinHieu: return "Bài 5: Qua ngã tư đèn tín hiệu";
            case ExamStep.DuongVongQuanhCo: return "Bài 6: Đường vòng quanh co";
            case ExamStep.GhepDocVaoNoiDo: return "Bài 7: Ghép dọc vào nơi đỗ";
            case ExamStep.TamDungNoiDuongSat: return "Bài 8: Tạm dừng nơi đường sắt";
            case ExamStep.ThayDoiSoDuongBang: return "Bài 9: Thay đổi số đường bằng";
            case ExamStep.GhepNgangVaoNoiDo: return "Bài 10: Ghép ngang vào nơi đỗ";
            case ExamStep.KetThuc: return "Bài 11: Kết thúc";
            default: return "Bài thi sát hạch B2";
        }
    }

    private void OnDrawGizmos()
    {
        // 1. Vẽ các đường tròn và đường định vị dưới gầm các bánh xe bên phụ (bên phải) để trực quan hóa
        if (Application.isPlaying && targetCar != null && currentStep == ExamStep.VetBanhXeAndDuongVuongGoc)
        {
            Gizmos.color = hasPassedBridgeMid ? Color.green : Color.red;
            
            // Vẽ bánh trước phải
            if (targetCar.frontRightCollider != null)
            {
                Vector3 wheelPos;
                Quaternion wheelRot;
                targetCar.frontRightCollider.GetWorldPose(out wheelPos, out wheelRot);
                Gizmos.DrawLine(wheelPos, wheelPos + Vector3.down * 0.5f);
                Gizmos.DrawWireSphere(wheelPos, 0.35f); // Vẽ bao quanh bánh xe
            }
            
            // Vẽ bánh sau phải
            if (targetCar.rearRightCollider != null)
            {
                Vector3 wheelPos;
                Quaternion wheelRot;
                targetCar.rearRightCollider.GetWorldPose(out wheelPos, out wheelRot);
                Gizmos.DrawLine(wheelPos, wheelPos + Vector3.down * 0.5f);
                Gizmos.DrawWireSphere(wheelPos, 0.35f); // Vẽ bao quanh bánh xe
            }
        }

        // 2. Vẽ vùng quy định màu Cyan sáng cho các vật thể roadStraightBridgeMid trong Scene
        GameObject[] allObjects = Object.FindObjectsByType<GameObject>(FindObjectsInactive.Include);
        foreach (var go in allObjects)
        {
            if (go != null && go.name.ToLower().Contains("roadstraightbridgemid"))
            {
                Collider col = go.GetComponent<Collider>();
                if (col != null)
                {
                    // Màu xanh da trời trong suốt vẽ khối hộp
                    Gizmos.color = new Color(0f, 1f, 1f, 0.35f);
                    Gizmos.DrawCube(col.bounds.center, col.bounds.size);

                    // Màu xanh da trời đậm vẽ đường viền khung dây
                    Gizmos.color = Color.cyan;
                    Gizmos.DrawWireCube(col.bounds.center, col.bounds.size);
                }
            }
        }
    }
}
