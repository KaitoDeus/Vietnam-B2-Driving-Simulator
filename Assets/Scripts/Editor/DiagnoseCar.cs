using UnityEngine;
using UnityEditor;
using System.IO;
using System.Text;

[InitializeOnLoad]
public class DiagnoseCar
{
    private static StringBuilder sb = new StringBuilder();

    static DiagnoseCar()
    {
        try
        {
            File.WriteAllText("D:\\Unity\\Vietnam B2 Driving Simulator\\diagnose_output.txt", "Static constructor executing...\n");
        }
        catch {}

        // Run diagnosis automatically when Unity compiles
        EditorApplication.delayCall += () => {
            Diagnose();
        };
    }

    [MenuItem("Tools/Vietnam B2 Simulator/Diagnose Player Car")]
    public static void Diagnose()
    {
        sb.Clear();
        Log("=== DIAGNOSING PLAYER CAR ===");

        GameObject car = GameObject.Find("PlayerCar");
        if (car == null)
        {
            Log("ERROR: PlayerCar not found in the scene!");
            SaveToFile();
            return;
        }

        // Kiểm tra xem xe có bị lỗi lệch tâm hoặc cấu hình sai không để tự động sửa chữa
        CarController controller = car.GetComponent<CarController>();
        Transform visuals = car.transform.Find("Visuals");
        Transform modelRoot = null;
        if (visuals != null && visuals.childCount > 0)
        {
            modelRoot = visuals.GetChild(0);
        }

        bool needsFix = false;
        if (modelRoot != null && (modelRoot.localPosition != Vector3.zero || modelRoot.localRotation != Quaternion.identity))
        {
            needsFix = true;
            Log("Auto-Fix: Phát hiện model hình ảnh bị lệch tâm so với root.");
        }
        if (controller != null && controller.wheelRotationOffset != Vector3.zero)
        {
            needsFix = true;
            Log("Auto-Fix: Phát hiện wheelRotationOffset chưa được reset về 0 cho xe Tocus.");
        }
        if (controller != null && controller.frontLeftTransform != null && controller.frontLeftTransform.name.EndsWith("_Pivot"))
        {
            needsFix = true;
            Log("Auto-Fix: Phát hiện cấu trúc Pivot ảo cũ cần dọn dẹp.");
        }

        if (needsFix)
        {
            Log("TIẾN HÀNH TỰ ĐỘNG SỬA CHỮA CẤU TRÚC XE...");
            AlignWheelCollidersToTocus.RebuildFromScratch(car);
            controller = car.GetComponent<CarController>(); // Cập nhật lại tham chiếu
        }

        Log("Position: " + car.transform.position);
        Log("Rotation: " + car.transform.rotation.eulerAngles);
        Log("Scale: " + car.transform.localScale);
        Log("Tag: " + car.tag + " | Layer: " + LayerMask.LayerToName(car.layer));

        Rigidbody rb = car.GetComponent<Rigidbody>();
        if (rb != null)
        {
            Log("Rigidbody found:");
            Log(" - Mass: " + rb.mass);
            Log(" - Is Kinematic: " + rb.isKinematic);
            Log(" - Use Gravity: " + rb.useGravity);
            Log(" - Constraints: " + rb.constraints);
        }
        else
        {
            Log("ERROR: Rigidbody NOT found on PlayerCar!");
        }

        Collider[] colliders = car.GetComponents<Collider>();
        Log("Colliders on root: " + colliders.Length);
        foreach (var col in colliders)
        {
            Log(" - Root Collider: " + col.GetType().Name + " | Is Trigger: " + col.isTrigger);
        }

        if (controller != null)
        {
            Log("CarController found:");
            Log(" - frontLeftCollider: " + (controller.frontLeftCollider != null ? controller.frontLeftCollider.name : "Null"));
            Log(" - frontRightCollider: " + (controller.frontRightCollider != null ? controller.frontRightCollider.name : "Null"));
            Log(" - rearLeftCollider: " + (controller.rearLeftCollider != null ? controller.rearLeftCollider.name : "Null"));
            Log(" - rearRightCollider: " + (controller.rearRightCollider != null ? controller.rearRightCollider.name : "Null"));
            Log(" - frontLeftTransform: " + (controller.frontLeftTransform != null ? controller.frontLeftTransform.name : "Null"));
            Log(" - frontRightTransform: " + (controller.frontRightTransform != null ? controller.frontRightTransform.name : "Null"));
            Log(" - rearLeftTransform: " + (controller.rearLeftTransform != null ? controller.rearLeftTransform.name : "Null"));
            Log(" - rearRightTransform: " + (controller.rearRightTransform != null ? controller.rearRightTransform.name : "Null"));
        }
        else
        {
            Log("ERROR: CarController NOT found on PlayerCar!");
        }

        WheelCollider[] wheelCols = car.GetComponentsInChildren<WheelCollider>();
        Log("WheelColliders in children: " + wheelCols.Length);
        foreach (var col in wheelCols)
        {
            Log($" - {col.name}: Center={col.center}, Radius={col.radius}, SuspDist={col.suspensionDistance}, Spring={col.suspensionSpring.spring}, Damper={col.suspensionSpring.damper}");
        }

        // Check if there are nested colliders that might collide
        Collider[] allColliders = car.GetComponentsInChildren<Collider>();
        Log("Total colliders in hierarchy: " + allColliders.Length);
        foreach (var col in allColliders)
        {
            if (col is WheelCollider) continue;
            Log($" - Child Collider: {col.name} ({col.GetType().Name}) on {col.gameObject.name} | Is Trigger: {col.isTrigger}");
        }

        SaveToFile();
    }

    private static void Log(string message)
    {
        Debug.Log(message);
        sb.AppendLine(message);
    }

    private static void SaveToFile()
    {
        try
        {
            File.WriteAllText("D:\\Unity\\Vietnam B2 Driving Simulator\\diagnose_output.txt", sb.ToString());
        }
        catch (System.Exception ex)
        {
            Debug.LogError("Failed to write diagnosis to file: " + ex.Message);
        }
    }
}
