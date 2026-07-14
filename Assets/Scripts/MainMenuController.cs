using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

public class MainMenuController : MonoBehaviour
{
    [Header("UI Panels")]
    [Tooltip("Panel chứa danh sách nút bấm chính của Menu")]
    public GameObject mainMenuPanel;
    
    [Tooltip("Panel hiển thị phần Cài đặt (Settings)")]
    public GameObject settingsPanel;
    
    [Tooltip("Panel hiển thị phần Thông tin (About)")]
    public GameObject aboutPanel;
    
    [Tooltip("Panel hiển thị Xác nhận thoát (Exit Confirmation)")]
    public GameObject exitConfirmationPanel;

    [Header("WebGL Test Settings")]
    [Tooltip("Tích chọn để giả lập chế độ WebGL ngay trong Editor nhằm test ẩn nút Giới thiệu/Thoát")]
    public bool simulateWebGLInEditor = false;

    [Header("Scene Settings")]
    [Tooltip("Tên của cảnh sa hình thực hành lái xe")]
    public string practiceSceneName = "Practice";

    private void Start()
    {
        // Khôi phục trạng thái âm thanh toàn cục
        AudioListener.pause = false;

        // Thiết lập trạng thái mặc định của các Panel khi bắt đầu
        if (mainMenuPanel != null) mainMenuPanel.SetActive(true);
        if (settingsPanel != null) settingsPanel.SetActive(false);
        if (aboutPanel != null) aboutPanel.SetActive(false);
        if (exitConfirmationPanel != null) exitConfirmationPanel.SetActive(false);

        // Đảm bảo con trỏ chuột hiển thị và không bị khóa khi ở Menu chính
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        // Tự động tìm và gán sự kiện cho các nút bấm
        BindMenuButtons();
    }

    private void BindMenuButtons()
    {
        if (mainMenuPanel == null)
        {
            // Thử tìm Panel_MainMenu nếu biến bị null
            Transform panelTrans = transform.Find("Panel_MainMenu");
            if (panelTrans == null) panelTrans = transform.Find("MainMenuPanel");
            if (panelTrans != null) mainMenuPanel = panelTrans.gameObject;
        }

        if (mainMenuPanel == null) return;

        bool isWebGL = false;
#if UNITY_WEBGL
        isWebGL = true;
#endif
#if UNITY_EDITOR
        if (simulateWebGLInEditor) isWebGL = true;
#endif

        Button[] buttons = mainMenuPanel.GetComponentsInChildren<Button>(true);
        foreach (Button btn in buttons)
        {
            string btnText = "";
            TMP_Text tmp = btn.GetComponentInChildren<TMP_Text>(true);
            if (tmp != null)
            {
                btnText = tmp.text;
            }
            else
            {
                Text legacyText = btn.GetComponentInChildren<Text>(true);
                if (legacyText != null) btnText = legacyText.text;
            }

            btnText = btnText.ToUpper().Trim();
            if (string.IsNullOrEmpty(btnText)) continue;

            if (btnText.Contains("LÝ THUYẾT") || btnText.Contains("LY THUYET"))
            {
                btn.onClick.RemoveListener(StartTheoryExam);
                btn.onClick.AddListener(StartTheoryExam);
            }
            else if (btnText.Contains("THỰC HÀNH") || btnText.Contains("THUC HANH"))
            {
                btn.onClick.RemoveListener(StartPracticalExam);
                btn.onClick.AddListener(StartPracticalExam);
            }
            else if (btnText.Contains("CÀI ĐẶT") || btnText.Contains("CAI DAT") || btnText.Contains("SETTINGS"))
            {
                btn.onClick.RemoveListener(OpenSettings);
                btn.onClick.AddListener(OpenSettings);
            }
            else if (btnText.Contains("GIỚI THIỆU") || btnText.Contains("GIOI THIEU") || btnText.Contains("ABOUT"))
            {
                if (isWebGL)
                {
                    btn.gameObject.SetActive(false);
                }
                else
                {
                    btn.onClick.RemoveListener(OpenAbout);
                    btn.onClick.AddListener(OpenAbout);
                }
            }
            else if (btnText.Contains("THOÁT") || btnText.Contains("THOAT") || btnText.Contains("EXIT"))
            {
                if (isWebGL)
                {
                    btn.gameObject.SetActive(false);
                }
                else
                {
                    btn.onClick.RemoveListener(ShowExitConfirmation);
                    btn.onClick.AddListener(ShowExitConfirmation);
                }
            }
        }
    }

    // ==========================================
    // 1. CHỨC NĂNG THI THỰC HÀNH (PRACTICAL EXAM)
    // ==========================================
    public void StartPracticalExam()
    {
        Debug.Log("[MainMenu] Khởi chạy bài thi sát hạch thực hành sa hình...");
        if (!string.IsNullOrEmpty(practiceSceneName))
        {
            SceneManager.LoadScene(practiceSceneName);
        }
        else
        {
            Debug.LogError("[MainMenu] Chưa chỉ định tên cảnh thi thực hành!");
        }
    }

    // ==========================================
    // 2. CHỨC NĂNG THI LÝ THUYẾT (THEORY EXAM - Mở rộng sau)
    // ==========================================
    public void StartTheoryExam()
    {
        Debug.Log("[MainMenu] Khởi chạy bài thi lý thuyết...");
        SceneManager.LoadScene("TheoryExam");
    }

    // ==========================================
    // 3. ĐIỀU HƯỚNG CÁC PANEL (SETTINGS & ABOUT)
    // ==========================================
    public void OpenSettings()
    {
        if (settingsPanel != null) settingsPanel.SetActive(true);
        if (mainMenuPanel != null) mainMenuPanel.SetActive(false);
    }

    public void CloseSettings()
    {
        if (settingsPanel != null) settingsPanel.SetActive(false);
        if (mainMenuPanel != null) mainMenuPanel.SetActive(true);
    }

    public void OpenAbout()
    {
        if (aboutPanel != null) aboutPanel.SetActive(true);
        if (mainMenuPanel != null) mainMenuPanel.SetActive(false);
    }

    public void CloseAbout()
    {
        if (aboutPanel != null) aboutPanel.SetActive(false);
        if (mainMenuPanel != null) mainMenuPanel.SetActive(true);
    }

    // ==========================================
    // 4. XỬ LÝ THOÁT GAME (EXIT SYSTEM)
    // ==========================================
    public void ShowExitConfirmation()
    {
        if (exitConfirmationPanel != null) exitConfirmationPanel.SetActive(true);
        // Giữ menu chính hiển thị mờ phía sau hoặc ẩn đi tùy thiết kế
    }

    public void CancelExit()
    {
        if (exitConfirmationPanel != null) exitConfirmationPanel.SetActive(false);
    }

    public void ConfirmExit()
    {
        Debug.Log("[MainMenu] Thoát ứng dụng...");
        #if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
        #else
        Application.Quit();
        #endif
    }
}
