#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.UI;
using TMPro;

public class ConfigureSpeedometer : EditorWindow
{
    [MenuItem("Tools/Configure Speedometer Only")]
    public static void SetupSpeedometer()
    {
        // 1. Tìm Canvas hoặc HUD_Root trong Scene hiện tại
        GameObject parentGo = GameObject.Find("HUD_Root");
        if (parentGo == null)
        {
            parentGo = GameObject.Find("HUD_Canvas");
        }
        if (parentGo == null)
        {
            Canvas canvas = Object.FindFirstObjectByType<Canvas>();
            if (canvas != null) parentGo = canvas.gameObject;
        }

        if (parentGo == null)
        {
            Debug.LogError("Không tìm thấy HUD_Root, HUD_Canvas hoặc Canvas nào trong Scene!");
            EditorUtility.DisplayDialog("Lỗi", "Không tìm thấy Canvas nào trong Scene để tạo đồng hồ!", "OK");
            return;
        }

        Undo.IncrementCurrentGroup();
        int groupIndex = Undo.GetCurrentGroup();

        // 2. Tìm hoặc tạo Panel_Speedometer
        Transform speedoTrans = parentGo.transform.Find("Panel_Speedometer");
        GameObject panelSpeedo;
        RectTransform speedoRt;

        if (speedoTrans != null)
        {
            panelSpeedo = speedoTrans.gameObject;
            speedoRt = panelSpeedo.GetComponent<RectTransform>();
            
            // Xóa toàn bộ con của Panel_Speedometer để dựng lại sạch sẽ
            int childCount = speedoTrans.childCount;
            for (int i = childCount - 1; i >= 0; i--)
            {
                Undo.DestroyObjectImmediate(speedoTrans.GetChild(i).gameObject);
            }
        }
        else
        {
            panelSpeedo = CreateUIObject("Panel_Speedometer", parentGo);
            speedoRt = panelSpeedo.GetComponent<RectTransform>();
            // Neo ở góc dưới bên phải
            SetAnchorsAndOffsets(speedoRt, new Vector2(1, 0), new Vector2(1, 0), new Vector2(0.5f, 0.5f), new Vector2(-140, 140), new Vector2(220, 220));
        }

        // Tải sprite tròn cơ bản
        Sprite circleSprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Knob.psd");

        // 3. Tự động tạo / lấy các Material Procedural
        string matFolder = "Assets/Materials";
        if (!System.IO.Directory.Exists(matFolder))
        {
            System.IO.Directory.CreateDirectory(matFolder);
        }

        Shader circleShader = Shader.Find("UI/ProceduralCircle");
        if (circleShader == null)
        {
            Debug.LogError("Không tìm thấy Shader UI/ProceduralCircle!");
            return;
        }

        // Nền & Viền
        Material matBase = GetOrCreateCircleMaterial(matFolder, "Mat_Speedo_Base", circleShader, 0f, 0.48f, 0f, 360f, new Color(1, 1, 1, 1));
        // Xanh lá (336 đến 120 độ)
        Material matGreen = GetOrCreateCircleMaterial(matFolder, "Mat_Arc_Green", circleShader, 0.38f, 0.45f, 336f, 120f, new Color(0.18f, 0.8f, 0.44f, 0.9f));
        // Vàng (288 đến 336 độ)
        Material matYellow = GetOrCreateCircleMaterial(matFolder, "Mat_Arc_Yellow", circleShader, 0.38f, 0.45f, 288f, 336f, new Color(0.95f, 0.77f, 0.06f, 0.9f));
        // Đỏ (240 đến 288 độ)
        Material matRed = GetOrCreateCircleMaterial(matFolder, "Mat_Arc_Red", circleShader, 0.38f, 0.45f, 240f, 288f, new Color(0.91f, 0.3f, 0.24f, 0.9f));
        // Nắp kim xanh dương
        Material matCap = GetOrCreateCircleMaterial(matFolder, "Mat_Needle_Cap", circleShader, 0f, 0.48f, 0f, 360f, new Color(0.09f, 0.47f, 0.84f, 1f));

        // 4. Xây dựng lại các cấu phần mặt đồng hồ
        Color panelBorderColor = new Color(0.16f, 0.23f, 0.31f, 0.8f);
        Color bgColor = new Color(0.08f, 0.09f, 0.12f, 0.95f);

        // BorderRing
        GameObject borderGo = CreateImage("BorderRing", panelSpeedo, panelBorderColor, Vector2.zero, new Vector2(224, 224), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), null);
        borderGo.GetComponent<Image>().material = matBase;

