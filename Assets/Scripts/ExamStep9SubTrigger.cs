using UnityEngine;

public class ExamStep9SubTrigger : MonoBehaviour
{
    public enum Step9SignType
    {
        Sign1_StartSpeedUp = 1,   // Biển 1: Bắt đầu tăng số (>20km/h)
        Sign2_StartSpeedDown = 2, // Biển 2: Bắt đầu giảm số (<20km/h)
        Sign3_EndStep9 = 3        // Biển 3: Biển rẽ trái / Kết thúc Bài 9
    }

    [Header("Cấu hình Biển báo Bài 9")]
    [Tooltip("Chọn loại biển báo trên sân thi cho Bài 9 (1: Tăng số >20km/h, 2: Giảm số <20km/h, 3: Rẽ trái/Kết thúc)")]
    public Step9SignType signType = Step9SignType.Sign1_StartSpeedUp;

    private void OnTriggerEnter(Collider other)
    {
        CheckTrigger(other);
    }

    private void OnTriggerStay(Collider other)
    {
        CheckTrigger(other);
    }

    private void CheckTrigger(Collider other)
    {
        if (other.CompareTag("Player") || other.GetComponentInParent<CarController>() != null)
        {
            if (ExamManager.Instance != null && ExamManager.Instance.currentStep == ExamStep.ThayDoiSoDuongBang)
            {
                ExamManager.Instance.SetStep9Segment((int)signType);
            }
        }
    }

    private void OnDrawGizmos()
    {
        Color gizmoColor = Color.cyan;
        switch (signType)
        {
            case Step9SignType.Sign1_StartSpeedUp:
                gizmoColor = new Color(0.1f, 0.8f, 1f, 0.4f);
                break;
            case Step9SignType.Sign2_StartSpeedDown:
                gizmoColor = new Color(1f, 0.5f, 0f, 0.4f);
                break;
            case Step9SignType.Sign3_EndStep9:
                gizmoColor = new Color(0.2f, 1f, 0.2f, 0.4f);
                break;
        }

        Gizmos.color = gizmoColor;
        Collider col = GetComponent<Collider>();
        if (col != null && col is BoxCollider box)
        {
            Matrix4x4 oldMatrix = Gizmos.matrix;
            Gizmos.matrix = transform.localToWorldMatrix;
            Gizmos.DrawCube(box.center, box.size);
            Gizmos.color = new Color(gizmoColor.r, gizmoColor.g, gizmoColor.b, 0.9f);
            Gizmos.DrawWireCube(box.center, box.size);
            Gizmos.matrix = oldMatrix;
        }
    }
}
