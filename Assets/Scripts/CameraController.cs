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
    
    [Header("First Person Orbit Settings")]
    public float mouseSensitivityFpX = 3f;
    public float mouseSensitivityFpY = 2f;
    public float minPitch = -10f;           // Góc nhìn xuống tối đa
    public float maxPitch = 60f;            // Góc nhìn lên tối đa
    public float autoAlignDelay = 2.0f;     // Thời gian tự động căn thẳng sau xe khi không chạm chuột (giây)
    public float autoAlignSpeed = 2f;       // Tốc độ tự động xoay camera về sau xe

    private CameraMode currentMode;
    public CameraMode CurrentMode => currentMode;
    private Camera cam;
    
    private float orbitY = 0f;              // Góc xoay ngang (Yaw)
    private float orbitX = 12f;             // Góc xoay dọc (Pitch) mặc định

    private float fpYaw = 0f;               // Góc xoay ngang cabin tương đối (Yaw)
    private float fpPitch = 0f;             // Góc xoay dọc cabin tương đối (Pitch)
    private float lastFpInputTime = 0f;
    private bool isFpOrbiting = false;

    private Transform virtualSeatTarget;    // Điểm tự tạo làm dự phòng
    private MonoBehaviour cinemachineBrain; // Hỗ trợ tương thích với Cinemachine
    private bool wasCursorLockedLastFrame = false;
    private float lastCarYaw = 0f;

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

        // Tải độ nhạy chuột từ Settings đã lưu
        UpdateSensitivitySettings();

        // Giảm chiều cao camera đi 10% theo yêu cầu
        height *= 0.9f;

        // Cài đặt góc xoay ban đầu cho camera góc nhìn thứ 3 theo hướng xe
        if (carTarget != null)
        {
            orbitY = carTarget.eulerAngles.y;
            lastCarYaw = carTarget.eulerAngles.y;
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

    private void UpdateCursorState()
    {
        if (Time.timeScale > 0f && !InstructionsPopup.IsActive)
        {
            if (Cursor.lockState != CursorLockMode.Locked)
            {
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
            }
        }
        else
        {
            if (Cursor.lockState == CursorLockMode.Locked)
            {
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
            }
        }
    }

    private void LateUpdate()
    {
        if (carTarget == null) return;

        UpdateCursorState();

        // Tắt CinemachineBrain để camera controller này hoàn toàn kiểm soát cả 2 góc nhìn
        if (cinemachineBrain != null && cinemachineBrain.enabled)
        {
            cinemachineBrain.enabled = false;
        }

        if (currentMode == CameraMode.FirstPerson)
        {
            UpdateFirstPerson();
        }
        else
        {
            UpdateThirdPerson();
        }

        // Cập nhật trạng thái khóa chuột của khung hình trước
        wasCursorLockedLastFrame = Cursor.lockState == CursorLockMode.Locked;
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
                fpYaw = 0f;
                fpPitch = 0f;
                isFpOrbiting = false;
                if (cam != null)
                {
                    cam.nearClipPlane = firstPersonNearClip;
                }
                Debug.Log("Chuyển sang góc nhìn thứ nhất (First-person) trong cabin.");
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
            orbitX = 12f;
            lastCarYaw = carTarget.eulerAngles.y;
            if (cam != null)
            {
                cam.nearClipPlane = originalNearClip;
            }
            Debug.Log("Chuyển sang góc nhìn thứ ba (Third-person).");
        }
    }

    private bool IsDrivingOrTurning()
    {
        // 1. Kiểm tra phím di chuyển W/S (Vertical) hoặc cua A/D (Horizontal)
        bool hasInput = Mathf.Abs(Input.GetAxis("Vertical")) > 0.1f || Mathf.Abs(Input.GetAxis("Horizontal")) > 0.1f;
        if (hasInput) return true;

        // 2. Kiểm tra vận tốc vật lý thực tế của xe
        if (carTarget != null)
        {
            Rigidbody rb = carTarget.GetComponent<Rigidbody>();
            if (rb != null)
            {
                // Vận tốc di chuyển > 2 km/h hoặc tốc độ xoay góc > 0.1 rad/s
                bool isMoving = rb.linearVelocity.magnitude * 3.6f > 2f;
                bool isRotating = Mathf.Abs(rb.angularVelocity.y) > 0.1f;
                if (isMoving || isRotating) return true;
            }
        }
        return false;
    }

    private void UpdateFirstPerson()
    {
        if (driverSeatTarget == null) return;

        // Bám sát vị trí trong khoang lái của xe
        transform.position = driverSeatTarget.position;

        // 1. Nhận input xoay tự do từ chuột khi người chơi rê chuột
        // Chỉ nhận khi chuột đã bị khóa và khung hình trước cũng đã khóa để tránh bước nhảy đột ngột (mouse spike) khi vừa khóa chuột
        float mouseX = 0f;
        float mouseY = 0f;
        if (Cursor.lockState == CursorLockMode.Locked && wasCursorLockedLastFrame)
        {
            mouseX = Input.GetAxis("Mouse X");
            mouseY = Input.GetAxis("Mouse Y");
        }
        bool isMouseMoving = Mathf.Abs(mouseX) > 0.01f || Mathf.Abs(mouseY) > 0.01f;

        if (isMouseMoving)
        {
            fpYaw += mouseX * mouseSensitivityFpX;
            fpPitch -= mouseY * mouseSensitivityFpY;

            // Giới hạn góc xoay ngang (Yaw) trong cabin tối đa 180 độ (±90 độ từ chính giữa) để chân thực như người
            fpYaw = Mathf.Clamp(fpYaw, -90f, 90f);
            fpPitch = Mathf.Clamp(fpPitch, -30f, 40f);

            lastFpInputTime = Time.time;
            isFpOrbiting = true;
        }

        // 2. Tự động căn thẳng camera về phía trước của ghế lái khi không xoay chuột một thời gian
        // HOẶC khi người chơi đang di chuyển/cua và không chủ động rê chuột
        if (isFpOrbiting && (Time.time - lastFpInputTime > autoAlignDelay || (IsDrivingOrTurning() && !isMouseMoving)))
        {
            isFpOrbiting = false;
        }

        if (!isFpOrbiting)
        {
            fpYaw = Mathf.LerpAngle(fpYaw, 0f, Time.deltaTime * autoAlignSpeed);
            fpPitch = Mathf.Lerp(fpPitch, 0f, Time.deltaTime * autoAlignSpeed);
        }

        // 3. Tính toán hướng xoay = Hướng của xe + Góc quay tương đối từ chuột
        transform.rotation = driverSeatTarget.rotation * Quaternion.Euler(fpPitch, fpYaw, 0f);
    }

    private void UpdateThirdPerson()
    {
        // Bám theo hướng xoay Y của xe để giữ nguyên góc quay tương đối khi xe cua
        float currentCarYaw = carTarget.eulerAngles.y;
        float deltaYaw = Mathf.DeltaAngle(lastCarYaw, currentCarYaw);
        orbitY += deltaYaw;
        lastCarYaw = currentCarYaw;

        // 1. Nhận input xoay tự do từ chuột khi người chơi rê chuột
        // Chỉ nhận khi chuột đã bị khóa và khung hình trước cũng đã khóa để tránh bước nhảy đột ngột (mouse spike) khi vừa khóa chuột
        float mouseX = 0f;
        float mouseY = 0f;
        if (Cursor.lockState == CursorLockMode.Locked && wasCursorLockedLastFrame)
        {
            mouseX = Input.GetAxis("Mouse X");
            mouseY = Input.GetAxis("Mouse Y");
        }
        bool isMouseMoving = Mathf.Abs(mouseX) > 0.01f || Mathf.Abs(mouseY) > 0.01f;

        if (isMouseMoving)
        {
            orbitY += mouseX * mouseSensitivityX;
            orbitX -= mouseY * mouseSensitivityY;
            orbitX = Mathf.Clamp(orbitX, minPitch, maxPitch);
        }

        // 2. Tính toán vị trí và góc quay mục tiêu quanh tâm nhìn (pivot) của xe
        Quaternion rotation = Quaternion.Euler(orbitX, orbitY, 0f);
        // Dùng tỉ lệ height để đặt tâm nhìn (pivot) tầm trung thân xe (khoảng 1.1m với height=2.75) giúp camera luôn hướng thẳng vào xe
        Vector3 pivot = carTarget.position + Vector3.up * (height * 0.4f);
        Vector3 targetPosition = pivot - (rotation * Vector3.forward * distance);

        // 3. Di chuyển camera mịn màng (Damping)
        transform.position = Vector3.Lerp(transform.position, targetPosition, Time.deltaTime * followDamping);
        transform.rotation = Quaternion.Slerp(transform.rotation, rotation, Time.deltaTime * rotationDamping);
    }

    private void OnDisable()
    {
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    private void OnDestroy()
    {
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    public void UpdateSensitivitySettings()
    {
        // Tự động điều chỉnh độ nhạy chuột chuẩn nhất dựa theo độ phân giải màn hình
        // Sử dụng tỷ lệ màn hình 1920 làm chuẩn (Reference Resolution) để đảm bảo cùng 1 khoảng cách di chuyển chuột vật lý
        // sẽ xoay góc camera giống nhau trên mọi máy tính (1080p, 2K, 4K, v.v.)
        float referenceWidth = 1920f;
        float currentWidth = (Screen.width > 0) ? Screen.width : referenceWidth;
        float resolutionFactor = referenceWidth / currentWidth;

        // Tốc độ xoay chuẩn tối ưu hóa cho trải nghiệm mượt mà nhất
        float standardSensX = 1.8f * resolutionFactor;
        float standardSensY = 1.2f * resolutionFactor;

        mouseSensitivityX = standardSensX;
        mouseSensitivityY = standardSensY;

        mouseSensitivityFpX = standardSensX;
        mouseSensitivityFpY = standardSensY;
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
