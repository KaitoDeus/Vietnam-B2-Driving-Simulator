using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

[RequireComponent(typeof(CanvasGroup))]
public class NotificationController : MonoBehaviour
{
    public enum NotificationState
    {
        Warning,
        Success
    }

    [Header("UI Components")]
    public TMP_Text notificationText;
    public Image backgroundImage;
    private CanvasGroup canvasGroup;

    [Header("Aesthetic Settings")]
    public Color warningBgColor = new Color(0.898f, 0.224f, 0.208f, 0.95f); // #E53935 Red
    public Color successBgColor = new Color(0.263f, 0.627f, 0.278f, 0.95f); // #43A047 Green
    public float fadeDuration = 0.25f;

    [Header("Editor Preview")]
    public NotificationState previewState = NotificationState.Warning;
    [TextArea(2, 4)]
    public string previewMessage = "Đi sai làn đường";

    private Coroutine activeFadeRoutine;
    private Coroutine activeDisplayRoutine;

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
    }

    /// <summary>
    /// Hiển thị thông báo với nội dung và trạng thái tương ứng
    /// </summary>
    public void Show(string message, NotificationState state, float duration = 3f)
    {
        gameObject.SetActive(true);
        
        if (notificationText != null)
        {
            notificationText.text = message;
        }

        if (backgroundImage != null)
        {
            backgroundImage.color = (state == NotificationState.Success) ? successBgColor : warningBgColor;
        }

        if (activeDisplayRoutine != null) StopCoroutine(activeDisplayRoutine);
        if (activeFadeRoutine != null) StopCoroutine(activeFadeRoutine);

        activeDisplayRoutine = StartCoroutine(DisplaySequence(duration));
    }

    private IEnumerator DisplaySequence(float duration)
    {
        // Fade in
        activeFadeRoutine = StartCoroutine(FadeRoutine(1f));
        yield return activeFadeRoutine;
        
        // Chờ hiển thị
        yield return new WaitForSecondsRealtime(duration);
        
        // Fade out
        activeFadeRoutine = StartCoroutine(FadeRoutine(0f));
        yield return activeFadeRoutine;
        
        gameObject.SetActive(false);
    }

    private IEnumerator FadeRoutine(float targetAlpha)
    {
        if (canvasGroup == null) canvasGroup = GetComponent<CanvasGroup>();
        
        float startAlpha = canvasGroup.alpha;
        float elapsed = 0f;

        while (elapsed < fadeDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            canvasGroup.alpha = Mathf.Lerp(startAlpha, targetAlpha, elapsed / fadeDuration);
            yield return null;
        }

        canvasGroup.alpha = targetAlpha;
    }

    // ========================================================
    // EDITOR CONTEXT MENU PREVIEWS
    // ========================================================

    [ContextMenu("Preview Notification (Warning)")]
    public void PreviewWarningState()
    {
        SetupPreview(previewMessage, NotificationState.Warning);
    }

    [ContextMenu("Preview Notification (Success)")]
    public void PreviewSuccessState()
    {
        SetupPreview("Hoàn thành bài thi", NotificationState.Success);
    }

    [ContextMenu("Hide Preview")]
    public void HidePreview()
    {
        canvasGroup = GetComponent<CanvasGroup>();
        if (canvasGroup != null) canvasGroup.alpha = 0f;
    }

    private void SetupPreview(string message, NotificationState state)
    {
        canvasGroup = GetComponent<CanvasGroup>();
        if (canvasGroup != null) canvasGroup.alpha = 1f;

        if (notificationText != null)
        {
            notificationText.text = message;
        }

        if (backgroundImage != null)
        {
            backgroundImage.color = (state == NotificationState.Success) ? successBgColor : warningBgColor;
        }
    }

    /// <summary>
    /// Tạo Panel_Notification động tại runtime nếu chưa được thiết lập sẵn trong scene.
    /// Tự động tái sử dụng Sprite bo góc và Font của các phần tử HUD xung quanh để đồng bộ giao diện.
    /// </summary>
    public static NotificationController Create(Transform parent)
    {
        GameObject panelGo = new GameObject("Panel_Notification", typeof(RectTransform));
        panelGo.transform.SetParent(parent, false);

        RectTransform panelRect = panelGo.GetComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(0.5f, 1f); // Top Center
        panelRect.anchorMax = new Vector2(0.5f, 1f);
        panelRect.pivot = new Vector2(0.5f, 0.5f);
        panelRect.anchoredPosition = new Vector2(0f, -220f);

        // Tìm sprite bo góc từ các panel khác (ví dụ Panel_Pause)
        Sprite roundedSprite = null;
        Image[] allImages = parent.GetComponentsInChildren<Image>(true);
        foreach (var img in allImages)
        {
            if (img.sprite != null && img.type == Image.Type.Sliced && img.name.Contains("Pause"))
            {
                roundedSprite = img.sprite;
                break;
            }
        }
        if (roundedSprite == null)
        {
            foreach (var img in allImages)
            {
                if (img.sprite != null && img.type == Image.Type.Sliced)
                {
                    roundedSprite = img.sprite;
                    break;
                }
            }
        }

        Image bgImage = panelGo.AddComponent<Image>();
        if (roundedSprite != null)
        {
            bgImage.sprite = roundedSprite;
            bgImage.type = Image.Type.Sliced;
        }
        bgImage.color = new Color(0.898f, 0.224f, 0.208f, 0.95f);

        CanvasGroup canvasGroup = panelGo.AddComponent<CanvasGroup>();
        canvasGroup.alpha = 0f;

        HorizontalLayoutGroup layout = panelGo.AddComponent<HorizontalLayoutGroup>();
        layout.padding = new RectOffset(20, 20, 8, 8);
        layout.childAlignment = TextAnchor.MiddleCenter;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = false;
        layout.childForceExpandHeight = false;

        ContentSizeFitter fitter = panelGo.AddComponent<ContentSizeFitter>();
        fitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        GameObject textGo = new GameObject("Txt_Notification", typeof(RectTransform), typeof(TextMeshProUGUI));
        textGo.transform.SetParent(panelGo.transform, false);

        TextMeshProUGUI textComp = textGo.GetComponent<TextMeshProUGUI>();

        // Tìm Font TMPro đồng bộ từ các text hiện có
        TMP_FontAsset mainFont = null;
        TMP_Text[] allTexts = parent.GetComponentsInChildren<TMP_Text>(true);
        foreach (var txt in allTexts)
        {
            if (txt.font != null)
            {
                mainFont = txt.font;
                break;
            }
        }
        if (mainFont != null)
        {
            textComp.font = mainFont;
        }

        textComp.text = "Đi sai làn đường";
        textComp.fontSize = 16f;
        textComp.fontStyle = FontStyles.Bold;
        textComp.color = Color.white;
        textComp.alignment = TextAlignmentOptions.Center;

        NotificationController controller = panelGo.AddComponent<NotificationController>();
        controller.notificationText = textComp;
        controller.backgroundImage = bgImage;

        panelGo.SetActive(false);
        return controller;
    }
}
