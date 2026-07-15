using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class TheoryExamManager : MonoBehaviour
{
    [Header("Question UI")]
    public TMP_Text txtTimer;
    public TMP_Text txtQuestionNumber;
    public TMP_Text txtQuestionContent;
    public Image imgQuestion;          // Ảnh minh họa của câu hỏi

    [Header("Answer Buttons")]
    public Button answerA;
    public Button answerB;
    public Button answerC;
    public Button answerD;

    [Header("Navigation Buttons")]
    public Button btnPrevious;
    public Button btnNext;

    [Header("Panels")]
    public GameObject selectionPanel; // Panel chọn bộ đề thi
    public GameObject examPanel;      // Panel làm bài thi
    public GameObject submitPopup;    // Popup xác nhận nộp bài
    public GameObject resultPanel;    // Panel kết quả thi
    public GameObject tipsPanel;      // Panel mẹo thi lý thuyết

    [Header("Result UI")]
    public TMP_Text txtResult;
    public TMP_Text txtScore;
    public TMP_Text txtWarning;
    public GameObject warningContainer;
    public TMP_Text txtTime;    
    public TMP_Text txtProgress;
    public TMP_Text testDebug;

    [Header("Debug Settings")]
    public bool useMockData = false; // Bật mặc định để sinh câu hỏi mẫu không tải dữ liệu thật gây lỗi

    [Header("Question Configuration")]
    public GameObject questionButtonPrefab;
    public Transform questionListPanel;

    private List<TheoryQuestion> questions = new List<TheoryQuestion>();
    private List<int> selectedAnswers = new List<int>();
    private int currentQuestionIndex = 0;
    private float remainingTime = 22 * 60;
    
    // Trạng thái chế độ
    private bool isPracticeMode = false;
    private int selectedSetIndex = 0;
    public bool isReviewMode = false;
    private TheoryQuestionData examData;
    public List<QuestionButton> questionButtons = new List<QuestionButton>();

    private void Start()
    {
        // Tự động tìm kiếm ImgQuestion nếu chưa được gán
        if (imgQuestion == null && examPanel != null)
        {
            Transform t = FindChildRecursive(examPanel.transform, "ImgQuestion");
            if (t != null) imgQuestion = t.GetComponent<Image>();
        }

        // Tải trước dữ liệu bộ đề thi
        LoadExamData();

        // Ẩn các panel thi và kết quả ban đầu
        if (examPanel != null) examPanel.SetActive(false);
        if (submitPopup != null) submitPopup.SetActive(false);
        if (resultPanel != null) resultPanel.SetActive(false);
        if (tipsPanel != null) tipsPanel.SetActive(false);
        if (questionListPanel != null) questionListPanel.gameObject.SetActive(false);
        
        // Hiện panel chọn bộ đề
        if (selectionPanel != null) selectionPanel.SetActive(true);

        // Gán event click cho các đáp án
        if (answerA != null) answerA.onClick.AddListener(SelectAnswerA);
        if (answerB != null) answerB.onClick.AddListener(SelectAnswerB);
        if (answerC != null) answerC.onClick.AddListener(SelectAnswerC);
        if (answerD != null) answerD.onClick.AddListener(SelectAnswerD);

        // Tự động gán listener cho nút Về Menu và Nộp bài trong Header màn hình thi
        if (examPanel != null)
        {
            Transform btnBackHeader = examPanel.transform.Find("Header/Btn_Back");
            if (btnBackHeader != null)
            {
                Button btn = btnBackHeader.GetComponent<Button>();
                if (btn != null)
                {
                    btn.onClick.RemoveAllListeners();
                    btn.onClick.AddListener(BackToSelection);
                }
            }

            Transform btnSubmitHeader = examPanel.transform.Find("Header/Btn_Submit");
            if (btnSubmitHeader != null)
            {
                Button btn = btnSubmitHeader.GetComponent<Button>();
                if (btn != null)
                {
                    btn.onClick.RemoveAllListeners();
                    btn.onClick.AddListener(ShowSubmitPopup);
                }
            }
        }

        // Tự động gán listener cho các nút trong SubmitPopup
        if (submitPopup != null)
        {
            Transform btnConfirm = submitPopup.transform.Find("Btn_Confirm");
            if (btnConfirm == null) btnConfirm = submitPopup.transform.Find("Panel_Card/Btn_Confirm");
            if (btnConfirm == null) btnConfirm = submitPopup.transform.Find("Panel_Card/Panel_Buttons/Btn_Confirm");
            if (btnConfirm != null)
            {
                Button btn = btnConfirm.GetComponent<Button>();
                if (btn != null)
                {
                    btn.onClick.RemoveAllListeners();
                    btn.onClick.AddListener(ConfirmSubmit);
                }
            }

            Transform btnCancel = submitPopup.transform.Find("Btn_Cancel");
            if (btnCancel == null) btnCancel = submitPopup.transform.Find("Panel_Card/Btn_Cancel");
            if (btnCancel == null) btnCancel = submitPopup.transform.Find("Panel_Card/Panel_Buttons/Btn_Cancel");
            if (btnCancel != null)
            {
                Button btn = btnCancel.GetComponent<Button>();
                if (btn != null)
                {
                    btn.onClick.RemoveAllListeners();
                    btn.onClick.AddListener(CancelSubmit);
                }
            }
        }

        // Tự động tìm kiếm và liên kết các nút chọn bộ đề trong selectionPanel tại runtime
        if (selectionPanel != null)
        {
            for (int i = 0; i < 3; i++)
            {
                int setIndex = i;
                Transform card = selectionPanel.transform.Find($"Card_De_{i + 1}");
                if (card == null)
                {
                    // Tìm trong Panel_CardsContainer
                    Transform cardsContainer = selectionPanel.transform.Find("Panel_CardsContainer");
                    if (cardsContainer != null) card = cardsContainer.Find($"Card_De_{i + 1}");
                }
                if (card != null)
                {
                    Button cardBtn = null;
                    // Tìm nút Btn_Thi nằm dưới Bg hoặc trực tiếp dưới card
                    Transform btnThiTrans = card.Find("Bg/Btn_Thi");
                    if (btnThiTrans == null) btnThiTrans = card.Find("Btn_Thi");
                    
                    if (btnThiTrans != null)
                    {
                        cardBtn = btnThiTrans.GetComponent<Button>();
                    }
                    
                    // Fallback tìm Button con bất kỳ hoặc trên chính Card
                    if (cardBtn == null)
                    {
                        cardBtn = card.GetComponentInChildren<Button>(true);
                    }

                    if (cardBtn != null)
                    {
                        cardBtn.onClick.RemoveAllListeners();
                        cardBtn.onClick.AddListener(() => InitExam(setIndex, false));
                    }
                }
            }

            // Gán listener cho các chức năng khác
            Transform otherPanel = selectionPanel.transform.Find("Panel_OtherFunctions");
            if (otherPanel != null)
            {
                Transform btnMeo = otherPanel.Find("Btn_Meo");
                if (btnMeo != null)
                {
                    Button btn = btnMeo.GetComponent<Button>();
                    if (btn != null)
                    {
                        btn.onClick.RemoveAllListeners();
                        btn.onClick.AddListener(ShowTipsPanel);
                    }
                }

                Transform btnPdf = otherPanel.Find("Btn_Pdf");
                if (btnPdf != null)
                {
                    Button btn = btnPdf.GetComponent<Button>();
                    if (btn != null)
                    {
                        btn.onClick.RemoveAllListeners();
                        btn.onClick.AddListener(() => {
                            StartCoroutine(ShowTemporaryMessage(btn, "Tải tài liệu pdf"));
                        });
                    }
                }

                Transform btnVideo = otherPanel.Find("Btn_Video");
                if (btnVideo != null)
                {
                    Button btn = btnVideo.GetComponent<Button>();
                    if (btn != null)
                    {
                        btn.onClick.RemoveAllListeners();
                        btn.onClick.AddListener(() => {
                            StartCoroutine(ShowTemporaryMessage(btn, "Video thực hành"));
                        });
                    }
                }
            }

            // Gán listener cho nút Trở lại Menu chính
            Transform btnBack = selectionPanel.transform.Find("Btn_Back");
            if (btnBack != null)
            {
                Button btn = btnBack.GetComponent<Button>();
                if (btn != null)
                {
                    btn.onClick.RemoveAllListeners();
                    btn.onClick.AddListener(BackToMenu);
                }
            }
        }

        // Gán listener cho nút trở lại trong tipsPanel
        if (tipsPanel != null)
        {
            Transform btnBackMeo = tipsPanel.transform.Find("Btn_BackMeo");
            if (btnBackMeo != null)
            {
                Button btn = btnBackMeo.GetComponent<Button>();
                if (btn != null)
                {
                    btn.onClick.RemoveAllListeners();
                    btn.onClick.AddListener(HideTipsPanel);
                }
            }
        }

        // Gán listener cho các nút trong resultPanel
        if (resultPanel != null)
        {
            Transform btnBackResult = resultPanel.transform.Find("Btn_BackResult");
            if (btnBackResult != null)
            {
                Button btn = btnBackResult.GetComponent<Button>();
                if (btn != null)
                {
                    btn.onClick.RemoveAllListeners();
                    btn.onClick.AddListener(BackToSelection);
                }
            }

            Transform btnSelection = resultPanel.transform.Find("Panel_Buttons/Btn_Selection");
            if (btnSelection != null)
            {
                Button btn = btnSelection.GetComponent<Button>();
                if (btn != null)
                {
                    btn.onClick.RemoveAllListeners();
                    btn.onClick.AddListener(BackToSelection);
                }
            }

            Transform btnReview = resultPanel.transform.Find("Panel_Buttons/Btn_Review");
            if (btnReview != null)
            {
                Button btn = btnReview.GetComponent<Button>();
                if (btn != null)
                {
                    btn.onClick.RemoveAllListeners();
                    btn.onClick.AddListener(ReviewExam);
                }
            }

            Transform btnRetry = resultPanel.transform.Find("Panel_Buttons/Btn_Retry");
            if (btnRetry != null)
            {
                Button btn = btnRetry.GetComponent<Button>();
                if (btn != null)
                {
                    btn.onClick.RemoveAllListeners();
                    btn.onClick.AddListener(RetryExam);
                }
            }
        }
    }

    private void Update()
    {
        // Nếu đang ở màn hình chọn bộ đề hoặc đã kết thúc thi thì dừng đếm ngược
        if (selectionPanel != null && selectionPanel.activeSelf) return;
        if (resultPanel != null && resultPanel.activeSelf) return;
        if (remainingTime <= 0) return;

        // Nếu ở chế độ ôn tập (Học), hiển thị chữ ôn tập và không đếm ngược
        if (isPracticeMode)
        {
            if (txtTimer != null) txtTimer.text = "Chế độ: Ôn tập";
            return;
        }

        // Chế độ Thi thử: đếm ngược 22 phút
        remainingTime -= Time.deltaTime;
        UpdateTimerUI();

        if (remainingTime <= 0)
        {
            remainingTime = 0;
            ConfirmSubmit(); // Tự động nộp bài khi hết giờ
        }
    }

    /// <summary>
    /// Hàm khởi chạy bài thi khi người dùng bấm nút ở màn hình chọn bộ đề
    /// </summary>
    public void InitExam(int setIndex, bool isPractice)
    {
        selectedSetIndex = setIndex;
        isPracticeMode = isPractice;
        currentQuestionIndex = 0;
        isReviewMode = false;
        
        // Cấu hình thời gian làm bài: Thi thử = 22 phút
        remainingTime = isPracticeMode ? 9999f : 22f * 60f;

        // Load bộ câu hỏi tương ứng
        LoadQuestions(selectedSetIndex);

        // Reset danh sách đáp án đã chọn (-1 là chưa trả lời)
        selectedAnswers = new List<int>();
        for (int i = 0; i < questions.Count; i++)
        {
            selectedAnswers.Add(-1);
        }

        // Ẩn panel chọn bộ đề và hiển thị panel thi
        if (selectionPanel != null) selectionPanel.SetActive(false);
        if (submitPopup != null) submitPopup.SetActive(false);
        if (resultPanel != null) resultPanel.SetActive(false);
        
        if (examPanel != null) examPanel.SetActive(true);
        if (questionListPanel != null) questionListPanel.gameObject.SetActive(true);

        // Kích hoạt lại các nút lựa chọn
        if (answerA != null) { answerA.interactable = true; answerA.enabled = true; }
        if (answerB != null) { answerB.interactable = true; answerB.enabled = true; }
        if (answerC != null) { answerC.interactable = true; answerC.enabled = true; }
        if (answerD != null) { answerD.interactable = true; answerD.enabled = true; }

        // Dựng danh sách các nút câu hỏi bên phải
        CreateQuestionButtons();
        
        // Hiển thị câu hỏi đầu tiên
        ShowQuestion();
        UpdateTimerUI();
        UpdateProgressUI();
    }

    private void LoadExamData()
    {
        if (useMockData) return;
        if (examData != null) return;

        TextAsset jsonAsset = Resources.Load<TextAsset>("theory_questions");
        if (jsonAsset != null)
        {
            examData = JsonUtility.FromJson<TheoryQuestionData>(jsonAsset.text);
            Debug.Log($"[TheoryExam] Loaded {examData.sets.Count} question sets successfully.");
        }
        else
        {
            Debug.LogError("[TheoryExam] Could not load theory_questions.json from Resources!");
        }
    }

    private void GenerateMockQuestions()
    {
        questions.Clear();
        for (int i = 0; i < 35; i++)
        {
            TheoryQuestion q = new TheoryQuestion();
            q.question = $"Đây là nội dung câu hỏi mẫu thứ {i + 1} phục vụ việc thiết kế và kiểm thử giao diện Theory Exam.";
            q.answerA = $"Đáp án mẫu A cho câu hỏi số {i + 1}";
            q.answerB = $"Đáp án mẫu B cho câu hỏi số {i + 1}";
            q.answerC = $"Đáp án mẫu C cho câu hỏi số {i + 1}";
            q.answerD = $"Đáp án mẫu D cho câu hỏi số {i + 1}";
            q.correctAnswer = Random.Range(0, 4);
            q.explanation = $"Đây là phần giải thích chi tiết đáp án đúng cho câu hỏi mẫu số {i + 1}.";
            q.isCritical = (i == 3 || i == 15); // Tạo 2 câu điểm liệt để test giao diện
            q.imageName = ""; // Không có ảnh để tránh lỗi tải ảnh
            questions.Add(q);
        }
    }

    private void LoadQuestions(int setIndex)
    {
        questions.Clear();

        if (useMockData)
        {
            GenerateMockQuestions();
            return;
        }

        LoadExamData();

        if (examData == null || examData.sets == null || examData.sets.Count == 0)
        {
            Debug.LogWarning("[TheoryExam] Dữ liệu câu hỏi bị lỗi hoặc trống. Tự động chuyển sang chế độ Mock Questions.");
            GenerateMockQuestions();
            return;
        }

        // Nếu setIndex nằm trong khoảng của bộ đề thực tế (0, 1, 2)
        if (setIndex >= 0 && setIndex < examData.sets.Count)
        {
            foreach (var q in examData.sets[setIndex].questions)
            {
                questions.Add(q);
            }
        }
        else
        {
            // Đề thi thử ngẫu nhiên: Chọn 35 câu từ tất cả các đề sẵn có
            List<TheoryQuestion> allQuestions = new List<TheoryQuestion>();
            foreach (var set in examData.sets)
            {
                allQuestions.AddRange(set.questions);
            }

            List<TheoryQuestion> criticalPool = new List<TheoryQuestion>();
            List<TheoryQuestion> normalPool = new List<TheoryQuestion>();
            foreach (var q in allQuestions)
            {
                if (q.isCritical) criticalPool.Add(q);
                else normalPool.Add(q);
            }

            Shuffle(criticalPool);
            Shuffle(normalPool);

            // B2 chuẩn: Có từ 1-3 câu điểm liệt trong đề ngẫu nhiên
            int numCritical = Mathf.Min(3, criticalPool.Count);
            for (int i = 0; i < numCritical; i++)
            {
                questions.Add(criticalPool[i]);
            }
            int numNormal = 35 - numCritical;
            for (int i = 0; i < numNormal && i < normalPool.Count; i++)
            {
                questions.Add(normalPool[i]);
            }

            Shuffle(questions);
        }

        if (questions.Count == 0)
        {
            GenerateMockQuestions();
        }
    }

    private void Shuffle<T>(List<T> list)
    {
        for (int i = list.Count - 1; i > 0; i--)
        {
            int rnd = Random.Range(0, i + 1);
            T temp = list[i];
            list[i] = list[rnd];
            list[rnd] = temp;
        }
    }

    private Transform FindChildRecursive(Transform parent, string name)
    {
        if (parent.name == name) return parent;
        for (int i = 0; i < parent.childCount; i++)
        {
            Transform found = FindChildRecursive(parent.GetChild(i), name);
            if (found != null) return found;
        }
        return null;
    }

    private void AdjustLayout(bool hasImage)
    {
        if (answerA == null || answerB == null || answerC == null || answerD == null) return;

        RectTransform rA = answerA.GetComponent<RectTransform>();
        RectTransform rB = answerB.GetComponent<RectTransform>();
        RectTransform rC = answerC.GetComponent<RectTransform>();
        RectTransform rD = answerD.GetComponent<RectTransform>();

        RectTransform rQContent = txtQuestionContent != null ? txtQuestionContent.GetComponent<RectTransform>() : null;
        RectTransform rQNum = txtQuestionNumber != null ? txtQuestionNumber.GetComponent<RectTransform>() : null;
        RectTransform rImg = imgQuestion != null ? imgQuestion.GetComponent<RectTransform>() : null;

        RectTransform rAnswerArea = rA.parent as RectTransform;

        // Lấy kích thước thực tế của panel chính
        RectTransform examPanelRect = examPanel != null ? examPanel.GetComponent<RectTransform>() : null;
        float examPanelWidth = examPanelRect != null ? examPanelRect.rect.width : 1000f;

        // Chiều rộng của khu vực đáp án (chiếm 92% chiều rộng panel)
        float answerAreaWidth = examPanelWidth * 0.92f;

        float paddingBottom = 12f;
        float paddingTop = 12f;
        float spacingVertical = 12f;
        float totalAnswerAreaHeight = 0f;

        if (hasImage)
        {
            // --- CÓ HÌNH ẢNH: Bố cục đáp án 2x2 để tiết kiệm chiều cao ---
            float buttonWidth = answerAreaWidth * 0.49f;

            float heightA = GetButtonPreferredHeight(rA, buttonWidth);
            float heightB = GetButtonPreferredHeight(rB, buttonWidth);
            float heightC = GetButtonPreferredHeight(rC, buttonWidth);
            float heightD = GetButtonPreferredHeight(rD, buttonWidth);

            bool hasRow1 = (rA != null && rA.gameObject.activeSelf) || (rB != null && rB.gameObject.activeSelf);
            bool hasRow2 = (rC != null && rC.gameObject.activeSelf) || (rD != null && rD.gameObject.activeSelf);

            float row1Height = hasRow1 ? Mathf.Max(heightA, heightB) : 0f;
            float row2Height = hasRow2 ? Mathf.Max(heightC, heightD) : 0f;

            totalAnswerAreaHeight = paddingBottom + paddingTop;
            if (hasRow1) totalAnswerAreaHeight += row1Height;
            if (hasRow2) totalAnswerAreaHeight += row2Height;
            if (hasRow1 && hasRow2) totalAnswerAreaHeight += spacingVertical;

            // Sắp xếp các nút trong bố cục 2x2
            float row2BottomY = paddingBottom;
            float row1BottomY = paddingBottom;
            if (hasRow2) row1BottomY += row2Height + spacingVertical;

            if (rA != null)
            {
                rA.anchorMin = new Vector2(0f, 0f);
                rA.anchorMax = new Vector2(0.49f, 0f);
                rA.offsetMin = new Vector2(0f, row1BottomY);
                rA.offsetMax = new Vector2(0f, row1BottomY + row1Height);
            }
            if (rB != null)
            {
                rB.anchorMin = new Vector2(0.51f, 0f);
                rB.anchorMax = new Vector2(1f, 0f);
                rB.offsetMin = new Vector2(0f, row1BottomY);
                rB.offsetMax = new Vector2(0f, row1BottomY + row1Height);
            }
            if (rC != null)
            {
                rC.anchorMin = new Vector2(0f, 0f);
                rC.anchorMax = new Vector2(0.49f, 0f);
                rC.offsetMin = new Vector2(0f, row2BottomY);
                rC.offsetMax = new Vector2(0f, row2BottomY + row2Height);
            }
            if (rD != null)
            {
                rD.anchorMin = new Vector2(0.51f, 0f);
                rD.anchorMax = new Vector2(1f, 0f);
                rD.offsetMin = new Vector2(0f, row2BottomY);
                rD.offsetMax = new Vector2(0f, row2BottomY + row2Height);
            }
        }
        else
        {
            // --- KHÔNG CÓ HÌNH ẢNH: Bố cục đáp án 1 cột dọc (Full width) để đọc rõ ràng hơn ---
            List<RectTransform> activeButtons = new List<RectTransform>();
            if (rD != null && rD.gameObject.activeSelf) activeButtons.Add(rD);
            if (rC != null && rC.gameObject.activeSelf) activeButtons.Add(rC);
            if (rB != null && rB.gameObject.activeSelf) activeButtons.Add(rB);
            if (rA != null && rA.gameObject.activeSelf) activeButtons.Add(rA);

            float currentY = paddingBottom;
            for (int i = 0; i < activeButtons.Count; i++)
            {
                RectTransform rBtn = activeButtons[i];
                float btnHeight = GetButtonPreferredHeight(rBtn, answerAreaWidth);

                rBtn.anchorMin = new Vector2(0f, 0f);
                rBtn.anchorMax = new Vector2(1f, 0f);
                rBtn.offsetMin = new Vector2(0f, currentY);
                rBtn.offsetMax = new Vector2(0f, currentY + btnHeight);

                currentY += btnHeight + spacingVertical;
            }
            totalAnswerAreaHeight = currentY - spacingVertical + paddingTop;
        }

        // Cập nhật kích thước và vị trí của AnswerArea
        if (rAnswerArea != null)
        {
            rAnswerArea.anchorMin = new Vector2(0.04f, 0f);
            rAnswerArea.anchorMax = new Vector2(0.96f, 0f);
            rAnswerArea.offsetMin = new Vector2(0f, 100f);
            rAnswerArea.offsetMax = new Vector2(0f, 100f + totalAnswerAreaHeight);
        }

        // --- CẤU HÌNH VỊ TRÍ CÂU HỎI VÀ HÌNH ẢNH TRÊN ANSWER AREA ---
        // Đặt biên dưới của câu hỏi và hình ảnh ở vị trí tuyệt đối (offset) phía trên AnswerArea (20px gap)
        float questionBottomOffset = 100f + totalAnswerAreaHeight + 20f;

        if (hasImage)
        {
            // Có hình: chia 2 cột bên trên
            if (rQContent != null)
            {
                rQContent.anchorMin = new Vector2(0.14f, 0f);
                rQContent.anchorMax = new Vector2(0.52f, 0.87f);
                rQContent.offsetMin = new Vector2(0f, questionBottomOffset);
                rQContent.offsetMax = new Vector2(0f, 0f);
            }
            if (rQNum != null)
            {
                rQNum.anchorMin = new Vector2(0.04f, 0.80f);
                rQNum.anchorMax = new Vector2(0.12f, 0.87f);
                rQNum.offsetMin = rQNum.offsetMax = Vector2.zero;
            }
            if (rImg != null)
            {
                rImg.gameObject.SetActive(true);
                rImg.anchorMin = new Vector2(0.55f, 0f);
                rImg.anchorMax = new Vector2(0.96f, 0.87f);
                rImg.offsetMin = new Vector2(0f, questionBottomOffset);
                rImg.offsetMax = new Vector2(0f, 0f);
            }
        }
        else
        {
            // Không hình: chiếm trọn chiều ngang, ẩn hình
            if (rQContent != null)
            {
                rQContent.anchorMin = new Vector2(0.14f, 0f);
                rQContent.anchorMax = new Vector2(0.96f, 0.87f);
                rQContent.offsetMin = new Vector2(0f, questionBottomOffset);
                rQContent.offsetMax = new Vector2(0f, 0f);
            }
            if (rQNum != null)
            {
                rQNum.anchorMin = new Vector2(0.04f, 0.80f);
                rQNum.anchorMax = new Vector2(0.12f, 0.87f);
                rQNum.offsetMin = rQNum.offsetMax = Vector2.zero;
            }
            if (rImg != null)
            {
                rImg.gameObject.SetActive(false);
            }
        }
    }

    private float GetButtonPreferredHeight(RectTransform rBtn, float buttonWidth)
    {
        if (rBtn == null || !rBtn.gameObject.activeSelf) return 0f;
        TMP_Text txt = rBtn.GetComponentInChildren<TMP_Text>(true);
        if (txt == null) return 55f;

        // Đồng nhất padding và lề trong của text trước khi đo
        RectTransform rTxt = txt.GetComponent<RectTransform>();
        if (rTxt != null)
        {
            rTxt.anchorMin = Vector2.zero;
            rTxt.anchorMax = Vector2.one;
            rTxt.offsetMin = new Vector2(20f, 5f);
            rTxt.offsetMax = new Vector2(-20f, -5f);
        }

        // Tính toán preferredHeight theo chiều rộng nút trừ đi 40px padding
        Vector2 preferredSize = txt.GetPreferredValues(txt.text, buttonWidth - 40f, 0f);
        float preferredH = preferredSize.y + 22f; // Chiều cao chữ + 22px padding dọc (11px trên, 11px dưới)
        return Mathf.Max(55f, preferredH); // Chiều cao tối thiểu 55px
    }

    private void FormatAnswerText(Button button, string prefix, string text)
    {
        if (button == null) return;
        bool hasText = !string.IsNullOrEmpty(text);
        button.gameObject.SetActive(hasText);
        if (hasText)
        {
            TMP_Text txt = button.GetComponentInChildren<TMP_Text>(true);
            if (txt != null)
            {
                txt.text = prefix + " " + text;
                txt.enableAutoSizing = true;
                txt.fontSizeMin = 13.5f;
                txt.fontSizeMax = 16.5f; // Giới hạn cỡ chữ để tránh to nhỏ bất thường
                txt.textWrappingMode = TextWrappingModes.Normal;
                txt.overflowMode = TextOverflowModes.Overflow; // Không cắt chữ, cho phép chữ hiển thị đầy đủ
                txt.alignment = TextAlignmentOptions.Left; // Căn lề trái dễ đọc

                RectTransform rTxt = txt.GetComponent<RectTransform>();
                if (rTxt != null)
                {
                    rTxt.anchorMin = Vector2.zero;
                    rTxt.anchorMax = Vector2.one;
                    rTxt.offsetMin = new Vector2(20f, 5f);
                    rTxt.offsetMax = new Vector2(-20f, -5f);
                }
            }
        }
    }

    private void ShowQuestion()
    {
        if (questions.Count == 0) return;

        TheoryQuestion q = questions[currentQuestionIndex];

        // 1. Đánh dấu câu điểm liệt bằng chữ màu cam đỏ để người học chú ý (chỉ hiển thị khi đang ở chế độ xem lại)
        string prefix = (q.isCritical && isReviewMode) ? "<color=#FF3B30>[CÂU ĐIỂM LIỆT]</color> " : "";
        
        if (txtQuestionNumber != null)
            txtQuestionNumber.text = (currentQuestionIndex + 1).ToString();
            
        if (txtQuestionContent != null)
        {
            txtQuestionContent.enableAutoSizing = true;
            txtQuestionContent.fontSizeMin = 15f;
            txtQuestionContent.fontSizeMax = 18f; // Giới hạn cỡ chữ câu hỏi
            txtQuestionContent.textWrappingMode = TextWrappingModes.Normal;
            txtQuestionContent.overflowMode = TextOverflowModes.Overflow;

            string questionText = prefix + q.question;
            if (isReviewMode && !string.IsNullOrEmpty(q.explanation))
            {
                questionText += $"\n\n<color=#0078D4><b>Giải thích:</b> {q.explanation}</color>";
            }
            txtQuestionContent.text = questionText;
        }

        // 2. Thiết lập nội dung văn bản cho các nút đáp án (phải làm TRƯỚC khi tính toán layout)
        FormatAnswerText(answerA, "A.", q.answerA);
        FormatAnswerText(answerB, "B.", q.answerB);
        FormatAnswerText(answerC, "C.", q.answerC);
        FormatAnswerText(answerD, "D.", q.answerD);

        // 3. Điều chỉnh bố cục linh hoạt co dãn theo câu hỏi có/không có hình ảnh và độ dài text đáp án
        AdjustLayout(!string.IsNullOrEmpty(q.imageName));

        // Hiển thị hình ảnh minh họa nếu có
        if (imgQuestion != null)
        {
            imgQuestion.preserveAspect = true; // Luôn giữ tỷ lệ ảnh nguyên bản để tránh méo ảnh
            if (!string.IsNullOrEmpty(q.imageName))
            {
                Sprite imgSprite = Resources.Load<Sprite>($"TheoryImages/{q.imageName}");
                if (imgSprite == null)
                {
                    Texture2D tex = Resources.Load<Texture2D>($"TheoryImages/{q.imageName}");
                    if (tex != null)
                    {
                        imgSprite = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f));
                        Debug.Log($"[TheoryExam] Loaded image {q.imageName} as Texture2D and created Sprite successfully.");
                    }
                }

                if (imgSprite != null)
                {
                    imgQuestion.gameObject.SetActive(true);
                    imgQuestion.sprite = imgSprite;
                }
                else
                {
                    Debug.LogWarning($"[TheoryExam] Could not load sprite/texture from Resources/TheoryImages/{q.imageName}");
                    imgQuestion.gameObject.SetActive(false);
                }
            }
            else
            {
                imgQuestion.gameObject.SetActive(false);
            }
        }

        HighlightSelectedAnswer();
    }

    public void NextQuestion()
    {
        if (currentQuestionIndex < questions.Count - 1)
        {
            currentQuestionIndex++;
            ShowQuestion();
            UpdateQuestionListUI();
        }
    }

    public void PreviousQuestion()
    {
        if (currentQuestionIndex > 0)
        {
            currentQuestionIndex--;
            ShowQuestion();
            UpdateQuestionListUI();
        }
    }

    public void SelectAnswerA() => SelectAnswer(0);
    public void SelectAnswerB() => SelectAnswer(1);
    public void SelectAnswerC() => SelectAnswer(2);
    public void SelectAnswerD() => SelectAnswer(3);

    private void SelectAnswer(int index)
    {
        selectedAnswers[currentQuestionIndex] = index;
        HighlightSelectedAnswer();
        UpdateQuestionListUI();
        UpdateProgressUI(); 
    }

    private void HighlightSelectedAnswer()
    {
        // Reset màu nền các nút đáp án về màu trắng xanh dịu theo Figma
        Color normalColor = new Color(0.93f, 0.98f, 0.99f);
        if (answerA != null) { if (answerA.image != null) answerA.image.color = normalColor; answerA.interactable = true; answerA.enabled = !isReviewMode; }
        if (answerB != null) { if (answerB.image != null) answerB.image.color = normalColor; answerB.interactable = true; answerB.enabled = !isReviewMode; }
        if (answerC != null) { if (answerC.image != null) answerC.image.color = normalColor; answerC.interactable = true; answerC.enabled = !isReviewMode; }
        if (answerD != null) { if (answerD.image != null) answerD.image.color = normalColor; answerD.interactable = true; answerD.enabled = !isReviewMode; }

        int saved = selectedAnswers[currentQuestionIndex];
        TheoryQuestion q = questions[currentQuestionIndex];

        if (isReviewMode)
        {
            // Trong chế độ xem lại:
            // - Đáp án đúng có màu xanh lá rõ ràng
            // - Đáp án người dùng chọn sai có màu đỏ rõ ràng
            Color correctColor = new Color(0.55f, 0.90f, 0.55f); // Xanh lá rõ ràng
            Color incorrectColor = new Color(0.95f, 0.60f, 0.60f); // Đỏ rõ ràng
            Color correctSelectedColor = new Color(0.40f, 0.85f, 0.40f); // Xanh lá đậm nổi bật

            // Highlight đáp án đúng
            if (q.correctAnswer == 0 && answerA != null && answerA.image != null) answerA.image.color = correctColor;
            if (q.correctAnswer == 1 && answerB != null && answerB.image != null) answerB.image.color = correctColor;
            if (q.correctAnswer == 2 && answerC != null && answerC.image != null) answerC.image.color = correctColor;
            if (q.correctAnswer == 3 && answerD != null && answerD.image != null) answerD.image.color = correctColor;

            // Highlight đáp án đã chọn
            if (saved != -1)
            {
                if (saved == q.correctAnswer)
                {
                    if (saved == 0 && answerA != null && answerA.image != null) answerA.image.color = correctSelectedColor;
                    if (saved == 1 && answerB != null && answerB.image != null) answerB.image.color = correctSelectedColor;
                    if (saved == 2 && answerC != null && answerC.image != null) answerC.image.color = correctSelectedColor;
                    if (saved == 3 && answerD != null && answerD.image != null) answerD.image.color = correctSelectedColor;
                }
                else
                {
                    if (saved == 0 && answerA != null && answerA.image != null) answerA.image.color = incorrectColor;
                    if (saved == 1 && answerB != null && answerB.image != null) answerB.image.color = incorrectColor;
                    if (saved == 2 && answerC != null && answerC.image != null) answerC.image.color = incorrectColor;
                    if (saved == 3 && answerD != null && answerD.image != null) answerD.image.color = incorrectColor;
                }
            }
        }
        else
        {
            // Chế độ thi bình thường: Tô màu xanh lá cây (#1FA659) khi được chọn
            Color selectedColor = new Color(0.12f, 0.65f, 0.35f);
            if (saved == 0 && answerA != null && answerA.image != null) answerA.image.color = selectedColor;
            if (saved == 1 && answerB != null && answerB.image != null) answerB.image.color = selectedColor;
            if (saved == 2 && answerC != null && answerC.image != null) answerC.image.color = selectedColor;
            if (saved == 3 && answerD != null && answerD.image != null) answerD.image.color = selectedColor;
        }
    }

    public bool IsQuestionCorrect(int index)
    {
        if (index < 0 || index >= questions.Count || index >= selectedAnswers.Count) return false;
        return selectedAnswers[index] == questions[index].correctAnswer;
    }

    public void ShowSubmitPopup()
    {
        if (submitPopup != null)
        {
            submitPopup.SetActive(true);
            submitPopup.transform.SetAsLastSibling();
        }
    }

    public void CancelSubmit()
    {
        if (submitPopup != null)
            submitPopup.SetActive(false);
    }

    public void ConfirmSubmit()
    {
        if (submitPopup != null) submitPopup.SetActive(false);

        if (examPanel != null) examPanel.SetActive(false);
        if (questionListPanel != null) questionListPanel.gameObject.SetActive(false);

        if (resultPanel != null)
        {
            resultPanel.SetActive(true);
            resultPanel.transform.SetAsLastSibling();
        }

        CalculateResult();

        if (answerA != null) { answerA.interactable = true; answerA.enabled = false; }
        if (answerB != null) { answerB.interactable = true; answerB.enabled = false; }
        if (answerC != null) { answerC.interactable = true; answerC.enabled = false; }
        if (answerD != null) { answerD.interactable = true; answerD.enabled = false; }
    }

    private void UpdateWarningContainerStyle(bool passed, string text, Color primaryColor, Color bgColor)
    {
        if (warningContainer == null) return;
        
        warningContainer.SetActive(true);
        
        // Cập nhật màu viền (nằm ở Image của warningContainer)
        Image borderImg = warningContainer.GetComponent<Image>();
        if (borderImg != null)
        {
            borderImg.color = primaryColor;
        }
        
        // Cập nhật màu nền (nằm ở Image của child tên "Bg")
        Transform bgTrans = warningContainer.transform.Find("Bg");
        if (bgTrans != null)
        {
            Image bgImg = bgTrans.GetComponent<Image>();
            if (bgImg != null)
            {
                bgImg.color = bgColor;
            }
        }
        
        // Cập nhật nội dung chữ
        if (txtWarning != null)
        {
            txtWarning.text = text;
            txtWarning.color = primaryColor;
        }
    }

    private void CalculateResult()
    {
        int correct = 0;
        bool failedCritical = false;

        for (int i = 0; i < questions.Count; i++)
        {
            bool isCorrect = (selectedAnswers[i] == questions[i].correctAnswer);
            if (isCorrect)
            {
                correct++;
            }
            else
            {
                // Nếu trả lời sai câu hỏi điểm liệt
                if (questions[i].isCritical)
                {
                    failedCritical = true;
                }
            }
        }

        int total = questions.Count;
        
        // Điều kiện ĐẠT của hạng B2: Đúng từ 32/35 câu trở lên VÀ không sai câu điểm liệt
        bool passed = (correct >= 32) && !failedCritical;

        if (txtResult != null)
        {
            txtResult.text = passed ? "Đạt" : "Trượt";
            txtResult.color = passed ? new Color(0.12f, 0.65f, 0.35f) : new Color(0.85f, 0.22f, 0.22f); // Beautiful green and red
        }

        if (txtScore != null)
        {
            txtScore.text = $"Số câu đúng: {correct}/{total}";
        }

        if (passed)
        {
            UpdateWarningContainerStyle(true, "Chúc mừng! Bạn đã ĐẠT bài thi lý thuyết hạng B2.", new Color(0.12f, 0.65f, 0.35f), new Color(0.9f, 0.98f, 0.93f));
        }
        else
        {
            string warnText = failedCritical ? "Bạn đã TRƯỢT do trả lời sai câu hỏi ĐIỂM LIỆT!" : "Bạn đã TRƯỢT do không đủ số câu đúng tối thiểu (32/35).";
            UpdateWarningContainerStyle(false, warnText, new Color(0.85f, 0.22f, 0.22f), new Color(1.0f, 0.94f, 0.94f));
        }

        if (txtTime != null)
        {
            txtTime.text = $"Ngày giờ thi: {System.DateTime.Now:dd/MM/yyyy HH:mm}";
        }
    }

    private void UpdateTimerUI()
    {
        if (txtTimer == null) return;
        
        int m = Mathf.FloorToInt(remainingTime / 60);
        int s = Mathf.FloorToInt(remainingTime % 60);
        txtTimer.text = $"Thời gian còn lại: {m:00}:{s:00}";
    }

    public void JumpToQuestion(int index)
    {
        if (index < 0 || index >= questions.Count) return;

        currentQuestionIndex = index;
        ShowQuestion();
        UpdateQuestionListUI();
    }

    public void UpdateQuestionListUI()
    {
        for (int i = 0; i < questionButtons.Count; i++)
        {
            if (questionButtons[i] == null) continue;
            bool isCurrent = (i == currentQuestionIndex);
            bool isAnswered = false;
            if (i < selectedAnswers.Count)
            {
                isAnswered = selectedAnswers[i] != -1;
            }

            questionButtons[i].SetState(isCurrent, isAnswered);
        }
    }

    private void CreateQuestionButtons()
    {
        // Xóa các nút câu hỏi cũ
        if (questionListPanel != null)
        {
            foreach (Transform child in questionListPanel)
            {
                if (child != null) Destroy(child.gameObject);
            }
        }

        questionButtons.Clear();

        for (int i = 0; i < questions.Count; i++)
        {
            if (questionButtonPrefab == null || questionListPanel == null) break;
            
            GameObject obj = Instantiate(questionButtonPrefab, questionListPanel);
            if (obj == null) continue;

            QuestionButton btn = obj.GetComponent<QuestionButton>();
            if (btn != null)
            {
                btn.Init(i, this);
                questionButtons.Add(btn);
            }

            TMP_Text txt = obj.GetComponentInChildren<TMP_Text>();
            if (txt != null) txt.text = (i + 1).ToString();
        }

        UpdateQuestionListUI();
    }

    private void UpdateProgressUI()
    {
        int answered = 0;
        for (int i = 0; i < selectedAnswers.Count; i++)
        {
            if (selectedAnswers[i] != -1)
                answered++;
        }

        if (txtProgress != null)
            txtProgress.text = $"Đã làm: {answered}/{questions.Count}";
    }

    public void RetryExam()
    {
        InitExam(selectedSetIndex, isPracticeMode);
    }

    public void ReviewExam()
    {
        isReviewMode = true;
        if (resultPanel != null) resultPanel.SetActive(false);
        if (examPanel != null) examPanel.SetActive(true);
        if (questionListPanel != null) questionListPanel.gameObject.SetActive(true);

        if (answerA != null) { answerA.interactable = true; answerA.enabled = false; }
        if (answerB != null) { answerB.interactable = true; answerB.enabled = false; }
        if (answerC != null) { answerC.interactable = true; answerC.enabled = false; }
        if (answerD != null) { answerD.interactable = true; answerD.enabled = false; }

        currentQuestionIndex = 0;
        ShowQuestion();
        UpdateQuestionListUI();
    }

    public void BackToMenu()
    {
        SceneManager.LoadScene("MainMenu");
    }

    /// <summary>
    /// Quay lại màn hình chọn bộ đề thi
    /// </summary>
    public void BackToSelection()
    {
        isReviewMode = false;
        if (examPanel != null) examPanel.SetActive(false);
        if (submitPopup != null) submitPopup.SetActive(false);
        if (resultPanel != null) resultPanel.SetActive(false);
        if (tipsPanel != null) tipsPanel.SetActive(false);
        if (questionListPanel != null) questionListPanel.gameObject.SetActive(false);

        if (selectionPanel != null) selectionPanel.SetActive(true);
    }

    /// <summary>
    /// Hiển thị panel Mẹo thi lý thuyết
    /// </summary>
    public void ShowTipsPanel()
    {
        if (selectionPanel != null) selectionPanel.SetActive(false);
        if (tipsPanel != null) tipsPanel.SetActive(true);
    }

    /// <summary>
    /// Ẩn panel Mẹo thi lý thuyết
    /// </summary>
    public void HideTipsPanel()
    {
        if (tipsPanel != null) tipsPanel.SetActive(false);
        if (selectionPanel != null) selectionPanel.SetActive(true);
    }

