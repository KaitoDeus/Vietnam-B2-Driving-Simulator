using UnityEngine;
using UnityEngine.UI;

public class QuestionButton : MonoBehaviour
{
    public int questionIndex;
    public TheoryExamManager manager;

    private Image img;
    private Button btn;

    public void Init(int index, TheoryExamManager m)
    {
        questionIndex = index;
        manager = m;

        img = GetComponent<Image>();
        btn = GetComponent<Button>();

        btn.onClick.RemoveAllListeners();
        btn.onClick.AddListener(OnClick);
    }

    private void OnClick()
    {
        if (manager == null) return;

        manager.JumpToQuestion(questionIndex);
    }

    public void SetState(bool isCurrent, bool isAnswered)
{
    if (img == null)
        img = GetComponent<Image>();

    if (isCurrent)
    {
        img.color = Color.yellow; // đang làm
    }
    else if (isAnswered)
    {
        img.color = Color.green; // đã trả lời
    }
    else
    {
        img.color = Color.gray; // chưa làm
    }
}
}