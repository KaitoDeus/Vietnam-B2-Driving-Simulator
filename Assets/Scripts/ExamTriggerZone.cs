using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Quản lý Vùng Trigger Khu Vực Xác Định (Zone Trigger) dành riêng cho Bài 2, 3, 7, 8, 10.
/// Bất kỳ bộ phận nào của xe (đầu xe, bánh xe, thân xe) vừa chạm/đi vào vùng BoxCollider 
/// thì thời gian đếm ngược bài thi lập tức được kích hoạt.
/// </summary>
[RequireComponent(typeof(Collider))]
public class ExamTriggerZone : MonoBehaviour
{
    [Header("Cấu hình Vùng Bài Thi")]
    [Tooltip("Bài thi áp dụng vùng này (Bài 2, 3, 7, 8, 10)")]
    public ExamStep examStep;

    [Tooltip("Trạng thái hoạt động của vùng trigger")]
    public bool isZoneActive = true;

    private HashSet<Collider> activeColliders = new HashSet<Collider>();
    private bool isCurrentlyInside = false;

    private void Awake()
    {
        Collider col = GetComponent<Collider>();
        if (col != null)
        {
            col.isTrigger = true;
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!isZoneActive) return;

        if (IsPlayerCar(other))
        {
            activeColliders.Add(other);
            UpdateZoneState();
        }
    }

    private void OnTriggerStay(Collider other)
    {
        if (!isZoneActive) return;

        if (IsPlayerCar(other))
        {
            if (!activeColliders.Contains(other))
            {
                activeColliders.Add(other);
                UpdateZoneState();
            }
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (!isZoneActive) return;

        if (IsPlayerCar(other))
        {
            activeColliders.Remove(other);
            UpdateZoneState();
        }
    }

    private bool IsPlayerCar(Collider col)
    {
        if (col == null) return false;
        return col.CompareTag("Player") || col.GetComponentInParent<CarController>() != null || col.attachedRigidbody != null;
    }

    private void UpdateZoneState()
    {
        activeColliders.RemoveWhere(c => c == null || !c.enabled || !c.gameObject.activeInHierarchy);
        
        bool isInside = activeColliders.Count > 0;

        if (isCurrentlyInside != isInside)
        {
            isCurrentlyInside = isInside;
            if (ExamManager.Instance != null)
            {
                ExamManager.Instance.SetZoneTriggerState(examStep, isInside);
            }
        }
    }

    private void OnDisable()
    {
        activeColliders.Clear();
        isCurrentlyInside = false;
        if (ExamManager.Instance != null)
        {
            ExamManager.Instance.SetZoneTriggerState(examStep, false);
        }
    }

    private void OnDrawGizmos()
    {
        Color fillColor = isCurrentlyInside ? new Color(0f, 1f, 0f, 0.35f) : new Color(0f, 0.9f, 1f, 0.35f);
        Color wireColor = isCurrentlyInside ? Color.green : new Color(0f, 1f, 1f, 0.9f);

        Gizmos.color = fillColor;
        Collider col = GetComponent<Collider>();
        if (col != null)
        {
            if (col is BoxCollider box)
            {
                Matrix4x4 oldMatrix = Gizmos.matrix;
                Gizmos.matrix = transform.localToWorldMatrix;
                Gizmos.DrawCube(box.center, box.size);
                Gizmos.color = wireColor;
                Gizmos.DrawWireCube(box.center, box.size);
                Gizmos.matrix = oldMatrix;
            }
        }
    }
}
