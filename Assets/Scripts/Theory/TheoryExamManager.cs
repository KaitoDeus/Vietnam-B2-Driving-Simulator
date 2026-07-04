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

    [Header("Answer Buttons")]
    public Button answerA;
    public Button answerB;
    public Button answerC;
    public Button answerD;

    [Header("Navigation Buttons")]
    public Button btnPrevious;
    public Button btnNext;

    [Header("Panels")]
    public GameObject examPanel;
    public GameObject submitPopup;
    public GameObject resultPanel;

    [Header("Result UI")]
    public TMP_Text txtResult;
    public TMP_Text txtScore;
    public TMP_Text txtWarning;
    public TMP_Text txtTime;    
    public TMP_Text txtProgress;
    public TMP_Text testDebug;
    


    private List<TheoryQuestion> questions = new List<TheoryQuestion>();
    private List<int> selectedAnswers = new List<int>();
    public GameObject questionButtonPrefab;
    public Transform questionListPanel;
    private int currentQuestionIndex = 0;
    private float remainingTime = 20 * 60;


    private void Start()
{
    CreateSampleQuestions();

    selectedAnswers = new List<int>();
    for (int i = 0; i < questions.Count; i++)
        selectedAnswers.Add(-1);

    submitPopup.SetActive(false);
    resultPanel.SetActive(false);

    answerA.onClick.AddListener(SelectAnswerA);
    answerB.onClick.AddListener(SelectAnswerB);
    answerC.onClick.AddListener(SelectAnswerC);
    answerD.onClick.AddListener(SelectAnswerD);

    ShowQuestion();
    UpdateTimerUI();
    CreateQuestionButtons();
    UpdateProgressUI();
}

    private void Update()
    {
        if (remainingTime <= 0) return;

        remainingTime -= Time.deltaTime;
        UpdateTimerUI();
    }


    private void ShowQuestion()
{
    TheoryQuestion q = questions[currentQuestionIndex];

    txtQuestionNumber.text = $"Câu {currentQuestionIndex + 1}/{questions.Count}";
    txtQuestionContent.text = q.question;

    answerA.GetComponentInChildren<TMP_Text>().text = "A. " + q.answerA;
    answerB.GetComponentInChildren<TMP_Text>().text = "B. " + q.answerB;
    answerC.GetComponentInChildren<TMP_Text>().text = "C. " + q.answerC;
    answerD.GetComponentInChildren<TMP_Text>().text = "D. " + q.answerD;

   
    answerA.image.color = Color.white;
    answerB.image.color = Color.white;
    answerC.image.color = Color.white;
    answerD.image.color = Color.white;

  
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
        answerA.image.color = Color.white;
        answerB.image.color = Color.white;
        answerC.image.color = Color.white;
        answerD.image.color = Color.white;

        int saved = selectedAnswers[currentQuestionIndex];

        if (saved == 0) answerA.image.color = Color.yellow;
        if (saved == 1) answerB.image.color = Color.yellow;
        if (saved == 2) answerC.image.color = Color.yellow;
        if (saved == 3) answerD.image.color = Color.yellow;
    }

    public void ShowSubmitPopup()
{
    submitPopup.SetActive(true);
    submitPopup.transform.SetAsLastSibling();
}

