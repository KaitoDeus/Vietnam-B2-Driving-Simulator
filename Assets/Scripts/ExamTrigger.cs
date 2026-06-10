using UnityEngine;

public class ExamTrigger : MonoBehaviour
{
    [Header("Cấu hình Bài thi")]
    [Tooltip("Chọn bài thi tương ứng với vùng đất này")]
    public ExamStep examStep;

    private void OnTriggerEnter(Collider other)
    {
        // Kiểm tra xem đối tượng chạm vào có phải là xe của người chơi không
        if (other.CompareTag("Player") || other.GetComponentInParent<CarController>() != null)
        {
            if (ExamManager.Instance != null)
            {
                ExamManager.Instance.EnterExamStep(examStep);
            }
        }
    }
}
