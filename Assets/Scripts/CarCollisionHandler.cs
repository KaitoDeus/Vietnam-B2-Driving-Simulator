using UnityEngine;

public class CarCollisionHandler : MonoBehaviour
{
    private float lastCollisionTime = -10f;
    public float collisionCooldown = 2.0f; // Cooldown between deductions
    public float minCollisionVelocity = 0.3f; // Minimum impact speed to trigger deduction

    private void OnCollisionEnter(Collision collision)
    {
        // Don't deduct if exam is not active
        if (ExamManager.Instance == null || !ExamManager.Instance.isExamActive) return;

        // Check cooldown
        if (Time.time - lastCollisionTime < collisionCooldown) return;

        // Filter out collisions with Ground or Road
        GameObject obj = collision.gameObject;
        string nameLower = obj.name.ToLower();
        string tagLower = obj.tag.ToLower();

        if (nameLower.Contains("ground") || 
            nameLower.Contains("road") || 
            nameLower.Contains("terrain") || 
            nameLower.Contains("track") ||
            tagLower.Contains("ground") || 
            tagLower.Contains("road"))
        {
            return;
        }

        // Filter out low-velocity impacts (e.g. minor friction/vibrations)
        if (collision.relativeVelocity.magnitude < minCollisionVelocity) return;

        // Valid collision with curb/barrier/obstacle
        lastCollisionTime = Time.time;
        Debug.Log($"[Collision] Hitted: {obj.name} with velocity: {collision.relativeVelocity.magnitude}");
        ExamManager.Instance.DeductPoints(5, "Va chạm chướng ngại vật/vỉa hè");
    }
}
