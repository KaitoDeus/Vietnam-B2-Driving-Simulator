using System.Collections;
using UnityEngine;

public class TrafficLightIntersection : MonoBehaviour
{
    [System.Serializable]
    public class LightGroup
    {
        public string name = "Group";
        public TrafficLight[] lights;
    }

    [Header("Traffic Light Groups")]
    [Tooltip("Nhóm đèn A (ví dụ: Hướng Bắc - Nam)")]
    public LightGroup groupA = new LightGroup() { name = "Group A" };
    [Tooltip("Nhóm đèn B (ví dụ: Hướng Đông - Tây)")]
    public LightGroup groupB = new LightGroup() { name = "Group B" };

    [Header("Timings (Seconds)")]
    public int greenDuration = 15;
    public int yellowDuration = 3;

    private bool isRunning = true;

    private void Start()
    {
        if (groupA.lights == null || groupA.lights.Length == 0 || groupB.lights == null || groupB.lights.Length == 0)
        {
            Debug.LogWarning("[TrafficLightIntersection] Chưa gán đủ đèn giao thông cho các nhóm A và B.");
            return;
        }

        Debug.Log($"[TrafficLightIntersection] Khởi tạo chu kỳ đèn giao thông thành công. Nhóm A: {groupA.lights.Length} đèn, Nhóm B: {groupB.lights.Length} đèn.");
        StartCoroutine(IntersectionCycleRoutine());
    }

    private IEnumerator IntersectionCycleRoutine()
    {
        while (isRunning)
        {
            // --- GIAI ĐOẠN 1: Nhóm A Xanh, Nhóm B Đỏ ---
            // Thời gian đèn Xanh của nhóm A = greenDuration
            // Đèn Đỏ của nhóm B = greenDuration + yellowDuration (bằng tổng thời gian xanh + vàng của nhóm A)
            int totalCycle1 = greenDuration;
            for (int i = totalCycle1; i > 0; i--)
            {
                SetGroupState(groupA, TrafficLightState.Green, i);
                SetGroupState(groupB, TrafficLightState.Red, i + yellowDuration);
                yield return new WaitForSeconds(1f);
            }

            // --- GIAI ĐOẠN 2: Nhóm A Vàng, Nhóm B vẫn Đỏ ---
            // Thời gian đèn Vàng của nhóm A = yellowDuration
            // Đèn Đỏ của nhóm B đếm ngược nốt yellowDuration giây cuối cùng
            int totalCycle2 = yellowDuration;
            for (int i = totalCycle2; i > 0; i--)
            {
                SetGroupState(groupA, TrafficLightState.Yellow, i);
                SetGroupState(groupB, TrafficLightState.Red, i);
                yield return new WaitForSeconds(1f);
            }

            // --- GIAI ĐOẠN 3: Nhóm A Đỏ, Nhóm B Xanh ---
            // Đèn Đỏ của nhóm A = greenDuration + yellowDuration
            // Đèn Xanh của nhóm B = greenDuration
            int totalCycle3 = greenDuration;
            for (int i = totalCycle3; i > 0; i--)
            {
                SetGroupState(groupA, TrafficLightState.Red, i + yellowDuration);
                SetGroupState(groupB, TrafficLightState.Green, i);
                yield return new WaitForSeconds(1f);
            }

            // --- GIAI ĐOẠN 4: Nhóm A vẫn Đỏ, Nhóm B Vàng ---
            // Đèn Đỏ của nhóm A đếm ngược nốt yellowDuration giây cuối cùng
            // Đèn Vàng của nhóm B = yellowDuration
            int totalCycle4 = yellowDuration;
            for (int i = totalCycle4; i > 0; i--)
            {
                SetGroupState(groupA, TrafficLightState.Red, i);
                SetGroupState(groupB, TrafficLightState.Yellow, i);
                yield return new WaitForSeconds(1f);
            }
        }
    }

    private void SetGroupState(LightGroup group, TrafficLightState state, int secondsRemaining)
    {
        foreach (var light in group.lights)
        {
            if (light != null)
            {
                light.SetState(state, secondsRemaining);
            }
        }
    }

    private void OnDisable()
    {
        isRunning = false;
    }
}
