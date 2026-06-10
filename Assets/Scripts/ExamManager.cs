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

    private HashSet<ExamStep> completedSteps = new HashSet<ExamStep>();

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    private void Start()
    {
        // Tự động phát âm thanh mời khởi hành khi vào game
        StartCoroutine(StartExamDelayed());
    }

    private IEnumerator StartExamDelayed()
    {
        yield return new WaitForSeconds(1.5f);
        PlayVoice(voiceStartExam);
        isExamActive = true;
    }

    private void Update()
    {
        if (isExamActive && totalExamTime > 0)
        {
            totalExamTime -= Time.deltaTime;
            if (totalExamTime <= 0)
            {
                FailExam("Hết thời gian thi quy định!");
            }
        }
    }

    // Hàm gọi khi xe chạm vào Trigger của một bài thi mới
    public void EnterExamStep(ExamStep newStep)
    {
        if (!isExamActive || currentStep == newStep || completedSteps.Contains(newStep)) return;

        // Chống đi tắt (Kiểm tra trình tự cơ bản, có thể bỏ qua nếu sa hình ngã tư đi qua nhiều lần)
        if (newStep != ExamStep.XuatPhat && !completedSteps.Contains(newStep - 1) && newStep != ExamStep.QuaNgaTuDenTinHieu)
        {
            Debug.LogWarning($"Xe đi sai trình tự bài thi! Đang nhảy cóc đến: {newStep}");
        }

        currentStep = newStep;
        completedSteps.Add(newStep);
        
        Debug.Log($"Bắt đầu Bài Thi: {newStep}");

        // 1. Phát tiếng bính boong nhận bài
        PlaySFX(soundBinhBoong);

        // 2. Phát giọng nói tiếng Việt hướng dẫn
        AudioClip clipToPlay = GetVoiceClipForStep(newStep);
        if (clipToPlay != null)
        {
            // Phát giọng đọc với độ trễ nhẹ sau tiếng bính boong
            StartCoroutine(PlayVoiceWithDelay(clipToPlay, 0.8f));
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
        if (isExamActive) // Kiểm tra đề phòng xe đã trượt trong thời gian chờ
        {
            PlayVoice(clip);
        }
    }

    public void DeductPoints(int points, string reason)
    {
        if (!isExamActive) return;

        currentScore -= points;
        PlaySFX(soundTingTingError);
        Debug.Log($"Bị trừ {points} điểm. Lý do: {reason}. Điểm hiện tại: {currentScore}");

        if (currentScore < 80)
        {
            FailExam("Điểm số dưới 80 điểm!");
        }
    }

    public void FailExam(string reason)
    {
        if (!isExamActive) return;

        isExamActive = false;
        PlayVoice(voiceFailExam);
        Debug.Log($"THI TRƯỢT! Lý do: {reason}");
    }

    public void PassExam()
    {
        if (!isExamActive) return;

        isExamActive = false;
        PlayVoice(voicePassExam);
        Debug.Log("CHÚC MỪNG BẠN ĐÃ THI ĐẠT!");
    }
}
