using UnityEngine;
using System.Collections.Generic;

public class ExamTrigger : MonoBehaviour
{
    [Header("Cấu hình Bài thi")]
    [Tooltip("Chọn bài thi tương ứng với vùng đất này")]
    public ExamStep examStep;

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
            }
            ExamManager.Instance.SetInsideTrigger(examStep, isInside);
        }
    }

    private void OnDisable()
    {
        activeColliders.Clear();
    }
}
