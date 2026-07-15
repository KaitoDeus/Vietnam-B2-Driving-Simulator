using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class InstructionsPopup : MonoBehaviour
{
    private static InstructionsPopup activeInstance;
    private TextMeshProUGUI spaceHintText;

    private Sprite roundedSprite;
    private Sprite solidSprite;

    public static bool IsActive => activeInstance != null;

    public static void Create(Transform parent)
    {
        if (activeInstance != null)
        {
            Destroy(activeInstance.gameObject);
        }

        // Tạo GameObject root cho Popup
        GameObject go = new GameObject("InstructionsPopup");
        go.transform.SetParent(parent, false);

        // Cấu hình Canvas và RectTransform toàn màn hình
        RectTransform rect = go.AddComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.sizeDelta = Vector2.zero;
        rect.anchoredPosition = Vector2.zero;

        // Thêm script điều khiển
        activeInstance = go.AddComponent<InstructionsPopup>();
        activeInstance.BuildUI();
    }

    private Sprite CreateRoundedSprite(int width, int height, int cornerRadius, Color color)
    {
        Texture2D tex = new Texture2D(width, height, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Bilinear;
        tex.wrapMode = TextureWrapMode.Clamp;

        Color[] pixels = new Color[width * height];
        float cx = width / 2f;
        float cy = height / 2f;
        float halfW = width / 2f;
        float halfH = height / 2f;
        float r = cornerRadius;

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                float px = x + 0.5f;
                float py = y + 0.5f;

                float dx = Mathf.Max(0f, Mathf.Abs(px - cx) - (halfW - r));
                float dy = Mathf.Max(0f, Mathf.Abs(py - cy) - (halfH - r));
                float dist = Mathf.Sqrt(dx * dx + dy * dy);

                float alpha = 1f;
                if (dist > r)
                {
                    alpha = 0f;
                }
                else if (dist > r - 1.5f)
                {
                    alpha = Mathf.Clamp01((r - dist) / 1.5f);
                }

                pixels[y * width + x] = new Color(color.r, color.g, color.b, color.a * alpha);
            }
        }

        tex.SetPixels(pixels);
        tex.Apply();

        Vector4 border = new Vector4(cornerRadius, cornerRadius, cornerRadius, cornerRadius);
        return Sprite.Create(tex, new Rect(0, 0, width, height), new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect, border);
    }

    private Sprite CreateSolidSprite(Color color)
    {
        Texture2D tex = new Texture2D(2, 2, TextureFormat.RGBA32, false);
        Color[] pixels = new Color[4];
        for (int i = 0; i < pixels.Length; i++) pixels[i] = color;
        tex.SetPixels(pixels);
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, 2, 2), new Vector2(0.5f, 0.5f));
    }

    private void BuildUI()
    {
        // 1. Phân giải Font mặc định của Unity
        TMP_FontAsset commonFont = null;
        TextMeshProUGUI[] allTmp = Object.FindObjectsByType<TextMeshProUGUI>(FindObjectsInactive.Include);
        foreach (var t in allTmp)
        {
            if (t.font != null)
            {
                commonFont = t.font;
                break;
            }
        }

        // Tạo sprite bo góc và sprite đặc cứng procedurally để không bị lỗi thiếu tài nguyên khi đóng gói
        roundedSprite = CreateRoundedSprite(64, 64, 12, Color.white);
        solidSprite = CreateSolidSprite(Color.white);

        // Tạm dừng thời gian trong game khi đang xem hướng dẫn
        Time.timeScale = 0f;

        // 2. Tạo Overlay Background (Đậm, mờ hậu cảnh)
        GameObject overlayGo = new GameObject("Overlay");
        overlayGo.transform.SetParent(transform, false);
        RectTransform overlayRect = overlayGo.AddComponent<RectTransform>();
        overlayRect.anchorMin = Vector2.zero;
        overlayRect.anchorMax = Vector2.one;
        overlayRect.sizeDelta = Vector2.zero;

        Image overlayImg = overlayGo.AddComponent<Image>();
        overlayImg.sprite = solidSprite;
        overlayImg.color = new Color(0.04f, 0.05f, 0.07f, 0.85f); // Dark charcoal transparent

        // 3. Tạo Main Card (Hộp thoại chính)
        GameObject cardGo = new GameObject("MainCard");
        cardGo.transform.SetParent(transform, false);
        RectTransform cardRect = cardGo.AddComponent<RectTransform>();
        cardRect.anchorMin = new Vector2(0.5f, 0.5f);
        cardRect.anchorMax = new Vector2(0.5f, 0.5f);
        cardRect.pivot = new Vector2(0.5f, 0.5f);
        cardRect.sizeDelta = new Vector2(650f, 500f);

        Image cardImg = cardGo.AddComponent<Image>();
        cardImg.sprite = roundedSprite;
        cardImg.type = Image.Type.Sliced;
        cardImg.color = new Color(0.1f, 0.12f, 0.16f, 0.97f); // Glassmorphism Slate Dark

        // Cấu hình Layout Group cho Card
        VerticalLayoutGroup cardLayout = cardGo.AddComponent<VerticalLayoutGroup>();
        cardLayout.padding = new RectOffset(40, 40, 30, 30);
        cardLayout.spacing = 15;
        cardLayout.childAlignment = TextAnchor.UpperCenter;
        cardLayout.childControlHeight = true;
        cardLayout.childForceExpandHeight = false;
        cardLayout.childControlWidth = false;
        cardLayout.childForceExpandWidth = false;

        // 4. Tiêu đề (Title)
        GameObject titleGo = new GameObject("TitleText");
        titleGo.transform.SetParent(cardGo.transform, false);
        RectTransform titleRect = titleGo.AddComponent<RectTransform>();
        titleRect.sizeDelta = new Vector2(570f, 35f);

        TextMeshProUGUI titleTxt = titleGo.AddComponent<TextMeshProUGUI>();
        if (commonFont != null) titleTxt.font = commonFont;
        titleTxt.text = "HƯỚNG DẪN ĐIỀU KHIỂN XE";
        titleTxt.fontSize = 22;
        titleTxt.fontWeight = FontWeight.Bold;
        titleTxt.color = new Color(0.95f, 0.75f, 0.15f, 1f); // Gold Color
        titleTxt.alignment = TextAlignmentOptions.Center;

        // 6. Danh sách các phím điều khiển (Content Group)
        GameObject contentGo = new GameObject("ContentGroup");
        contentGo.transform.SetParent(cardGo.transform, false);
        RectTransform contentRect = contentGo.AddComponent<RectTransform>();
        contentRect.sizeDelta = new Vector2(570f, 360f);

        VerticalLayoutGroup contentLayout = contentGo.AddComponent<VerticalLayoutGroup>();
        contentLayout.spacing = 8;
        contentLayout.childControlHeight = true;
        contentLayout.childForceExpandHeight = false;
        contentLayout.childControlWidth = false;
        contentLayout.childForceExpandWidth = false;

        AddControlRow(contentGo.transform, new string[] { "T" }, "Khởi động / Tắt động cơ xe (Engine Start/Stop)", commonFont, roundedSprite);
        AddControlRow(contentGo.transform, new string[] { "1", "2", "3" }, "Cài số D (Tiến) / N (Mo) / R (Lùi)", commonFont, roundedSprite);
        AddControlRow(contentGo.transform, new string[] { "W", "S" }, "Nhấn ga (khi ở số D) / Nhấn phanh chân (khi ở số D)", commonFont, roundedSprite);
        AddControlRow(contentGo.transform, new string[] { "A", "D" }, "Xoay vô lăng điều hướng sang Trái / Phải", commonFont, roundedSprite);
        AddControlRow(contentGo.transform, new string[] { "Space" }, "Phanh tay (Đè giữ để phanh xe, thả ra để nhả phanh)", commonFont, roundedSprite);
        AddControlRow(contentGo.transform, new string[] { "C" }, "Đổi góc nhìn camera (Cabin / Ngoài xe)", commonFont, roundedSprite);
        AddControlRow(contentGo.transform, new string[] { "Q", "E" }, "Xi-nhan rẽ Trái / Phải", commonFont, roundedSprite);
        AddControlRow(contentGo.transform, new string[] { "F" }, "Đèn cảnh báo khẩn cấp (Hazard Light)", commonFont, roundedSprite);
        AddControlRow(contentGo.transform, new string[] { "L" }, "Bật / Tắt đèn pha cos", commonFont, roundedSprite);

        // 7. Thanh chia cách (Divider)
        GameObject dividerGo = new GameObject("Divider");
        dividerGo.transform.SetParent(cardGo.transform, false);
        RectTransform divRect = dividerGo.AddComponent<RectTransform>();
        divRect.sizeDelta = new Vector2(570f, 2f);
        Image divImg = dividerGo.AddComponent<Image>();
        divImg.sprite = solidSprite;
        divImg.color = new Color(0.2f, 0.23f, 0.28f, 1f);

        LayoutElement divLayout = dividerGo.AddComponent<LayoutElement>();
        divLayout.minHeight = 2f;
        divLayout.preferredHeight = 2f;

        // 8. Dòng chữ hướng dẫn nhấn SPACE để bắt đầu (Nhấp nháy nhẹ)
        GameObject hintGo = new GameObject("SpaceHintText");
        hintGo.transform.SetParent(cardGo.transform, false);
        RectTransform hintRect = hintGo.AddComponent<RectTransform>();
        hintRect.sizeDelta = new Vector2(570f, 30f);

        spaceHintText = hintGo.AddComponent<TextMeshProUGUI>();
        if (commonFont != null) spaceHintText.font = commonFont;
        spaceHintText.text = "NHẤN PHÍM [ SPACE ] ĐỂ BẮT ĐẦU BÀI THI";
        spaceHintText.fontSize = 15;
        spaceHintText.fontWeight = FontWeight.Bold;
        spaceHintText.color = new Color(0.12f, 0.8f, 0.45f, 1f); // Emerald Green
        spaceHintText.alignment = TextAlignmentOptions.Center;
        spaceHintText.verticalAlignment = VerticalAlignmentOptions.Middle;
    }

    private void AddControlRow(Transform parent, string[] keys, string description, TMP_FontAsset font, Sprite roundedSprite)
    {
        GameObject rowGo = new GameObject("Row_" + description);
        rowGo.transform.SetParent(parent, false);

        RectTransform rowRect = rowGo.GetComponent<RectTransform>();
        if (rowRect == null) rowRect = rowGo.AddComponent<RectTransform>();
        rowRect.sizeDelta = new Vector2(570f, 28f);

        HorizontalLayoutGroup rowLayout = rowGo.AddComponent<HorizontalLayoutGroup>();
        rowLayout.spacing = 15;
        rowLayout.childAlignment = TextAnchor.MiddleLeft;
        rowLayout.childControlWidth = false;
        rowLayout.childForceExpandWidth = false;
        rowLayout.childControlHeight = false;
        rowLayout.childForceExpandHeight = false;

        // Container bên trái chứa các phím bấm
        GameObject keysContainer = new GameObject("KeysContainer");
        keysContainer.transform.SetParent(rowGo.transform, false);
        RectTransform keysRect = keysContainer.AddComponent<RectTransform>();
        keysRect.sizeDelta = new Vector2(120f, 28f);

        HorizontalLayoutGroup keysLayout = keysContainer.AddComponent<HorizontalLayoutGroup>();
        keysLayout.spacing = 6;
        keysLayout.childAlignment = TextAnchor.MiddleRight;
        keysLayout.childControlWidth = false;
        keysLayout.childForceExpandWidth = false;
        keysLayout.childControlHeight = false;
        keysLayout.childForceExpandHeight = false;

        foreach (string key in keys)
        {
            CreateKeyBubble(keysContainer.transform, key, font, roundedSprite);
        }

        // Text mô tả bên phải
        GameObject descGo = new GameObject("DescriptionText");
        descGo.transform.SetParent(rowGo.transform, false);
        RectTransform descRect = descGo.AddComponent<RectTransform>();
        descRect.sizeDelta = new Vector2(435f, 28f);

        TextMeshProUGUI descTxt = descGo.AddComponent<TextMeshProUGUI>();
        if (font != null) descTxt.font = font;
        descTxt.text = description;
        descTxt.fontSize = 13f;
        descTxt.color = new Color(0.85f, 0.88f, 0.93f, 1f);
        descTxt.alignment = TextAlignmentOptions.Left;
        descTxt.verticalAlignment = VerticalAlignmentOptions.Middle;
    }

    private void CreateKeyBubble(Transform parent, string keyText, TMP_FontAsset font, Sprite roundedSprite)
    {
        GameObject bubble = new GameObject("Key_" + keyText);
        bubble.transform.SetParent(parent, false);

        Image img = bubble.AddComponent<Image>();
        img.sprite = roundedSprite;
        img.type = Image.Type.Sliced;
        img.color = new Color(0.2f, 0.24f, 0.31f, 1f); // Slate keyboard key base

        RectTransform bubbleRect = bubble.GetComponent<RectTransform>();
        bubbleRect.sizeDelta = new Vector2(30f, 24f);
        if (keyText == "Space")
        {
            bubbleRect.sizeDelta = new Vector2(65f, 24f);
        }

        GameObject txtGo = new GameObject("Text");
        txtGo.transform.SetParent(bubble.transform, false);
        
        RectTransform txtRect = txtGo.AddComponent<RectTransform>();
        txtRect.anchorMin = Vector2.zero;
        txtRect.anchorMax = Vector2.one;
        txtRect.sizeDelta = Vector2.zero;
        txtRect.anchoredPosition = Vector2.zero;

        TextMeshProUGUI txt = txtGo.AddComponent<TextMeshProUGUI>();
        if (font != null) txt.font = font;
        txt.text = keyText;
        txt.fontSize = 10.5f;
        txt.fontWeight = FontWeight.Bold;
        txt.color = new Color(0.95f, 0.97f, 1.0f, 1f);
        txt.alignment = TextAlignmentOptions.Center;
        txt.verticalAlignment = VerticalAlignmentOptions.Middle;
    }

    private void Update()
    {
        // Hiệu ứng nhấp nháy/pulse cho dòng chữ Space sử dụng thời gian thực (vì timeScale = 0)
        if (spaceHintText != null)
        {
            float alpha = 0.4f + Mathf.PingPong(Time.unscaledTime * 1.8f, 0.6f);
            Color col = spaceHintText.color;
            col.a = alpha;
            spaceHintText.color = col;
        }

        // Nhấn phím SPACE để bắt đầu bài thi
        if (Input.GetKeyDown(KeyCode.Space))
        {
            OnStartClicked();
        }
    }

    private void OnStartClicked()
    {
        // Khôi phục thời gian bình thường trong game
        Time.timeScale = 1f;

        // Kích hoạt bài thi thực sự từ ExamManager
        if (ExamManager.Instance != null)
        {
            ExamManager.Instance.StartExam();
        }

        activeInstance = null;
        Destroy(gameObject);
    }

    private void OnDestroy()
    {
        if (roundedSprite != null)
        {
            if (roundedSprite.texture != null) Destroy(roundedSprite.texture);
            Destroy(roundedSprite);
        }
        if (solidSprite != null)
        {
            if (solidSprite.texture != null) Destroy(solidSprite.texture);
            Destroy(solidSprite);
        }
    }
}
