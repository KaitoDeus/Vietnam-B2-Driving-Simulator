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
    public float engineBrakeTorque = 15f;     // Lực phanh động cơ nhẹ khi thả ga (giúp xe trôi được trớn xa hơn)
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

    [HideInInspector]
    public bool isEngineOn = false; // Trạng thái nổ/tắt máy xe (Phím I)
    [HideInInspector] public bool isLeftBlinkerOn = false;
    [HideInInspector] public bool isRightBlinkerOn = false;
    [HideInInspector] public bool isHazardOn = false;

    private Rigidbody rb;
    private float moveInput;
    private float turnInput;
    private float brakeInput;

    private WheelFrictionCurve defaultRearLeftSidewaysFriction;
    private WheelFrictionCurve defaultRearRightSidewaysFriction;
    private float blinkTimer = 0f;
    private bool blinkState = false;
    private AudioSource blinkerAudioSource;

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

    private void Start()
    {
        rb = GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.centerOfMass += centerOfMassOffset;
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

        // Tìm toàn bộ Renderer đèn của xe để gán hiệu ứng nhấp nháy trực tiếp lên chất liệu
        FindAndRegisterBlinkerRenderers(transform);
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

            // Điều khiển hộp số (D/R/N)
            if (Input.GetKeyDown(gearDriveKey))
            {
                currentGear = GearState.D;
                Debug.Log("[Hộp số] Chuyển sang số TIẾN (D)");
            }
            else if (Input.GetKeyDown(gearNeutralKey))
            {
                currentGear = GearState.N;
                Debug.Log("[Hộp số] Chuyển sang số MO (N)");
            }
            else if (Input.GetKeyDown(gearReverseKey))
            {
                currentGear = GearState.R;
                Debug.Log("[Hộp số] Chuyển sang số LÙI (R)");
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
        }
        else
        {
            moveInput = 0f;
            turnInput = 0f;
            isLeftBlinkerOn = false;
            isRightBlinkerOn = false;
            isHazardOn = false;
            currentGear = GearState.N;
        }

        brakeInput = Input.GetKey(KeyCode.Space) ? 1f : 0f;

        // Cập nhật nhấp nháy đèn tín hiệu
        UpdateBlinkers();

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

        float throttle = Mathf.Max(0f, moveInput);
        float torque = 0f;

        if (currentGear == GearState.D)
        {
            torque = throttle * maxMotorTorque;
        }
        else if (currentGear == GearState.R)
        {
            torque = -throttle * maxMotorTorque;
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

        // Space bar: phanh tay
        if (brakeInput > 0f)
        {
            currentBrake = brakeInput * maxBrakeTorque;
        }
        // Phím lùi (S / Down Arrow) đóng vai trò phanh chân chân thực
        else if (moveInput < 0f)
        {
            currentBrake = -moveInput * maxBrakeTorque;
        }
        // Thả phím ga: phanh động cơ nhẹ
        else if (moveInput == 0f)
        {
            currentBrake = engineBrakeTorque;
        }

        // Tự động kích hoạt phanh đỗ (Handbrake) khi xe dừng hẳn để tránh bị trôi dốc ảo
        if (moveInput == 0f && brakeInput == 0f && rb != null && rb.linearVelocity.magnitude < 0.15f)
        {
            currentBrake = parkingBrakeTorque;
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

        // Nếu đang nhấn phanh tay (Space)
        bool isHandbraking = Input.GetKey(KeyCode.Space);

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
}