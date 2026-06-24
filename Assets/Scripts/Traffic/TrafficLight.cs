using UnityEngine;
using TMPro;

public enum TrafficLightState
{
    Red,
    Yellow,
    Green,
    Off
}

public class TrafficLight : MonoBehaviour
{
    [Header("Visuals (Renderer / GameObjects)")]
    public GameObject redLightVisual;
    public GameObject yellowLightVisual;
    public GameObject greenLightVisual;

    [Header("Materials (Optional if using emission swapping)")]
    public Renderer lightRenderer;
    public int redMaterialIndex = 0;
    public int yellowMaterialIndex = 1;
    public int greenMaterialIndex = 2;
    
    public Material redOnMaterial;
    public Material redOffMaterial;
    public Material yellowOnMaterial;
    public Material yellowOffMaterial;
    public Material greenOnMaterial;
    public Material greenOffMaterial;

    [Header("Countdown UI")]
    public TextMeshPro countdownText;

    [Header("Mesh Swapper (For models requiring mesh changes per light)")]
    public Mesh offMesh;
    public Mesh redMesh;
    public Mesh yellowMesh;
    public Mesh greenMesh;

    public TrafficLightState CurrentState => currentState;
    private TrafficLightState currentState = TrafficLightState.Off;
    private bool isInitialized = false;

    public void SetState(TrafficLightState state, int secondsRemaining)
    {
        bool stateChanged = !isInitialized || currentState != state;
        currentState = state;
        isInitialized = true;

        if (stateChanged)
        {
            // 0. Mesh swapping disabled to keep housing black. Only material changes are used.

            // 1. Điều khiển bật/tắt GameObject của đèn tương ứng (Cách đơn giản nhất)
            if (redLightVisual != null) redLightVisual.SetActive(state == TrafficLightState.Red);
            if (yellowLightVisual != null) yellowLightVisual.SetActive(state == TrafficLightState.Yellow);
            if (greenLightVisual != null) greenLightVisual.SetActive(state == TrafficLightState.Green);

            // 2. Thay đổi Material phát sáng (Emission) nếu dùng chung 1 mesh Renderer
            if (lightRenderer != null)
            {
                Material[] sharedMats = lightRenderer.sharedMaterials;
                // Kiểm tra nếu các chỉ số vật liệu trùng nhau (dùng chung 1 slot như mô hình Tarbo-CITY)
                if (redMaterialIndex == yellowMaterialIndex && yellowMaterialIndex == greenMaterialIndex)
                {
                    int targetIndex = redMaterialIndex;
                    if (sharedMats.Length > targetIndex)
                    {
                        Material targetMat = yellowOffMaterial;
                        switch (state)
                        {
                            case TrafficLightState.Red:
                                targetMat = redOnMaterial;
                                break;
                            case TrafficLightState.Yellow:
                                targetMat = yellowOnMaterial;
                                break;
                            case TrafficLightState.Green:
                                targetMat = greenOnMaterial;
                                break;
                            case TrafficLightState.Off:
                            default:
                                targetMat = yellowOffMaterial;
                                break;
                        }
                        sharedMats[targetIndex] = targetMat;
                        lightRenderer.sharedMaterials = sharedMats;
                    }
                }
                else
                {
                    // Trường hợp các đèn nằm ở các slot material khác nhau
                    if (sharedMats.Length > Mathf.Max(redMaterialIndex, yellowMaterialIndex, greenMaterialIndex))
                    {
                        sharedMats[redMaterialIndex] = (state == TrafficLightState.Red) ? redOnMaterial : redOffMaterial;
                        sharedMats[yellowMaterialIndex] = (state == TrafficLightState.Yellow) ? yellowOnMaterial : yellowOffMaterial;
                        sharedMats[greenMaterialIndex] = (state == TrafficLightState.Green) ? greenOnMaterial : greenOffMaterial;
                        lightRenderer.sharedMaterials = sharedMats;
                    }
                }
            }
        }

        // 3. Hiển thị đồng hồ đếm ngược bằng TextMeshPro
        if (countdownText != null)
        {
            if (state == TrafficLightState.Off)
            {
                countdownText.text = "";
            }
            else
            {
                // Định dạng hiển thị 2 chữ số (ví dụ: 09, 05)
                countdownText.text = secondsRemaining.ToString("D2");
                
                // Đổi màu chữ số tương ứng với màu đèn hiện tại để tăng độ thẩm mỹ
                if (state == TrafficLightState.Red) countdownText.color = Color.red;
                else if (state == TrafficLightState.Yellow) countdownText.color = Color.yellow;
                else if (state == TrafficLightState.Green) countdownText.color = Color.green;
            }
        }
    }
}
