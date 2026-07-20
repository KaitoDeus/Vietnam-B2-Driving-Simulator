using UnityEngine;

/// <summary>
/// Xử lý va chạm thành/thân xe với chướng ngại vật và tường dải phân cách theo chuẩn thi B2.
/// </summary>
public class CarCollisionHandler : MonoBehaviour
{
    private float lastCollisionTime = -10f;

    [Tooltip("Thời gian chờ giữa 2 lần trừ điểm va chạm (giây)")]
    public float collisionCooldown = 2.0f;

    [Tooltip("Vận tốc va chạm tối thiểu để tính lỗi (m/s)")]
    public float minCollisionVelocity = 0.35f;

    private void Start()
    {
        AdjustCarHitbox();
    }

    /// <summary>
    /// Căn chỉnh BoxCollider thành xe khớp với Mesh thực tế,
    /// tách biệt khoảng sáng gầm và bánh xe để loại bỏ lỗi va chạm ảo.
    /// </summary>
    public void AdjustCarHitbox()
    {
        BoxCollider boxCol = GetComponent<BoxCollider>();
        if (boxCol == null)
        {
            boxCol = gameObject.AddComponent<BoxCollider>();
        }

        MeshRenderer[] renderers = GetComponentsInChildren<MeshRenderer>();
        if (renderers != null && renderers.Length > 0)
        {
            Bounds combinedBounds = new Bounds();
            bool initialized = false;

            foreach (var r in renderers)
            {
                if (r == null || !r.enabled) continue;
                string rName = r.gameObject.name.ToLower();
                
                // Bỏ qua bánh xe, bóng gầm và phụ kiện ngoài thân xe
                if (rName.Contains("wheel") || rName.Contains("tire") || rName.Contains("banhxe") || rName.Contains("shadow")) continue;

                if (!initialized)
                {
                    combinedBounds = r.bounds;
                    initialized = true;
                }
                else
                {
                    combinedBounds.Encapsulate(r.bounds);
                }
            }

            if (initialized)
            {
                Vector3 localCenter = transform.InverseTransformPoint(combinedBounds.center);
                Vector3 localSize = transform.InverseTransformVector(combinedBounds.size);
                localSize = new Vector3(Mathf.Abs(localSize.x), Mathf.Abs(localSize.y), Mathf.Abs(localSize.z));

                // Thu gọn kích thước vừa sát vỏ tôn thành xe
                localSize.x *= 0.90f;
                localSize.z *= 0.94f;

                // Nâng đáy Collider lên 0.22m để bóc tách phần gầm và bánh xe
                float groundClearance = 0.22f;
                localSize.y = Mathf.Max(0.4f, localSize.y - groundClearance);
                localCenter.y += groundClearance * 0.5f;

                boxCol.center = localCenter;
                boxCol.size = localSize;

                Debug.Log($"[CarCollisionHandler] Tinh chỉnh Hitbox thành xe: Center={localCenter}, Size={localSize}");
            }
        }
    }

    private void OnCollisionEnter(Collision collision)
    {
        ProcessCollision(collision);
    }

    private void OnCollisionStay(Collision collision)
    {
        ProcessCollision(collision);
    }

    private void ProcessCollision(Collision collision)
    {
        if (ExamManager.Instance == null || !ExamManager.Instance.isExamActive) return;
        if (Time.time - lastCollisionTime < collisionCooldown) return;
        if (collision.collider != null && collision.collider.isTrigger) return;

        GameObject obj = collision.gameObject;
        string nameLower = obj.name.ToLower();
        string tagLower = obj.tag.ToLower();

        // Bỏ qua mặt đường, địa hình và cầu vệt
        if (nameLower.Contains("ground") || 
            nameLower.Contains("road") || 
            nameLower.Contains("terrain") || 
            nameLower.Contains("track") ||
            tagLower.Contains("ground") || 
            tagLower.Contains("road"))
        {
            return;
        }

        // Bỏ qua lực va chạm quá nhỏ
        if (collision.relativeVelocity.magnitude < minCollisionVelocity) return;

        // Chỉ tính va chạm thực sự từ thành/thân xe (BoxCollider)
        bool isBodyCollision = false;

        for (int i = 0; i < collision.contactCount; i++)
        {
            ContactPoint contact = collision.GetContact(i);

            // Bỏ qua va chạm xuất phát từ Bánh xe (WheelCollider)
            if (contact.thisCollider is WheelCollider)
            {
                continue;
            }

            // Kiểm tra mặt tường đứng (|normal.y| <= 0.65) và độ cao tiếp xúc phần thân (>= 0.15m)
            if (Mathf.Abs(contact.normal.y) <= 0.65f)
            {
                float relativeHeight = contact.point.y - transform.position.y;
                if (relativeHeight >= 0.15f)
                {
                    isBodyCollision = true;
                    break;
                }
            }
        }

        if (!isBodyCollision) return;

        // Trừ điểm phạt va chạm chướng ngại vật
        lastCollisionTime = Time.time;
        Debug.Log($"[Collision] Va chạm thành xe: {obj.name} (Vận tốc: {collision.relativeVelocity.magnitude:F2} m/s)");
        ExamManager.Instance.DeductPoints(5, "Va chạm chướng ngại vật/vỉa hè");
    }
}
