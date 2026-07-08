using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class QuestionButton : MonoBehaviour
{
    public int questionIndex;
    public TheoryExamManager manager;

    private Image img;
    private Button btn;
    private TMP_Text txt;

    public void Init(int index, TheoryExamManager m)
    {
        questionIndex = index;
        manager = m;

        img = GetComponent<Image>();
        btn = GetComponent<Button>();
        txt = GetComponentInChildren<TMP_Text>();

        if (btn != null)
        {
            btn.onClick.RemoveAllListeners();
            btn.onClick.AddListener(OnClick);
        }
    }

    private void OnClick()
    {
        if (manager == null) return;

        manager.JumpToQuestion(questionIndex);
    }

    public void SetState(bool isCurrent, bool isAnswered)
    {
        if (img == null) img = GetComponent<Image>();
        if (txt == null) txt = GetComponentInChildren<TMP_Text>();



        if (isCurrent)
        {
            if (img != null) img.color = new Color(0.12f, 0.65f, 0.35f); // Màu xanh lá cây #1FA659
            if (txt != null)
            {
                txt.color = Color.white;
                txt.text = (questionIndex + 1).ToString();
            }
        }
        else if (isAnswered)
        {
            if (img != null) img.color = Color.white; // Nền trắng
            if (txt != null)
            {
                txt.color = new Color(0.1f, 0.13f, 0.17f); // Chữ đen/xám đậm
                txt.text = (questionIndex + 1).ToString();
            }
        }
        else
        {
            if (img != null) img.color = new Color(0.93f, 0.95f, 0.96f); // Xám nhạt #EEF2F6
            if (txt != null)
            {
                txt.color = new Color(0.42f, 0.48f, 0.55f); // Hiển thị số câu với màu xám trung tính dễ nhìn
                txt.text = (questionIndex + 1).ToString();
            }
        }
    }
}