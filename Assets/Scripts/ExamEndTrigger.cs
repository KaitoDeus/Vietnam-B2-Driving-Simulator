using UnityEngine;

public class ExamEndTrigger : MonoBehaviour
{
    [Header("Cấu hình Bài thi kết thúc")]
    [Tooltip("Chọn bài thi tương ứng muốn báo hoàn thành khi chạm vào vùng này")]
    public ExamStep stepToEnd;

    private void OnTriggerEnter(Collider other)
    {
        TriggerCheck(other);
    }

    private void OnTriggerStay(Collider other)
    {
        TriggerCheck(other);
    }

    private void TriggerCheck(Collider other)
    {
        // Kiểm tra xem đối tượng chạm vào có phải là xe của người chơi không
        if (other.CompareTag("Player") || other.GetComponentInParent<CarController>() != null)
        {
            if (ExamManager.Instance != null)
            {
                ExamManager.Instance.TriggerStepEnd(stepToEnd);
            }
        }
    }
}
