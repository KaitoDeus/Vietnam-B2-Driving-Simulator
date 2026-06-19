using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

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

    [Header("Scene Settings")]
    [Tooltip("Tên của cảnh sa hình thực hành lái xe")]
    public string practiceSceneName = "Practice";

    private void Start()
    {
        // Thiết lập trạng thái mặc định của các Panel khi bắt đầu
        if (mainMenuPanel != null) mainMenuPanel.SetActive(true);
        if (settingsPanel != null) settingsPanel.SetActive(false);
        if (aboutPanel != null) aboutPanel.SetActive(false);
        if (exitConfirmationPanel != null) exitConfirmationPanel.SetActive(false);

        // Đảm bảo con trỏ chuột hiển thị và không bị khóa khi ở Menu chính
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
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
        Debug.Log("[MainMenu] Khởi chạy bài thi lý thuyết (Tính năng đang phát triển)...");
        // Có thể mở một panel thông báo hoặc load cảnh thi lý thuyết tại đây
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
