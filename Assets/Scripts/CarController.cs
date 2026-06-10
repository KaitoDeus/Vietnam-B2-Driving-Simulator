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

    [HideInInspector]
    public bool isEngineOn = false; // Trạng thái nổ/tắt máy xe (Phím I)

    private Rigidbody rb;
    private float moveInput;
    private float turnInput;
    private float brakeInput;

    private void Start()
    {
        rb = GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.centerOfMass += centerOfMassOffset;
        }
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
        }
        else
        {
            moveInput = 0f;
            turnInput = 0f;
        }
        brakeInput = Input.GetKey(KeyCode.Space) ? 1f : 0f;
    }

    private void FixedUpdate()
    {
        ApplySteering();
        ApplyMotor();
        ApplyBraking();
        ApplyAntiRoll();
        UpdateAllWheels();
    }

    private void ApplySteering()
    {
        float currentSteerAngle = turnInput * maxSteerAngle;
        if (frontLeftCollider != null) frontLeftCollider.steerAngle = currentSteerAngle;
        if (frontRightCollider != null) frontRightCollider.steerAngle = currentSteerAngle;
    }

    private void ApplyMotor()
    {
        float torque = 0f;
        float localSpeed = GetLocalForwardVelocity();

        // Kiểm tra xem người chơi có đang phanh bằng nút di chuyển ngược hướng hay không
        bool isBrakingWithPedal = false;
        if (moveInput != 0f && Mathf.Abs(localSpeed) > 0.5f)
        {
            if (Mathf.Sign(moveInput) != Mathf.Sign(localSpeed))
            {
                isBrakingWithPedal = true;
            }
        }

        if (!isBrakingWithPedal && isEngineOn)
        {
            torque = moveInput * maxMotorTorque;
        }

        // Cầu sau chủ động (RWD)
        if (rearLeftCollider != null) rearLeftCollider.motorTorque = torque;
        if (rearRightCollider != null) rearRightCollider.motorTorque = torque;
    }

    private void ApplyBraking()
    {
        float currentBrake = 0f;
        float localSpeed = GetLocalForwardVelocity();

        if (brakeInput > 0f)
        {
            currentBrake = brakeInput * maxBrakeTorque;
        }
        else if (moveInput != 0f && Mathf.Abs(localSpeed) > 0.5f && Mathf.Sign(moveInput) != Mathf.Sign(localSpeed))
        {
            // Nhấn nút lùi khi đang tiến (hoặc tiến khi đang lùi) -> Kích hoạt phanh chân chân thực
            currentBrake = maxBrakeTorque * 0.6f; 
        }
        else if (moveInput == 0f)
        {
            // Lực phanh tự nhiên (phanh động cơ & lực cản lăn) khi buông ga -> Đã giảm nhỏ để xe trôi trớn xa hơn
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
}