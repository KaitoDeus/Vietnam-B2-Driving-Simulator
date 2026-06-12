#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;

/// <summary>
/// Editor tool tự động cấu hình hệ thống bài thi B2:
/// 1. Tạo ExamManager GameObject với 2 AudioSource (voice + SFX)
/// 2. Gán toàn bộ AudioClip vào đúng slot
/// 3. Tạo 11 ExamTrigger zone với BoxCollider (isTrigger) tại các vị trí trên sa hình
/// 4. Gán tag "Player" cho PlayerCar
/// 
/// Tự động chạy khi thoát Play Mode nếu ExamManager chưa tồn tại.
/// Hoặc chạy thủ công qua menu: Tools -> Configure Exam System
/// </summary>
[InitializeOnLoad]
public class ConfigureExamSystem
{
    static ConfigureExamSystem()
    {
        EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
    }

    private static void OnPlayModeStateChanged(PlayModeStateChange state)
    {
        if (state == PlayModeStateChange.EnteredEditMode)
        {
            // Chỉ chạy Setup nếu chưa có ExamManager trong scene
            ExamManager existing = Object.FindFirstObjectByType<ExamManager>();
            if (existing == null)
            {
                Debug.Log("[ConfigureExamSystem] Phát hiện ExamManager chưa tồn tại, tự động cấu hình...");
                EditorApplication.delayCall += Setup;
            }
        }
    }

