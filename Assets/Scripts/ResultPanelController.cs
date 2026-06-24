using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

[RequireComponent(typeof(CanvasGroup))]
public class ResultPanelController : MonoBehaviour
{
    [Header("Top Accent")]
    public Image topAccentBar;

    [Header("Status Header")]
    public Image iconCircle;
    public TMP_Text iconText;
    public GameObject passIconVisual;
    public GameObject failIconVisual;
    public TMP_Text statusText;
    public TMP_Text stepNameText;

    [Header("Score")]
    public TMP_Text scoreText;

    [Header("Mistakes Panel")]
    public Transform mistakesContainer;
    public GameObject mistakeItemTemplate; // Template item containing error text and deduction text
    public TMP_Text noMistakesText;        // Shown when score is 100

    [Header("Buttons")]
    public Button restartButton;
    public Button menuButton;

    [Header("Colors")]
    public Color passColor = new Color(0.18f, 0.8f, 0.44f);      // Green (#2ECC71)
    public Color passIconColor = new Color(0.11f, 0.49f, 0.22f);  // Darker Green (#1E7D38)
    public Color failColor = new Color(0.91f, 0.3f, 0.24f);      // Red (#E74C3C)
    public Color failIconColor = new Color(0.76f, 0.12f, 0.08f);  // Darker Red (#C21F14)
    public Color deductionColor = new Color(0.95f, 0.37f, 0.06f); // Orange (#F35F10)
    
    private CanvasGroup canvasGroup;
    private float fadeDuration = 0.4f;

    private void Awake()
    {
        canvasGroup = GetComponent<CanvasGroup>();
        if (canvasGroup == null)
        {
            canvasGroup = gameObject.AddComponent<CanvasGroup>();
        }
        
        // Hide on start in actual play
        if (Application.isPlaying)
        {
            canvasGroup.alpha = 0f;
            gameObject.SetActive(false);
        }

        // Add button listeners
        if (restartButton != null)
        {
            restartButton.onClick.RemoveAllListeners();
            restartButton.onClick.AddListener(RestartExam);
        }

        if (menuButton != null)
        {
            menuButton.onClick.RemoveAllListeners();
            menuButton.onClick.AddListener(GoToMenu);
        }

        // Hide template
        if (mistakeItemTemplate != null)
        {
            mistakeItemTemplate.SetActive(false);
        }
    }

    /// <summary>
    /// Hiển thị màn hình báo cáo kết quả thi sát hạch
    /// </summary>
    public void Setup(bool isPass, string stepName, int score, List<ExamManager.DeductionRecord> mistakes)
    {
        // Kích hoạt panel
        gameObject.SetActive(true);
        Time.timeScale = 0f; // Dừng game để người chơi xem kết quả
        AudioListener.pause = true; // Mute tất cả âm thanh để đúng logic thi trượt/đạt

        // Hiện con trỏ chuột
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        // Cấu hình màu sắc và icon
        if (isPass)
        {
            if (topAccentBar != null) topAccentBar.color = passColor;
            if (iconCircle != null) iconCircle.color = passIconColor;
            if (iconText != null) iconText.text = "✓";
            if (passIconVisual != null) passIconVisual.SetActive(false);
            if (failIconVisual != null) failIconVisual.SetActive(false);
            if (statusText != null)
            {
                statusText.text = "ĐẠT";
                statusText.color = passColor;
            }
            if (scoreText != null)
            {
                scoreText.text = $"<color=#{ColorUtility.ToHtmlStringRGB(passColor)}>{score}</color><size=60%><color=#FFFFFF>/100</color></size>";
            }
        }
        else
        {
            if (topAccentBar != null) topAccentBar.color = failColor;
            if (iconCircle != null) iconCircle.color = failIconColor;
            if (iconText != null) iconText.text = "✕";
            if (passIconVisual != null) passIconVisual.SetActive(false);
            if (failIconVisual != null) failIconVisual.SetActive(false);
            if (statusText != null)
            {
                statusText.text = "KHÔNG ĐẠT";
                statusText.color = failColor;
            }
            if (scoreText != null)
            {
                scoreText.text = $"<color=#{ColorUtility.ToHtmlStringRGB(failColor)}>{score}</color><size=60%><color=#FFFFFF>/100</color></size>";
            }
        }

        // Cập nhật tên bài thi kết thúc
        if (stepNameText != null)
        {
            stepNameText.text = stepName;
        }

        // Xóa danh sách lỗi cũ (trừ template)
        if (mistakesContainer != null && mistakeItemTemplate != null)
        {
            foreach (Transform child in mistakesContainer)
            {
                if (child.gameObject != mistakeItemTemplate)
                {
                    Destroy(child.gameObject);
                }
            }

            // Tạo danh sách lỗi mới
            if (mistakes == null || mistakes.Count == 0)
            {
                if (noMistakesText != null)
                {
                    noMistakesText.gameObject.SetActive(true);
                    noMistakesText.text = "Tuyệt vời! Bạn không phạm lỗi nào.";
                }
            }
            else
            {
                if (noMistakesText != null)
                {
                    noMistakesText.gameObject.SetActive(false);
                }

                foreach (var mistake in mistakes)
                {
                    GameObject itemGo = Instantiate(mistakeItemTemplate, mistakesContainer);
                    itemGo.SetActive(true);

                    // Tìm các text component con
                    TMP_Text[] texts = itemGo.GetComponentsInChildren<TMP_Text>();
                    if (texts.Length >= 2)
                    {
                        texts[0].text = $"- {mistake.reason}";
                        texts[1].text = $"-{mistake.points}";
                        texts[1].color = deductionColor;
                    }
                }
            }
        }

        // Chạy hiệu ứng hiển thị mượt mà
        StartCoroutine(FadeInRoutine());
    }

    private IEnumerator FadeInRoutine()
    {
        float elapsed = 0f;
        canvasGroup.alpha = 0f;

        while (elapsed < fadeDuration)
        {
            elapsed += Time.unscaledDeltaTime; // Sử dụng unscaledDeltaTime vì TimeScale = 0
            canvasGroup.alpha = Mathf.Lerp(0f, 1f, elapsed / fadeDuration);
            yield return null;
        }

        canvasGroup.alpha = 1f;
    }

    public void RestartExam()
    {
        AudioListener.pause = false; // Bật lại âm thanh
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    public void GoToMenu()
    {
        AudioListener.pause = false; // Bật lại âm thanh
        Time.timeScale = 1f;
        SceneManager.LoadScene("MainMenu");
    }
}
