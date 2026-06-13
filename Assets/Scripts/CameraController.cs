using UnityEngine;

public class CameraController : MonoBehaviour
{
    public enum CameraMode
    {
        ThirdPerson,
        FirstPerson
    }

    [Header("Targets")]
    [Tooltip("Đối tượng xe cần đi theo (PlayerCar)")]
    public Transform carTarget;
    [Tooltip("Điểm đặt camera khoang lái (nếu trống sẽ tự tìm hoặc tự đặt offset)")]
    public Transform driverSeatTarget;

    [Header("General Settings")]
    public CameraMode initialMode = CameraMode.ThirdPerson;
    public KeyCode switchKey = KeyCode.C;
    [Tooltip("Vị trí tương đối mặc định của ghế lái nếu không gán driverSeatTarget")]
    public Vector3 defaultFirstPersonOffset = new Vector3(-0.38f, 1.15f, 0.3f);
    [Tooltip("Khoảng cách cắt gần (Near Clip Plane) khi ở góc nhìn cabin để tránh bị mất hình nội thất")]
    public float firstPersonNearClip = 0.02f;

    private float originalNearClip = 0.3f;

    [Header("Third Person Settings")]
    public float distance = 5.5f;           // Khoảng cách từ camera tới xe
    public float height = 1.8f;             // Chiều cao của camera so với xe
    public float followDamping = 6f;        // Độ mượt khi bám đuôi vị trí
    public float rotationDamping = 4f;      // Độ mượt khi xoay theo hướng xe
    
    [Header("Third Person Orbit Settings")]
    public float mouseSensitivityX = 3f;    // Độ nhạy chuột ngang
    public float mouseSensitivityY = 2f;    // Độ nhạy chuột dọc
    public float minPitch = -10f;           // Góc nhìn xuống tối đa
    public float maxPitch = 60f;            // Góc nhìn lên tối đa
    public float autoAlignDelay = 2.0f;     // Thời gian tự động căn thẳng sau xe khi không chạm chuột (giây)
    public float autoAlignSpeed = 2f;       // Tốc độ tự động xoay camera về sau xe

    private CameraMode currentMode;
    public CameraMode CurrentMode => currentMode;
    private Camera cam;
    
    private float orbitY = 0f;              // Góc xoay ngang (Yaw)
    private float orbitX = 12f;             // Góc xoay dọc (Pitch) mặc định
    private float lastInputTime = 0f;
    private bool isOrbiting = false;
    private Transform virtualSeatTarget;    // Điểm tự tạo làm dự phòng
    private MonoBehaviour cinemachineBrain; // Hỗ trợ tương thích với Cinemachine

    private void Start()
    {
        cam = GetComponent<Camera>();
        if (cam == null)
        {
            cam = Camera.main;
        }

        if (cam != null)
        {
            originalNearClip = cam.nearClipPlane;
        }

        currentMode = initialMode;

        // Tìm CinemachineBrain nếu có trên camera này
        cinemachineBrain = GetComponent("CinemachineBrain") as MonoBehaviour;
        if (cinemachineBrain != null)
        {
            Debug.Log("[CameraController] Tìm thấy CinemachineBrain. Hệ thống sẽ tự động chuyển đổi quyền kiểm soát.");
        }

        // Tự tìm xe nếu chưa gán
        if (carTarget == null)
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
            {
                carTarget = player.transform;
            }
            else
            {
                GameObject playerCar = GameObject.Find("PlayerCar");
                if (playerCar != null) carTarget = playerCar.transform;
            }
        }

        // Khởi tạo điểm đặt camera
        InitDriverSeat();

