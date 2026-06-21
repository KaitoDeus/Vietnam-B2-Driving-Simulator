using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(NotificationController))]
public class NotificationControllerEditor : Editor
{
    public override void OnInspectorGUI()
    {
        // Vẽ các trường dữ liệu mặc định trong Inspector
        DrawDefaultInspector();

        NotificationController controller = (NotificationController)target;

        GUILayout.Space(15);
        GUILayout.Label("Chế Độ Xem Thử (Editor Previews)", EditorStyles.boldLabel);

        GUILayout.BeginHorizontal();
        
        // Nút xem thử màu xanh lá (Success)
        GUI.backgroundColor = new Color(0.3f, 0.8f, 0.3f);
        if (GUILayout.Button("Xem thử Success (Màu Xanh)", GUILayout.Height(35)))
        {
            controller.PreviewSuccessState();
        }

        // Nút xem thử màu đỏ (Warning)
        GUI.backgroundColor = new Color(0.9f, 0.3f, 0.3f);
        if (GUILayout.Button("Xem thử Warning (Màu Đỏ)", GUILayout.Height(35)))
        {
            controller.PreviewWarningState();
        }

        GUILayout.EndHorizontal();

        // Nút ẩn xem thử
        GUI.backgroundColor = Color.white;
        GUILayout.Space(8);
        if (GUILayout.Button("Ẩn Xem Thử (Hide)", GUILayout.Height(25)))
        {
            controller.HidePreview();
        }
    }
}
