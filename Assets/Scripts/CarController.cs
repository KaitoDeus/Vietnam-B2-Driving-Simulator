using UnityEngine;

public class CarController : MonoBehaviour
{
    [Header("Wheel Colliders")]
    public WheelCollider frontLeftCollider;
    public WheelCollider frontRightCollider;
    public WheelCollider rearLeftCollider;
    public WheelCollider rearRightCollider;

    [Header("Wheel Visuals")]
    public Transform frontLeftTransform;
    public Transform frontRightTransform;
    public Transform rearLeftTransform;
    public Transform rearRightTransform;

    [Header("Car Settings")]
    public float maxMotorTorque = 1500f;  // N.m
    public float maxBrakeTorque = 3000f;  // N.m
    public float maxSteerAngle = 35f;     // Degrees
    public Vector3 centerOfMassOffset = new Vector3(0f, -0.8f, 0f); // Trọng tâm thấp hơn giúp xe đầm và ổn định khi cua gấp
    public Vector3 wheelRotationOffset = Vector3.zero;
    
    [Header("Deceleration & Anti-Roll")]
    public float antiRollForce = 5000f;       // Lực của thanh cân bằng chống lật (Anti-roll bar force)
    public float engineBrakeTorque = 30f;     // Lực phanh động cơ vừa phải khi thả ga để xe giảm tốc tự nhiên và mượt mà
    public float parkingBrakeTorque = 1000f;   // Lực phanh tay tự động khóa bánh khi dừng hẳn

    [Header("Drift & Downforce")]
    [Tooltip("Hệ số ma sát ngang của bánh sau khi kéo phanh tay (Space) để drift")]
    public float driftRearSidewaysStiffness = 0.4f;
    [Tooltip("Lực ghì xe xuống mặt đường để tránh bay xe/sốc dốc khi chạy nhanh")]
    public float downforceForce = 50f;

    public enum GearState
    {
        D, // Drive (Số tiến)
        N, // Neutral (Số mo)
        R  // Reverse (Số lùi)
    }

    [Header("Gears")]
    public GearState currentGear = GearState.N;
    public KeyCode gearDriveKey = KeyCode.Alpha1;
    public KeyCode gearNeutralKey = KeyCode.Alpha2;
    public KeyCode gearReverseKey = KeyCode.Alpha3;

    [Header("Automatic Transmission Settings")]
    [HideInInspector] public int currentAutomaticGear = 1;
    [HideInInspector] public float engineRPM = 1000f;
    public float maxRPM = 6000f;
    public float minRPM = 1000f;

    [Header("Indicators & Blinker Keys")]
    public KeyCode leftBlinkerKey = KeyCode.Q;
    public KeyCode rightBlinkerKey = KeyCode.E;
    public KeyCode hazardKey = KeyCode.F;

    [Header("Indicator Visuals")]
    [Tooltip("Object nhấp nháy cho xi-nhan trái (nếu trống sẽ tự tìm Blinker_L hoặc Indicator_L)")]
    public GameObject leftBlinkerVisuals;
    [Tooltip("Object nhấp nháy cho xi-nhan phải (nếu trống sẽ tự tìm Blinker_R hoặc Indicator_R)")]
    public GameObject rightBlinkerVisuals;
    public float blinkInterval = 0.5f;
    [Tooltip("Âm thanh nhấp nháy xi-nhan")]
    public AudioClip blinkerSound;
    [Tooltip("Texture đèn xi-nhan chất lượng cao không bị nứt vỡ")]
    public Texture cleanBlinkerTexture;

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (cleanBlinkerTexture == null)
        {
            cleanBlinkerTexture = UnityEditor.AssetDatabase.LoadAssetAtPath<Texture>("Assets/Car_Low/Tocus/Textures/Tocus_ligth.tif");
        }
    }
