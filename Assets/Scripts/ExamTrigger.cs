using UnityEngine;
using System.Collections.Generic;

public class ExamTrigger : MonoBehaviour
{
    [Header("Cấu hình Bài thi")]
    [Tooltip("Chọn bài thi tương ứng với vùng đất này")]
    public ExamStep examStep;

    [Tooltip("Kích hoạt đếm ngược thời gian ngay khi xe đi vào Trigger này (áp dụng cho Bài 2, 3, 7, 8, 10)")]
    public bool isZoneEntryTrigger = false;

    private HashSet<Collider> activeColliders = new HashSet<Collider>();

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player") || other.GetComponentInParent<CarController>() != null)
        {
            activeColliders.Add(other);
            UpdateInsideState();
        }
    }

    private void OnTriggerStay(Collider other)
    {
        if (other.CompareTag("Player") || other.GetComponentInParent<CarController>() != null)
        {
            if (!activeColliders.Contains(other))
            {
                activeColliders.Add(other);
                UpdateInsideState();
            }
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player") || other.GetComponentInParent<CarController>() != null)
        {
            activeColliders.Remove(other);
            UpdateInsideState();
        }
    }

    private void UpdateInsideState()
    {
        activeColliders.RemoveWhere(c => c == null || !c.enabled || !c.gameObject.activeInHierarchy);

        bool isInside = activeColliders.Count > 0;
        if (ExamManager.Instance != null)
        {
            if (isInside)
            {
                ExamManager.Instance.EnterExamStep(examStep);

                // Nếu đánh dấu là Zone Entry Trigger, kích hoạt đếm ngược thời gian bài thi lập tức
                if (isZoneEntryTrigger)
                {
                    ExamManager.Instance.SetZoneTriggerState(examStep, true);
                }
            }
            ExamManager.Instance.SetInsideTrigger(examStep, isInside);

            if (isZoneEntryTrigger)
            {
                ExamManager.Instance.SetZoneTriggerState(examStep, isInside);
            }
        }
    }

    private void OnDisable()
    {
        activeColliders.Clear();
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = isZoneEntryTrigger ? new Color(0f, 1f, 0.5f, 0.4f) : new Color(1f, 0.8f, 0f, 0.3f);
        Collider col = GetComponent<Collider>();
        if (col != null && col is BoxCollider box)
        {
            Matrix4x4 oldMatrix = Gizmos.matrix;
            Gizmos.matrix = transform.localToWorldMatrix;
            Gizmos.DrawCube(box.center, box.size);
            Gizmos.color = isZoneEntryTrigger ? new Color(0f, 1f, 0.5f, 0.9f) : new Color(1f, 0.8f, 0f, 0.8f);
            Gizmos.DrawWireCube(box.center, box.size);
            Gizmos.matrix = oldMatrix;
        }
    }
}
