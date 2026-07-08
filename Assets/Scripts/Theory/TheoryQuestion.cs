using UnityEngine;

[System.Serializable]
public class TheoryQuestion
{
    public string question;

    public string answerA;
    public string answerB;
    public string answerC;
    public string answerD;

    public int correctAnswer;
    public bool isCritical; // Trạng thái câu hỏi điểm liệt
    public string imageName; // Tên của file ảnh minh họa (không có phần mở rộng) trong thư mục Resources
    public string explanation; // Giải thích đáp án đúng
}