#endif

    [Header("Scale Settings")]
    [Tooltip("Tự động thay đổi kích thước của xe khi bắt đầu (1.35 = phóng to 35%)")]
    public float autoScaleFactor = 1.35f;

    [HideInInspector]
    public bool isEngineOn = false; // Trạng thái nổ/tắt máy xe (Phím I)
    [HideInInspector] public bool isLeftBlinkerOn = false;
    [HideInInspector] public bool isRightBlinkerOn = false;
    [HideInInspector] public bool isHazardOn = false;
    [HideInInspector] public bool isLowBeamOn = false;
    [HideInInspector] public bool isHighBeamOn = false;

    [Header("B2 Exam Settings")]
    public bool isSeatbeltFastened = true;
    public bool isHandbrakeOn = false;

    public float CurrentSpeed => rb != null ? rb.linearVelocity.magnitude * 3.6f : 0f;

    private Rigidbody rb;
    private float moveInput;
    private float turnInput;
    private float brakeInput;

    private WheelFrictionCurve defaultRearLeftSidewaysFriction;
    private WheelFrictionCurve defaultRearRightSidewaysFriction;
    private float blinkTimer = 0f;
    private bool blinkState = false;
    private AudioSource blinkerAudioSource;
    private AudioSource seatbeltAudioSource;
    private AudioClip seatbeltClickSound;
    private AudioClip handbrakeClickSound;

    private struct BlinkerLightState
    {
        public Renderer renderer;
        public Color originalColor;
        public Color originalEmission;
        public bool wasEmissionEnabled;
        public Texture originalMainTex;
        public Light pointLight;
    }

    private System.Collections.Generic.List<BlinkerLightState> leftBlinkerRenderers = new System.Collections.Generic.List<BlinkerLightState>();
    private System.Collections.Generic.List<BlinkerLightState> rightBlinkerRenderers = new System.Collections.Generic.List<BlinkerLightState>();

    private struct HeadlightState
    {
        public Renderer renderer;
        public Color originalEmission;
        public bool wasEmissionEnabled;
        public Light spotlight;
        public bool isLeft;
    }

    private System.Collections.Generic.List<HeadlightState> headlightStates = new System.Collections.Generic.List<HeadlightState>();

    private void Start()
    {
        // Tự động scale kích thước xe và điều chỉnh các WheelCollider tương ứng tỉ lệ
        if (autoScaleFactor > 0.01f && autoScaleFactor != 1.0f)
        {
            transform.localScale = new Vector3(autoScaleFactor, autoScaleFactor, autoScaleFactor);
            if (frontLeftCollider != null) frontLeftCollider.radius *= autoScaleFactor;
            if (frontRightCollider != null) frontRightCollider.radius *= autoScaleFactor;
            if (rearLeftCollider != null) rearLeftCollider.radius *= autoScaleFactor;
            if (rearRightCollider != null) rearRightCollider.radius *= autoScaleFactor;

            if (frontLeftCollider != null) frontLeftCollider.suspensionDistance *= autoScaleFactor;
            if (frontRightCollider != null) frontRightCollider.suspensionDistance *= autoScaleFactor;
            if (rearLeftCollider != null) rearLeftCollider.suspensionDistance *= autoScaleFactor;
            if (rearRightCollider != null) rearRightCollider.suspensionDistance *= autoScaleFactor;
        }

        // Tự động gắn thêm component xử lý va chạm trừ điểm
        gameObject.AddComponent<CarCollisionHandler>();

        rb = GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.mass = 1800f; // Khôi phục khối lượng chuẩn 1.8 tấn giúp xe đủ công suất vượt dốc đề-ba dễ dàng
            rb.linearDamping = 0.05f; // Khôi phục lực cản không khí tiêu chuẩn
            rb.centerOfMass += centerOfMassOffset * autoScaleFactor;
        }

        // Lưu lại ma sát ngang mặc định của bánh sau
        if (rearLeftCollider != null) defaultRearLeftSidewaysFriction = rearLeftCollider.sidewaysFriction;
        if (rearRightCollider != null) defaultRearRightSidewaysFriction = rearRightCollider.sidewaysFriction;

        // Tự tìm đèn xi nhan nếu chưa gán
        if (leftBlinkerVisuals == null)
        {
            Transform tL = FindDeepChild(transform, "Blinker_L") ?? FindDeepChild(transform, "Indicator_L") ?? FindDeepChild(transform, "LeftBlinker");
            if (tL != null) leftBlinkerVisuals = tL.gameObject;
        }
        if (rightBlinkerVisuals == null)
        {
            Transform tR = FindDeepChild(transform, "Blinker_R") ?? FindDeepChild(transform, "Indicator_R") ?? FindDeepChild(transform, "RightBlinker");
            if (tR != null) rightBlinkerVisuals = tR.gameObject;
        }

        // Tạo AudioSource cho tiếng tạch tạch của xi-nhan
        blinkerAudioSource = gameObject.AddComponent<AudioSource>();
        blinkerAudioSource.playOnAwake = false;
        blinkerAudioSource.loop = false;
        blinkerAudioSource.spatialBlend = 0f; // Âm thanh cabin 2D
        blinkerAudioSource.volume = 0.4f * PlayerPrefs.GetFloat("SFXVolume", 0.8f);

        // Nếu chưa gán âm thanh trong Inspector, tự động tạo âm thanh tạch-tạch cơ học chất lượng cao
        if (blinkerSound == null)
        {
            blinkerSound = CreateProceduralBlinkerSound();
        }

        seatbeltAudioSource = gameObject.AddComponent<AudioSource>();
        seatbeltAudioSource.playOnAwake = false;
        seatbeltAudioSource.loop = false;
        seatbeltAudioSource.spatialBlend = 0f;
        seatbeltAudioSource.volume = 0.8f * PlayerPrefs.GetFloat("SFXVolume", 0.8f);

        seatbeltClickSound = CreateProceduralSeatbeltSound();
        handbrakeClickSound = CreateProceduralHandbrakeSound();

        // Tìm toàn bộ Renderer đèn của xe để gán hiệu ứng nhấp nháy trực tiếp lên chất liệu
        FindAndRegisterBlinkerRenderers(transform);

        // Tìm toàn bộ Renderer đèn pha/cos của xe để gán hiệu ứng chiếu sáng và tự động tạo Spotlight
        FindAndRegisterHeadlights(transform);
    }

    private void Update()
    {
        // Nhấn phím I để nổ/tắt máy
        if (Input.GetKeyDown(KeyCode.I))
        {
            isEngineOn = !isEngineOn;
        }

        if (isEngineOn)
        {
            moveInput = Input.GetAxis("Vertical");
            turnInput = Input.GetAxis("Horizontal");

            // Cho phép chuyển số thủ công bằng các phím Alpha1 (1 - D), Alpha2 (2 - N), Alpha3 (3 - R)
            if (Input.GetKeyDown(gearDriveKey))
            {
                currentGear = GearState.D;
                Debug.Log("[Hộp số] Chuyển thủ công sang số D (Tiến)");
            }
            else if (Input.GetKeyDown(gearNeutralKey))
            {
                currentGear = GearState.N;
                Debug.Log("[Hộp số] Chuyển thủ công sang số N (Mo)");
            }
            else if (Input.GetKeyDown(gearReverseKey))
            {
                currentGear = GearState.R;
                Debug.Log("[Hộp số] Chuyển thủ công sang số R (Lùi)");
            }

            // Tự động điều khiển hộp số (D/R) dựa trên hướng di chuyển và phím bấm (W/S)
            float localForwardVel = GetLocalForwardVelocity();
            if (moveInput > 0.05f)
            {
                // Chỉ chuyển sang số D khi xe đã gần dừng hẳn để tránh bị khựng/dừng đột ngột
                if (localForwardVel < -0.05f)
                {
                    currentGear = GearState.R;
                }
                else
                {
                    currentGear = GearState.D;
                }
            }
            else if (moveInput < -0.05f)
            {
                // Chỉ chuyển sang số R khi xe đã gần dừng hẳn để tránh bị khựng/dừng đột ngột
                if (localForwardVel > 0.05f)
                {
                    currentGear = GearState.D;
                }
                else
                {
                    currentGear = GearState.R;
                }
            }
            else
            {
                // Khi thả phím điều khiển (moveInput == 0):
                // GIỮ NGUYÊN số hiện tại (D hoặc R) để xe tiếp tục trôi và bò tự động (creep).
                // Chỉ tự động trả về số N nếu tắt động cơ.
                if (!isEngineOn)
                {
                    currentGear = GearState.N;
                }
            }

            // Điều khiển xi-nhan và Hazard
            if (Input.GetKeyDown(leftBlinkerKey))
            {
                isLeftBlinkerOn = !isLeftBlinkerOn;
                if (isLeftBlinkerOn) isRightBlinkerOn = false;
                Debug.Log($"[Xi-nhan] Trái: {(isLeftBlinkerOn ? "BẬT" : "TẮT")}");
            }

            if (Input.GetKeyDown(rightBlinkerKey))
            {
                isRightBlinkerOn = !isRightBlinkerOn;
                if (isRightBlinkerOn) isLeftBlinkerOn = false;
                Debug.Log($"[Xi-nhan] Phải: {(isRightBlinkerOn ? "BẬT" : "TẮT")}");
            }

            if (Input.GetKeyDown(hazardKey))
            {
                isHazardOn = !isHazardOn;
                Debug.Log($"[Hazard] Đèn khẩn cấp: {(isHazardOn ? "BẬT" : "TẮT")}");
            }

            if (Input.GetKeyDown(KeyCode.L))
            {
                isLowBeamOn = !isLowBeamOn;
                if (!isLowBeamOn) isHighBeamOn = false;
                Debug.Log($"[Đèn Pha/Cos] Đèn cos (Low beam): {(isLowBeamOn ? "BẬT" : "TẮT")}");
            }

            if (Input.GetKeyDown(KeyCode.H))
            {
                isHighBeamOn = !isHighBeamOn;
                if (isHighBeamOn) isLowBeamOn = true;
                Debug.Log($"[Đèn Pha/Cos] Đèn pha (High beam): {(isHighBeamOn ? "BẬT" : "TẮT")}");
            }
        }
        else
        {
            moveInput = 0f;
            turnInput = 0f;
            isLeftBlinkerOn = false;
            isRightBlinkerOn = false;
            isHazardOn = false;
            isLowBeamOn = false;
            isHighBeamOn = false;
            currentGear = GearState.N;
        }


        // Phím Space để kích hoạt phanh tay đã bị vô hiệu hóa

        // Cập nhật tắt máy đột ngột
        UpdateEngineStall();

        // Cập nhật hệ thống số tự động và RPM động cơ
        UpdateAutomaticTransmission();

        // Cập nhật nhấp nháy đèn tín hiệu
        UpdateBlinkers();

        // Cập nhật đèn pha/cos (Emission và Spotlight)
        UpdateHeadlights();

        UpdateAllWheels();
    }

    private void FixedUpdate()
    {
        ApplySteering();
        ApplyMotor();
        ApplyBraking();
        ApplyAntiRoll();
        ApplyDriftFriction();
        ApplyDownforce();
    }

    private void ApplySteering()
    {
        float currentSteerAngle = turnInput * maxSteerAngle;
        if (frontLeftCollider != null) frontLeftCollider.steerAngle = currentSteerAngle;
        if (frontRightCollider != null) frontRightCollider.steerAngle = currentSteerAngle;
    }

    private void ApplyMotor()
    {
        if (!isEngineOn)
        {
            if (frontLeftCollider != null) frontLeftCollider.motorTorque = 0f;
            if (frontRightCollider != null) frontRightCollider.motorTorque = 0f;
            if (rearLeftCollider != null) rearLeftCollider.motorTorque = 0f;
            if (rearRightCollider != null) rearRightCollider.motorTorque = 0f;
            return;
        }

        // Xác định xe có đang phanh hay không (đối với bò tự động Creep Torque)
        bool isBraking = isHandbrakeOn;
        if (currentGear == GearState.D && moveInput < 0f) isBraking = true;
        if (currentGear == GearState.R && moveInput > 0f) isBraking = true;

        float throttle = 0f;
        float torque = 0f;
        float speedKmh = CurrentSpeed;

        if (currentGear == GearState.D)
        {
            throttle = Mathf.Max(0f, moveInput);
            if (throttle > 0f)
            {
                // Mô-men xoắn phụ thuộc vào chân ga và cấp số tự động hiện tại (số thấp kéo khỏe hơn)
                float gearFactor = 1f;
                switch (currentAutomaticGear)
                {
                    case 1: gearFactor = 1.0f; break;
                    case 2: gearFactor = 0.8f; break;
                    case 3: gearFactor = 0.65f; break;
                    case 4: gearFactor = 0.5f; break;
                    case 5: gearFactor = 0.4f; break;
                }
                torque = throttle * maxMotorTorque * gearFactor;
            }
            else if (!isBraking && speedKmh < 8f)
            {
                // Bò tự động (Creep Torque) khi nhả phanh ở số D
                torque = 180f * (1f - (speedKmh / 8f));
            }
        }
        else if (currentGear == GearState.R)
        {
            throttle = Mathf.Max(0f, -moveInput);
            if (throttle > 0f)
            {
                torque = -throttle * maxMotorTorque * 0.8f; // Số lùi có giới hạn lực kéo
            }
            else if (!isBraking && speedKmh < 6f)
            {
                // Bò lùi tự động khi nhả phanh ở số R
                torque = -140f * (1f - (speedKmh / 6f));
            }
        }
        else // Neutral (N)
        {
            torque = 0f;
        }

        // Cầu sau chủ động (RWD)
        if (rearLeftCollider != null) rearLeftCollider.motorTorque = torque;
        if (rearRightCollider != null) rearRightCollider.motorTorque = torque;
    }

    private void ApplyBraking()
    {
        float currentBrake = 0f;

        // Phanh tay gài dứt khoát
        if (isHandbrakeOn)
        {
            currentBrake = maxBrakeTorque;
        }
        else
        {
            if (currentGear == GearState.D)
            {
                // Khi đang ở số tiến, nhấn lùi (moveInput < 0) là phanh
                if (moveInput < 0f)
                {
                    currentBrake = -moveInput * maxBrakeTorque;
                }
            }
            else if (currentGear == GearState.R)
            {
                // Khi đang ở số lùi, nhấn tiến (moveInput > 0) là phanh
                if (moveInput > 0f)
                {
                    currentBrake = moveInput * maxBrakeTorque;
                }
            }
            else // GearState.N
            {
                // Ở số N, nhấn phím nào cũng là phanh nếu xe đang trôi
                if (moveInput < 0f)
                {
                    currentBrake = -moveInput * maxBrakeTorque;
                }
                else if (moveInput > 0f)
                {
                    currentBrake = moveInput * maxBrakeTorque;
                }
            }

            // Thả phím ga/lùi: chỉ áp dụng phanh dừng đỗ (parkingBrakeTorque) nếu xe ở số N hoặc tắt máy.
            // Nếu ở số D hoặc R, xe phải được phép trôi và bò tự động (creep) tự nhiên mà không bị phanh tay cản trở.
            if (moveInput == 0f)
            {
                if (currentGear == GearState.N || !isEngineOn)
                {
                    if (CurrentSpeed < 1.0f)
                    {
                        // Nội suy tuyến tính mượt mà từ lực phanh động cơ nhẹ (engineBrakeTorque) lên phanh dừng đỗ (parkingBrakeTorque) để tránh khựng giật
                        float t = 1f - (CurrentSpeed / 1.0f);
                        currentBrake = Mathf.Lerp(engineBrakeTorque, parkingBrakeTorque, t);
                    }
                    else
                    {
                        currentBrake = engineBrakeTorque;
                    }
                }
                else
                {
                    // Ở số D hoặc R và nổ máy, không dùng phanh đỗ hay phanh động cơ để xe bò tự nhiên
                    currentBrake = 0f;
                }
            }
        }

        if (frontLeftCollider != null) frontLeftCollider.brakeTorque = currentBrake;
        if (frontRightCollider != null) frontRightCollider.brakeTorque = currentBrake;
        if (rearLeftCollider != null) rearLeftCollider.brakeTorque = currentBrake;
        if (rearRightCollider != null) rearRightCollider.brakeTorque = currentBrake;
    }

    private void ApplyAntiRoll()
    {
        // Phân bổ lực cân bằng chống lật cho cầu trước và cầu sau
        ApplyAntiRollBar(frontLeftCollider, frontRightCollider);
        ApplyAntiRollBar(rearLeftCollider, rearRightCollider);
    }

    private void ApplyAntiRollBar(WheelCollider leftCol, WheelCollider rightCol)
    {
        if (leftCol == null || rightCol == null || rb == null) return;

        WheelHit hit;
        float travelL = 1.0f;
        float travelR = 1.0f;

        // Tính toán độ nén của lò xo giảm xóc bánh bên trái
        bool groundedL = leftCol.GetGroundHit(out hit);
        if (groundedL)
        {
            travelL = (-leftCol.transform.InverseTransformPoint(hit.point).y - leftCol.radius) / leftCol.suspensionDistance;
        }

        // Tính toán độ nén của lò xo giảm xóc bánh bên phải
        bool groundedR = rightCol.GetGroundHit(out hit);
        if (groundedR)
        {
            travelR = (-rightCol.transform.InverseTransformPoint(hit.point).y - rightCol.radius) / rightCol.suspensionDistance;
        }

        // Lực cân bằng tỷ lệ thuận với chênh lệch độ nén của hai bên
        float antiRollForceAmount = (travelL - travelR) * antiRollForce;

        // Áp dụng lực ngược chiều lên hai bánh xe để giữ xe thăng bằng
        if (groundedL)
        {
            rb.AddForceAtPosition(leftCol.transform.up * -antiRollForceAmount, leftCol.transform.position);
        }
        if (groundedR)
        {
            rb.AddForceAtPosition(rightCol.transform.up * antiRollForceAmount, rightCol.transform.position);
        }
    }

    private void UpdateAllWheels()
    {
        UpdateWheel(frontLeftCollider, frontLeftTransform);
        UpdateWheel(frontRightCollider, frontRightTransform);
        UpdateWheel(rearLeftCollider, rearLeftTransform);
        UpdateWheel(rearRightCollider, rearRightTransform);
    }

    private void UpdateWheel(WheelCollider col, Transform trans)
    {
        if (col == null || trans == null) return;

        Vector3 position;
        Quaternion rotation;
        col.GetWorldPose(out position, out rotation);

        trans.position = position;
        trans.rotation = rotation * Quaternion.Euler(wheelRotationOffset);
    }

    public float GetLocalForwardVelocity()
    {
        if (rb == null) return 0f;
        return Vector3.Dot(rb.linearVelocity, transform.forward);
    }

    private void ApplyDriftFriction()
    {
        if (rearLeftCollider == null || rearRightCollider == null) return;

        // Nếu đang nhấn phanh tay (Space) - đã vô hiệu hóa
        bool isHandbraking = false;

        if (isHandbraking)
        {
            // Giảm mạnh ma sát ngang bánh sau để xe trượt bánh (drift)
            WheelFrictionCurve wfcL = rearLeftCollider.sidewaysFriction;
            wfcL.stiffness = driftRearSidewaysStiffness;
            rearLeftCollider.sidewaysFriction = wfcL;

            WheelFrictionCurve wfcR = rearRightCollider.sidewaysFriction;
            wfcR.stiffness = driftRearSidewaysStiffness;
            rearRightCollider.sidewaysFriction = wfcR;
        }
        else
        {
            // Trả lại ma sát mặc định khi không phanh tay
            rearLeftCollider.sidewaysFriction = defaultRearLeftSidewaysFriction;
            rearRightCollider.sidewaysFriction = defaultRearRightSidewaysFriction;
        }
    }

    private void ApplyDownforce()
    {
        if (rb != null)
        {
            // Lực ghì tỉ lệ thuận với vận tốc tiến của xe để dìm xe xuống dốc khi đi nhanh
            float speed = rb.linearVelocity.magnitude;
            rb.AddForce(-transform.up * downforceForce * speed);
        }
    }

    private void UpdateBlinkers()
    {
        bool leftActive = isLeftBlinkerOn || isHazardOn;
        bool rightActive = isRightBlinkerOn || isHazardOn;

        if (leftActive || rightActive)
        {
            blinkTimer += Time.deltaTime;
            if (blinkTimer >= blinkInterval)
            {
                blinkTimer = 0f;
                blinkState = !blinkState;
                if (blinkerSound != null && blinkerAudioSource != null)
                {
                    // Giả lập tiếng tick-tock cơ học: nốt cao (1.0) khi bật đèn, nốt trầm (0.82) khi tắt đèn
                    blinkerAudioSource.pitch = blinkState ? 1.0f : 0.82f;
                    blinkerAudioSource.PlayOneShot(blinkerSound);
                }
            }
        }
        else
        {
            blinkState = false;
            blinkTimer = 0f;
        }

        // Cập nhật đèn xi nhan trái
        bool showLeft = leftActive && blinkState;
        if (leftBlinkerVisuals != null) leftBlinkerVisuals.SetActive(showLeft);
        for (int i = 0; i < leftBlinkerRenderers.Count; i++)
        {
            SetBlinkerRendererState(leftBlinkerRenderers[i], showLeft);
        }

        // Cập nhật đèn xi nhan phải
        bool showRight = rightActive && blinkState;
        if (rightBlinkerVisuals != null) rightBlinkerVisuals.SetActive(showRight);
        for (int i = 0; i < rightBlinkerRenderers.Count; i++)
        {
            SetBlinkerRendererState(rightBlinkerRenderers[i], showRight);
        }
    }

    private void SetBlinkerRendererState(BlinkerLightState state, bool isOn)
    {
        if (state.renderer == null || state.renderer.material == null) return;

        if (isOn)
        {
            // Thay thế texture main và emission bằng texture sạch chất lượng cao để tránh răng cưa và vết nứt vỡ
            if (cleanBlinkerTexture != null)
            {
                state.renderer.material.SetTexture("_MainTex", cleanBlinkerTexture);
                state.renderer.material.SetTexture("_EmissionMap", cleanBlinkerTexture);
            }
            else if (state.originalMainTex != null)
            {
                state.renderer.material.SetTexture("_EmissionMap", state.originalMainTex);
            }

            state.renderer.material.EnableKeyword("_EMISSION");
            
            // Màu hổ phách ấm áp rực rỡ với cường độ vừa phải (1.8f) để tránh cháy sáng mất chi tiết và răng cưa
            state.renderer.material.SetColor("_EmissionColor", new Color(1.0f, 0.38f, 0.0f) * 1.8f);

            // Bật đèn Point Light mềm phát sáng xung quanh thân xe và mặt đường
            if (state.pointLight != null)
            {
                state.pointLight.enabled = true;
            }
        }
        else
        {
            // Trả lại trạng thái phát sáng và texture ban đầu
            state.renderer.material.SetColor("_EmissionColor", state.originalEmission);
            state.renderer.material.SetTexture("_EmissionMap", null);
            state.renderer.material.SetTexture("_MainTex", state.originalMainTex);

            if (state.wasEmissionEnabled)
            {
                state.renderer.material.EnableKeyword("_EMISSION");
            }
            else
            {
                state.renderer.material.DisableKeyword("_EMISSION");
            }

            // Tắt đèn Point Light
            if (state.pointLight != null)
            {
                state.pointLight.enabled = false;
            }
        }
    }

    private void UpdateHeadlights()
    {
        for (int i = 0; i < headlightStates.Count; i++)
        {
            HeadlightState state = headlightStates[i];
            if (state.renderer == null || state.renderer.material == null) continue;

            if (isLowBeamOn)
            {
                // Bật Emission cho chất liệu đèn pha/cos sáng trắng ấm
                state.renderer.material.EnableKeyword("_EMISSION");
                float emissionIntensity = isHighBeamOn ? 4.5f : 2.2f;
                state.renderer.material.SetColor("_EmissionColor", new Color(1.0f, 0.95f, 0.85f) * emissionIntensity);

                // Cấu hình Spotlight chiếu sáng đường đi phía trước
                if (state.spotlight != null)
                {
                    state.spotlight.enabled = true;
                    if (isHighBeamOn)
                    {
                        state.spotlight.range = 45f;
                        state.spotlight.intensity = 5.0f;
                        state.spotlight.spotAngle = 40f;
                        state.spotlight.transform.localRotation = Quaternion.Euler(2f, 0f, 0f);
                    }
                    else
                    {
                        state.spotlight.range = 22f;
                        state.spotlight.intensity = 2.5f;
                        state.spotlight.spotAngle = 55f;
                        state.spotlight.transform.localRotation = Quaternion.Euler(8f, state.isLeft ? -5f : 5f, 0f);
                    }
                }
            }
            else
            {
                // Tắt Emission
                state.renderer.material.SetColor("_EmissionColor", state.originalEmission);
                if (!state.wasEmissionEnabled)
                {
                    state.renderer.material.DisableKeyword("_EMISSION");
                }

                // Tắt Spotlight
                if (state.spotlight != null)
                {
                    state.spotlight.enabled = false;
                }
            }
        }
    }

    private void FindAndRegisterHeadlights(Transform parent)
    {
        Renderer r = parent.GetComponent<Renderer>();
        if (r != null)
        {
            string nameLower = parent.name.ToLower();
            // Tìm cụm đèn trước: tên chứa "light" và "front" (ví dụ Tocus_Light_Front_Left)
            if (nameLower.Contains("light") && nameLower.Contains("front"))
            {
                bool isLeft = nameLower.Contains("left") || nameLower.Contains("_l");
                AddHeadlightRenderer(r, isLeft);
            }
        }

        for (int i = 0; i < parent.childCount; i++)
        {
            FindAndRegisterHeadlights(parent.GetChild(i));
        }
    }

    private void AddHeadlightRenderer(Renderer r, bool isLeft)
    {
        HeadlightState state = new HeadlightState();
        state.renderer = r;
        state.isLeft = isLeft;
        if (r.material != null)
        {
            state.originalEmission = r.material.HasProperty("_EmissionColor") ? r.material.GetColor("_EmissionColor") : Color.black;
            state.wasEmissionEnabled = r.material.IsKeywordEnabled("_EMISSION");

            // Tạo Spotlight phụ phát sáng về phía trước, đặt làm con của Car để di chuyển theo xe
            GameObject lightGo = new GameObject(r.name + "_SpotLight");
            lightGo.transform.SetParent(transform, false);
            
            // Lấy vị trí trung tâm của cụm đèn trong không gian cục bộ của xe
            Vector3 worldCenter = r.bounds.center;
            Vector3 localCenter = transform.InverseTransformPoint(worldCenter);
            localCenter.z += 0.2f; // Đẩy đèn ra trước một chút tránh tự đổ bóng hoặc bị che bởi cản trước
            lightGo.transform.localPosition = localCenter;

            Light l = lightGo.AddComponent<Light>();
            l.type = LightType.Spot;
            l.color = new Color(1.0f, 0.95f, 0.85f); // Sáng trắng ấm tự nhiên
            l.shadows = LightShadows.Soft; // Đổ bóng mềm
            l.enabled = false;

            state.spotlight = l;
            headlightStates.Add(state);
        }
    }

    private void FindAndRegisterBlinkerRenderers(Transform parent)
    {
        Renderer r = parent.GetComponent<Renderer>();
        if (r != null)
        {
            string nameLower = parent.name.ToLower();
            // Lọc chính xác tên chứa "light" và phân biệt "left"/"right"
            if (nameLower.Contains("light"))
            {
                if (nameLower.Contains("left"))
                {
                    AddBlinkerRenderer(r, true);
                }
                else if (nameLower.Contains("right"))
                {
                    AddBlinkerRenderer(r, false);
                }
            }
        }

        for (int i = 0; i < parent.childCount; i++)
        {
            FindAndRegisterBlinkerRenderers(parent.GetChild(i));
        }
    }

    private void AddBlinkerRenderer(Renderer r, bool isLeft)
    {
        BlinkerLightState state = new BlinkerLightState();
        state.renderer = r;
        if (r.material != null)
        {
            state.originalColor = r.material.color;
            state.originalEmission = r.material.HasProperty("_EmissionColor") ? r.material.GetColor("_EmissionColor") : Color.black;
            state.wasEmissionEnabled = r.material.IsKeywordEnabled("_EMISSION");
            state.originalMainTex = r.material.HasProperty("_MainTex") ? r.material.GetTexture("_MainTex") : null;

            // Tạo Point Light phụ phát sáng mềm mại tại trung tâm hình học của cụm đèn
            GameObject lightGo = new GameObject(r.name + "_PointLight");
            lightGo.transform.SetParent(r.transform, false);
            
            // Đặt vị trí cục bộ khớp chính xác với tâm Mesh Bounds để không bị lệch trục
            Vector3 localCenter = r.transform.InverseTransformPoint(r.bounds.center);
            lightGo.transform.localPosition = localCenter;

            Light l = lightGo.AddComponent<Light>();
            l.type = LightType.Point;
            l.color = new Color(1.0f, 0.45f, 0.0f); // Màu xi-nhan cam hổ phách ấm áp
            l.range = 2.0f; // Tầm phủ vừa phải
            l.intensity = 1.5f; // Cường độ sáng dịu mắt
            l.shadows = LightShadows.None;
            l.enabled = false;

            state.pointLight = l;
            
            if (isLeft) leftBlinkerRenderers.Add(state);
            else rightBlinkerRenderers.Add(state);
        }
    }

    private AudioClip CreateProceduralBlinkerSound()
    {
        int sampleRate = 44100;
        float duration = 0.05f; // click rất ngắn gọn
        int sampleCount = (int)(sampleRate * duration);
        float[] samples = new float[sampleCount];

        for (int i = 0; i < sampleCount; i++)
        {
            float t = (float)i / sampleRate;
            // Tạo sóng sine suy hao nhanh kết hợp tần số cao mô phỏng tiếng gõ cơ học của rơ-le xi-nhan
            float freq = 1200f;
            float envelope = Mathf.Exp(-t * 120f); // Tắt nhanh để tạo tiếng gõ đanh
            samples[i] = Mathf.Sin(2f * Mathf.PI * freq * t) * envelope * 0.4f;
        }

        AudioClip clip = AudioClip.Create("BlinkerTick", sampleCount, 1, sampleRate, false);
        clip.SetData(samples, 0);
        return clip;
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

    // Logic tắt máy đột ngột (Engine Stall)
    private void UpdateEngineStall()
    {
        // Đối với xe số tự động, việc dừng xe khi vẫn cài số D/R không làm chết máy
        // do hệ thống biến mô thủy lực (torque converter) tự động trượt slip.
    }

    private void UpdateAutomaticTransmission()
    {
        if (!isEngineOn)
        {
            engineRPM = 0f;
            currentAutomaticGear = 1;
            return;
        }

        if (currentGear == GearState.N)
        {
            currentAutomaticGear = 1;
            // Thả ga ở N: Vòng tua về mức Garanty (idle) cộng một chút khi nháy ga
            float targetIdle = 900f + (Mathf.Max(0f, moveInput) * 3500f);
            engineRPM = Mathf.Lerp(engineRPM, targetIdle, Time.deltaTime * 6f);
            return;
        }

        if (currentGear == GearState.R)
        {
            currentAutomaticGear = 1;
            float targetR = 900f + (CurrentSpeed * 180f) + (Mathf.Max(0f, moveInput) * 1200f);
            engineRPM = Mathf.Lerp(engineRPM, Mathf.Min(targetR, maxRPM), Time.deltaTime * 5f);
            return;
        }

        // --- Cấu hình tự động chuyển số (D) ---
        float speed = CurrentSpeed;
        int targetGear = 1;

        if (speed > 52f) targetGear = 5;
        else if (speed > 38f) targetGear = 4;
        else if (speed > 24f) targetGear = 3;
        else if (speed > 10f) targetGear = 2;
        else targetGear = 1;

        // Tránh giật nhảy số liên tục khi đi mấp mé giới hạn (Hysteresis)
        if (targetGear < currentAutomaticGear && speed > 0.1f)
        {
            float lowerThreshold = 0f;
            if (currentAutomaticGear == 5) lowerThreshold = 48f;
            else if (currentAutomaticGear == 4) lowerThreshold = 34f;
            else if (currentAutomaticGear == 3) lowerThreshold = 20f;
            else if (currentAutomaticGear == 2) lowerThreshold = 8f;

            if (speed > lowerThreshold)
            {
                targetGear = currentAutomaticGear;
            }
        }

        if (targetGear != currentAutomaticGear)
        {
            // Hiệu ứng hẫng nhẹ vòng tua máy khi chuyển cấp số
            engineRPM = Mathf.Max(minRPM + 200f, engineRPM - 900f);
            currentAutomaticGear = targetGear;
            Debug.Log($"[Automatic Transmission] Shifting to D{currentAutomaticGear} (Speed: {speed:F1} km/h)");
        }

        // Tính toán RPM thực tế dựa trên tốc độ và cấp số hiện tại
        float gearFactor = 1f;
        switch (currentAutomaticGear)
        {
            case 1: gearFactor = 230f; break;
            case 2: gearFactor = 140f; break;
            case 3: gearFactor = 95f; break;
            case 4: gearFactor = 70f; break;
            case 5: gearFactor = 50f; break;
        }

        float calculatedRPM = 900f + (speed * gearFactor) + (Mathf.Max(0f, moveInput) * 1500f);
        engineRPM = Mathf.Lerp(engineRPM, Mathf.Clamp(calculatedRPM, minRPM, maxRPM), Time.deltaTime * 7f);
    }

    private void StallEngine(string reason)
    {
        isEngineOn = false;
        Debug.Log($"[Tắt máy] Xe bị chết máy! Lý do: {reason}");
        
        if (ExamManager.Instance != null && ExamManager.Instance.isExamActive)
        {
            ExamManager.Instance.DeductPoints(5, $"Chết máy xe: {reason}");
        }
    }

    private AudioClip CreateProceduralSeatbeltSound()
    {
        int sampleRate = 44100;
        float duration = 0.15f;
        int sampleCount = (int)(sampleRate * duration);
        float[] samples = new float[sampleCount];

        for (int i = 0; i < sampleCount; i++)
        {
            float t = (float)i / sampleRate;
            float freq1 = 1500f;
            float envelope1 = Mathf.Exp(-t * 200f);
            
            float freq2 = 250f;
            float envelope2 = Mathf.Exp(-t * 30f);

            samples[i] = (Mathf.Sin(2f * Mathf.PI * freq1 * t) * envelope1 * 0.3f) +
                         (Mathf.Sin(2f * Mathf.PI * freq2 * t) * envelope2 * 0.4f);
        }

        AudioClip clip = AudioClip.Create("SeatbeltClick", sampleCount, 1, sampleRate, false);
        clip.SetData(samples, 0);
        return clip;
    }

    private AudioClip CreateProceduralHandbrakeSound()
    {
        int sampleRate = 44100;
        float duration = 0.25f;
        int sampleCount = (int)(sampleRate * duration);
        float[] samples = new float[sampleCount];

        for (int i = 0; i < sampleCount; i++)
        {
            float t = (float)i / sampleRate;
            
            float ratchet = 0f;
            for (int click = 0; click < 4; click++)
            {
                float clickTime = click * 0.05f;
                if (t >= clickTime)
                {
                    float clickT = t - clickTime;
                    ratchet += Mathf.Sin(2f * Mathf.PI * 1000f * clickT) * Mathf.Exp(-clickT * 150f) * 0.25f;
                }
            }

            float thudTime = 0.18f;
            float thud = 0f;
            if (t >= thudTime)
            {
                float thudT = t - thudTime;
                thud = Mathf.Sin(2f * Mathf.PI * 120f * thudT) * Mathf.Exp(-thudT * 40f) * 0.5f;
            }

            samples[i] = ratchet + thud;
        }

        AudioClip clip = AudioClip.Create("HandbrakeClick", sampleCount, 1, sampleRate, false);
        clip.SetData(samples, 0);
        return clip;
    }
}