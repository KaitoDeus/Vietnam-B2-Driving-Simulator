using UnityEngine;
using UnityEditor;

public class AlignWheelCollidersToTocus : EditorWindow
{
    private GameObject carObject;

    [MenuItem("Tools/Vietnam B2 Simulator/Align Wheel Colliders to Tocus")]
    public static void ShowWindow()
    {
        GetWindow<AlignWheelCollidersToTocus>("Align Wheel Colliders");
    }

    private void OnGUI()
    {
        GUILayout.Label("Align Wheel Colliders & Setup Tocus Wheels", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox("Chọn các tùy chọn bên dưới để căn chỉnh hoặc xóa đi tạo lại hoàn toàn các WheelCollider từ đầu.", MessageType.Info);

        carObject = (GameObject)EditorGUILayout.ObjectField("Player Car", carObject, typeof(GameObject), true);

        if (carObject == null)
        {
            carObject = GameObject.Find("PlayerCar");
        }

        EditorGUILayout.Space();

        if (GUILayout.Button("Cách 1: Căn chỉnh dựa trên Collider hiện tại"))
        {
            if (carObject == null)
            {
                EditorUtility.DisplayDialog("Error", "Please select or assign the PlayerCar GameObject.", "OK");
                return;
            }
            ExecuteAlignment(carObject);
        }

        EditorGUILayout.Space();

        GUI.backgroundColor = new Color(0.9f, 0.4f, 0.4f); // Màu đỏ nhạt cảnh báo
        if (GUILayout.Button("Cách 2: Xóa hết WheelColliders cũ & Tạo mới lại từ đầu"))
        {
            if (carObject == null)
            {
                EditorUtility.DisplayDialog("Error", "Please select or assign the PlayerCar GameObject.", "OK");
                return;
            }
            if (EditorUtility.DisplayDialog("Xác nhận", "Hành động này sẽ XÓA TOÀN BỘ nhóm WheelColliders cũ và tự động tạo mới + cấu hình lại từ đầu cho xe Tocus. Bạn có chắc chắn không?", "Có", "Không"))
            {
                RebuildFromScratch(carObject);
            }
        }
        GUI.backgroundColor = Color.white;
    }

    private void ExecuteAlignment(GameObject car)
    {
        CarController controller = car.GetComponent<CarController>();
        if (controller == null)
        {
            EditorUtility.DisplayDialog("Error", "CarController component not found on PlayerCar!", "OK");
            return;
        }

        Transform visuals = car.transform.Find("Visuals");
        if (visuals == null)
        {
            EditorUtility.DisplayDialog("Error", "Could not find 'Visuals' child under PlayerCar.", "OK");
            return;
        }

        // Căn chỉnh tâm root của xe trùng với model hình ảnh để tránh lỗi lệch vật lý
        AlignRootToVisuals(car, visuals);

        // Tìm mesh bánh xe (hỗ trợ cả các lỗi gõ sai chính tả như Font thay vì Front)
        Transform flVisual = FindDeepChild(visuals, "Tocus_Wheel_Left_Front");
        if (flVisual == null) flVisual = FindDeepChild(visuals, "Tocus_Wheel_Left_Font");
        Transform frVisual = FindDeepChild(visuals, "Tocus_Wheel_Right_Front");
        if (frVisual == null) frVisual = FindDeepChild(visuals, "Tocus_Wheel_Right_Font");
        Transform rlVisual = FindDeepChild(visuals, "Tocus_Wheel_Left_Back");
        if (rlVisual == null) rlVisual = FindDeepChild(visuals, "Tocus_Wheel_Left_Rear");
        Transform rrVisual = FindDeepChild(visuals, "Tocus_Wheel_Right_Back");
        if (rrVisual == null) rrVisual = FindDeepChild(visuals, "Tocus_Wheel_Right_Rear");

        if (flVisual == null || frVisual == null || rlVisual == null || rrVisual == null)
        {
            EditorUtility.DisplayDialog("Error", "Could not find Tocus wheel meshes under Visuals.\n\nPlease ensure you have drag-dropped the Tocus model under Visuals.", "OK");
            return;
        }

        WheelCollider flCol = controller.frontLeftCollider;
        WheelCollider frCol = controller.frontRightCollider;
        WheelCollider rlCol = controller.rearLeftCollider;
        WheelCollider rrCol = controller.rearRightCollider;

        if (flCol == null || frCol == null || rlCol == null || rrCol == null)
        {
            flCol = car.transform.Find("WheelColliders/Collider_FL")?.GetComponent<WheelCollider>();
            frCol = car.transform.Find("WheelColliders/Collider_FR")?.GetComponent<WheelCollider>();
            rlCol = car.transform.Find("WheelColliders/Collider_RL")?.GetComponent<WheelCollider>();
            rrCol = car.transform.Find("WheelColliders/Collider_RR")?.GetComponent<WheelCollider>();
        }

        if (flCol == null || frCol == null || rlCol == null || rrCol == null)
        {
            EditorUtility.DisplayDialog("Error", "Could not find 4 WheelColliders (Collider_FL, Collider_FR, etc.) on PlayerCar.", "OK");
            return;
        }

        Undo.RegisterCompleteObjectUndo(car, "Align Wheel Colliders");
        Undo.RecordObjects(new Object[] { flCol.transform, frCol.transform, rlCol.transform, rrCol.transform, flCol, frCol, rlCol, rrCol, controller }, "Align Wheel Colliders and Offsets");

        Transform wheelCollidersGroup = flCol.transform.parent;
        if (wheelCollidersGroup != null)
        {
            wheelCollidersGroup.SetParent(car.transform);
            wheelCollidersGroup.localPosition = Vector3.zero;
            wheelCollidersGroup.localRotation = Quaternion.identity;
            wheelCollidersGroup.localScale = Vector3.one;
        }

        flCol.transform.position = flVisual.position;
        frCol.transform.position = frVisual.position;
        rlCol.transform.position = rlVisual.position;
        rrCol.transform.position = rrVisual.position;

        float flRadius = GetWheelRadius(flVisual);
        float frRadius = GetWheelRadius(frVisual);
        float rlRadius = GetWheelRadius(rlVisual);
        float rrRadius = GetWheelRadius(rrVisual);
        float averageRadius = (flRadius + frRadius + rlRadius + rrRadius) / 4f;

        if (averageRadius < 0.1f || averageRadius > 1.5f) averageRadius = 0.34f;
        
        flCol.radius = averageRadius;
        frCol.radius = averageRadius;
        rlCol.radius = averageRadius;
        rrCol.radius = averageRadius;

        // Dọn dẹp pivot ảo cũ nếu có để giữ cấu trúc sạch
        CleanUpOldPivots(flVisual);
        CleanUpOldPivots(frVisual);
        CleanUpOldPivots(rlVisual);
        CleanUpOldPivots(rrVisual);

        controller.frontLeftTransform = flVisual;
        controller.frontRightTransform = frVisual;
        controller.rearLeftTransform = rlVisual;
        controller.rearRightTransform = rrVisual;
        controller.wheelRotationOffset = Vector3.zero; // Xe Tocus không cần xoay 90 độ Z như cylinder

        EditorUtility.SetDirty(car);
        EditorUtility.SetDirty(controller);
        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(car.scene);

        EditorUtility.DisplayDialog("Success", "Căn chỉnh WheelCollider thành công!\n\n- Bán kính lốp xe: " + averageRadius + "m.\n- Đã dọn dẹp các Pivot ảo và liên kết trực tiếp mesh vào CarController.", "OK");
    }

    public static void RebuildFromScratch(GameObject car)
    {
        CarController controller = car.GetComponent<CarController>();
        if (controller == null)
        {
            EditorUtility.DisplayDialog("Error", "CarController component not found on PlayerCar!", "OK");
            return;
        }

        Transform visuals = car.transform.Find("Visuals");
        if (visuals == null)
        {
            EditorUtility.DisplayDialog("Error", "Could not find 'Visuals' child under PlayerCar.", "OK");
            return;
        }

        // Căn chỉnh tâm root của xe trùng với model hình ảnh để tránh lệch vật lý
        AlignRootToVisuals(car, visuals);

        // Tìm mesh bánh xe (hỗ trợ cả các lỗi gõ sai chính tả như Font thay vì Front)
        Transform flVisual = FindDeepChild(visuals, "Tocus_Wheel_Left_Front");
        if (flVisual == null) flVisual = FindDeepChild(visuals, "Tocus_Wheel_Left_Font");
        Transform frVisual = FindDeepChild(visuals, "Tocus_Wheel_Right_Front");
        if (frVisual == null) frVisual = FindDeepChild(visuals, "Tocus_Wheel_Right_Font");
        Transform rlVisual = FindDeepChild(visuals, "Tocus_Wheel_Left_Back");
        if (rlVisual == null) rlVisual = FindDeepChild(visuals, "Tocus_Wheel_Left_Rear");
        Transform rrVisual = FindDeepChild(visuals, "Tocus_Wheel_Right_Back");
        if (rrVisual == null) rrVisual = FindDeepChild(visuals, "Tocus_Wheel_Right_Rear");

        if (flVisual == null || frVisual == null || rlVisual == null || rrVisual == null)
        {
            EditorUtility.DisplayDialog("Error", "Could not find Tocus wheel meshes under Visuals.\n\nPlease ensure you have drag-dropped the Tocus model under Visuals.", "OK");
            return;
        }

        Undo.RegisterCompleteObjectUndo(car, "Rebuild Wheel Colliders");

        // 1. Xóa nhóm WheelColliders cũ (ở bất kỳ đâu dưới PlayerCar)
        Transform oldGroup = car.transform.Find("WheelColliders");
        if (oldGroup == null) oldGroup = car.transform.Find("Visuals/WheelColliders");
        if (oldGroup != null)
        {
            Undo.DestroyObjectImmediate(oldGroup.gameObject);
        }

        // Dọn dẹp pivot ảo cũ nếu có để giữ cấu trúc sạch
        CleanUpOldPivots(flVisual);
        CleanUpOldPivots(frVisual);
        CleanUpOldPivots(rlVisual);
        CleanUpOldPivots(rrVisual);

        // 2. Tạo nhóm WheelColliders mới ở gốc PlayerCar
        GameObject newGroupObj = new GameObject("WheelColliders");
        Transform newGroup = newGroupObj.transform;
        newGroup.SetParent(car.transform);
        newGroup.localPosition = Vector3.zero;
        newGroup.localRotation = Quaternion.identity;
        newGroup.localScale = Vector3.one;
        Undo.RegisterCreatedObjectUndo(newGroupObj, "Create WheelColliders Group");

        // 3. Tạo 4 Collider mới tinh khớp chính xác tọa độ bánh xe thật
        string[] names = { "Collider_FL", "Collider_FR", "Collider_RL", "Collider_RR" };
        WheelCollider[] newCols = new WheelCollider[4];
        Transform[] visualTargets = { flVisual, frVisual, rlVisual, rrVisual };

        // Tính toán bán kính tự động
        float flRadius = GetWheelRadius(flVisual);
        float frRadius = GetWheelRadius(frVisual);
        float rlRadius = GetWheelRadius(rlVisual);
        float rrRadius = GetWheelRadius(rrVisual);
        float averageRadius = (flRadius + frRadius + rlRadius + rrRadius) / 4f;
        if (averageRadius < 0.1f || averageRadius > 1.5f) averageRadius = 0.34f;

        for (int i = 0; i < 4; i++)
        {
            GameObject colObj = new GameObject(names[i]);
            colObj.transform.SetParent(newGroup);
            colObj.transform.position = visualTargets[i].position; // Vị trí chính tâm bánh xe 3D
            colObj.transform.rotation = Quaternion.identity;
            colObj.transform.localScale = Vector3.one;

            WheelCollider col = colObj.AddComponent<WheelCollider>();
            
            // Cấu hình vật lý chuẩn cho ô tô Sedan thực tế
            col.mass = 20f;
            col.radius = averageRadius;
            col.suspensionDistance = 0.15f;

            // Spring & Damper để xe nhún đầm bám đường
            JointSpring spring = col.suspensionSpring;
            spring.spring = 35000f;
            spring.damper = 4500f;
            spring.targetPosition = 0.5f;
            col.suspensionSpring = spring;

            newCols[i] = col;
            Undo.RegisterCreatedObjectUndo(colObj, "Create " + names[i]);
        }

        // 5. Gán liên kết toàn bộ trực tiếp vào CarController (không dùng Pivot ảo)
        controller.frontLeftCollider = newCols[0];
        controller.frontRightCollider = newCols[1];
        controller.rearLeftCollider = newCols[2];
        controller.rearRightCollider = newCols[3];

        controller.frontLeftTransform = flVisual;
        controller.frontRightTransform = frVisual;
        controller.rearLeftTransform = rlVisual;
        controller.rearRightTransform = rrVisual;
        controller.wheelRotationOffset = Vector3.zero; // Reset offset to zero for Tocus

        EditorUtility.SetDirty(car);
        EditorUtility.SetDirty(controller);
        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(car.scene);

        EditorUtility.DisplayDialog("Success", "Đã xóa sạch WheelColliders cũ và tự động tạo mới thành công!\n\n- Các WheelCollider đã được định vị tại trục bánh xe.\n- Bán kính lốp được tự động áp dụng là: " + averageRadius + "m.\n- Đã sửa căn chỉnh trục xe, loại bỏ hoàn toàn Pivot ảo và liên kết trực tiếp vào CarController.", "OK");
    }

    private static void CleanUpOldPivots(Transform wheelMesh)
    {
        if (wheelMesh == null) return;
        Transform parent = wheelMesh.parent;
        if (parent != null && parent.name.EndsWith("_Pivot"))
        {
            Transform grandParent = parent.parent;
            wheelMesh.SetParent(grandParent);
            wheelMesh.localPosition = parent.localPosition;
            wheelMesh.localRotation = Quaternion.identity;
            wheelMesh.localScale = Vector3.one;
            Undo.DestroyObjectImmediate(parent.gameObject);
        }
    }

    private static void AlignRootToVisuals(GameObject car, Transform visuals)
    {
        Transform modelRoot = null;
        for (int i = 0; i < visuals.childCount; i++)
        {
            Transform child = visuals.GetChild(i);
            if (child.name.Contains("Tocus") || child.name == "Body" || child.name == "car")
            {
                modelRoot = child;
                break;
            }
        }
        if (modelRoot == null && visuals.childCount > 0)
        {
            modelRoot = visuals.GetChild(0);
        }

        if (modelRoot != null)
        {
            Vector3 offsetPos = modelRoot.localPosition;
            Quaternion offsetRot = modelRoot.localRotation;

            if (offsetPos != Vector3.zero || offsetRot != Quaternion.identity)
            {
                Debug.Log($"[AlignWheelColliders] Phát hiện visual model bị lệch tâm. LocalPosition: {offsetPos}, LocalRotation: {offsetRot.eulerAngles}. Đang căn chỉnh root PlayerCar...");

                Vector3 targetWorldPos = modelRoot.position;
                Quaternion targetWorldRot = modelRoot.rotation;

                Undo.RecordObject(car.transform, "Align Root to Visuals");
                Undo.RecordObject(modelRoot, "Reset Model Local Transform");

                // Di chuyển root PlayerCar tới tâm thực tế của hình ảnh
                car.transform.position = targetWorldPos;
                car.transform.rotation = targetWorldRot;

                // Reset model con về (0,0,0)
                modelRoot.localPosition = Vector3.zero;
                modelRoot.localRotation = Quaternion.identity;
                modelRoot.localScale = Vector3.one;

                // Đảm bảo nhóm Visuals cũng ở tâm (0,0,0)
                Undo.RecordObject(visuals, "Reset Visuals Local Transform");
                visuals.localPosition = Vector3.zero;
                visuals.localRotation = Quaternion.identity;
                visuals.localScale = Vector3.one;
            }
        }
    }

    private static float GetWheelRadius(Transform wheel)
    {
        MeshFilter filter = wheel.GetComponent<MeshFilter>();
        if (filter == null) filter = wheel.GetComponentInChildren<MeshFilter>();
        
        if (filter != null && filter.sharedMesh != null)
        {
            Vector3 size = filter.sharedMesh.bounds.size;
            float worldHeight = size.y * wheel.lossyScale.y;
            return worldHeight / 2f;
        }
        return 0.34f;
    }

    private static Transform FindDeepChild(Transform parent, string name)
    {
        if (parent.name == name) return parent;
        for (int i = 0; i < parent.childCount; i++)
        {
            Transform result = FindDeepChild(parent.GetChild(i), name);
            if (result != null) return result;
        }
        return null;
    }
}
