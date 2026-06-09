using UnityEngine;
using UnityEditor;
using TMPro;

public class CreateStartText : EditorWindow
{
    [MenuItem("Tools/Vietnam B2 Simulator/Create Start Text (Road Marking)")]
    public static void CreateRoadText()
    {
        GameObject car = GameObject.Find("PlayerCar");
        Vector3 spawnPos = new Vector3(266.2f, 0.02f, -37.8f); // Default starting area position
        Quaternion spawnRot = Quaternion.Euler(90f, 0f, 0f);

        if (car != null)
        {
            // Đặt text ở phía trước xe 4m, áp sát mặt đường để làm vạch kẻ đường
            spawnPos = car.transform.position + car.transform.forward * 4f;
            spawnPos.y = 0.02f; // Nâng nhẹ để tránh lỗi Z-fighting với mặt đường nhựa

            // Xoay nằm phẳng trên mặt đất và quay theo hướng của xe
            float carYaw = car.transform.eulerAngles.y;
            spawnRot = Quaternion.Euler(90f, carYaw, 0f);
        }

        GameObject textObj = new GameObject("Text_XuatPhat");
        textObj.transform.position = spawnPos;
        textObj.transform.rotation = spawnRot;

        TextMeshPro tmp = textObj.AddComponent<TextMeshPro>();
        tmp.text = "XUẤT PHÁT";
        tmp.fontSize = 12;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color = Color.white;
        tmp.fontStyle = FontStyles.Bold;

        // Đăng ký hệ thống Undo của Unity để người dùng có thể Ctrl+Z dễ dàng
        Undo.RegisterCreatedObjectUndo(textObj, "Create Xuat Phat Text");
        Selection.activeGameObject = textObj;

        Debug.Log($"[B2 Simulator] Đã tạo chữ 'XUẤT PHÁT' tại tọa độ {spawnPos}");
    }

    [MenuItem("Tools/Vietnam B2 Simulator/Create Start Text (Floating Above Car)")]
    public static void CreateFloatingText()
    {
        GameObject car = GameObject.Find("PlayerCar");
        Vector3 spawnPos = new Vector3(266.2f, 2.5f, -37.8f);
        Quaternion spawnRot = Quaternion.identity;

        if (car != null)
        {
            // Đặt text lơ lửng ngay phía trên xe 2.5m
            spawnPos = car.transform.position + Vector3.up * 2.5f;
            spawnRot = Quaternion.Euler(0f, car.transform.eulerAngles.y, 0f);
        }

        GameObject textObj = new GameObject("Text_XuatPhat_Floating");
        textObj.transform.position = spawnPos;
        textObj.transform.rotation = spawnRot;
        
        // Gán làm con của PlayerCar để nó di chuyển theo xe nếu muốn
        if (car != null)
        {
            textObj.transform.SetParent(car.transform);
        }

        TextMeshPro tmp = textObj.AddComponent<TextMeshPro>();
        tmp.text = "XUẤT PHÁT";
        tmp.fontSize = 8;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color = Color.yellow;
        tmp.fontStyle = FontStyles.Bold;

        Undo.RegisterCreatedObjectUndo(textObj, "Create Floating Xuat Phat Text");
        Selection.activeGameObject = textObj;

        Debug.Log($"[B2 Simulator] Đã tạo chữ lơ lửng 'XUẤT PHÁT' phía trên xe tại {spawnPos}");
    }
}