    [MenuItem("Tools/Configure Exam System")]
    public static void Setup()
    {
        Debug.Log("[ConfigureExamSystem] Bắt đầu cấu hình hệ thống thi...");

        // ===== 1. TẠO HOẶC TÌM ExamManager =====
        ExamManager examManager = Object.FindFirstObjectByType<ExamManager>();
        GameObject examManagerGo;

        if (examManager == null)
        {
            examManagerGo = new GameObject("ExamManager");
            examManager = examManagerGo.AddComponent<ExamManager>();
            Debug.Log("[ConfigureExamSystem] Đã tạo ExamManager mới.");
        }
        else
        {
            examManagerGo = examManager.gameObject;
            Debug.Log("[ConfigureExamSystem] Tìm thấy ExamManager hiện tại.");
        }

        // ===== 2. CẤU HÌNH AudioSource =====
        AudioSource[] sources = examManagerGo.GetComponents<AudioSource>();

        // Cần ít nhất 2 AudioSource: voiceSource và sfxSource
        if (sources.Length < 2)
        {
            // Xóa source cũ nếu có 1 để tạo lại
            for (int i = sources.Length - 1; i >= 0; i--)
            {
                Object.DestroyImmediate(sources[i]);
            }

            // Tạo Voice AudioSource
            AudioSource voiceSource = examManagerGo.AddComponent<AudioSource>();
            voiceSource.playOnAwake = false;
            voiceSource.loop = false;
            voiceSource.spatialBlend = 0f; // 2D sound (phát đều)
            voiceSource.volume = 1f;

            // Tạo SFX AudioSource
            AudioSource sfxSource = examManagerGo.AddComponent<AudioSource>();
            sfxSource.playOnAwake = false;
            sfxSource.loop = false;
            sfxSource.spatialBlend = 0f; // 2D sound
            sfxSource.volume = 1f;

            examManager.voiceSource = voiceSource;
            examManager.sfxSource = sfxSource;
            Debug.Log("[ConfigureExamSystem] Đã tạo 2 AudioSource (Voice + SFX).");
        }
        else
        {
            // Gán lại nếu chưa gán
            if (examManager.voiceSource == null) examManager.voiceSource = sources[0];
            if (examManager.sfxSource == null) examManager.sfxSource = sources[1];
        }

        // ===== 3. GÁN AUDIO CLIPS =====
        string voiceDir = "Assets/Audio/Voice";

        examManager.soundBinhBoong = LoadClip($"{voiceDir}/sound_binh_boong.wav");
        examManager.soundTingTingError = LoadClip($"{voiceDir}/sound_ting_ting_error.wav");

        examManager.voiceStartExam = LoadClip($"{voiceDir}/voice_start_exam.mp3");
        examManager.voicePassExam = LoadClip($"{voiceDir}/voice_pass_exam.mp3");
        examManager.voiceFailExam = LoadClip($"{voiceDir}/voice_fail_exam.mp3");

        examManager.voiceXuatPhat = LoadClip($"{voiceDir}/voice_xuat_phat.mp3");
        examManager.voiceDungNhuongDiBo = LoadClip($"{voiceDir}/voice_dung_nhuong_di_bo.mp3");
        examManager.voiceDePa = LoadClip($"{voiceDir}/voice_depa.mp3");
        examManager.voiceVetBanhXe = LoadClip($"{voiceDir}/voice_vet_banh_xe.mp3");
        examManager.voiceNgaTu = LoadClip($"{voiceDir}/voice_nga_tu.mp3");
        examManager.voiceChuS = LoadClip($"{voiceDir}/voice_chu_s.mp3");
        examManager.voiceGhepDoc = LoadClip($"{voiceDir}/voice_ghep_doc.mp3");
        examManager.voiceDuongSat = LoadClip($"{voiceDir}/voice_duong_sat.mp3");
        examManager.voiceTangGiamSo = LoadClip($"{voiceDir}/voice_tang_giam_so.mp3");
        examManager.voiceGhepNgang = LoadClip($"{voiceDir}/voice_ghep_ngang.mp3");
        examManager.voiceKetThuc = LoadClip($"{voiceDir}/voice_ket_thuc.mp3");

        EditorUtility.SetDirty(examManager);
        Debug.Log("[ConfigureExamSystem] Đã gán toàn bộ AudioClip cho ExamManager.");

        // ===== 4. GÁN TAG "Player" CHO PlayerCar =====
        // Đảm bảo tag "Player" tồn tại (đây là tag mặc định của Unity)
        GameObject playerCar = GameObject.Find("PlayerCar");
        if (playerCar != null)
        {
            playerCar.tag = "Player";
            EditorUtility.SetDirty(playerCar);
            Debug.Log("[ConfigureExamSystem] Đã gán tag 'Player' cho PlayerCar.");
        }
        else
        {
            Debug.LogWarning("[ConfigureExamSystem] Không tìm thấy PlayerCar trong scene!");
        }

        // ===== 5. TẠO EXAM TRIGGER ZONES =====
        // Tìm hoặc tạo parent container
        GameObject triggersParent = GameObject.Find("ExamTriggers");
        if (triggersParent != null)
        {
            // Xóa các trigger cũ để tạo lại
            Object.DestroyImmediate(triggersParent);
            Debug.Log("[ConfigureExamSystem] Đã xóa ExamTriggers cũ, tạo lại...");
        }
        triggersParent = new GameObject("ExamTriggers");

        // Định nghĩa vị trí các trigger zone trên sa hình
        // Dựa trên layout thực tế của sa hình B2 Việt Nam
        // PlayerCar bắt đầu tại (270.3, 1.3, -37.85) hướng xấp xỉ -90°Y (đi về phía -X)
        //
        // Sa hình B2 tiêu chuẩn: xe đi theo chiều kim đồng hồ
        // Các vị trí trigger phải đặt VÀO ĐÚNG làn đường mà xe sẽ đi qua
        //
        // LƯU Ý: Các vị trí này là ước lượng ban đầu dựa trên cấu trúc scene.
        // Bạn cần chỉnh sửa vị trí trong Unity Editor (di chuyển các trigger) cho phù hợp 
        // với layout sa hình thực tế của bạn.

        var triggerData = new (ExamStep step, string name, Vector3 position, Vector3 size, float rotationY)[]
        {
            // Bài 1: Xuất phát - ngay vị trí xe bắt đầu, phía trước xe
            (ExamStep.XuatPhat, 
             "Trigger_01_XuatPhat", 
             new Vector3(260f, 1.5f, -37.85f), 
             new Vector3(8f, 4f, 6f), 0f),

            // Bài 2: Dừng nhường đường đi bộ - vạch kẻ đường cho người đi bộ (zebra crossing)
            (ExamStep.DungNhuongDuongDiBo, 
             "Trigger_02_DungNhuongDiBo", 
             new Vector3(210f, 1.5f, -37.85f), 
             new Vector3(8f, 4f, 6f), 0f),

            // Bài 3: Dừng và khởi hành ngang dốc (Đề-pa)
            (ExamStep.DungAndKhoiHanhNgangDoc, 
             "Trigger_03_DePa", 
             new Vector3(170f, 2.5f, -37.85f), 
             new Vector3(8f, 4f, 6f), 0f),

            // Bài 4: Vệt bánh xe và đường vòng vuông góc
            (ExamStep.VetBanhXeAndDuongVuongGoc, 
             "Trigger_04_VetBanhXe", 
             new Vector3(130f, 1.5f, -37.85f), 
             new Vector3(8f, 4f, 6f), 0f),

            // Bài 5: Qua ngã tư có đèn tín hiệu - tại khu vực ngã tư đèn giao thông
            (ExamStep.QuaNgaTuDenTinHieu, 
             "Trigger_05_NgaTu", 
             new Vector3(90f, 1.5f, -37.85f), 
             new Vector3(8f, 4f, 6f), 0f),

            // Bài 6: Đường vòng quanh co (Chữ S)
            (ExamStep.DuongVongQuanhCo, 
             "Trigger_06_ChuS", 
             new Vector3(50f, 1.5f, -37.85f), 
             new Vector3(8f, 4f, 6f), 0f),

            // Bài 7: Ghép xe dọc vào nơi đỗ (Chuồng dọc)
            (ExamStep.GhepDocVaoNoiDo, 
             "Trigger_07_GhepDoc", 
             new Vector3(10f, 1.5f, -37.85f), 
             new Vector3(8f, 4f, 6f), 0f),

            // Bài 8: Tạm dừng nơi có đường sắt
            (ExamStep.TamDungNoiDuongSat, 
             "Trigger_08_DuongSat", 
             new Vector3(-30f, 1.5f, -37.85f), 
             new Vector3(8f, 4f, 6f), 0f),

            // Bài 9: Thay đổi số trên đường bằng
            (ExamStep.ThayDoiSoDuongBang, 
             "Trigger_09_TangGiamSo", 
             new Vector3(-70f, 1.5f, -37.85f), 
             new Vector3(8f, 4f, 6f), 0f),

            // Bài 10: Ghép xe ngang vào nơi đỗ (Chuồng ngang)
            (ExamStep.GhepNgangVaoNoiDo, 
             "Trigger_10_GhepNgang", 
             new Vector3(-110f, 1.5f, -37.85f), 
             new Vector3(8f, 4f, 6f), 0f),

            // Bài 11: Kết thúc - cuối sa hình
            (ExamStep.KetThuc, 
             "Trigger_11_KetThuc", 
             new Vector3(-150f, 1.5f, -37.85f), 
             new Vector3(8f, 4f, 6f), 0f),
        };

        foreach (var data in triggerData)
        {
            GameObject triggerGo = new GameObject(data.name);
            triggerGo.transform.SetParent(triggersParent.transform);
            triggerGo.transform.position = data.position;
            triggerGo.transform.rotation = Quaternion.Euler(0f, data.rotationY, 0f);

            // Thêm BoxCollider trigger
            BoxCollider box = triggerGo.AddComponent<BoxCollider>();
            box.isTrigger = true;
            box.size = data.size;
            box.center = Vector3.zero;

            // Thêm ExamTrigger component
            ExamTrigger trigger = triggerGo.AddComponent<ExamTrigger>();
            trigger.examStep = data.step;

            EditorUtility.SetDirty(triggerGo);
        }

        Debug.Log($"[ConfigureExamSystem] Đã tạo {triggerData.Length} ExamTrigger zones.");

        // ===== 6. LƯU SCENE =====
        EditorUtility.SetDirty(triggersParent);
        AssetDatabase.SaveAssets();
        EditorSceneManager.SaveOpenScenes();
        Debug.Log("[ConfigureExamSystem] Cấu hình hệ thống thi hoàn tất! Scene đã được lưu.");
        Debug.Log("[ConfigureExamSystem] LƯU Ý: Bạn cần di chuyển các ExamTrigger trong Scene View để khớp với layout sa hình thực tế.");
    }

    private static AudioClip LoadClip(string path)
    {
        AudioClip clip = AssetDatabase.LoadAssetAtPath<AudioClip>(path);
        if (clip == null)
        {
            Debug.LogWarning($"[ConfigureExamSystem] Không tìm thấy AudioClip: {path}");
        }
        return clip;
    }
}
#endif