        // Cài đặt góc xoay ban đầu cho camera góc nhìn thứ 3 theo hướng xe
        if (carTarget != null)
        {
            orbitY = carTarget.eulerAngles.y;
        }
    }

    private void InitDriverSeat()
    {
        if (carTarget == null) return;

        // Nếu người dùng chưa gán, thử tìm trong cấu trúc phân cấp các GameObject con
        if (driverSeatTarget == null)
        {
            driverSeatTarget = FindDeepChild(carTarget, "DriverSeat") ?? 
                               FindDeepChild(carTarget, "CameraCabin") ?? 
                               FindDeepChild(carTarget, "CabinCamera") ??
                               FindDeepChild(carTarget, "Seat_Front_L");
        }

        // Nếu vẫn không tìm thấy, tự động tạo một virtual transform làm điểm mắt
        if (driverSeatTarget == null)
        {
            GameObject vSeat = new GameObject("VirtualDriverSeat");
            vSeat.transform.SetParent(carTarget);
            vSeat.transform.localPosition = defaultFirstPersonOffset;
            vSeat.transform.localRotation = Quaternion.identity;
            virtualSeatTarget = vSeat.transform;
            driverSeatTarget = virtualSeatTarget;
            Debug.Log($"[CameraController] Tự động tạo vị trí camera ảo ghế lái tại: {defaultFirstPersonOffset}");
        }
    }

    private void Update()
    {
        if (Input.GetKeyDown(switchKey))
        {
            ToggleCameraMode();
        }
    }

    private void LateUpdate()
    {
        if (carTarget == null) return;

        // Bật/tắt CinemachineBrain tương ứng với chế độ camera để tránh bị Cinemachine ghi đè vị trí thủ công
        if (cinemachineBrain != null)
        {
            bool enableBrain = (currentMode == CameraMode.ThirdPerson);
            if (cinemachineBrain.enabled != enableBrain)
            {
                cinemachineBrain.enabled = enableBrain;
            }
        }

        if (currentMode == CameraMode.FirstPerson)
        {
            UpdateFirstPerson();
        }
        else
        {
            // Chỉ chạy UpdateThirdPerson thủ công nếu không sử dụng Cinemachine điều khiển
            if (cinemachineBrain == null || !cinemachineBrain.enabled)
            {
                UpdateThirdPerson();
            }
        }
    }

    private void ToggleCameraMode()
    {
        if (driverSeatTarget == null)
        {
            InitDriverSeat();
        }

        if (currentMode == CameraMode.ThirdPerson)
        {
            if (driverSeatTarget != null)
            {
                currentMode = CameraMode.FirstPerson;
                if (cam != null)
                {
                    cam.nearClipPlane = firstPersonNearClip;
                }
                Debug.Log("Chuyển sang góc nhìn thứ nhất (First-person).");
            }
            else
            {
                Debug.LogWarning("Không thể chuyển sang góc nhìn thứ nhất do chưa gán vị trí ghế lái.");
            }
        }
        else
        {
            currentMode = CameraMode.ThirdPerson;
            orbitY = carTarget.eulerAngles.y;
            orbitX = 12f; // Trả lại góc nghiêng tiêu chuẩn
            if (cam != null)
            {
                cam.nearClipPlane = originalNearClip;
            }
            Debug.Log("Chuyển sang góc nhìn thứ ba (Third-person).");
        }
    }

    private void UpdateFirstPerson()
    {
        if (driverSeatTarget == null) return;

        // Bám sát vị trí và hướng nhìn trong khoang lái của xe
        transform.position = driverSeatTarget.position;
        transform.rotation = driverSeatTarget.rotation;
    }

    private void UpdateThirdPerson()
    {
        // 1. Nhận input xoay tự do từ chuột khi người chơi rê chuột
        float mouseX = Input.GetAxis("Mouse X");
        float mouseY = Input.GetAxis("Mouse Y");

        if (Mathf.Abs(mouseX) > 0.01f || Mathf.Abs(mouseY) > 0.01f)
        {
            orbitY += mouseX * mouseSensitivityX;
            orbitX -= mouseY * mouseSensitivityY;
            orbitX = Mathf.Clamp(orbitX, minPitch, maxPitch);
            lastInputTime = Time.time;
            isOrbiting = true;
        }

        // 2. Tự động căn thẳng camera về phía sau đuôi xe khi không xoay chuột một thời gian
        if (isOrbiting && Time.time - lastInputTime > autoAlignDelay)
        {
            isOrbiting = false;
        }

        if (!isOrbiting)
        {
            float targetYaw = carTarget.eulerAngles.y;
            orbitY = Mathf.LerpAngle(orbitY, targetYaw, Time.deltaTime * autoAlignSpeed);
            orbitX = Mathf.Lerp(orbitX, 12f, Time.deltaTime * autoAlignSpeed);
        }

        // 3. Tính toán vị trí và góc quay mục tiêu
        Quaternion rotation = Quaternion.Euler(orbitX, orbitY, 0f);
        Vector3 targetPosition = carTarget.position - (rotation * Vector3.forward * distance) + (Vector3.up * height);

        // 4. Di chuyển camera mịn màng (Damping)
        transform.position = Vector3.Lerp(transform.position, targetPosition, Time.deltaTime * followDamping);
        transform.rotation = Quaternion.Slerp(transform.rotation, rotation, Time.deltaTime * rotationDamping);
    }

    private Transform FindDeepChild(Transform parent, string name)
    {
        if (parent.name.ToLower().Contains(name.ToLower())) return parent;
        for (int i = 0; i < parent.childCount; i++)
        {
            Transform result = FindDeepChild(parent.GetChild(i), name);
            if (result != null) return result;
        }
        return null;
    }
}