        // BgCircle
        GameObject bgGo = CreateImage("BgCircle", panelSpeedo, bgColor, Vector2.zero, new Vector2(220, 220), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), null);
        bgGo.GetComponent<Image>().material = matBase;

        // Green Zone
        GameObject arcGreen = CreateImage("Arc_Green", panelSpeedo, Color.white, Vector2.zero, new Vector2(220, 220), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), null);
        arcGreen.GetComponent<Image>().material = matGreen;

        // Yellow Zone
        GameObject arcYellow = CreateImage("Arc_Yellow", panelSpeedo, Color.white, Vector2.zero, new Vector2(220, 220), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), null);
        arcYellow.GetComponent<Image>().material = matYellow;

        // Red Zone
        GameObject arcRed = CreateImage("Arc_Red", panelSpeedo, Color.white, Vector2.zero, new Vector2(220, 220), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), null);
        arcRed.GetComponent<Image>().material = matRed;

        // NeedlePivot (Trục kim)
        GameObject needlePivotGo = CreateUIObject("NeedlePivot", panelSpeedo);
        RectTransform needlePivotRt = needlePivotGo.GetComponent<RectTransform>();
        SetAnchorsAndOffsets(needlePivotRt, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(12, 12));

        // Needle (Kim tốc độ) - dùng thanh hình chữ nhật màu trắng sắc nét, không dùng sprite bo tròn đầu
        GameObject needleGo = CreateImage("Needle", needlePivotGo, Color.white, new Vector2(0, 0), new Vector2(3, 108), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), null);
        needlePivotRt.localRotation = Quaternion.Euler(0, 0, 120f);

        // PivotCap (Nắp kim màu xanh dương)
        GameObject capGo = CreateImage("PivotCap", needlePivotGo, Color.white, Vector2.zero, new Vector2(22, 22), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), null);
        capGo.GetComponent<Image>().material = matCap;

        // Font chữ hiển thị
        TMP_FontAsset robotoFont = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/TextMesh Pro/Examples & Extras/Resources/Fonts & Materials/Roboto-Bold SDF.asset");
        if (robotoFont == null)
        {
            robotoFont = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/TextMesh Pro/Resources/Fonts & Materials/LiberationSans SDF.asset");
        }

        // Txt_SpeedVal (Số tốc độ)
        TMP_Text speedValText = CreateText("Txt_SpeedVal", panelSpeedo, "0", 48, TextAlignmentOptions.Center, Color.white, new Vector2(0, -30), new Vector2(120, 55), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f));
        speedValText.fontStyle = FontStyles.Bold;
        if (robotoFont != null) speedValText.font = robotoFont;

        // Txt_SpeedUnit (Đơn vị km/h)
        TMP_Text speedUnitText = CreateText("Txt_SpeedUnit", panelSpeedo, "km/h", 16, TextAlignmentOptions.Center, new Color(0.2f, 0.6f, 1.0f), new Vector2(0, -65), new Vector2(100, 25), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f));
        if (robotoFont != null) speedUnitText.font = robotoFont;

        // 5. Liên kết tự động sang HUDController
        HUDController hudCtrl = Object.FindFirstObjectByType<HUDController>();
        if (hudCtrl != null)
        {
            SerializedObject serializedHud = new SerializedObject(hudCtrl);
            serializedHud.FindProperty("speedometerNeedle").objectReferenceValue = needlePivotRt;
            serializedHud.FindProperty("digitalSpeedText").objectReferenceValue = speedValText;
            serializedHud.ApplyModifiedProperties();
            Debug.Log("Đã liên kết thành công Needle và Text vào HUDController.");
        }
        else
        {
            Debug.LogWarning("Không tìm thấy component HUDController trong Scene để liên kết tham chiếu tự động!");
        }

        Undo.CollapseUndoOperations(groupIndex);

        // Lưu thay đổi Scene
        EditorSceneManager.MarkSceneDirty(UnityEngine.SceneManagement.SceneManager.GetActiveScene());
        Debug.Log("Đã cấu hình xong Panel_Speedometer mà không chạm tới các Panel khác!");
        EditorUtility.DisplayDialog("Thành công", "Đã dựng xong đồng hồ tốc độ sắc nét hoàn chỉnh!", "OK");
    }

    private static Material GetOrCreateCircleMaterial(string folder, string matName, Shader shader, float inner, float outer, float startAng, float endAng, Color col)
    {
        string path = folder + "/" + matName + ".mat";
        Material mat = AssetDatabase.LoadAssetAtPath<Material>(path);
        
        if (mat == null)
        {
            mat = new Material(shader);
            AssetDatabase.CreateAsset(mat, path);
        }

        mat.shader = shader;
        mat.SetColor("_Color", col);
        mat.SetFloat("_InnerRadius", inner);
        mat.SetFloat("_OuterRadius", outer);
        mat.SetFloat("_Smoothness", 0.005f);
        mat.SetFloat("_StartAngle", startAng);
        mat.SetFloat("_EndAngle", endAng);

        EditorUtility.SetDirty(mat);
        return mat;
    }

    private static GameObject CreateUIObject(string name, GameObject parent)
    {
        GameObject go = new GameObject(name, typeof(RectTransform));
        if (parent != null)
        {
            go.transform.SetParent(parent.transform, false);
        }
        return go;
    }

    private static GameObject CreateImage(string name, GameObject parent, Color color, Vector2 anchoredPosition, Vector2 size, Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot, Sprite sprite = null)
    {
        GameObject go = CreateUIObject(name, parent);
        Image img = go.AddComponent<Image>();
        img.color = color;
        if (sprite != null)
        {
            img.sprite = sprite;
            img.type = Image.Type.Sliced;
        }
        RectTransform rt = go.GetComponent<RectTransform>();
        SetAnchorsAndOffsets(rt, anchorMin, anchorMax, pivot, anchoredPosition, size);
        return go;
    }

    private static TMP_Text CreateText(string name, GameObject parent, string content, float fontSize, TextAlignmentOptions alignment, Color color, Vector2 anchoredPosition, Vector2 size, Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot)
    {
        GameObject go = CreateUIObject(name, parent);
        TMP_Text text = go.AddComponent<TextMeshProUGUI>();
        text.text = content;
        text.fontSize = fontSize;
        text.alignment = alignment;
        text.color = color;
        
        RectTransform rt = go.GetComponent<RectTransform>();
        SetAnchorsAndOffsets(rt, anchorMin, anchorMax, pivot, anchoredPosition, size);
        return text;
    }

    private static void SetAnchorsAndOffsets(RectTransform rt, Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot, Vector2 anchoredPosition, Vector2 size)
    {
        rt.anchorMin = anchorMin;
        rt.anchorMax = anchorMax;
        rt.pivot = pivot;
        rt.anchoredPosition = anchoredPosition;
        rt.sizeDelta = size;
    }
}
#endif