public void CancelSubmit()
{
    submitPopup.SetActive(false);
}


    public void ConfirmSubmit()
{
    submitPopup.SetActive(false);

    examPanel.SetActive(false);
    questionListPanel.gameObject.SetActive(false);

    resultPanel.SetActive(true);
    resultPanel.transform.SetAsLastSibling();

    CalculateResult();

    answerA.interactable = false;
    answerB.interactable = false;
    answerC.interactable = false;
    answerD.interactable = false;
}

    private void CalculateResult()
{
    int correct = 0;

    for (int i = 0; i < questions.Count; i++)
    {
        if (selectedAnswers[i] == questions[i].correctAnswer)
            correct++;
    }

    int total = questions.Count;
    bool passed = correct >= total * 0.8f;

    txtResult.text = passed ? "Đậu" : "Trượt";
    txtResult.color = passed ? Color.green : Color.red;

    txtScore.text = $"Số câu đúng: {correct}/{total}";

    txtWarning.text = passed
        ? "Chúc mừng! Bạn đã hoàn thành bài thi."
        : "Bạn chưa đạt. Hãy ôn tập và thử lại.";

    txtTime.text = $"Ngày giờ thi: {System.DateTime.Now:dd/MM/yyyy HH:mm}";
}

    private void UpdateTimerUI()
    {
        int m = Mathf.FloorToInt(remainingTime / 60);
        int s = Mathf.FloorToInt(remainingTime % 60);
        txtTimer.text = $"Thời gian còn lại: {m:00}:{s:00}";
    }

    private void CreateSampleQuestions()
{
    questions.Add(new TheoryQuestion
    {
        question = "Câu 1?",
        answerA = "A1",
        answerB = "B1",
        answerC = "C1",
        answerD = "D1",
        correctAnswer = 0
    });

    questions.Add(new TheoryQuestion
    {
        question = "Câu 2?",
        answerA = "A2",
        answerB = "B2",
        answerC = "C2",
        answerD = "D2",
        correctAnswer = 1
    });

    questions.Add(new TheoryQuestion
    {
        question = "Câu 3?",
        answerA = "A3",
        answerB = "B3",
        answerC = "C3",
        answerD = "D3",
        correctAnswer = 2
    });
}

private bool IsAllAnswered()
{
    for (int i = 0; i < selectedAnswers.Count; i++)
    {
        if (selectedAnswers[i] == -1)
            return false;
    }
    return true;
}


public void JumpToQuestion(int index)
{
    if (index < 0 || index >= questions.Count) return;

    currentQuestionIndex = index;
    ShowQuestion();
    UpdateQuestionListUI();
}

public List<QuestionButton> questionButtons = new List<QuestionButton>();

public void UpdateQuestionListUI()
{
    for (int i = 0; i < questionButtons.Count; i++)
    {
        bool isCurrent = (i == currentQuestionIndex);
        bool isAnswered = selectedAnswers[i] != -1;

        questionButtons[i].SetState(isCurrent, isAnswered);
    }
}
private void CreateQuestionButtons()
{
    questionButtons.Clear();

    for (int i = 0; i < questions.Count; i++)
    {
        GameObject obj = Instantiate(questionButtonPrefab, questionListPanel);

        QuestionButton btn = obj.GetComponent<QuestionButton>();
        btn.Init(i, this);

        TMP_Text txt = obj.GetComponentInChildren<TMP_Text>();
        txt.text = (i + 1).ToString();

        questionButtons.Add(btn);
    }

    UpdateQuestionListUI();
    UpdateProgressUI();
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
    currentQuestionIndex = 0;
    remainingTime = 20 * 60;

    selectedAnswers.Clear();
    for (int i = 0; i < questions.Count; i++)
        selectedAnswers.Add(-1);

    answerA.interactable = true;
    answerB.interactable = true;
    answerC.interactable = true;
    answerD.interactable = true;

    resultPanel.SetActive(false);
    examPanel.SetActive(true);
    questionListPanel.gameObject.SetActive(true);

    ShowQuestion();
    UpdateTimerUI();
    UpdateQuestionListUI();
    UpdateProgressUI();
}

public void ReviewExam()
{
    resultPanel.SetActive(false);
    examPanel.SetActive(true);
    questionListPanel.gameObject.SetActive(true);

    answerA.interactable = false;
    answerB.interactable = false;
    answerC.interactable = false;
    answerD.interactable = false;

    currentQuestionIndex = 0;

    ShowQuestion();
    UpdateQuestionListUI();
}

public void BackToMenu()
{
    UnityEngine.SceneManagement.SceneManager.LoadScene("MainMenu");
}

}