#if UNITY_EDITOR
    public void PreviewPassedState()
    {
        if (resultPanel != null) resultPanel.SetActive(true);
        if (txtResult != null)
        {
            txtResult.text = "Đạt";
            txtResult.color = new Color(0.12f, 0.65f, 0.35f);
        }
        if (txtScore != null)
        {
            txtScore.text = "Số câu đúng: 35/35";
        }
        UpdateWarningContainerStyle(true, "Chúc mừng! Bạn đã ĐẠT bài thi lý thuyết hạng B2.", new Color(0.12f, 0.65f, 0.35f), new Color(0.9f, 0.98f, 0.93f));
        if (txtTime != null)
        {
            txtTime.text = "Ngày giờ thi: 28/06/2026 20:29";
        }
        UnityEditor.EditorUtility.SetDirty(this);
        if (resultPanel != null) UnityEditor.EditorUtility.SetDirty(resultPanel);
    }

    public void PreviewFailedState()
    {
        if (resultPanel != null) resultPanel.SetActive(true);
        if (txtResult != null)
        {
            txtResult.text = "Trượt";
            txtResult.color = new Color(0.85f, 0.22f, 0.22f);
        }
        if (txtScore != null)
        {
            txtScore.text = "Số câu đúng: 31/35";
        }
        UpdateWarningContainerStyle(false, "Bạn đã TRƯỢT do trả lời sai câu hỏi ĐIỂM LIỆT!", new Color(0.85f, 0.22f, 0.22f), new Color(1.0f, 0.94f, 0.94f));
        if (txtTime != null)
        {
            txtTime.text = "Ngày giờ thi: 28/06/2026 20:29";
        }
        UnityEditor.EditorUtility.SetDirty(this);
        if (resultPanel != null) UnityEditor.EditorUtility.SetDirty(resultPanel);
    }

    public void HideResultPreview()
    {
        if (resultPanel != null)
        {
            resultPanel.SetActive(false);
            UnityEditor.EditorUtility.SetDirty(resultPanel);
        }
        UnityEditor.EditorUtility.SetDirty(this);
    }
#endif

    private System.Collections.IEnumerator ShowTemporaryMessage(Button button, string originalText)
    {
        if (button == null) yield break;
        TMP_Text txt = button.GetComponentInChildren<TMP_Text>();
        if (txt == null) yield break;
        
        button.interactable = false;
        txt.text = "Tính năng đang phát triển";
        
        yield return new WaitForSecondsRealtime(2f);
        
        txt.text = originalText;
        button.interactable = true;
    }
}

[System.Serializable]
public class TheoryQuestionSet
{
    public int setIndex;
    public string setName;
    public List<TheoryQuestion> questions;
}

[System.Serializable]
public class TheoryQuestionData
{
    public List<TheoryQuestionSet> sets;
}